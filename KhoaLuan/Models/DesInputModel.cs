using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace KhoaLuan.Models
{
    public class DesInputModel
    {
        [Display(Name = "PlainText")]
        public string PlainText { get; set; }

        [Display(Name = "CipherText")]
        public string CipherText { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập khóa")]
        [Display(Name = "Key")]
        public string Key { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn hành động")]
        [Display(Name = "Action")]
        public string Action { get; set; } // "Encrypt" hoặc "Decrypt"

        [Required(ErrorMessage = "Vui lòng chọn định dạng đầu vào")]
        [Display(Name = "InputFormat")]
        public string InputFormat { get; set; } // "ASCII", "Hex", hoặc "Binary"

        [Required(ErrorMessage = "Vui lòng chọn định dạng khóa")]
        [Display(Name = "KeyFormat")]
        public string KeyFormat { get; set; } // "ASCII", "Hex", hoặc "Binary"
    }
}