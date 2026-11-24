using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamOnlineSystem.Models;

public class DanhMucCauHoi
{
    [Key]
    public int Id { get; set; }

    [Display(Name = "Tên danh mục")]
    [Required(ErrorMessage = "Vui lòng nhập tên danh mục")]
    public string TenDanhMuc { get; set; } = string.Empty;

    // Danh mục cha (null = danh mục gốc)
    [Display(Name = "Danh mục cha")]
    public int? DanhMucChaId { get; set; }
    [ForeignKey(nameof(DanhMucChaId))]
    public DanhMucCauHoi? DanhMucCha { get; set; }

    [Display(Name = "Môn học")]
    [Required(ErrorMessage = "Vui lòng chọn môn học")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn môn học")]
    public int MonHocId { get; set; }
    [ForeignKey(nameof(MonHocId))]
    public MonHoc? MonHoc { get; set; }

    // Không bắt buộc nhập; cột DB vẫn NOT NULL (cấu hình trong ApplicationDbContext), lưu chuỗi rỗng nếu bỏ trống
    [Display(Name = "Mô tả")]
    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public string? MoTa { get; set; } = string.Empty;
}
