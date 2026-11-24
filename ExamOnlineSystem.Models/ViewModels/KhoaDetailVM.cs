namespace ExamOnlineSystem.Models.ViewModels;

// Trang chi tiết khoa: thông tin khoa + các bộ môn, lớp, môn học, giảng viên thuộc khoa
public class KhoaDetailVM
{
    public Khoa Khoa { get; set; } = null!;
    public List<KhoaBoMonItem> BoMons { get; set; } = new();
    public List<KhoaLopItem> Lops { get; set; } = new();
    public List<KhoaMonHocItem> MonHocs { get; set; } = new();
    public List<KhoaGiaoVienItem> GiaoViens { get; set; } = new();
    public int SoHocPhan { get; set; }
    public int SoSinhVien { get; set; }
}

public class KhoaBoMonItem
{
    public int Id { get; set; }
    public string MaBoMon { get; set; } = string.Empty;
    public string TenBoMon { get; set; } = string.Empty;
    public int SoThuTu { get; set; }
    public int SoGiaoVien { get; set; }
    public int SoMonHoc { get; set; }
}

public class KhoaLopItem
{
    public int Id { get; set; }
    public string MaLop { get; set; } = string.Empty;
    public string TenLop { get; set; } = string.Empty;
    public int SiSo { get; set; }
}

public class KhoaMonHocItem
{
    public int Id { get; set; }
    public string MaMonHoc { get; set; } = string.Empty;
    public string TenMonHoc { get; set; } = string.Empty;
    public string TenBoMon { get; set; } = string.Empty;
    public string HinhThucThi { get; set; } = string.Empty;
    public int ThoiGianThi { get; set; }
    public string TrangThai { get; set; } = string.Empty;
}

public class KhoaGiaoVienItem
{
    public int Id { get; set; }
    public string MaGiaoVien { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string TenBoMon { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string SoDienThoai { get; set; } = string.Empty;
}
