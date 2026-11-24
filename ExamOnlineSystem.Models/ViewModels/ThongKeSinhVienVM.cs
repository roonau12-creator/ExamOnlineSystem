namespace ExamOnlineSystem.Models.ViewModels;

// ===== Thống kê sinh viên =====
// Gom kết quả học tập của từng sinh viên: số lịch thi được giao, số bài đã nộp, điểm trung bình,
// xếp loại và lớp học phần đã đăng ký. Điểm luôn quy về thang 10 (QuyDoiDiem.He10)
// vì mỗi đề thi có thang điểm riêng.

// Trang thống kê sinh viên (/Admin/ThongKe/SinhVien)
public class ThongKeSinhVienTongQuanVM
{
    public List<ThongKeSinhVienVM> SinhViens { get; set; } = new();

    public int TongSinhVien => SinhViens.Count;
    public int SoNam => SinhViens.Count(s => s.GioiTinh);
    public int SoNu => TongSinhVien - SoNam;
    public int SoDangHoatDong => SinhViens.Count(s => s.DangHoatDong);
    public int SoDaKhoa => SinhViens.Count(s => !s.DangHoatDong);
    public int SoChuaDangNhap => SinhViens.Count(s => s.LanDangNhapCuoi == null);

    // Đã từng dự thi (có ít nhất một bài đã nộp) và chưa thi lần nào
    public int SoDaThi => SinhViens.Count(s => s.SoBaiDaNop > 0);
    public int SoChuaThi => SinhViens.Count(s => s.SoLichThi > 0 && s.SoBaiDaNop == 0);
    public int SoChuaCoLichThi => SinhViens.Count(s => s.SoLichThi == 0);

    public int TongLuotDuThi => SinhViens.Sum(s => s.SoLichThi);
    public int TongBaiDaNop => SinhViens.Sum(s => s.SoBaiDaNop);
    public int TongBaiCoDiem => SinhViens.Sum(s => s.SoBaiCoDiem);
    public int TongBaiChuaCham => SinhViens.Sum(s => s.SoBaiChuaCham);
    public int TongBaiDat => SinhViens.Sum(s => s.SoBaiDat);
    public int TongLopHocPhan => SinhViens.Sum(s => s.SoLopHocPhan);

    public int TiLeNop => TongLuotDuThi == 0 ? 0 : (int)Math.Round(TongBaiDaNop * 100.0 / TongLuotDuThi);
    public int TiLeDat => TongBaiCoDiem == 0 ? 0 : (int)Math.Round(TongBaiDat * 100.0 / TongBaiCoDiem);

    // Điểm trung bình toàn hệ thống: bình quân có trọng số theo số bài đã chấm của từng sinh viên
    public double? DiemTrungBinhHe10
    {
        get
        {
            var coDiem = SinhViens.Where(s => s.SoBaiCoDiem > 0 && s.DiemTrungBinhHe10.HasValue).ToList();
            if (coDiem.Count == 0)
            {
                return null;
            }
            var tongBai = coDiem.Sum(s => s.SoBaiCoDiem);
            return Math.Round(coDiem.Sum(s => s.DiemTrungBinhHe10!.Value * s.SoBaiCoDiem) / tongBai, 2);
        }
    }

    // Phân bố xếp loại theo điểm trung bình của từng sinh viên (chỉ sinh viên đã có điểm)
    public List<PhanBoDiemKhungVM> PhanBoXepLoai { get; set; } = new();

    // Nhóm theo khoa / lớp
    public List<ThongKeNhomSinhVienVM> TheoKhoa { get; set; } = new();
    public List<ThongKeNhomSinhVienVM> TheoLop { get; set; } = new();
}

