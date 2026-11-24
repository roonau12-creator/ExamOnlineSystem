using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamOnlineSystem.Models;

public class MonHoc
{
    // Giá trị cho các dropdown trên form
    public static readonly string[] DanhSachHinhThucThi = { "Trắc nghiệm", "Tự luận", "Trắc nghiệm + Tự luận" };
    public static readonly string[] DanhSachTrangThai = { "Hoạt động", "Ngừng hoạt động" };

    [Key]
    public int Id { get; set; }

    [Display(Name = "Khoa")]
    [Required(ErrorMessage = "Vui lòng chọn khoa")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn khoa")]
    public int KhoaId { get; set; }

    [ForeignKey(nameof(KhoaId))]
    public Khoa? Khoa { get; set; }

    [Display(Name = "Bộ môn")]
    [Required(ErrorMessage = "Vui lòng chọn bộ môn")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn bộ môn")]
    public int BoMonId { get; set; }

    [ForeignKey(nameof(BoMonId))]
    public BoMon? BoMon { get; set; }

    [Display(Name = "Mã môn học")]
    [Required(ErrorMessage = "Vui lòng nhập mã môn học")]
    public string MaMonHoc { get; set; } = string.Empty;

    [Display(Name = "Tên môn học")]
    [Required(ErrorMessage = "Vui lòng nhập tên môn học")]
    public string TenMonHoc { get; set; } = string.Empty;

    // Không bắt buộc nhập; cột DB vẫn NOT NULL (cấu hình trong ApplicationDbContext), lưu chuỗi rỗng nếu bỏ trống
    [Display(Name = "Tên tiếng Anh")]
    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public string? TenTiengAnh { get; set; } = string.Empty;

    [Display(Name = "Mô tả")]
    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public string? MoTa { get; set; } = string.Empty;

    [Display(Name = "Hình thức thi")]
    [Required(ErrorMessage = "Vui lòng chọn hình thức thi")]
    public string HinhThucThi { get; set; } = string.Empty;

    [Display(Name = "Thời gian thi (phút)")]
    [Required(ErrorMessage = "Vui lòng nhập thời gian thi")]
    [Range(1, 600, ErrorMessage = "Thời gian thi phải từ 1 đến 600 phút")]
    public int ThoiGianThi { get; set; }

    [Display(Name = "Số thứ tự")]
    [Required(ErrorMessage = "Vui lòng nhập số thứ tự")]
    [Range(1, int.MaxValue, ErrorMessage = "Số thứ tự phải lớn hơn 0")]
    public int SoThuTu { get; set; }

    [Display(Name = "Trạng thái")]
    [Required(ErrorMessage = "Vui lòng chọn trạng thái")]
    public string TrangThai { get; set; } = "Hoạt động";
}
