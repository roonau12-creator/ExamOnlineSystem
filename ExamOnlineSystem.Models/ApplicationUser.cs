using Microsoft.AspNetCore.Identity;

namespace ExamOnlineSystem.Models;

public class ApplicationUser : IdentityUser
{
    public string HoTen { get; set; } = null!;        // hiện trên header: "Nguyễn Văn An"
    public string? ChucDanh { get; set; }             // vd: Giảng viên, Trưởng bộ môn
    public int? KhoaId { get; set; }                  // khoa công tác, có thể để trống
    public bool DangHoatDong { get; set; } = true;    // quản trị khóa/mở tài khoản
    // Npgsql chỉ nhận DateTime kiểu UTC cho cột timestamp with time zone; hiển thị dùng ToLocalTime()
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;
    public DateTime? LanDangNhapCuoi { get; set; }
}