using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamOnlineSystem.Models;

public class HocPhan
{
    [Key]
    public int Id { get; set; }

    [Display(Name = "Khoa")]
    [Required(ErrorMessage = "Vui lòng chọn khoa")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn khoa")]
    public int KhoaId { get; set; }

    [ForeignKey(nameof(KhoaId))]
    public Khoa? Khoa { get; set; }

    [Display(Name = "Môn học")]
    [Required(ErrorMessage = "Vui lòng chọn môn học")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn môn học")]
    public int MonHocId { get; set; }

    [ForeignKey(nameof(MonHocId))]
    public MonHoc? MonHoc { get; set; }

    [Display(Name = "Mã học phần")]
    [Required(ErrorMessage = "Vui lòng nhập mã học phần")]
    public string MaHocPhan { get; set; } = string.Empty;

    [Display(Name = "Tên học phần")]
    [Required(ErrorMessage = "Vui lòng nhập tên học phần")]
    public string TenHocPhan { get; set; } = string.Empty;

    [Display(Name = "Số tín chỉ")]
    [Required(ErrorMessage = "Vui lòng nhập số tín chỉ")]
    [Range(1, 20, ErrorMessage = "Số tín chỉ phải từ 1 đến 20")]
    public int SoTinChi { get; set; }
}
