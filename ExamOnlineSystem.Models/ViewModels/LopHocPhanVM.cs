namespace ExamOnlineSystem.Models.ViewModels;

// Một dòng trong bảng danh sách lớp học phần (/Admin/LopHocPhan)
public class LopHocPhanListItemVM
{
    public int Id { get; set; }
    public string MaLopHocPhan { get; set; } = string.Empty;
    public string TenLopHocPhan { get; set; } = string.Empty;

    public int HocPhanId { get; set; }
    public string MaHocPhan { get; set; } = string.Empty;
    public string TenHocPhan { get; set; } = string.Empty;
    public string TenMonHoc { get; set; } = string.Empty;
    public string TenKhoa { get; set; } = string.Empty;
    public int SoTinChi { get; set; }

    public int NamHocId { get; set; }
    public string MaNamHoc { get; set; } = string.Empty;
    public bool NamHienTai { get; set; }

    // 1 = Học kỳ 1, 2 = Học kỳ 2, 3 = Học kỳ hè
    public int HocKy { get; set; }

    public int SiSoToiDa { get; set; }
    public int SoSinhVien { get; set; }
    public int SoGiaoVien { get; set; }

    // Họ tên giảng viên phụ trách; rỗng khi chưa phân công
    public string GiaoVienPhuTrach { get; set; } = string.Empty;

    public string GhiChu { get; set; } = string.Empty;
}

// Một giảng viên trong danh sách chọn của trang phân công
public class GiaoVienPhanCongVM
{
    public int Id { get; set; }
    public string MaGiaoVien { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int BoMonId { get; set; }
    public string TenBoMon { get; set; } = string.Empty;
    public int KhoaId { get; set; }
    public string TenKhoa { get; set; } = string.Empty;

    // Chỉ có giá trị với giảng viên đã được phân công: 1 = Phụ trách, 2 = Trợ giảng
    public int VaiTro { get; set; }

    // Số lớp học phần giảng viên đang được phân công (để thấy khối lượng)
    public int SoLopHocPhan { get; set; }
}

// Trang chi tiết lớp học phần
public class LopHocPhanDetailVM
{
    public LopHocPhan LopHocPhan { get; set; } = null!;
    public List<GiaoVienPhanCongVM> GiaoViens { get; set; } = new();
    public List<SinhVienChonVM> SinhViens { get; set; } = new();
}

// Trang "Phân công giảng viên, sinh viên" (/Admin/LopHocPhan/PhanCong/{id}).
// Danh sách ứng viên (giảng viên / sinh viên chưa có trong lớp) được tải riêng qua Ajax.
public class PhanCongLopHocPhanVM
{
    public LopHocPhan LopHocPhan { get; set; } = null!;

    // Giảng viên đã phân công, phụ trách lên trước
    public List<GiaoVienPhanCongVM> GiaoVienTrongLop { get; set; } = new();

    // Sinh viên đã có trong lớp học phần, sắp theo mã sinh viên
    public List<SinhVienChonVM> SinhVienTrongLop { get; set; } = new();

    // Cảnh báo của lần phân công vừa thực hiện (không phải lỗi)
    public List<string> CanhBao { get; set; } = new();

    // Tab đang mở sau khi redirect: "giaovien" hoặc "sinhvien"
    public string Tab { get; set; } = "giaovien";
}

// Kết quả của một lần phân công giảng viên / sinh viên
public class PhanCongResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    // Việc đã làm xong nhưng người dùng cần biết (không phải lỗi)
    public List<string> CanhBao { get; set; } = new();

    public static PhanCongResult Loi(string message) => new() { Success = false, Message = message };
    public static PhanCongResult Ok(string message) => new() { Success = true, Message = message };
}
