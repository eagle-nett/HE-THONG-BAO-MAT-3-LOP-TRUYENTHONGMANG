using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace KhoaLuan.Controllers
{
    public class RSAController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Index(string p, string q, string e)
        {
            long pVal, qVal, eVal = 0;
            bool eProvided = !string.IsNullOrEmpty(e);

            try
            {
                // Kiểm tra đầu vào
                if (!long.TryParse(p, out pVal) || !long.TryParse(q, out qVal))
                {
                    ViewBag.Error = "Lỗi: p và q phải là số nguyên.";
                    return View();
                }
                if (!IsPrime(pVal) || !IsPrime(qVal))
                {
                    ViewBag.Error = "Lỗi: p và q phải là số nguyên tố.";
                    return View();
                }

                // Tính n và phi
                long n = pVal * qVal;
                long phi = (pVal - 1) * (qVal - 1);

                // Xử lý e
                if (eProvided)
                {
                    if (!long.TryParse(e, out eVal) || eVal <= 1 || eVal >= phi || GCD(eVal, phi) != 1)
                    {
                        ViewBag.Error = "Lỗi: e phải là số nguyên, 1 < e < φ(n), và GCD(e, φ(n)) = 1.";
                        return View();
                    }
                }
                else
                {
                    // Tìm e nếu không được cung cấp
                    eVal = FindE(phi);
                }

                // Tính d
                long d = ModInverse(eVal, phi);

                // Lưu các giá trị vào ViewBag
                ViewBag.P = pVal;
                ViewBag.Q = qVal;
                ViewBag.E = eVal;
                ViewBag.N = n;
                ViewBag.D = d;
                ViewBag.Phi = phi;

                // Quá trình tạo khóa
                ViewBag.KeyGenProcess = new List<string>
                {
                    $"Bước 1: Nhập hai số nguyên tố p = {pVal}, q = {qVal}",
                    $"Bước 2: Tính n = p * q = {pVal} * {qVal} = {n}",
                    $"Bước 3: Tính φ(n) = (p-1) * (q-1) = ({pVal}-1) * ({qVal}-1) = {phi}",
                    eProvided
                        ? $"Bước 4: Nhập e = {eVal}, kiểm tra GCD(e, φ(n)) = 1"
                        : $"Bước 4: Chọn e sao cho GCD(e, φ(n)) = 1 và 1 < e < φ(n). Chọn e = {eVal}",
                    $"Bước 5: Tính d sao cho (d * e) mod φ(n) = 1. Tìm d = {d}"
                };
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Lỗi: {ex.Message}";
            }

            return View();
        }

        [HttpGet]
        public JsonResult GenerateRandom()
        {
            try
            {
                Random rand = new Random();
                long p, q, e;

                // Sinh p và q ngẫu nhiên (trong khoảng 100 đến 1000 để đơn giản)
                do
                {
                    p = rand.Next(100, 1000);
                } while (!IsPrime(p));

                do
                {
                    q = rand.Next(100, 1000);
                } while (!IsPrime(q) || q == p);

                // Tính phi
                long phi = (p - 1) * (q - 1);

                // Thử các giá trị e phổ biến (65537, 17, 3) trước
                long[] commonE = { 65537, 17, 3 };
                e = commonE.FirstOrDefault(x => x < phi && GCD(x, phi) == 1);

                // Nếu không tìm được e phổ biến, dùng FindE
                if (e == 0)
                {
                    e = FindE(phi);
                }

                return Json(new { p, q, e }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { error = $"Lỗi khi sinh giá trị ngẫu nhiên: {ex.Message}" }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult Sign()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Sign(long d, long n, long message)
        {
            try
            {
                ViewBag.D = d;
                ViewBag.N = n;
                ViewBag.Message = message;

                var steps = new List<string>();
                long computedSignature = PowMod(message, d, n, steps);
                ViewBag.ComputedSignature = computedSignature;
                ViewBag.Process = $"Ký số: s = m^d mod n = {message}^{d} mod {n} = {computedSignature}";
                ViewBag.CalculationSteps = steps;
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Lỗi: {ex.Message}";
            }

            return View();
        }

        public ActionResult Verify()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Verify(long e, long n, long signature)
        {
            try
            {
                ViewBag.E = e;
                ViewBag.N = n;
                ViewBag.Signature = signature;

                var steps = new List<string>();
                long verifiedMessage = PowMod(signature, e, n, steps);
                ViewBag.VerifiedMessage = verifiedMessage;
                ViewBag.Process = $"Kiểm tra: m' = s^e mod n = {signature}^{e} mod {n} = {verifiedMessage}";
                ViewBag.CalculationSteps = steps;
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Lỗi: {ex.Message}";
            }

            return View();
        }

        private bool IsPrime(long number)
        {
            if (number <= 1) return false;
            if (number == 2) return true;
            if (number % 2 == 0) return false;
            for (long i = 3; i <= Math.Sqrt(number); i += 2)
            {
                if (number % i == 0) return false;
            }
            return true;
        }

        private long FindE(long phi)
        {
            for (long e = 3; e < phi; e += 2)
            {
                if (GCD(e, phi) == 1)
                    return e;
            }
            throw new Exception("Không tìm được e phù hợp.");
        }

        private long GCD(long a, long b)
        {
            while (b != 0)
            {
                long temp = b;
                b = a % b;
                a = temp;
            }
            return a;
        }

        private long ModInverse(long e, long phi)
        {
            long t = 0, newT = 1;
            long r = phi, newR = e;
            while (newR != 0)
            {
                long quotient = r / newR;
                (t, newT) = (newT, t - quotient * newT);
                (r, newR) = (newR, r - quotient * newR);
            }
            if (r > 1) throw new Exception("Không có nghịch đảo modular.");
            if (t < 0) t += phi;
            return t;
        }

        private long PowMod(long baseVal, long exp, long mod, List<string> steps)
        {
            long result = 1;
            baseVal %= mod;
            steps.Add($"Khởi tạo: result = 1, base = {baseVal}, mod = {mod}");
            steps.Add($"Lũy thừa (exp) = {exp}, chuyển sang nhị phân: {Convert.ToString(exp, 2)}");

            int stepCount = 0;
            while (exp > 0)
            {
                steps.Add($"Bước {++stepCount}:");
                steps.Add($"  - exp = {exp}, bit thấp nhất = {(exp & 1)}");
                if ((exp & 1) == 1)
                {
                    steps.Add($"  - Vì bit = 1, result = (result * base) mod mod = ({result} * {baseVal}) mod {mod} = {(result * baseVal) % mod}");
                    result = (result * baseVal) % mod;
                }
                else
                {
                    steps.Add($"  - Vì bit = 0, không cập nhật result");
                }
                steps.Add($"  - base = (base * base) mod mod = ({baseVal} * {baseVal}) mod {mod} = {(baseVal * baseVal) % mod}");
                baseVal = (baseVal * baseVal) % mod;
                exp >>= 1;
                steps.Add($"  - exp = exp >> 1 = {exp}");
            }
            steps.Add($"Kết quả cuối: result = {result}");
            return result;
        }
    }
}