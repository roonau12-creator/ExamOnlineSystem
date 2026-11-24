namespace ExamOnlineSystem.Models.ViewModels;

// Trang chi tiết bộ môn: thông tin bộ môn + môn học, học phần, giảng viên thuộc bộ môn
public class BoMonDetailVM
{
    public BoMon BoMon { get; set; } = null!; // kèm Khoa
    public List<BoMonMonHocItem> MonHocs { get; set; } = new();
    public List<BoMonHocPhanItem> HocPhans { get; set; } = new();
    public List<BoMonGiaoVienItem> GiaoViens { get; set; } = new();
    public int SoDeThi { get; set; }
}

public class BoMonMonHocItem
{
    public int Id { get; set; }
    public string MaMonHoc { get; set; } = string.Empty;
    public string TenMonHoc { get; set; } = string.Empty;
    public string HinhThucThi { get; set; } = string.Empty;
    public int ThoiGianThi { get; set; }
    public string TrangThai { get; set; } = string.Empty;
    public int SoHocPhan { get; set; }
    public int SoDeThi { get; set; }
}

public class BoMonHocPhanItem
{
    public int Id { get; set; }
    public string MaHocPhan { get; set; } = string.Empty;
    public string TenHocPhan { get; set; } = string.Empty;
    public string TenMonHoc { get; set; } = string.Empty;
    public int SoTinChi { get; set; }
}

public class BoMonGiaoVienItem
{
    public int Id { get; set; }
    public string MaGiaoVien { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string SoDienThoai { get; set; } = string.Empty;
}
