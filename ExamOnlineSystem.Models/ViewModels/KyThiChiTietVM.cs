namespace ExamOnlineSystem.Models.ViewModels;

// Trang "Chi tiết kỳ thi" của sinh viên (/Customer/LichThi/ChiTiet/{id}).
// Chỉ thông tin chung của kỳ thi + kết quả của chính sinh viên; không có nội dung câu hỏi.
public class KyThiChiTietVM
{
    public int LichThiId { get; set; }
    public string TenLichThi { get; set; } = string.Empty;
    public string GhiChu { get; set; } = string.Empty;
    public DateTime ThoiGianBatDau { get; set; } // UTC
    public DateTime ThoiGianKetThuc { get; set; } // UTC
    public string GiaoVien { get; set; } = string.Empty;
    public List<string> Lops { get; set; } = new();
    public int SoThiSinh { get; set; }

    public string MaMonHoc { get; set; } = string.Empty;
    public string TenMonHoc { get; set; } = string.Empty;
    public string TenDeThi { get; set; } = string.Empty;
    public int LoaiDeThi { get; set; } // 1 = Trắc nghiệm, 2 = Tự luận, 3 = Kết hợp
    public int ThoiLuong { get; set; } // phút làm bài
    public double TongDiem { get; set; }
    public int SoCauHoi { get; set; }
    public bool DaPhatHanh { get; set; } // chỉ đề đã phát hành mới dùng để thi

    // Của sinh viên đang xem
    public int TrangThai { get; set; } // 0 = Chưa thi, 1 = Đang làm bài, 2 = Đã nộp bài
    public double? Diem { get; set; }
    public List<SinhVienLanThiItem> LanThis { get; set; } = new();

    public double? DiemHe10 => QuyDoiDiem.He10(Diem, TongDiem);
}

// Trang "Xác nhận vào thi" (/Customer/LichThi/XacNhan/{id}): kiểm tra trước khi bắt đầu làm bài
public class XacNhanVaoThiVM
{
    public KyThiChiTietVM KyThi { get; set; } = null!;
    public SinhVien SinhVien { get; set; } = null!; // kèm Lop
    public List<KiemTraVaoThi> KiemTras { get; set; } = new();

    // Đủ điều kiện: không có mục nào "Không đạt"
    public bool SanSang => KiemTras.All(k => k.MucDo != MucDoKiemTra.KhongDat);

    // Trang làm bài chưa có -> nút "Bắt đầu làm bài" luôn tắt
    public bool CoTrangLamBai { get; set; }
}

public enum MucDoKiemTra { Dat, CanhBao, KhongDat }

public class KiemTraVaoThi
{
    public string Ten { get; set; } = string.Empty;
    public string MoTa { get; set; } = string.Empty;
    public MucDoKiemTra MucDo { get; set; }
}
