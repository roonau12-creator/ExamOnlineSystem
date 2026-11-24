using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamOnlineSystem.Models;

// Lớp học phần: một lần mở lớp của học phần trong một học kỳ của một năm học.
// Giảng viên được phân công qua LopHocPhanGiaoVien (tối đa 1 người phụ trách, còn lại trợ giảng),
// sinh viên được phân vào qua LopHocPhanSinhVien.
public class LopHocPhan
{
    [Key]
    public int Id { get; set; }

    [Display(Name = "Mã lớp học phần")]
    [Required(ErrorMessage = "Vui lòng nhập mã lớp học phần")]
    [StringLength(50, ErrorMessage = "Mã lớp học phần tối đa 50 ký tự")]
    public string MaLopHocPhan { get; set; } = string.Empty;

    [Display(Name = "Tên lớp học phần")]
    [Required(ErrorMessage = "Vui lòng nhập tên lớp học phần")]
    [StringLength(200, ErrorMessage = "Tên lớp học phần tối đa 200 ký tự")]
    public string TenLopHocPhan { get; set; } = string.Empty;

    [Display(Name = "Học phần")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn học phần")]
    public int HocPhanId { get; set; }

    [ForeignKey(nameof(HocPhanId))]
    public HocPhan? HocPhan { get; set; }

    [Display(Name = "Năm học")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn năm học")]
    public int NamHocId { get; set; }

    [ForeignKey(nameof(NamHocId))]
    public NamHoc? NamHoc { get; set; }

    // 1 = Học kỳ 1, 2 = Học kỳ 2, 3 = Học kỳ hè (xem LopHocPhanLabel.TenHocKy)
    [Display(Name = "Học kỳ")]
    [Range(1, 3, ErrorMessage = "Vui lòng chọn học kỳ")]
    public int HocKy { get; set; } = 1;

    [Display(Name = "Sĩ số tối đa")]
    [Range(1, 500, ErrorMessage = "Sĩ số tối đa phải từ 1 đến 500")]
    public int SiSoToiDa { get; set; } = 60;

    // Không bắt buộc trên form (string? để không bị validate "required" tự động của ASP.NET Core);
    // cột DB vẫn NOT NULL mặc định '' – xem cấu hình trong ApplicationDbContext
    [Display(Name = "Ghi chú")]
    [StringLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự")]
    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public string? GhiChu { get; set; } = string.Empty;

    public DateTime NgayTao { get; set; } = DateTime.UtcNow;

    public List<LopHocPhanGiaoVien> LopHocPhanGiaoViens { get; set; } = new();
    public List<LopHocPhanSinhVien> LopHocPhanSinhViens { get; set; } = new();
}

// Giảng viên được phân công cho lớp học phần
public class LopHocPhanGiaoVien
{
    [Key]
    public int Id { get; set; }

    public int LopHocPhanId { get; set; }
    [ForeignKey(nameof(LopHocPhanId))]
    public LopHocPhan? LopHocPhan { get; set; }

    public int GiaoVienId { get; set; }
    [ForeignKey(nameof(GiaoVienId))]
    public GiaoVien? GiaoVien { get; set; }

    // 1 = Phụ trách, 2 = Trợ giảng (xem LopHocPhanLabel.TenVaiTro)
    public int VaiTro { get; set; } = LopHocPhanVaiTro.PhuTrach;

    public DateTime NgayPhanCong { get; set; } = DateTime.UtcNow;
}

// Sinh viên được phân vào lớp học phần
public class LopHocPhanSinhVien
{
    [Key]
    public int Id { get; set; }

    public int LopHocPhanId { get; set; }
    [ForeignKey(nameof(LopHocPhanId))]
    public LopHocPhan? LopHocPhan { get; set; }

    public int SinhVienId { get; set; }
    [ForeignKey(nameof(SinhVienId))]
    public SinhVien? SinhVien { get; set; }

    public DateTime NgayDangKy { get; set; } = DateTime.UtcNow;
}

// Giá trị của LopHocPhanGiaoVien.VaiTro
public static class LopHocPhanVaiTro
{
    public const int PhuTrach = 1;
    public const int TroGiang = 2;
}
