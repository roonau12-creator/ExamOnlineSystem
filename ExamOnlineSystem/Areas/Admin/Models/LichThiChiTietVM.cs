using ExamOnlineSystem.Models;

namespace ExamOnlineSystem.Areas.Admin.Models;

// Dữ liệu cho trang Chi tiết lịch thi (/Admin/LichThi/Details/5)
public class LichThiChiTietVM
{
    public LichThi LichThi { get; set; } = null!;

    public string TenMonHoc { get; set; } = string.Empty;
    public string TenGiaoVien { get; set; } = string.Empty;

    // Sắp diễn ra / Đang diễn ra / Đã kết thúc
    public string TrangThai { get; set; } = string.Empty;

    // Sắp diễn ra: số phút đến lúc bắt đầu; Đang diễn ra: số phút đến lúc kết thúc
    public int ConLaiPhut { get; set; }

    // Quyền thao tác của người đang xem
    public bool SuaDuoc { get; set; }
    public bool XoaDuoc { get; set; }

    public List<LopDuThiVM> Lops { get; set; } = new();
    public List<SinhVienDuThiVM> SinhViens { get; set; } = new();

    public int KhungGioPhut => (int)Math.Round((LichThi.ThoiGianKetThuc - LichThi.ThoiGianBatDau).TotalMinutes);
}

public class LopDuThiVM
{
    public int Id { get; set; }
    public string MaLop { get; set; } = string.Empty;
    public string TenLop { get; set; } = string.Empty;
    public string TenKhoa { get; set; } = string.Empty;
    public int SoSinhVien { get; set; }
}

public class SinhVienDuThiVM
{
    public string MaSinhVien { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public DateOnly NgaySinh { get; set; }
    public bool GioiTinh { get; set; }
    public string MaLop { get; set; } = string.Empty;
    public string TenLop { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SoDienThoai { get; set; } = string.Empty;

    // 0: Chưa thi, 1: Đang làm bài, 2: Đã nộp bài
    public int TrangThai { get; set; }
    public DateTime? ThoiGianBatDau { get; set; }
    public DateTime? ThoiGianNopBai { get; set; }
    public double? Diem { get; set; }
}
