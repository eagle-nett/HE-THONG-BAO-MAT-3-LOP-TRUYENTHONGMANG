using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace KhoaLuan.Controllers
{
    public class DESController : Controller
    {
        // View nhập khóa chính tạo khóa con (64-bit)
        public ActionResult GenerateSubKeys()
        {
            return View();
        }

        [HttpPost]
        public ActionResult GenerateSubKeys(string inputKey, string inputType = "binary")
        {
            string binaryKey;

            if (string.IsNullOrEmpty(inputKey))
            {
                ViewBag.ErrorMessage = "Vui lòng nhập khóa!";
                return View();
            }

            StringBuilder binary = new StringBuilder();

            if (inputType == "ascii")
            {
                // Chuyển ASCII sang nhị phân
                if (!Regex.IsMatch(inputKey, @"^[ -~]+$"))
                {
                    ViewBag.ErrorMessage = "Chuỗi ASCII không hợp lệ! Vui lòng chỉ sử dụng ký tự in được (từ dấu cách đến ~).";
                    return View();
                }
                byte[] bytes = Encoding.ASCII.GetBytes(inputKey);
                foreach (byte b in bytes)
                {
                    binary.Append(Convert.ToString(b, 2).PadLeft(8, '0'));
                }
            }
            else if (inputType == "hex")
            {
                // Chuyển Hex sang nhị phân
                inputKey = inputKey.Trim();
                if (inputKey.Length != 16 || !Regex.IsMatch(inputKey, @"^[0-9A-Fa-f]{16}$"))
                {
                    ViewBag.ErrorMessage = "Vui lòng nhập chuỗi hex 16 ký tự hợp lệ (0-9, A-F)!";
                    return View();
                }
                foreach (char c in inputKey)
                {
                    int value = Convert.ToInt32(c.ToString(), 16);
                    binary.Append(Convert.ToString(value, 2).PadLeft(4, '0'));
                }
            }
            else if (inputType == "binary")
            {
                // Kiểm tra chuỗi nhị phân hợp lệ
                if (inputKey.Length != 64 || !inputKey.All(c => c == '0' || c == '1'))
                {
                    ViewBag.ErrorMessage = "Vui lòng nhập chuỗi nhị phân 64-bit hợp lệ!";
                    return View();
                }
                binary.Append(inputKey);
            }
            else
            {
                ViewBag.ErrorMessage = "Loại đầu vào không hợp lệ! Vui lòng chọn binary, hex hoặc ascii.";
                return View();
            }

            // Lưu chuỗi nhị phân gốc
            ViewBag.OriginalBinary = binary.ToString();

            // Điều chỉnh chuỗi nhị phân thành 64 bit
            binaryKey = binary.ToString();
            if (binaryKey.Length > 64)
            {
                binaryKey = binaryKey.Substring(0, 64);
                ViewBag.WarningMessage = "Chuỗi nhị phân dài hơn 64 bit, chỉ lấy 64 bit đầu.";
            }
            else if (binaryKey.Length < 64)
            {
                binaryKey = binaryKey.PadRight(64, '0');
                ViewBag.WarningMessage = "Chuỗi nhị phân ngắn hơn 64 bit, đã thêm 0 để đủ 64 bit.";
            }

            int[] PC1 = {
                57, 49, 41, 33, 25, 17, 9,
                1, 58, 50, 42, 34, 26, 18,
                10, 2, 59, 51, 43, 35, 27,
                19, 11, 3, 60, 52, 44, 36,
                63, 55, 47, 39, 31, 23, 15,
                7, 62, 54, 46, 38, 30, 22,
                14, 6, 61, 53, 45, 37, 29,
                21, 13, 5, 28, 20, 12, 4
            };

            string key56 = string.Join("", PC1.Select(i => binaryKey[i - 1]));

            ViewBag.C0 = key56.Substring(0, 28);
            ViewBag.D0 = key56.Substring(28, 28);

            List<string> CList = new List<string> { ViewBag.C0 };
            List<string> DList = new List<string> { ViewBag.D0 };
            List<string> subKeys = new List<string>();

            int[] leftShifts = { 1, 1, 2, 2, 2, 2, 2, 2, 1, 2, 2, 2, 2, 2, 2, 1 };
            int[] PC2 = {
                14, 17, 11, 24, 1, 5,
                3, 28, 15, 6, 21, 10,
                23, 19, 12, 4, 26, 8,
                16, 7, 27, 20, 13, 2,
                41, 52, 31, 37, 47, 55,
                30, 40, 51, 45, 33, 48,
                44, 49, 39, 56, 34, 53,
                46, 42, 50, 36, 29, 32
            };

            string C = ViewBag.C0;
            string D = ViewBag.D0;

            for (int i = 0; i < 16; i++)
            {
                C = LeftShift(C, leftShifts[i]);
                D = LeftShift(D, leftShifts[i]);

                CList.Add(C);
                DList.Add(D);

                string combined = C + D;
                string subKey = string.Join("", PC2.Select(j => combined[j - 1]));
                subKeys.Add(subKey);
            }

            ViewBag.BinaryKey = binaryKey;
            ViewBag.Key56 = key56;
            ViewBag.CList = CList.Skip(1).ToList(); // Bỏ C0, D0 để chỉ lấy Cn, Dn sau dịch
            ViewBag.DList = DList.Skip(1).ToList();
            ViewBag.SubKeys = subKeys;

            return View();
        }

        private string LeftShift(string input, int shifts)
        {
            return input.Substring(shifts) + input.Substring(0, shifts);
        }
    }
}