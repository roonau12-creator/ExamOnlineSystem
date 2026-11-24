namespace ExamOnlineSystem.Models.ViewModels;

// Một dòng trong bảng danh sách giáo viên (chỉ các trường cần hiển thị)
public class GiaoVienListItemVM
{
    public int Id { get; set; }
    public string MaGiaoVien { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SoDienThoai { get; set; } = string.Empty;
    public string? ChucDanh { get; set; }
    public int BoMonId { get; set; }
    public string TenBoMon { get; set; } = string.Empty;
    public int KhoaId { get; set; }
    public string TenKhoa { get; set; } = string.Empty;
    public bool DangHoatDong { get; set; }
    public DateTime? LanDangNhapCuoi { get; set; } // UTC
    public int SoCauHoi { get; set; }
    public int SoDeThi { get; set; }
}
