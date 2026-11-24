namespace ExamOnlineSystem.Models.ViewModels;

// Trang chi tiết giảng viên: thông tin giảng viên + bộ môn/khoa công tác + giảng viên cùng bộ môn + lớp học phần phụ trách
public class GiaoVienDetailVM
{
    public GiaoVienListItemVM GiaoVien { get; set; } = null!;
    public string MaBoMon { get; set; } = string.Empty;
    public int KhoaId { get; set; }
    public string MaKhoa { get; set; } = string.Empty;
    public List<GiaoVienListItemVM> DongNghieps { get; set; } = new();

    // Lớp học phần được phân công (phụ trách / trợ giảng), mới nhất trước
    public List<GiaoVienLopHocPhanItem> LopHocPhans { get; set; } = new();
}

// Một lớp học phần giảng viên được phân công
public class GiaoVienLopHocPhanItem
{
    public int Id { get; set; }
    public string MaLopHocPhan { get; set; } = string.Empty;
    public string TenLopHocPhan { get; set; } = string.Empty;
    public string MaHocPhan { get; set; } = string.Empty;
    public string TenHocPhan { get; set; } = string.Empty;
    public int SoTinChi { get; set; }
    public string TenNamHoc { get; set; } = string.Empty;
    public bool LaNamHienTai { get; set; }
    public int HocKy { get; set; }
    public int VaiTro { get; set; } // 1 = Phụ trách, 2 = Trợ giảng
    public int SoSinhVien { get; set; }
    public int SiSoToiDa { get; set; }
}
