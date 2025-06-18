using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net.Mail;
using System.Net;
using System.Configuration;
using System.ComponentModel.DataAnnotations;

namespace KhoaLuan.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        public ActionResult Index()
        {
            return View();
        }

        // GET: Home/GioiThieuDES
        public ActionResult GioiThieuDES()
        {
            ViewBag.Title = "Giới thiệu về thuật toán DES";
            return View();
        }

        public ActionResult HDSD()
        {
            ViewBag.Title = "Hướng dẫn sử dụng";
            return View();
        }

        public ActionResult Gt3tt()
        {
            ViewBag.Title = "Giới thiệu RSA, chữ ký số và MD5";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SendContactEmail(string name, string email, string message)
        {
            try
            {
                // Kiểm tra dữ liệu đầu vào từ form
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(message))
                {
                    ViewBag.Message = "Vui lòng điền đầy đủ thông tin.";
                    ViewBag.AlertClass = "alert-danger";
                    return View("Index");
                }

                if (!new EmailAddressAttribute().IsValid(email))
                {
                    ViewBag.Message = "Email không hợp lệ.";
                    ViewBag.AlertClass = "alert-danger";
                    return View("Index");
                }

                // Debug: Ghi dữ liệu đầu vào vào Output
                System.Diagnostics.Debug.WriteLine($"Name: {name}");
                System.Diagnostics.Debug.WriteLine($"Email: {email}");
                System.Diagnostics.Debug.WriteLine($"Message: {message}");

                // Lấy thông tin SMTP từ web.config
                var smtpEmail = ConfigurationManager.AppSettings["SmtpEmail"];
                var smtpPassword = ConfigurationManager.AppSettings["SmtpPassword"];
                var recipientEmail = ConfigurationManager.AppSettings["RecipientEmail"];

                // Kiểm tra giá trị từ web.config
                if (string.IsNullOrWhiteSpace(smtpEmail) || string.IsNullOrWhiteSpace(smtpPassword) || string.IsNullOrWhiteSpace(recipientEmail))
                {
                    ViewBag.Message = $"Lỗi cấu hình SMTP trong web.config. SmtpEmail: {smtpEmail}, SmtpPassword: {smtpPassword}, RecipientEmail: {recipientEmail}";
                    ViewBag.AlertClass = "alert-danger";
                    return View("Index");
                }

                // Cấu hình SMTP (sử dụng Gmail)
                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential(smtpEmail, smtpPassword),
                    EnableSsl = true,
                };

                // Tạo nội dung email (dùng HTML để định dạng)
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(smtpEmail),
                    Subject = "Liên hệ từ Website DES",
                    Body = "<h3>Liên hệ từ Website DES</h3>" +
                           "<table style='border-collapse: collapse; width: 100%;'>" +
                           "<tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Tên:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>" + HttpUtility.HtmlEncode(name) + "</td></tr>" +
                           "<tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Email:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>" + HttpUtility.HtmlEncode(email) + "</td></tr>" +
                           "<tr><td style='padding: 8px; border: 1px solid #ddd;'><strong>Tin nhắn:</strong></td><td style='padding: 8px; border: 1px solid #ddd;'>" + HttpUtility.HtmlEncode(message) + "</td></tr>" +
                           "</table>",
                    IsBodyHtml = true, // Sử dụng HTML
                };
                mailMessage.To.Add(recipientEmail);

                // Gửi email
                smtpClient.Send(mailMessage);

                // Thông báo thành công
                ViewBag.Message = "Tin nhắn của bạn đã được gửi thành công!";
                ViewBag.AlertClass = "alert-success";
            }
            catch (Exception ex)
            {
                // Thông báo lỗi
                ViewBag.Message = $"Có lỗi xảy ra khi gửi tin nhắn: {ex.Message}";
                ViewBag.AlertClass = "alert-danger";
            }

            return View("Index");
        }
    }
}