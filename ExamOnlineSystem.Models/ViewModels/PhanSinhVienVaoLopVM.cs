namespace ExamOnlineSystem.Models.ViewModels;

// Trang "Phân sinh viên vào lớp" (/Admin/Lop/PhanSinhVien/{id}):
// danh sách sinh viên đang thuộc lớp + các lớp khác để chuyển sinh viên sang.
// Danh sách ứng viên (sinh viên của lớp khác) được tải riêng qua GetSinhVienNgoaiLop.
public class PhanSinhVienVaoLopVM
{
    public Lop Lop { get; set; } = null!;

    // Sinh viên hiện thuộc lớp, sắp theo mã sinh viên
    public List<SinhVienChonVM> SinhVienTrongLop { get; set; } = new();

    // Các lớp khác – dùng cho ô "Chuyển sang lớp"
    public List<LopChonVM> LopKhac { get; set; } = new();

    // Cảnh báo của lần phân lớp vừa thực hiện (vd: sinh viên còn suất dự thi ở lớp cũ)
    public List<string> CanhBao { get; set; } = new();
}

// Một sinh viên trong danh sách chọn của trang phân lớp
public class SinhVienChonVM
{
    public int Id { get; set; }
    public string MaSinhVien { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public DateOnly NgaySinh { get; set; }
    // true = Nam, false = Nữ
    public bool GioiTinh { get; set; }
    public string Email { get; set; } = string.Empty;
    public int LopId { get; set; }
    public string MaLop { get; set; } = string.Empty;
    public string TenLop { get; set; } = string.Empty;
    public int KhoaId { get; set; }
    public string TenKhoa { get; set; } = string.Empty;
}

// Kết quả của một lần phân/chuyển sinh viên vào lớp
public class PhanLopResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    // Việc đã làm xong nhưng người dùng cần biết (không phải lỗi)
    public List<string> CanhBao { get; set; } = new();

    public static PhanLopResult Loi(string message) => new() { Success = false, Message = message };
}
