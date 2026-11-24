using System.ComponentModel.DataAnnotations;

namespace ExamOnlineSystem.Models;

public class Khoa
{
    [Key]
    public int Id { get; set; }

    [Display(Name = "Mã khoa")]
    [Required(ErrorMessage = "Vui lòng nhập mã khoa")]
    public string MaKhoa { get; set; } = string.Empty;

    [Display(Name = "Tên khoa")]
    [Required(ErrorMessage = "Vui lòng nhập tên khoa")]
    public string TenKhoa { get; set; } = string.Empty;

    [Display(Name = "Số thứ tự")]
    [Range(1, int.MaxValue, ErrorMessage = "Số thứ tự phải lớn hơn 0")]
    public int SoThuTu { get; set; }
}
