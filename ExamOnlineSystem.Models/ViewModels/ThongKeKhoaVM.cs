namespace ExamOnlineSystem.Models.ViewModels;

// ===== Thống kê khoa =====
// Gom toàn bộ số liệu của một khoa theo 3 nhóm, mỗi nhóm có cách quy thuộc riêng:
//  - Tổ chức & nhân sự: bộ môn, giảng viên (qua bộ môn), lớp, sinh viên thuộc khoa
//  - Chương trình & ngân hàng đề: môn học của khoa, học phần, danh mục câu hỏi, câu hỏi, đề thi
//    (đề thi / câu hỏi quy về khoa theo môn học, không theo người soạn)
//  - Kết quả thi: kết quả của sinh viên thuộc khoa; lịch thi tính theo giảng viên của khoa phụ trách
// Điểm luôn quy về thang 10 (QuyDoiDiem.He10) vì mỗi đề có thang điểm riêng.

// Trang thống kê khoa (/Admin/ThongKe/Khoa)
public class ThongKeKhoaTongQuanVM
{
    public List<ThongKeKhoaVM> Khoas { get; set; } = new();

    public int TongKhoa => Khoas.Count;
    public int TongBoMon => Khoas.Sum(k => k.SoBoMon);
    public int TongGiaoVien => Khoas.Sum(k => k.SoGiaoVien);
    public int TongLop => Khoas.Sum(k => k.SoLop);
    public int TongSinhVien => Khoas.Sum(k => k.SoSinhVien);
    public int TongMonHoc => Khoas.Sum(k => k.SoMonHoc);
    public int TongHocPhan => Khoas.Sum(k => k.SoHocPhan);
    public int TongCauHoi => Khoas.Sum(k => k.SoCauHoi);
    public int TongDeThi => Khoas.Sum(k => k.SoDeThi);
    public int TongLichThi => Khoas.Sum(k => k.SoLichThi);
    public int TongLuotDuThi => Khoas.Sum(k => k.SoLuotDuThi);
    public int TongBaiDaNop => Khoas.Sum(k => k.SoBaiDaNop);
    public int TongBaiCoDiem => Khoas.Sum(k => k.SoBaiCoDiem);
    public int TongBaiChuaCham => Khoas.Sum(k => k.SoBaiChuaCham);
    public int TongBaiDat => Khoas.Sum(k => k.SoBaiDat);

    // Khoa chưa có hoạt động chuyên môn nào (chưa có câu hỏi, đề thi, lịch thi)
    public int SoKhoaChuaCoHoatDong => Khoas.Count(k => k.ChuaCoHoatDong);
    public int SoKhoaChuaCoSinhVien => Khoas.Count(k => k.SoSinhVien == 0);

    public int TiLeDat => TongBaiCoDiem == 0 ? 0 : (int)Math.Round(TongBaiDat * 100.0 / TongBaiCoDiem);

    // Điểm trung bình toàn trường: bình quân có trọng số theo số bài đã chấm của từng khoa
    public double? DiemTrungBinhHe10
    {
        get
        {
            var coDiem = Khoas.Where(k => k.SoBaiCoDiem > 0 && k.DiemTrungBinhHe10.HasValue).ToList();
            if (coDiem.Count == 0)
            {
                return null;
            }
            var tongBai = coDiem.Sum(k => k.SoBaiCoDiem);
            return Math.Round(coDiem.Sum(k => k.DiemTrungBinhHe10!.Value * k.SoBaiCoDiem) / tongBai, 2);
        }
    }
}

// Một dòng thống kê của một khoa
public class ThongKeKhoaVM
{
    public int Id { get; set; }
    public string MaKhoa { get; set; } = string.Empty;
    public string TenKhoa { get; set; } = string.Empty;
    public int SoThuTu { get; set; }

    // Tổ chức & nhân sự
    public int SoBoMon { get; set; }
    public int SoGiaoVien { get; set; }
    public int SoGiaoVienDaKhoa { get; set; }
    public int SoLop { get; set; }
    public int SoSinhVien { get; set; }
    public int SoSinhVienNam { get; set; }

