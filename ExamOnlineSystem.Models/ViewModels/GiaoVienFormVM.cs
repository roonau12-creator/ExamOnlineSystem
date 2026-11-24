using System.ComponentModel.DataAnnotations;

namespace ExamOnlineSystem.Models.ViewModels;

// Các trường chung của form Thêm/Sửa giảng viên. Khoa không nhập: lấy theo khoa của bộ môn được chọn.
public abstract class GiaoVienFormVM
{
    [Display(Name = "Mã giảng viên")]
    [Required(ErrorMessage = "Vui lòng nhập mã giảng viên")]
    [StringLength(20, ErrorMessage = "Mã giảng viên tối đa 20 ký tự")]
    public string MaGiaoVien { get; set; } = string.Empty;

    [Display(Name = "Họ và tên")]
    [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
    public string HoTen { get; set; } = string.Empty;

    [Display(Name = "Chức danh")]
    public string? ChucDanh { get; set; }

    [Display(Name = "Bộ môn")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn bộ môn")]
    public int BoMonId { get; set; }

    [Display(Name = "Email")]
    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Số điện thoại")]
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại gồm 10 chữ số, bắt đầu bằng 0")]
    public string SoDienThoai { get; set; } = string.Empty;

    // Gợi ý cho ô Chức danh
    public static readonly string[] GoiYChucDanh =
        { "Giảng viên", "Giảng viên chính", "Giảng viên cao cấp", "Trưởng bộ môn", "Phó trưởng bộ môn" };
}

// Form thêm giảng viên: tạo hồ sơ giảng viên kèm tài khoản đăng nhập (vai trò Teacher)
public class GiaoVienCreateVM : GiaoVienFormVM
{
    [Display(Name = "Mật khẩu ban đầu")]
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu ban đầu")]
    [DataType(DataType.Password)]
    public string MatKhau { get; set; } = string.Empty;
}

// Form sửa giảng viên: đồng bộ luôn tài khoản đăng nhập
public class GiaoVienEditVM : GiaoVienFormVM
{
    public int Id { get; set; }

    // Để trống = giữ nguyên mật khẩu cũ
    [Display(Name = "Mật khẩu mới")]
    [DataType(DataType.Password)]
    public string? MatKhauMoi { get; set; }
}
