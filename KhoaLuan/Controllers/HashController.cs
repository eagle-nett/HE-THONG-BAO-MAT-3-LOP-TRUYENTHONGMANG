using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace KhoaLuan.Controllers
{
    public class HashController : Controller
    {
        // Trang nhập khóa chung để băm
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Index(string sharedKey)
        {
            if (string.IsNullOrEmpty(sharedKey))
            {
                ViewBag.ErrorMessage = "Vui lòng nhập khóa chung!";
                return View();
            }

            string hashedKey = HashMD5(sharedKey);
            ViewBag.SharedKey = sharedKey;
            ViewBag.HashedKey = hashedKey;

            return View();
        }

        // Trang nhập MD5 để lấy 64-bit
        public ActionResult Extract64Bit()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Extract64Bit(string hashedKey)
        {
            if (string.IsNullOrEmpty(hashedKey) || hashedKey.Length < 16)
            {
                ViewBag.ErrorMessage = "Vui lòng nhập chuỗi MD5 hợp lệ!";
                return View();
            }

            string first64Bit = hashedKey.Substring(0, 16);
            ViewBag.OriginalHash = hashedKey;
            ViewBag.First64Bit = first64Bit;

            return View();
        }

        // Chuyển đổi 16 ký tự hex sang nhị phân
        public ActionResult HexToBinary()
        {
            return View();
        }

        [HttpPost]
        public ActionResult HexToBinary(string hexInput)
        {
            if (string.IsNullOrEmpty(hexInput) || hexInput.Length != 16 || !hexInput.All(c => "0123456789abcdefABCDEF".Contains(c)))
            {
                ViewBag.ErrorMessage = "Vui lòng nhập đúng 16 ký tự hex hợp lệ!";
                return View();
            }

            string binaryOutput = string.Join("", hexInput.Select(c => Convert.ToString(Convert.ToInt32(c.ToString(), 16), 2).PadLeft(4, '0')));

            ViewBag.HexInput = hexInput;
            ViewBag.BinaryOutput = binaryOutput;

            return View();
        }

        // Hàm băm MD5
        private string HashMD5(string input)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(bytes);

                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }
}