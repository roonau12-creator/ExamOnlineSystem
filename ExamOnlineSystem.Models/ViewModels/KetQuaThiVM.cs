namespace ExamOnlineSystem.Models.ViewModels;

// ===== Quản lý kết quả: theo dõi kết quả thi =====
// Điểm gốc được chấm theo thang điểm của đề (DeThi.TongDiem). Để so sánh giữa các đề,
// mọi thống kê / xếp loại đều quy về thang 10 (xem QuyDoiDiem.He10).

// Một dòng trong bảng Theo dõi kết quả thi: tổng hợp kết quả của một lịch thi
public class KetQuaThiListItemVM
{
    public int Id { get; set; }                      // LichThiId
    public string TenLichThi { get; set; } = string.Empty;
    public int DeThiId { get; set; }
    public string MaDeThi { get; set; } = string.Empty;
    public string TenDeThi { get; set; } = string.Empty;
    public string TenMonHoc { get; set; } = string.Empty;
    public int ThoiLuong { get; set; }               // phút làm bài (theo đề)
    public double TongDiem { get; set; }             // thang điểm của đề
    public DateTime ThoiGianBatDau { get; set; }     // UTC
    public DateTime ThoiGianKetThuc { get; set; }    // UTC
    public int GiaoVienId { get; set; }
    public string TenGiaoVien { get; set; } = string.Empty;
    public List<string> Lops { get; set; } = new();

    public int SoSinhVien { get; set; }              // tổng sinh viên dự thi
    public int SoChuaThi { get; set; }
    public int SoDangThi { get; set; }
    public int SoDaNop { get; set; }
    public int SoCoDiem { get; set; }                // số bài đã có điểm
    public int SoChuaCham { get; set; }              // số bài đã nộp nhưng chưa có điểm
    public int SoDat { get; set; }                   // số bài đạt (>= 5 điểm thang 10)

    public double? DiemTrungBinh { get; set; }       // thang điểm của đề
    public double? DiemCaoNhat { get; set; }
    public double? DiemThapNhat { get; set; }

    // Tỉ lệ % trên tổng sinh viên dự thi
    public int TiLeNop => SoSinhVien == 0 ? 0 : (int)Math.Round(SoDaNop * 100.0 / SoSinhVien);
    // Tỉ lệ đạt tính trên số bài đã có điểm (chưa chấm thì chưa tính)
    public int TiLeDat => SoCoDiem == 0 ? 0 : (int)Math.Round(SoDat * 100.0 / SoCoDiem);

    public double? DiemTrungBinhHe10 => QuyDoiDiem.He10(DiemTrungBinh, TongDiem);
    public double? DiemCaoNhatHe10 => QuyDoiDiem.He10(DiemCaoNhat, TongDiem);
    public double? DiemThapNhatHe10 => QuyDoiDiem.He10(DiemThapNhat, TongDiem);
}

// Trang Theo dõi kết quả của một lịch thi (/Admin/KetQuaThi/Details/5)
public class KetQuaThiChiTietVM
{
    public KetQuaThiListItemVM TongHop { get; set; } = new();
    public string TrangThai { get; set; } = string.Empty;   // Sắp diễn ra / Đang diễn ra / Đã kết thúc
    public List<KetQuaSinhVienVM> SinhViens { get; set; } = new();

    // Phân bố điểm theo khung (thang 10), chỉ tính bài đã có điểm
    public List<PhanBoDiemKhungVM> PhanBo { get; set; } = new();
}

