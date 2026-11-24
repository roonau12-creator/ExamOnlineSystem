using System.ComponentModel.DataAnnotations;

namespace ExamOnlineSystem.Models.ViewModels;

// Trang "Hồ sơ cá nhân" của sinh viên (/Customer/HoSo)
public class HoSoSinhVienVM
{
    public SinhVien SinhVien { get; set; } = null!; // kèm Lop, Khoa
    public DateTime? NgayTaoTaiKhoan { get; set; } // UTC
    public DateTime? LanDangNhapCuoi { get; set; } // UTC

    public CapNhatLienHeVM LienHe { get; set; } = new();
    public DoiMatKhauVM DoiMatKhau { get; set; } = new();
}

// Thông tin sinh viên được tự sửa
public class CapNhatLienHeVM
{
    [Display(Name = "Số điện thoại")]
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại gồm 10 chữ số, bắt đầu bằng 0")]
    public string SoDienThoai { get; set; } = string.Empty;
}

public class DoiMatKhauVM
{
    [Display(Name = "Mật khẩu hiện tại")]
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
    [DataType(DataType.Password)]
    public string MatKhauHienTai { get; set; } = string.Empty;

    [Display(Name = "Mật khẩu mới")]
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự")]
    [DataType(DataType.Password)]
    public string MatKhauMoi { get; set; } = string.Empty;

    [Display(Name = "Nhập lại mật khẩu mới")]
    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu mới")]
    [Compare(nameof(MatKhauMoi), ErrorMessage = "Mật khẩu nhập lại không khớp")]
    [DataType(DataType.Password)]
    public string XacNhanMatKhau { get; set; } = string.Empty;
}
