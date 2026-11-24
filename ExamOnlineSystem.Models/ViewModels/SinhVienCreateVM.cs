using System.ComponentModel.DataAnnotations;

namespace ExamOnlineSystem.Models.ViewModels;

// Các trường chung của form Thêm/Sửa sinh viên. Khoa không nhập: lấy theo khoa của lớp được chọn.
public abstract class SinhVienFormVM
{
    [Display(Name = "Mã sinh viên")]
    [Required(ErrorMessage = "Vui lòng nhập mã sinh viên")]
    public string MaSinhVien { get; set; } = string.Empty;

    [Display(Name = "Họ và tên")]
    [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
    public string HoTen { get; set; } = string.Empty;

    [Display(Name = "Ngày sinh")]
    [Required(ErrorMessage = "Vui lòng nhập ngày sinh")]
    [DataType(DataType.Date)]
    public DateOnly? NgaySinh { get; set; }

    // true = Nam, false = Nữ
    [Display(Name = "Giới tính")]
    [Required(ErrorMessage = "Vui lòng chọn giới tính")]
    public bool? GioiTinh { get; set; }

    [Display(Name = "Lớp")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn lớp")]
    public int LopId { get; set; }

    [Display(Name = "Email")]
    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Số điện thoại")]
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại gồm 10 chữ số, bắt đầu bằng 0")]
    public string SoDienThoai { get; set; } = string.Empty;
}

// Form thêm sinh viên: tạo kèm tài khoản nên cần mật khẩu ban đầu
public class SinhVienCreateVM : SinhVienFormVM
{
    [Display(Name = "Mật khẩu ban đầu")]
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu ban đầu")]
    [DataType(DataType.Password)]
    public string MatKhau { get; set; } = string.Empty;
}

// Form sửa sinh viên
public class SinhVienEditVM : SinhVienFormVM
{
    public int Id { get; set; }
}
