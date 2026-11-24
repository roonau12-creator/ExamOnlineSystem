namespace ExamOnlineSystem.Models.ViewModels;

// Một dòng trong bảng danh sách lịch thi
public class LichThiListItemVM
{
    public int Id { get; set; }
    public string TenLichThi { get; set; } = string.Empty;
    public int DeThiId { get; set; }
    public string MaDeThi { get; set; } = string.Empty;
    public string TenDeThi { get; set; } = string.Empty;
    public string TenMonHoc { get; set; } = string.Empty;
    public int ThoiLuong { get; set; }              // phút làm bài (theo đề)
    public DateTime ThoiGianBatDau { get; set; }    // UTC
    public DateTime ThoiGianKetThuc { get; set; }   // UTC
    public int GiaoVienId { get; set; }
    public string TenGiaoVien { get; set; } = string.Empty;
    public string GhiChu { get; set; } = string.Empty;
    public List<string> Lops { get; set; } = new(); // tên lớp tham gia
    public int SoSinhVien { get; set; }              // tổng sinh viên các lớp tham gia
}

public static class TrangThaiLichThi
{
    public const string SapDienRa = "Sắp diễn ra";
    public const string DangDienRa = "Đang diễn ra";
    public const string DaKetThuc = "Đã kết thúc";

    // Trạng thái theo thời gian hiện tại (UTC)
    public static string Tinh(DateTime batDau, DateTime ketThuc, DateTime bayGio) =>
        bayGio < batDau ? SapDienRa : bayGio <= ketThuc ? DangDienRa : DaKetThuc;
}

// Form Tạo lịch thi (giờ nhập theo giờ địa phương của máy chủ, lưu UTC)
public class LichThiFormVM
{
    // 0 = tạo mới; > 0 = đang sửa lịch thi này
    public int Id { get; set; }

    [System.ComponentModel.DataAnnotations.Display(Name = "Tên lịch thi")]
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng nhập tên lịch thi")]
    [System.ComponentModel.DataAnnotations.StringLength(200, ErrorMessage = "Tên lịch thi tối đa 200 ký tự")]
    public string TenLichThi { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Display(Name = "Đề thi (đã phát hành)")]
    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn đề thi")]
    public int DeThiId { get; set; }

    [System.ComponentModel.DataAnnotations.Display(Name = "Bắt đầu")]
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng chọn thời gian bắt đầu")]
    public DateTime? ThoiGianBatDau { get; set; }

    [System.ComponentModel.DataAnnotations.Display(Name = "Kết thúc")]
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng chọn thời gian kết thúc")]
    public DateTime? ThoiGianKetThuc { get; set; }

    public List<int> LopIds { get; set; } = new();

    [System.ComponentModel.DataAnnotations.Display(Name = "Ghi chú")]
    [System.ComponentModel.DataAnnotations.DisplayFormat(ConvertEmptyStringToNull = false)]
    public string? GhiChu { get; set; } = string.Empty;
}

// Lớp để chọn vào lịch thi
public class LopChonVM
{
    public int Id { get; set; }
    public string MaLop { get; set; } = string.Empty;
    public string TenLop { get; set; } = string.Empty;
    public string TenKhoa { get; set; } = string.Empty;
    public int SoSinhVien { get; set; }
}

// Lớp bị trùng giờ với một lịch thi khác
public class TrungLichVM
{
    public string TenLop { get; set; } = string.Empty;
    public string TenLichThi { get; set; } = string.Empty;
    public DateTime ThoiGianBatDau { get; set; }  // UTC
    public DateTime ThoiGianKetThuc { get; set; } // UTC
}
