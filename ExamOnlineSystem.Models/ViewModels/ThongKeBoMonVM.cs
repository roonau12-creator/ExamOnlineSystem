namespace ExamOnlineSystem.Models.ViewModels;

// ===== Thống kê bộ môn =====
// Cách quy thuộc số liệu (giống thống kê khoa, hẹp xuống một bộ môn):
//  - Nhân sự: giảng viên thuộc bộ môn (GiaoVien.BoMonId)
//  - Chương trình & ngân hàng đề: môn học của bộ môn (MonHoc.BoMonId); học phần, danh mục,
//    câu hỏi, đề thi quy theo môn học, không theo người soạn
//  - Thi & kết quả: lịch thi do giảng viên của bộ môn phụ trách
// Điểm luôn quy về thang 10 (QuyDoiDiem.He10) vì mỗi đề có thang điểm riêng.

// Trang thống kê bộ môn (/Admin/ThongKe/BoMon)
public class ThongKeBoMonTongQuanVM
{
    public List<ThongKeBoMonVM> BoMons { get; set; } = new();

    public int TongBoMon => BoMons.Count;
    public int SoKhoa => BoMons.Select(b => b.KhoaId).Distinct().Count();
    public int TongGiaoVien => BoMons.Sum(b => b.SoGiaoVien);
    public int TongMonHoc => BoMons.Sum(b => b.SoMonHoc);
    public int TongHocPhan => BoMons.Sum(b => b.SoHocPhan);
    public int TongCauHoi => BoMons.Sum(b => b.SoCauHoi);
    public int TongDeThi => BoMons.Sum(b => b.SoDeThi);
    public int TongDeThiDaPhatHanh => BoMons.Sum(b => b.SoDeThiDaPhatHanh);
    public int TongLichThi => BoMons.Sum(b => b.SoLichThi);
    public int TongLuotDuThi => BoMons.Sum(b => b.SoLuotDuThi);
    public int TongBaiDaNop => BoMons.Sum(b => b.SoBaiDaNop);
    public int TongBaiCoDiem => BoMons.Sum(b => b.SoBaiCoDiem);
    public int TongBaiChuaCham => BoMons.Sum(b => b.SoBaiChuaCham);
    public int TongBaiDat => BoMons.Sum(b => b.SoBaiDat);

    public int SoBoMonChuaCoGiaoVien => BoMons.Count(b => b.SoGiaoVien == 0);
    public int SoBoMonChuaCoNganHangDe => BoMons.Count(b => b.ChuaCoNganHangDe);
    public int TiLeDat => TongBaiCoDiem == 0 ? 0 : (int)Math.Round(TongBaiDat * 100.0 / TongBaiCoDiem);

    // Điểm trung bình chung: bình quân có trọng số theo số bài đã chấm của từng bộ môn
    public double? DiemTrungBinhHe10
    {
        get
        {
            var coDiem = BoMons.Where(b => b.SoBaiCoDiem > 0 && b.DiemTrungBinhHe10.HasValue).ToList();
            if (coDiem.Count == 0)
            {
                return null;
            }
            var tongBai = coDiem.Sum(b => b.SoBaiCoDiem);
            return Math.Round(coDiem.Sum(b => b.DiemTrungBinhHe10!.Value * b.SoBaiCoDiem) / tongBai, 2);
        }
    }
}

// Một dòng thống kê của một bộ môn
public class ThongKeBoMonVM
{
    public int Id { get; set; }
    public string MaBoMon { get; set; } = string.Empty;
    public string TenBoMon { get; set; } = string.Empty;
    public int SoThuTu { get; set; }
    public int KhoaId { get; set; }
    public string MaKhoa { get; set; } = string.Empty;
    public string TenKhoa { get; set; } = string.Empty;

    // Nhân sự
    public int SoGiaoVien { get; set; }
    public int SoGiaoVienDaKhoa { get; set; }

    // Chương trình & ngân hàng đề (quy theo môn học của bộ môn)
    public int SoMonHoc { get; set; }
    public int SoMonHocHoatDong { get; set; }
    public int SoHocPhan { get; set; }
    public int SoLopHocPhan { get; set; }
    public int SoDanhMucCauHoi { get; set; }
    public int SoCauHoi { get; set; }
    public int SoCauHoiTracNghiem { get; set; }
    public int SoCauHoiTuLuan { get; set; }
    public int SoCauHoiDe { get; set; }
    public int SoCauHoiTrungBinh { get; set; }
    public int SoCauHoiKho { get; set; }
    public int SoCauHoiRatKho { get; set; }
    public int SoDeThi { get; set; }
    public int SoDeThiDaPhatHanh { get; set; }

    // Thi: lịch thi do giảng viên của bộ môn phụ trách
    public int SoLichThi { get; set; }
    public int SoLichThiDangDienRa { get; set; }

    // Kết quả các lịch thi đó
    public int SoLuotDuThi { get; set; }
    public int SoBaiDaNop { get; set; }
    public int SoBaiCoDiem { get; set; }
    public int SoBaiChuaCham { get; set; }
    public int SoBaiDat { get; set; }
    public double? DiemTrungBinhHe10 { get; set; }
    public double? DiemCaoNhatHe10 { get; set; }
    public double? DiemThapNhatHe10 { get; set; }

    public int SoDeThiChuaPhatHanh => SoDeThi - SoDeThiDaPhatHanh;
    public int TiLeNop => SoLuotDuThi == 0 ? 0 : (int)Math.Round(SoBaiDaNop * 100.0 / SoLuotDuThi);
    public int TiLeDat => SoBaiCoDiem == 0 ? 0 : (int)Math.Round(SoBaiDat * 100.0 / SoBaiCoDiem);
    public double CauHoiMoiGiaoVien => SoGiaoVien == 0 ? 0 : Math.Round((double)SoCauHoi / SoGiaoVien, 1);
    public double CauHoiMoiMonHoc => SoMonHoc == 0 ? 0 : Math.Round((double)SoCauHoi / SoMonHoc, 1);

    public bool ChuaCoNganHangDe => SoCauHoi == 0 && SoDeThi == 0;
    public string XepLoai => KetQuaLabel.XepLoai(DiemTrungBinhHe10);
}

// Trang chi tiết thống kê một bộ môn (/Admin/ThongKe/BoMonChiTiet/5)
public class ThongKeBoMonChiTietVM
{
    public ThongKeBoMonVM BoMon { get; set; } = new();
    public List<GiaoVienCuaBoMonVM> GiaoViens { get; set; } = new();

    // Dùng lại VM môn học của thống kê khoa (cùng các cột cần hiển thị)
    public List<MonHocCuaKhoaVM> MonHocs { get; set; } = new();

    // Phân bố điểm các bài thi thuộc lịch thi do giảng viên bộ môn phụ trách
    public List<PhanBoDiemKhungVM> PhanBo { get; set; } = new();
}

public class GiaoVienCuaBoMonVM
{
    public int Id { get; set; }
    public string MaGiaoVien { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string? ChucDanh { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool DangHoatDong { get; set; }
    public DateTime? LanDangNhapCuoi { get; set; }   // UTC
    public int SoCauHoi { get; set; }
    public int SoDeThi { get; set; }
    public int SoLichThi { get; set; }
    public int SoLopHocPhan { get; set; }
}
