using System.ComponentModel.DataAnnotations;

namespace ExamOnlineSystem.Models;

// Năm học (ví dụ 2025-2026): khung thời gian để gom lịch thi / học phần theo từng niên khóa.
// Chỉ một năm học được đánh dấu là năm học hiện tại (LaNamHienTai) tại mỗi thời điểm.
public class NamHoc
{
    [Key]
    public int Id { get; set; }

    [Display(Name = "Mã năm học")]
    [Required(ErrorMessage = "Vui lòng nhập mã năm học")]
    [StringLength(20, ErrorMessage = "Mã năm học tối đa 20 ký tự")]
    public string MaNamHoc { get; set; } = string.Empty;

    [Display(Name = "Tên năm học")]
    [Required(ErrorMessage = "Vui lòng nhập tên năm học")]
    [StringLength(100, ErrorMessage = "Tên năm học tối đa 100 ký tự")]
    public string TenNamHoc { get; set; } = string.Empty;

    [Display(Name = "Ngày bắt đầu")]
    [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu")]
    [DataType(DataType.Date)]
    public DateOnly NgayBatDau { get; set; }

    [Display(Name = "Ngày kết thúc")]
    [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc")]
    [DataType(DataType.Date)]
    public DateOnly NgayKetThuc { get; set; }

    [Display(Name = "Năm học hiện tại")]
    public bool LaNamHienTai { get; set; }

    // Không bắt buộc trên form (string? để không bị validate "required" tự động của ASP.NET Core);
    // cột DB vẫn NOT NULL mặc định '' – xem cấu hình trong ApplicationDbContext
    [Display(Name = "Ghi chú")]
    [StringLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự")]
    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public string? GhiChu { get; set; } = string.Empty;
}
