namespace ExamOnlineSystem.Models.ViewModels;

// Một dòng sinh viên đọc từ file Excel (đã kiểm tra định dạng). Dong = số dòng trong file để báo lỗi.
public class SinhVienImportRow
{
    public int Dong { get; set; }
    public string MaSinhVien { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public DateOnly NgaySinh { get; set; }
    public bool GioiTinh { get; set; }          // true = Nam, false = Nữ
    public string MaLop { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SoDienThoai { get; set; } = string.Empty;
    public string? MatKhau { get; set; }        // trống -> dùng mật khẩu mặc định
}

public class SinhVienImportResult
{
    public int Added { get; set; }
    public List<string> Skipped { get; set; } = new();   // mã SV đã có -> bỏ qua
    public List<string> Errors { get; set; } = new();    // có lỗi -> không nhập dòng nào
}