// Một dòng thống kê của một sinh viên
public class ThongKeSinhVienVM
{
    public int Id { get; set; }
    public string MaSinhVien { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public DateOnly NgaySinh { get; set; }
    public bool GioiTinh { get; set; }                // true = Nam, false = Nữ
    public int LopId { get; set; }
    public string MaLop { get; set; } = string.Empty;
    public string TenLop { get; set; } = string.Empty;
    public string TenKhoa { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SoDienThoai { get; set; } = string.Empty;

    public bool DangHoatDong { get; set; }
    public DateTime? LanDangNhapCuoi { get; set; }    // UTC
    public DateTime NgayTaoTaiKhoan { get; set; }     // UTC

    // Dự thi
    public int SoLichThi { get; set; }                // số lịch thi được giao
    public int SoChuaThi { get; set; }
    public int SoDangLamBai { get; set; }
    public int SoBaiDaNop { get; set; }
    public int SoLanThi { get; set; }                 // tổng số lượt vào làm bài
    public int SoBaiCoDiem { get; set; }
    public int SoBaiChuaCham { get; set; }
    public int SoBaiDat { get; set; }

    public double? DiemTrungBinhHe10 { get; set; }
    public double? DiemCaoNhatHe10 { get; set; }
    public double? DiemThapNhatHe10 { get; set; }

    // Học tập
    public int SoLopHocPhan { get; set; }

    public int TiLeNop => SoLichThi == 0 ? 0 : (int)Math.Round(SoBaiDaNop * 100.0 / SoLichThi);
    public int TiLeDat => SoBaiCoDiem == 0 ? 0 : (int)Math.Round(SoBaiDat * 100.0 / SoBaiCoDiem);

    // Xếp loại theo điểm trung bình (thang 10)
    public string XepLoai => KetQuaLabel.XepLoai(DiemTrungBinhHe10);
    public bool Dat => DiemTrungBinhHe10 >= 5;
    public bool ChuaDuThi => SoBaiDaNop == 0;

    public int Tuoi
    {
        get
        {
            var homNay = DateOnly.FromDateTime(DateTime.Today);
            var tuoi = homNay.Year - NgaySinh.Year;
            return NgaySinh > homNay.AddYears(-tuoi) ? tuoi - 1 : tuoi;
        }
    }
}

// Một nhóm (khoa hoặc lớp) trong thống kê sinh viên
public class ThongKeNhomSinhVienVM
{
    public string Ten { get; set; } = string.Empty;
    public string TenCha { get; set; } = string.Empty;   // lớp thì đây là tên khoa
    public int SoSinhVien { get; set; }
    public int SoNam { get; set; }
    public int SoNu { get; set; }
    public int SoDaThi { get; set; }
    public int SoLuotDuThi { get; set; }
    public int SoBaiDaNop { get; set; }
    public int SoBaiCoDiem { get; set; }
    public int SoBaiDat { get; set; }
    public double? DiemTrungBinhHe10 { get; set; }

    public int TiLeDat => SoBaiCoDiem == 0 ? 0 : (int)Math.Round(SoBaiDat * 100.0 / SoBaiCoDiem);
}

// Trang chi tiết thống kê một sinh viên (/Admin/ThongKe/SinhVienChiTiet/5)
public class ThongKeSinhVienChiTietVM
{
    public ThongKeSinhVienVM SinhVien { get; set; } = new();
    public List<KetQuaLichThiCuaSinhVienVM> LichThis { get; set; } = new();
    public List<LopHocPhanCuaSinhVienVM> LopHocPhans { get; set; } = new();

    // Phân bố điểm các bài đã chấm của sinh viên (thang 10)
    public List<PhanBoDiemKhungVM> PhanBo { get; set; } = new();
}

public class KetQuaLichThiCuaSinhVienVM
{
    public int Id { get; set; }                      // SinhVienDuThiId
    public int LichThiId { get; set; }
    public string TenLichThi { get; set; } = string.Empty;
    public string MaDeThi { get; set; } = string.Empty;
    public string TenMonHoc { get; set; } = string.Empty;
    public string TenGiaoVien { get; set; } = string.Empty;
    public DateTime ThoiGianBatDau { get; set; }     // UTC - khung giờ của lịch thi
    public DateTime ThoiGianKetThuc { get; set; }    // UTC
    public double TongDiem { get; set; }             // thang điểm của đề

    public int TrangThai { get; set; }               // 0 chưa thi, 1 đang làm, 2 đã nộp
    public DateTime? BatDauLamBai { get; set; }      // UTC
    public DateTime? NopBai { get; set; }            // UTC
    public int SoLanThi { get; set; }
    public int? LanThiId { get; set; }               // lần thi gần nhất, để xem bài làm
    public double? Diem { get; set; }

    public string TrangThaiLich { get; set; } = string.Empty;  // Sắp / Đang / Đã kết thúc
    public double? DiemHe10 => QuyDoiDiem.He10(Diem, TongDiem);
    public string XepLoai => KetQuaLabel.XepLoai(DiemHe10);
    public int? SoPhutLamBai => BatDauLamBai.HasValue && NopBai.HasValue
        ? (int)Math.Round((NopBai.Value - BatDauLamBai.Value).TotalMinutes)
        : null;
}

public class LopHocPhanCuaSinhVienVM
{
    public int Id { get; set; }
    public string MaLopHocPhan { get; set; } = string.Empty;
    public string TenLopHocPhan { get; set; } = string.Empty;
    public string TenMonHoc { get; set; } = string.Empty;
    public string TenNamHoc { get; set; } = string.Empty;
    public int HocKy { get; set; }
    public string GiaoVienPhuTrach { get; set; } = string.Empty;
    public DateTime NgayDangKy { get; set; }         // UTC
}