// Kết quả thi của một sinh viên trong một lịch thi
public class KetQuaSinhVienVM
{
    public int Id { get; set; }                      // SinhVienDuThiId
    public int SinhVienId { get; set; }
    public string MaSinhVien { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string MaLop { get; set; } = string.Empty;
    public string TenLop { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // 0: Chưa thi, 1: Đang làm bài, 2: Đã nộp bài
    public int TrangThai { get; set; }
    public DateTime? ThoiGianBatDau { get; set; }    // UTC
    public DateTime? ThoiGianNopBai { get; set; }    // UTC

    public int SoLanThi { get; set; }
    public int? LanThiId { get; set; }               // lần thi gần nhất (để xem bài làm)

    // Điểm chính thức trên SinhVienDuThi; nếu chưa có thì lấy điểm lần thi gần nhất đã chấm
    public double? Diem { get; set; }
    public double TongDiem { get; set; }             // thang điểm của đề, để quy đổi

    public double? DiemHe10 => QuyDoiDiem.He10(Diem, TongDiem);
    public string XepLoai => KetQuaLabel.XepLoai(DiemHe10);
    public bool Dat => DiemHe10 >= 5;

    // Số phút làm bài thực tế (nộp - bắt đầu)
    public int? SoPhutLamBai => ThoiGianBatDau.HasValue && ThoiGianNopBai.HasValue
        ? (int)Math.Round((ThoiGianNopBai.Value - ThoiGianBatDau.Value).TotalMinutes)
        : null;
}

// Một khung điểm trong biểu đồ phân bố
public class PhanBoDiemKhungVM
{
    public string Nhan { get; set; } = string.Empty;  // "8 - 10"
    public string XepLoai { get; set; } = string.Empty;
    public string Mau { get; set; } = "secondary";
    public int SoLuong { get; set; }
    public int TiLe { get; set; }                     // % trên số bài đã có điểm
}

// ===== Chi tiết bài làm của một lần thi (/Admin/KetQuaThi/BaiLam/5) =====
public class BaiThiChiTietVM
{
    public int LanThiId { get; set; }
    public int LanThu { get; set; }
    public int TrangThai { get; set; }                // 0: Đang làm bài, 1: Đã nộp bài, 2: Hết giờ
    public DateTime ThoiGianBatDau { get; set; }      // UTC
    public DateTime? ThoiGianNopBai { get; set; }     // UTC
    public double? Diem { get; set; }

    public int SinhVienDuThiId { get; set; }
    public string MaSinhVien { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string MaLop { get; set; } = string.Empty;
    public string TenLop { get; set; } = string.Empty;

    public int LichThiId { get; set; }
    public string TenLichThi { get; set; } = string.Empty;
    public int DeThiId { get; set; }
    public string MaDeThi { get; set; } = string.Empty;
    public string TenDeThi { get; set; } = string.Empty;
    public string TenMonHoc { get; set; } = string.Empty;
    public int ThoiLuong { get; set; }
    public double TongDiem { get; set; }

    public List<CauHoiBaiLamVM> CauHois { get; set; } = new();

    public int SoCau => CauHois.Count;
    public int SoCauTracNghiem => CauHois.Count(c => c.LoaiCauHoi != 2);
    public int SoCauTuLuan => CauHois.Count(c => c.LoaiCauHoi == 2);

    public int SoCauDaLam => CauHois.Count(c => c.DaTraLoi);
    public int SoCauBoTrong => CauHois.Count(c => c.KetQua == KetQuaCau.BoTrong);
    public int SoCauDung => CauHois.Count(c => c.KetQua == KetQuaCau.Dung);
    public int SoCauSai => CauHois.Count(c => c.KetQua == KetQuaCau.Sai);
    public int SoCauChuaCham => CauHois.Count(c => c.KetQua == KetQuaCau.ChuaCham);
    public int SoCauDaCham => CauHois.Count(c => c.KetQua == KetQuaCau.DaCham);

    // Điểm phần trắc nghiệm (chấm tự động) và tổng điểm đã chấm được của cả bài
    public decimal DiemTracNghiem => CauHois.Where(c => c.LoaiCauHoi != 2).Sum(c => c.DiemDatDuoc);
    public decimal DiemTuLuan => CauHois.Where(c => c.LoaiCauHoi == 2).Sum(c => c.DiemDatDuoc);
    public decimal DiemDaCham => DiemTracNghiem + DiemTuLuan;

    public double? DiemHe10 => QuyDoiDiem.He10(Diem, TongDiem);
    public int? SoPhutLamBai => ThoiGianNopBai.HasValue
        ? (int)Math.Round((ThoiGianNopBai.Value - ThoiGianBatDau).TotalMinutes)
        : null;
}

// Một câu hỏi trong bài làm, kèm đáp án sinh viên đã chọn
public class CauHoiBaiLamVM
{
    public int CauHoiLanThiId { get; set; }
    public int ThuTu { get; set; }
    public string NoiDung { get; set; } = string.Empty;
    public short LoaiCauHoi { get; set; }             // 1: Trắc nghiệm, 2: Tự luận
    public decimal DiemToiDa { get; set; }

    public string? NoiDungTraLoi { get; set; }        // bài tự luận
    public decimal? DiemCham { get; set; }
    public string? NhanXet { get; set; }

    public List<DapAnBaiLamVM> DapAns { get; set; } = new();

    public bool DaTraLoi => LoaiCauHoi == 2
        ? !string.IsNullOrWhiteSpace(NoiDungTraLoi)
        : DapAns.Any(d => d.DaChon);

    // Trắc nghiệm được chấm tự động (đúng khi chọn đủ và chỉ các đáp án đúng);
    // tự luận chỉ biết đã chấm hay chưa vì có điểm thành phần.
    public string KetQua
    {
        get
        {
            if (!DaTraLoi)
            {
                return KetQuaCau.BoTrong;
            }
            if (LoaiCauHoi == 2)
            {
                return DiemCham.HasValue ? KetQuaCau.DaCham : KetQuaCau.ChuaCham;
            }
            return DapAns.All(d => d.DaChon == d.LaDapAnDung) ? KetQuaCau.Dung : KetQuaCau.Sai;
        }
    }

    // Điểm sinh viên đạt được ở câu này: ưu tiên điểm giảng viên chấm, trắc nghiệm thì chấm tự động
    public decimal DiemDatDuoc => DiemCham ?? (KetQua == KetQuaCau.Dung ? DiemToiDa : 0m);
}

// Kết quả của một câu trong bài làm
public static class KetQuaCau
{
    public const string BoTrong = "botrong";
    public const string Dung = "dung";
    public const string Sai = "sai";
    public const string ChuaCham = "chuacham";
    public const string DaCham = "dacham";

    public static string Ten(string ketQua) => ketQua switch
    {
        Dung => "Đúng",
        Sai => "Sai",
        ChuaCham => "Chưa chấm",
        DaCham => "Đã chấm",
        _ => "Bỏ trống"
    };

    public static string Mau(string ketQua) => ketQua switch
    {
        Dung => "success",
        Sai => "danger",
        ChuaCham => "warning",
        DaCham => "info",
        _ => "secondary"
    };
}

public class DapAnBaiLamVM
{
    public int Id { get; set; }
    public int ThuTu { get; set; }
    public string NoiDung { get; set; } = string.Empty;
    public bool LaDapAnDung { get; set; }
    public bool DaChon { get; set; }
}

// Quy đổi điểm theo thang điểm của đề về thang 10
public static class QuyDoiDiem
{
    public static double? He10(double? diem, double tongDiem)
    {
        if (diem == null)
        {
            return null;
        }
        if (tongDiem <= 0)
        {
            return Math.Round(diem.Value, 2);
        }
        return Math.Round(diem.Value / tongDiem * 10, 2);
    }
}

// Tên hiển thị cho kết quả thi
public static class KetQuaLabel
{
    // TrangThai của SinhVienDuThi: 0 = Chưa thi, 1 = Đang làm bài, 2 = Đã nộp bài
    public const string ChuaThi = "Chưa thi";
    public const string DangLamBai = "Đang làm bài";
    public const string DaNopBai = "Đã nộp bài";

    public static string TenTrangThai(int trangThai) => trangThai switch
    {
        1 => DangLamBai,
        2 => DaNopBai,
        _ => ChuaThi
    };

    public static string MauTrangThai(int trangThai) => trangThai switch
    {
        1 => "warning",
        2 => "success",
        _ => "secondary"
    };

    // TrangThai của LanThi: 0 = Đang làm bài, 1 = Đã nộp bài, 2 = Hết giờ, 3 = Đình chỉ
    public static string TenTrangThaiLanThi(int trangThai) => trangThai switch
    {
        1 => "Đã nộp bài",
        2 => "Hết giờ",
        3 => "Đình chỉ",
        _ => "Đang làm bài"
    };

    // Xếp loại theo điểm thang 10
    public static string XepLoai(double? diemHe10) => diemHe10 switch
    {
        null => "Chưa chấm",
        >= 9 => "Xuất sắc",
        >= 8 => "Giỏi",
        >= 6.5 => "Khá",
        >= 5 => "Trung bình",
        >= 3.5 => "Yếu",
        _ => "Kém"
    };

    public static string MauXepLoai(string xepLoai) => xepLoai switch
    {
        "Xuất sắc" => "success",
        "Giỏi" => "primary",
        "Khá" => "info",
        "Trung bình" => "warning",
        "Yếu" => "danger",
        "Kém" => "dark",
        _ => "secondary"
    };

    // Các khung điểm (thang 10) dùng cho biểu đồ phân bố, từ cao xuống thấp
    public static readonly (string Nhan, string XepLoai, double Tu, double Den, string Mau)[] KhungDiem =
    {
        ("9 - 10", "Xuất sắc", 9, 10.01, "success"),
        ("8 - 9", "Giỏi", 8, 9, "primary"),
        ("6.5 - 8", "Khá", 6.5, 8, "info"),
        ("5 - 6.5", "Trung bình", 5, 6.5, "warning"),
        ("3.5 - 5", "Yếu", 3.5, 5, "danger"),
        ("0 - 3.5", "Kém", 0, 3.5, "dark")
    };
}
