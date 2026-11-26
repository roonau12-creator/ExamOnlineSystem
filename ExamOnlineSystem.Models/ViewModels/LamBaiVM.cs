namespace ExamOnlineSystem.Models.ViewModels;

// ===== Trang làm bài của sinh viên (/Customer/LamBai/Index/{lanThiId}) =====
// Dữ liệu này được gửi về máy sinh viên trong lúc thi nên KHÔNG chứa đáp án đúng,
// không chứa giải thích và không chứa điểm đã chấm của từng câu.
public class LamBaiVM
{
    public int LanThiId { get; set; }
    public int LanThu { get; set; }
    public int SoLanThi { get; set; }               // tổng số lượt của sinh viên trong kỳ thi này

    public int LichThiId { get; set; }
    public string TenLichThi { get; set; } = string.Empty;
    public string TenMonHoc { get; set; } = string.Empty;
    public string MaDeThi { get; set; } = string.Empty;
    public string TenDeThi { get; set; } = string.Empty;
    public string TenGiaoVien { get; set; } = string.Empty;

    public string MaSinhVien { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string MaLop { get; set; } = string.Empty;

    public int ThoiLuong { get; set; }              // phút làm bài theo đề
    public double TongDiem { get; set; }            // thang điểm của đề
    public int TrangThai { get; set; }              // 0: Đang làm bài, 1: Đã nộp bài, 2: Hết giờ, 3: Đình chỉ

    public DateTime ThoiGianBatDau { get; set; }    // UTC - lúc bấm bắt đầu
    public DateTime HetHan { get; set; }            // UTC - min(bắt đầu + thời lượng, giờ đóng kỳ thi)
    public DateTime LichThiKetThuc { get; set; }    // UTC - giờ đóng của kỳ thi

    public List<CauLamBaiVM> CauHois { get; set; } = new();

    // Hết hạn vì kỳ thi đóng sớm hơn thời lượng làm bài (vào thi muộn)
    public bool BiCatTheoGioDong => HetHan < ThoiGianBatDau.AddMinutes(ThoiLuong);

    public int SoCau => CauHois.Count;
    public int SoCauDaLam => CauHois.Count(c => c.DaTraLoi);
    public int SoCauTracNghiem => CauHois.Count(c => c.LoaiCauHoi != 2);
    public int SoCauTuLuan => CauHois.Count(c => c.LoaiCauHoi == 2);
    public int TiLeDaLam => SoCau == 0 ? 0 : (int)Math.Round(SoCauDaLam * 100.0 / SoCau);

    // Số giây còn lại để làm bài (0 = đã hết giờ)
    public int SoGiayConLai(DateTime bayGio) => Math.Max(0, (int)Math.Floor((HetHan - bayGio).TotalSeconds));
}

// Một câu hỏi trong trang làm bài, kèm lựa chọn sinh viên đang chọn
public class CauLamBaiVM
{
    public int CauHoiLanThiId { get; set; }
    public int ThuTu { get; set; }
    public string NoiDung { get; set; } = string.Empty;
    public short LoaiCauHoi { get; set; }           // 1: Trắc nghiệm, 2: Tự luận
    public decimal Diem { get; set; }
    public bool CoHinhAnh { get; set; }

    // Câu có nhiều đáp án đúng -> sinh viên chọn nhiều (checkbox) và phải chọn đủ mới được điểm
    public bool ChonNhieu { get; set; }

    public string? NoiDungTraLoi { get; set; }      // bài tự luận đã lưu
    public List<DapAnLamBaiVM> DapAns { get; set; } = new();

    public bool DaTraLoi => LoaiCauHoi == 2
        ? !string.IsNullOrWhiteSpace(NoiDungTraLoi)
        : DapAns.Any(d => d.DaChon);
}

// Một lựa chọn của câu trắc nghiệm (không mang thông tin đúng/sai)
public class DapAnLamBaiVM
{
    public int Id { get; set; }                     // DapAnLanThiId
    public int ThuTu { get; set; }
    public string NoiDung { get; set; } = string.Empty;
    public bool DaChon { get; set; }
}

// ===== Trang kết quả ngay sau khi nộp bài (/Customer/LamBai/KetQua/{lanThiId}) =====
public class KetQuaLamBaiVM
{
    public int LanThiId { get; set; }
    public int LanThu { get; set; }
    public int LichThiId { get; set; }
    public string TenLichThi { get; set; } = string.Empty;
    public string TenMonHoc { get; set; } = string.Empty;
    public string MaDeThi { get; set; } = string.Empty;
    public string TenDeThi { get; set; } = string.Empty;
    public string MaSinhVien { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;

    public int TrangThai { get; set; }              // 1: Đã nộp bài, 2: Hết giờ, 3: Đình chỉ
    public DateTime ThoiGianBatDau { get; set; }    // UTC
    public DateTime? ThoiGianNopBai { get; set; }   // UTC
    public int ThoiLuong { get; set; }
    public double TongDiem { get; set; }

    public int SoCau { get; set; }
    public int SoCauDaLam { get; set; }
    public int SoCauTuLuan { get; set; }

    // Điểm của lần thi: chỉ có khi bài được chấm xong (đề không có câu tự luận)
    public double? Diem { get; set; }
    public bool ChoCham => SoCauTuLuan > 0 && Diem == null;

    public int SoCauBoTrong => SoCau - SoCauDaLam;
    public double? DiemHe10 => QuyDoiDiem.He10(Diem, TongDiem);
    public string XepLoai => KetQuaLabel.XepLoai(DiemHe10);

    public int? SoPhutLamBai => ThoiGianNopBai.HasValue
        ? (int)Math.Round((ThoiGianNopBai.Value - ThoiGianBatDau).TotalMinutes)
        : null;
}
