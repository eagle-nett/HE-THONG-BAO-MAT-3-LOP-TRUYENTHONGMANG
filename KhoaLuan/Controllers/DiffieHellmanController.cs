using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace KhoaLuan.Controllers
{
    public class DiffieHellmanController : Controller
    {
        private static readonly Random rand = new Random();

        // GET: DiffieHellman
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Index(long p, long g, long a, long b)
        {
            // Kiểm tra p có phải số nguyên tố không
            if (!IsPrime(p))
            {
                ViewBag.ErrorMessage = "Số p phải là số nguyên tố!";
                return View();
            }

            // Kiểm tra g có hợp lệ không (1 < g < p)
            if (g <= 1 || g >= p)
            {
                ViewBag.ErrorMessage = "Số g phải nằm trong khoảng 1 < g < p!";
                return View();
            }

            // Kiểm tra a và b có hợp lệ không (phải dương)
            if (a <= 0 || b <= 0)
            {
                ViewBag.ErrorMessage = "Khóa bí mật a và b phải lớn hơn 0!";
                return View();
            }

            // Tính khóa công khai
            long A = PowerMod(g, a, p);
            long B = PowerMod(g, b, p);

            // Tính khóa chung từ cả hai phía
            long sharedKeyAlice = PowerMod(B, a, p);
            long sharedKeyBob = PowerMod(A, b, p);

            // Lưu công thức vào ViewBag
            ViewBag.AlicePublicKeyFormula = $"A = {g}^{a} mod {p} = {A}";
            ViewBag.BobPublicKeyFormula = $"B = {g}^{b} mod {p} = {B}";
            ViewBag.SharedKeyAliceFormula = $"Khóa chung Alice tính: K = {B}^{a} mod {p} = {sharedKeyAlice}";
            ViewBag.SharedKeyBobFormula = $"Khóa chung Bob tính: K = {A}^{b} mod {p} = {sharedKeyBob}";

            return View();
        }

        // Action để sinh số ngẫu nhiên cho p và g
        [HttpPost]
        public ActionResult GenerateRandom()
        {
            long p = GeneratePrime(1, 1000); // Sinh số nguyên tố trong khoảng 1000-10000
            long g = GeneratePrimitiveRoot(p);   // Sinh g là căn nguyên tố modulo p

            // Truyền giá trị ngẫu nhiên vào ViewBag để hiển thị trong form
            ViewBag.RandomP = p;
            ViewBag.RandomG = g;

            return View("Index");
        }

        // Hàm kiểm tra số nguyên tố
        private bool IsPrime(long n)
        {
            if (n < 2) return false;
            if (n == 2 || n == 3) return true;
            if (n % 2 == 0 || n % 3 == 0) return false;

            for (long i = 5; i * i <= n; i += 6)
            {
                if (n % i == 0 || n % (i + 2) == 0) return false;
            }
            return true;
        }

        // Hàm tính (base^exp) % mod
        private long PowerMod(long baseValue, long exp, long mod)
        {
            long result = 1;
            baseValue = baseValue % mod;

            while (exp > 0)
            {
                if ((exp & 1) == 1)
                    result = (result * baseValue) % mod;

                exp = exp >> 1;
                baseValue = (baseValue * baseValue) % mod;
            }
            return result;
        }

        // Sinh số nguyên tố ngẫu nhiên trong khoảng min đến max
        private long GeneratePrime(long min, long max)
        {
            while (true)
            {
                long candidate = rand.Next((int)Math.Min(min, int.MaxValue), (int)Math.Min(max + 1, int.MaxValue));
                if (IsPrime(candidate))
                    return candidate;
            }
        }

        // Sinh căn nguyên tố modulo p
        private long GeneratePrimitiveRoot(long p)
        {
            // Tính p-1 và phân tích thành thừa số nguyên tố
            long phi = p - 1;
            var factors = Factorize(phi);

            // Tìm g là căn nguyên tố
            for (long g = 2; g < p; g++)
            {
                bool isPrimitive = true;
                foreach (var factor in factors)
                {
                    if (PowerMod(g, phi / factor, p) == 1)
                    {
                        isPrimitive = false;
                        break;
                    }
                }
                if (isPrimitive)
                    return g;
            }
            return 2; // Trường hợp thất bại (rất hiếm), trả về 2
        }

        // Phân tích n thành các thừa số nguyên tố
        private List<long> Factorize(long n)
        {
            var factors = new List<long>();
            while (n % 2 == 0)
            {
                if (!factors.Contains(2)) factors.Add(2);
                n /= 2;
            }
            for (long i = 3; i * i <= n; i += 2)
            {
                while (n % i == 0)
                {
                    if (!factors.Contains(i)) factors.Add(i);
                    n /= i;
                }
            }
            if (n > 2) factors.Add(n);
            return factors;
        }
    }
}