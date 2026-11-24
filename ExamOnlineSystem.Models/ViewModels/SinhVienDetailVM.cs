namespace ExamOnlineSystem.Models.ViewModels;

// Trang chi tiết sinh viên: hồ sơ + tài khoản + lớp học phần đang học + lịch thi và kết quả + lịch sử thi
public class SinhVienDetailVM
{
    public SinhVien SinhVien { get; set; } = null!; // kèm Lop, Khoa

    // Tài khoản đăng nhập (null nếu hồ sơ không còn tài khoản)
    public bool? DangHoatDong { get; set; }
    public DateTime? LanDangNhapCuoi { get; set; } // UTC

    public List<SinhVienLopHocPhanItem> LopHocPhans { get; set; } = new();
    public List<SinhVienLichThiItem> LichThis { get; set; } = new();

    // Lịch sử thi: mọi lượt làm bài của sinh viên, mới nhất trước
    public List<SinhVienLanThiItem> LanThis { get; set; } = new();
}

public class SinhVienLanThiItem
{
    public int Id { get; set; }
    public int LichThiId { get; set; }
    public string TenLichThi { get; set; } = string.Empty;
    public string TenMonHoc { get; set; } = string.Empty;
    public int LanThu { get; set; }
    public DateTime ThoiGianBatDau { get; set; } // UTC
    public DateTime? ThoiGianNopBai { get; set; } // UTC
    public int ThoiLuong { get; set; } // phút, theo đề thi
    public int TrangThai { get; set; } // 0 = Đang làm bài, 1 = Đã nộp bài, 2 = Hết giờ
    public double? Diem { get; set; }
    public double TongDiem { get; set; }

    public double? DiemHe10 => QuyDoiDiem.He10(Diem, TongDiem);
    public TimeSpan? ThoiGianLam => ThoiGianNopBai - ThoiGianBatDau;
}

public class SinhVienLopHocPhanItem
{
    public int Id { get; set; }
    public int MonHocId { get; set; }
    public string MaLopHocPhan { get; set; } = string.Empty;
    public string TenLopHocPhan { get; set; } = string.Empty;
    public string MaHocPhan { get; set; } = string.Empty;
    public string TenHocPhan { get; set; } = string.Empty;
    public int SoTinChi { get; set; }
    public string TenNamHoc { get; set; } = string.Empty;
    public bool LaNamHienTai { get; set; }
    public int HocKy { get; set; }
    public string? GiaoVienPhuTrach { get; set; }
}

public class SinhVienLichThiItem
{
    public int LichThiId { get; set; }
    public int MonHocId { get; set; }
    public string TenLichThi { get; set; } = string.Empty;
    public string TenDeThi { get; set; } = string.Empty;
    public string TenMonHoc { get; set; } = string.Empty;
    public int ThoiLuong { get; set; } // phút làm bài, theo đề thi
    public string GhiChu { get; set; } = string.Empty;
    public DateTime ThoiGianBatDau { get; set; } // UTC
    public DateTime ThoiGianKetThuc { get; set; } // UTC
    public int TrangThai { get; set; } // 0 = Chưa thi, 1 = Đang làm bài, 2 = Đã nộp bài
    public double? Diem { get; set; }
    public double TongDiem { get; set; }
    public int SoLuotThi { get; set; }
    public int? LanThiMoiNhatId { get; set; } // để mở bài làm

    public double? DiemHe10 => QuyDoiDiem.He10(Diem, TongDiem);
}
