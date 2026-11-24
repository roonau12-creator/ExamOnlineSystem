namespace ExamOnlineSystem.Models.ViewModels;

// Một dòng trong bảng danh sách tài khoản (chỉ các trường cần hiển thị,
// không bao giờ trả PasswordHash / SecurityStamp ra ngoài)
public class UserListItemVM
{
    // Id của IdentityUser là string (GUID)
    public string Id { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SoDienThoai { get; set; } = string.Empty;
    public string? ChucDanh { get; set; }
    // Tên vai trò Identity: Admin / Teacher / Student (rỗng nếu tài khoản chưa được gán vai trò)
    public string VaiTro { get; set; } = string.Empty;
    public int? KhoaId { get; set; }
    public string TenKhoa { get; set; } = string.Empty;
    public bool DangHoatDong { get; set; }
    public DateTime NgayTao { get; set; }          // UTC
    public DateTime? LanDangNhapCuoi { get; set; } // UTC
    // Hồ sơ nghiệp vụ đang gắn với tài khoản (nếu có) – dùng để cảnh báo khi khóa tài khoản
    public string? MaSinhVien { get; set; }
    public string? MaGiaoVien { get; set; }
}
