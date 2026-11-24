namespace ExamOnlineSystem.Models.ViewModels;

// ===== Thống kê giảng viên =====
// Gom hoạt động chuyên môn của từng giảng viên: ngân hàng câu hỏi, đề thi, lịch thi phụ trách,
// lớp học phần được phân công và kết quả thi của các lịch thi do họ tổ chức.
// Điểm luôn quy về thang 10 (QuyDoiDiem.He10) vì mỗi đề có thang điểm riêng.

// Trang thống kê giảng viên (/Admin/ThongKe/GiaoVien)
public class ThongKeGiaoVienTongQuanVM
{
    public List<ThongKeGiaoVienVM> GiaoViens { get; set; } = new();

    public int TongGiaoVien => GiaoViens.Count;
    public int SoDangHoatDong => GiaoViens.Count(g => g.DangHoatDong);
    public int SoDaKhoa => GiaoViens.Count(g => !g.DangHoatDong);
    public int SoChuaCoHoatDong => GiaoViens.Count(g => g.ChuaCoHoatDong);
    public int SoChuaDangNhap => GiaoViens.Count(g => g.LanDangNhapCuoi == null);

    public int TongCauHoi => GiaoViens.Sum(g => g.SoCauHoi);
    public int TongDeThi => GiaoViens.Sum(g => g.SoDeThi);
    public int TongDeThiDaPhatHanh => GiaoViens.Sum(g => g.SoDeThiDaPhatHanh);
    public int TongLichThi => GiaoViens.Sum(g => g.SoLichThi);
    public int TongLopHocPhan => GiaoViens.Sum(g => g.SoLopHocPhan);
    public int TongLuotDuThi => GiaoViens.Sum(g => g.SoLuotDuThi);
    public int TongBaiDaNop => GiaoViens.Sum(g => g.SoBaiDaNop);
    public int TongBaiChuaCham => GiaoViens.Sum(g => g.SoBaiChuaCham);

    // Trung bình số câu hỏi / đề thi trên mỗi giảng viên
    public double TrungBinhCauHoi => TongGiaoVien == 0 ? 0 : Math.Round((double)TongCauHoi / TongGiaoVien, 1);
    public double TrungBinhDeThi => TongGiaoVien == 0 ? 0 : Math.Round((double)TongDeThi / TongGiaoVien, 1);

    // Nhóm theo khoa / bộ môn
    public List<ThongKeNhomVM> TheoKhoa { get; set; } = new();
    public List<ThongKeNhomVM> TheoBoMon { get; set; } = new();
}

