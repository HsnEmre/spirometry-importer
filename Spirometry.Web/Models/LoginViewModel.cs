using System.ComponentModel.DataAnnotations;

namespace Spirometry.Web.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Bir OSGB seçin.")]
        [Display(Name = "OSGB")]
        public string TenantCode { get; set; } = string.Empty;


        [Required(ErrorMessage = "Parolanızı girin.")]
        [DataType(DataType.Password)]
        [Display(Name = "Parola")]
        public string Password { get; set; } = string.Empty;
    }
}
