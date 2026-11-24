namespace ExamOnlineSystem.Models.ViewModels;

// Trang "Môn học của tôi" (/Customer/MonHoc): các môn sinh viên học (qua lớp học phần) hoặc thi
public class MonHocSinhVienVM
{
    public SinhVien SinhVien { get; set; } = null!;
    public bool XemTatCa { get; set; } // false = chỉ môn đang học trong năm học hiện tại
    public List<MonHocCuaToiItem> MonHocs { get; set; } = new();
    public int TongSoMon { get; set; }
    public int SoMonDangHoc { get; set; }
}

public class MonHocCuaToiItem
{
    public int MonHocId { get; set; }
    public string MaMonHoc { get; set; } = string.Empty;
    public string TenMonHoc { get; set; } = string.Empty;
    public string TenBoMon { get; set; } = string.Empty;
    public string HinhThucThi { get; set; } = string.Empty;
    public int ThoiGianThi { get; set; }

    public List<SinhVienLopHocPhanItem> LopHocPhans { get; set; } = new();
    public List<SinhVienLichThiItem> LichThis { get; set; } = new();

    public bool DangHoc => LopHocPhans.Any(l => l.LaNamHienTai);
    public int SoTinChi => LopHocPhans.Select(l => l.SoTinChi).DefaultIfEmpty(0).Max();
    public double? DiemCaoNhat => LichThis.Where(l => l.DiemHe10.HasValue).Select(l => l.DiemHe10).Max();
}
