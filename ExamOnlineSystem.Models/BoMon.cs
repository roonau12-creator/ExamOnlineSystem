using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamOnlineSystem.Models;

public class BoMon
{
    [Key]
    public int Id { get; set; }

    [Display(Name = "Khoa")]
    [Required(ErrorMessage = "Vui lòng chọn khoa")]
    public int KhoaId { get; set; }

    [ForeignKey(nameof(KhoaId))]
    public Khoa? Khoa { get; set; }
    
    [Display(Name = "Mã bộ môn")]
    [Required(ErrorMessage = "Vui lòng nhập mã bộ môn")]
    public string MaBoMon { get; set; } = string.Empty;

    [Display(Name = "Tên bộ môn")]
    [Required(ErrorMessage = "Vui lòng nhập tên bộ môn")]
    public string TenBoMon { get; set; } = string.Empty;

    [Display(Name = "Số thứ tự")]
    [Range(1, int.MaxValue, ErrorMessage = "Số thứ tự phải lớn hơn 0")]
    public int SoThuTu { get; set; }
}