    // Chương trình & ngân hàng đề (quy về khoa theo môn học)
    public int SoMonHoc { get; set; }
    public int SoMonHocHoatDong { get; set; }
    public int SoHocPhan { get; set; }
    public int SoLopHocPhan { get; set; }
    public int SoDanhMucCauHoi { get; set; }
    public int SoCauHoi { get; set; }
    public int SoCauHoiTracNghiem { get; set; }
    public int SoCauHoiTuLuan { get; set; }
    public int SoDeThi { get; set; }
    public int SoDeThiDaPhatHanh { get; set; }

    // Thi: lịch thi do giảng viên của khoa phụ trách
    public int SoLichThi { get; set; }
    public int SoLichThiDangDienRa { get; set; }

    // Kết quả thi của sinh viên thuộc khoa
    public int SoLuotDuThi { get; set; }
    public int SoSinhVienDaThi { get; set; }
    public int SoBaiDaNop { get; set; }
    public int SoBaiCoDiem { get; set; }
    public int SoBaiChuaCham { get; set; }
    public int SoBaiDat { get; set; }
    public double? DiemTrungBinhHe10 { get; set; }
    public double? DiemCaoNhatHe10 { get; set; }
    public double? DiemThapNhatHe10 { get; set; }

    public int SoSinhVienNu => SoSinhVien - SoSinhVienNam;
    public int SoDeThiChuaPhatHanh => SoDeThi - SoDeThiDaPhatHanh;
    public int TiLeNop => SoLuotDuThi == 0 ? 0 : (int)Math.Round(SoBaiDaNop * 100.0 / SoLuotDuThi);
    public int TiLeDat => SoBaiCoDiem == 0 ? 0 : (int)Math.Round(SoBaiDat * 100.0 / SoBaiCoDiem);
    public double SinhVienMoiLop => SoLop == 0 ? 0 : Math.Round((double)SoSinhVien / SoLop, 1);
    public double CauHoiMoiMonHoc => SoMonHoc == 0 ? 0 : Math.Round((double)SoCauHoi / SoMonHoc, 1);

    public bool ChuaCoHoatDong => SoCauHoi == 0 && SoDeThi == 0 && SoLichThi == 0;
    public string XepLoai => KetQuaLabel.XepLoai(DiemTrungBinhHe10);
}

// Trang chi tiết thống kê một khoa (/Admin/ThongKe/KhoaChiTiet/5)
public class ThongKeKhoaChiTietVM
{
    public ThongKeKhoaVM Khoa { get; set; } = new();
    public List<BoMonCuaKhoaVM> BoMons { get; set; } = new();
    public List<LopCuaKhoaVM> Lops { get; set; } = new();
    public List<MonHocCuaKhoaVM> MonHocs { get; set; } = new();

    // Phân bố xếp loại theo điểm trung bình của sinh viên trong khoa
    public List<PhanBoDiemKhungVM> PhanBo { get; set; } = new();
}

public class BoMonCuaKhoaVM
{
    public int Id { get; set; }
    public string MaBoMon { get; set; } = string.Empty;
    public string TenBoMon { get; set; } = string.Empty;
    public int SoGiaoVien { get; set; }
    public int SoMonHoc { get; set; }
    public int SoCauHoi { get; set; }
    public int SoDeThi { get; set; }
}

public class LopCuaKhoaVM
{
    public int Id { get; set; }
    public string MaLop { get; set; } = string.Empty;
    public string TenLop { get; set; } = string.Empty;
    public int SoSinhVien { get; set; }
    public int SoSinhVienDaThi { get; set; }
    public int SoLuotDuThi { get; set; }
    public int SoBaiDaNop { get; set; }
    public int SoBaiCoDiem { get; set; }
    public int SoBaiDat { get; set; }
    public double? DiemTrungBinhHe10 { get; set; }

    public int TiLeDat => SoBaiCoDiem == 0 ? 0 : (int)Math.Round(SoBaiDat * 100.0 / SoBaiCoDiem);
}

public class MonHocCuaKhoaVM
{
    public int Id { get; set; }
    public string MaMonHoc { get; set; } = string.Empty;
    public string TenMonHoc { get; set; } = string.Empty;
    public string TenBoMon { get; set; } = string.Empty;
    public string TrangThai { get; set; } = string.Empty;
    public int SoHocPhan { get; set; }
    public int SoDanhMucCauHoi { get; set; }
    public int SoCauHoi { get; set; }
    public int SoDeThi { get; set; }
    public int SoDeThiDaPhatHanh { get; set; }
}
