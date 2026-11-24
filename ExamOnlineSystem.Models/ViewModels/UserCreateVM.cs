using System.ComponentModel.DataAnnotations;

namespace ExamOnlineSystem.Models.ViewModels;

// Các trường chung của form Thêm/Sửa tài khoản. Email cũng là tên đăng nhập.
public abstract class UserFormVM
{
    [Display(Name = "Họ và tên")]
    [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
    public string HoTen { get; set; } = string.Empty;

    [Display(Name = "Email (tên đăng nhập)")]
    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Số điện thoại")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại gồm 10 chữ số, bắt đầu bằng 0")]
    public string? SoDienThoai { get; set; }

    [Display(Name = "Chức danh")]
    public string? ChucDanh { get; set; }

    // 0 = không chọn khoa (vd: tài khoản quản trị hệ thống)
    [Display(Name = "Khoa công tác")]
    public int KhoaId { get; set; }

    [Display(Name = "Vai trò")]
    [Required(ErrorMessage = "Vui lòng chọn vai trò")]
    public string VaiTro { get; set; } = string.Empty;
}

// Form thêm tài khoản: cần mật khẩu ban đầu
public class UserCreateVM : UserFormVM
{
    [Display(Name = "Mật khẩu ban đầu")]
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu ban đầu")]
    [DataType(DataType.Password)]
    public string MatKhau { get; set; } = string.Empty;
}

// Form sửa tài khoản: mật khẩu để trống = giữ nguyên mật khẩu cũ
public class UserEditVM : UserFormVM
{
    public string Id { get; set; } = string.Empty;

    [Display(Name = "Đặt lại mật khẩu")]
    [DataType(DataType.Password)]
    public string? MatKhauMoi { get; set; }

    // Chỉ để hiển thị trên form, không cho sửa ở đây (khóa/mở khóa làm ở trang danh sách)
    public bool DangHoatDong { get; set; }
    public string? MaSinhVien { get; set; }
    public string? MaGiaoVien { get; set; }
}