// Một dòng thống kê của một giảng viên
public class ThongKeGiaoVienVM
{
    public int Id { get; set; }
    public string MaGiaoVien { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string? ChucDanh { get; set; }
    public string Email { get; set; } = string.Empty;
    public string SoDienThoai { get; set; } = string.Empty;
    public int BoMonId { get; set; }
    public string TenBoMon { get; set; } = string.Empty;
    public string TenKhoa { get; set; } = string.Empty;
    public bool DangHoatDong { get; set; }
    public DateTime? LanDangNhapCuoi { get; set; }   // UTC
    public DateTime NgayTaoTaiKhoan { get; set; }    // UTC

    // Ngân hàng câu hỏi
    public int SoCauHoi { get; set; }
    public int SoCauHoiTracNghiem { get; set; }
    public int SoCauHoiTuLuan { get; set; }
    public int SoCauHoiDe { get; set; }
    public int SoCauHoiTrungBinh { get; set; }
    public int SoCauHoiKho { get; set; }
    public int SoCauHoiRatKho { get; set; }
    public int SoDanhMucCauHoi { get; set; }         // số danh mục đã có câu hỏi của giảng viên

    // Đề thi
    public int SoDeThi { get; set; }
    public int SoDeThiDaPhatHanh { get; set; }
    public int SoMonHocSoanDe { get; set; }          // số môn học đã soạn đề
    public int SoDeThiChuaPhatHanh => SoDeThi - SoDeThiDaPhatHanh;

    // Lịch thi phụ trách
    public int SoLichThi { get; set; }
    public int SoLichThiSapDienRa { get; set; }
    public int SoLichThiDangDienRa { get; set; }
    public int SoLichThiDaKetThuc { get; set; }

    // Lớp học phần được phân công
    public int SoLopHocPhan { get; set; }
    public int SoLopHocPhanPhuTrach { get; set; }
    public int SoSinhVienLopHocPhan { get; set; }    // tổng lượt sinh viên trong các lớp học phần
    public int SoLopHocPhanTroGiang => SoLopHocPhan - SoLopHocPhanPhuTrach;

    // Kết quả thi của các lịch thi do giảng viên phụ trách
    public int SoLuotDuThi { get; set; }
    public int SoBaiDaNop { get; set; }
    public int SoBaiCoDiem { get; set; }
    public int SoBaiChuaCham { get; set; }
    public int SoBaiDat { get; set; }
    public double? DiemTrungBinhHe10 { get; set; }

    public int TiLeNop => SoLuotDuThi == 0 ? 0 : (int)Math.Round(SoBaiDaNop * 100.0 / SoLuotDuThi);
    public int TiLeDat => SoBaiCoDiem == 0 ? 0 : (int)Math.Round(SoBaiDat * 100.0 / SoBaiCoDiem);

    // Chưa phát sinh hoạt động chuyên môn nào
    public bool ChuaCoHoatDong => SoCauHoi == 0 && SoDeThi == 0 && SoLichThi == 0 && SoLopHocPhan == 0;

    // Mức độ hoạt động, tính thô theo khối lượng đã tạo (để xếp nhóm và tô màu)
    public string MucDoHoatDong => ChuaCoHoatDong ? "Chưa hoạt động"
        : SoCauHoi >= 50 || SoDeThi >= 5 || SoLichThi >= 5 ? "Rất tích cực"
        : SoCauHoi >= 20 || SoDeThi >= 2 || SoLichThi >= 2 ? "Tích cực"
        : "Ít hoạt động";
}

// Một nhóm (khoa hoặc bộ môn) trong thống kê
public class ThongKeNhomVM
{
    public string Ten { get; set; } = string.Empty;
    public string TenCha { get; set; } = string.Empty;   // bộ môn thì đây là tên khoa
    public int SoGiaoVien { get; set; }
    public int SoCauHoi { get; set; }
    public int SoDeThi { get; set; }
    public int SoLichThi { get; set; }
    public int SoLopHocPhan { get; set; }
}

// Trang chi tiết thống kê một giảng viên (/Admin/ThongKe/GiaoVienChiTiet/5)
public class ThongKeGiaoVienChiTietVM
{
    public ThongKeGiaoVienVM GiaoVien { get; set; } = new();
    public List<DeThiCuaGiaoVienVM> DeThis { get; set; } = new();
    public List<LichThiCuaGiaoVienVM> LichThis { get; set; } = new();
    public List<LopHocPhanCuaGiaoVienVM> LopHocPhans { get; set; } = new();
    public List<DanhMucCauHoiCuaGiaoVienVM> DanhMucs { get; set; } = new();
}

public class DeThiCuaGiaoVienVM
{
    public int Id { get; set; }
    public string MaDeThi { get; set; } = string.Empty;
    public string TenDeThi { get; set; } = string.Empty;
    public string TenMonHoc { get; set; } = string.Empty;
    public int LoaiDeThi { get; set; }
    public int ThoiLuong { get; set; }
    public double TongDiem { get; set; }
    public int SoCau { get; set; }
    public bool DaPhatHanh { get; set; }
    public DateTime? NgayPhatHanh { get; set; }  // UTC
    public int SoLichThi { get; set; }
}

public class LichThiCuaGiaoVienVM
{
    public int Id { get; set; }
    public string TenLichThi { get; set; } = string.Empty;
    public string MaDeThi { get; set; } = string.Empty;
    public string TenMonHoc { get; set; } = string.Empty;
    public DateTime ThoiGianBatDau { get; set; }   // UTC
    public DateTime ThoiGianKetThuc { get; set; }  // UTC
    public int SoSinhVien { get; set; }
    public int SoDaNop { get; set; }
    public int SoChuaCham { get; set; }
    public double? DiemTrungBinhHe10 { get; set; }

    public string TrangThai { get; set; } = string.Empty;
    public int TiLeNop => SoSinhVien == 0 ? 0 : (int)Math.Round(SoDaNop * 100.0 / SoSinhVien);
}

public class LopHocPhanCuaGiaoVienVM
{
    public int Id { get; set; }
    public string MaLopHocPhan { get; set; } = string.Empty;
    public string TenLopHocPhan { get; set; } = string.Empty;
    public string TenHocPhan { get; set; } = string.Empty;
    public string TenNamHoc { get; set; } = string.Empty;
    public int HocKy { get; set; }
    public int VaiTro { get; set; }                 // 1 = Phụ trách, 2 = Trợ giảng
    public int SiSoToiDa { get; set; }
    public int SoSinhVien { get; set; }
}

public class DanhMucCauHoiCuaGiaoVienVM
{
    public int Id { get; set; }
    public string TenDanhMuc { get; set; } = string.Empty;
    public string TenMonHoc { get; set; } = string.Empty;
    public int SoCauHoi { get; set; }
    public int SoCauHoiTracNghiem { get; set; }
    public int SoCauHoiTuLuan { get; set; }
}

// Màu hiển thị cho mức độ hoạt động của giảng viên
public static class ThongKeGiaoVienLabel
{
    public static string MauMucDo(string mucDo) => mucDo switch
    {
        "Rất tích cực" => "success",
        "Tích cực" => "primary",
        "Ít hoạt động" => "warning",
        _ => "secondary"
    };

    // Các mức độ theo thứ tự hiển thị
    public static readonly string[] CacMucDo = { "Rất tích cực", "Tích cực", "Ít hoạt động", "Chưa hoạt động" };
}
