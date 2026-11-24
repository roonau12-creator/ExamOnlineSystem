namespace ExamOnlineSystem.Models.ViewModels;

// ===== Dashboard thống kê (/Admin/ThongKe) =====
// Gộp 12 nhóm số liệu của toàn hệ thống vào một trang duy nhất:
//   1. Tài khoản      2. Giảng viên     3. Sinh viên      4. Khoa
//   5. Bộ môn         6. Môn học        7. Đề thi         8. Lịch thi
//   9. Lượt dự thi   10. Kết quả (đã chấm / chưa chấm)
//  11. Biểu đồ theo tháng – học kỳ – năm học              12. Hoạt động gần đây
//
// Quy ước chung:
//  - Mọi mốc thời gian lưu UTC (cột timestamptz của Npgsql), hiển thị bằng ToLocalTime().
//  - Điểm luôn quy về thang 10 (QuyDoiDiem.He10) vì mỗi đề thi có thang điểm riêng.
//  - Một lượt dự thi được tính vào mốc thời gian của lịch thi (ThoiGianBatDau), không phải
//    thời điểm nộp bài, để lịch thi và kết quả của nó luôn nằm cùng một cột trên biểu đồ.
public class ThongKeDashboardVM
{
    // Thời điểm lấy số liệu (UTC)
    public DateTime CapNhatLuc { get; set; } = DateTime.UtcNow;

    // ----- 1. Tài khoản (bảng AspNetUsers) -----
    public int TongTaiKhoan { get; set; }
    public int SoTaiKhoanHoatDong { get; set; }
    public int SoTaiKhoanQuanTri { get; set; }
    public int SoTaiKhoanGiangVien { get; set; }
    public int SoTaiKhoanSinhVien { get; set; }
    public int SoTaiKhoanCoVaiTro { get; set; }
    public int SoTaiKhoanMoi30Ngay { get; set; }
    public int SoDangNhap7Ngay { get; set; }

    public int SoTaiKhoanDaKhoa => TongTaiKhoan - SoTaiKhoanHoatDong;
    public int SoTaiKhoanChuaPhanQuyen => TongTaiKhoan - SoTaiKhoanCoVaiTro;

    // ----- 2. Giảng viên -----
    public int TongGiaoVien { get; set; }
    public int SoGiaoVienDaKhoa { get; set; }
    // Giảng viên đã soạn ít nhất một câu hỏi hoặc một đề thi
    public int SoGiaoVienCoHoatDong { get; set; }

    public int SoGiaoVienDangHoatDong => TongGiaoVien - SoGiaoVienDaKhoa;

    // ----- 3. Sinh viên -----
    public int TongSinhVien { get; set; }
    public int SoSinhVienNam { get; set; }
    public int SoSinhVienDaThi { get; set; }
    public int TongLop { get; set; }

    public int SoSinhVienNu => TongSinhVien - SoSinhVienNam;

    // ----- 4. Khoa & 5. Bộ môn -----
    public int TongKhoa { get; set; }
    public int TongBoMon { get; set; }

    // ----- 6. Môn học & ngân hàng câu hỏi -----
    public int TongMonHoc { get; set; }
    public int SoMonHocHoatDong { get; set; }
    public int TongHocPhan { get; set; }
    public int TongLopHocPhan { get; set; }
    public int TongDanhMucCauHoi { get; set; }
    public int TongCauHoi { get; set; }
    public int SoCauHoiTracNghiem { get; set; }
    public int SoCauHoiTuLuan { get; set; }

    // ----- 7. Đề thi -----
    public int TongDeThi { get; set; }
    public int SoDeThiDaPhatHanh { get; set; }

    public int SoDeThiChuaPhatHanh => TongDeThi - SoDeThiDaPhatHanh;

    // ----- 8. Lịch thi -----
    public int TongLichThi { get; set; }
    public int SoLichThiDangDienRa { get; set; }
    public int SoLichThiSapToi { get; set; }
    public int SoLichThiDaKetThuc { get; set; }

    // ----- 9. Lượt dự thi (SinhVienDuThi) -----
    public int TongLuotDuThi { get; set; }
    public int SoBaiDaNop { get; set; }
    public int SoDangLamBai { get; set; }
    public int SoChuaThi { get; set; }

    // ----- 10. Kết quả -----
    public int SoBaiDaCham { get; set; }
    public int SoBaiChuaCham { get; set; }
    public int SoBaiDat { get; set; }
    public double? DiemTrungBinhHe10 { get; set; }
    public double? DiemCaoNhatHe10 { get; set; }
    public double? DiemThapNhatHe10 { get; set; }
    public List<PhanBoDiemKhungVM> PhanBo { get; set; } = new();

    // ----- 11. Biểu đồ theo thời gian -----
    // 12 tháng gần nhất
    public List<MocThongKeVM> TheoThang { get; set; } = new();
    // Các học kỳ đã có hoạt động (tối đa 8 kỳ gần nhất)
    public List<MocThongKeVM> TheoHocKy { get; set; } = new();
    // Các năm học đã có hoạt động (tối đa 6 năm gần nhất)
    public List<MocThongKeVM> TheoNamHoc { get; set; } = new();

    // ----- 12. Hoạt động gần đây -----
    public List<HoatDongGanDayVM> HoatDongGanDay { get; set; } = new();

    // ----- Tỉ lệ / nhãn dẫn xuất -----
    public int TiLeNop => TongLuotDuThi == 0 ? 0 : (int)Math.Round(SoBaiDaNop * 100.0 / TongLuotDuThi);
    public int TiLeDat => SoBaiDaCham == 0 ? 0 : (int)Math.Round(SoBaiDat * 100.0 / SoBaiDaCham);
    public int TiLeDaCham => SoBaiDaNop == 0
        ? 0
        : Math.Min(100, (int)Math.Round(SoBaiDaCham * 100.0 / SoBaiDaNop));

    public string XepLoaiChung => KetQuaLabel.XepLoai(DiemTrungBinhHe10);
}

// Một mốc trên biểu đồ theo thời gian (một tháng, một học kỳ hoặc một năm học).
// Nhan là nhãn trục X, NhanPhu là dòng phụ bên dưới (năm), NhanDayDu dùng cho tooltip.
public class MocThongKeVM
{
    public string Nhan { get; set; } = string.Empty;
    public string NhanPhu { get; set; } = string.Empty;
    public string NhanDayDu { get; set; } = string.Empty;

    // Mốc đang diễn ra (tháng này / học kỳ này / năm học hiện hành)
    public bool LaHienTai { get; set; }

    public int SoLichThi { get; set; }
    public int SoLopHocPhan { get; set; }
    public int SoLuotDuThi { get; set; }
    public int SoBaiDaNop { get; set; }
    public int SoBaiDaCham { get; set; }
    public int SoBaiDat { get; set; }
    public double? DiemTrungBinhHe10 { get; set; }

    public int SoChuaNop => SoLuotDuThi - SoBaiDaNop;
    public int TiLeNop => SoLuotDuThi == 0 ? 0 : (int)Math.Round(SoBaiDaNop * 100.0 / SoLuotDuThi);
    public int TiLeDat => SoBaiDaCham == 0 ? 0 : (int)Math.Round(SoBaiDat * 100.0 / SoBaiDaCham);

    public bool CoDuLieu => SoLichThi > 0 || SoLuotDuThi > 0 || SoLopHocPhan > 0;
}

// Một dòng trong danh sách hoạt động gần đây của hệ thống
public class HoatDongGanDayVM
{
    // Thời điểm diễn ra (UTC)
    public DateTime ThoiDiem { get; set; }

    // Nhóm hoạt động: "Tài khoản", "Đề thi", "Lịch thi", "Lớp học phần", "Bài thi"
    public string Loai { get; set; } = string.Empty;

    // Tên icon bootstrap-icons (không gồm tiền tố "bi-") và màu ngữ nghĩa của Bootstrap
    public string Icon { get; set; } = "dot";
    public string Mau { get; set; } = "secondary";

    public string TieuDe { get; set; } = string.Empty;
    public string MoTa { get; set; } = string.Empty;

    // Liên kết tới bản ghi gốc trong Area Admin (bỏ trống nếu không có trang chi tiết)
    public string? Controller { get; set; }
    public string? Action { get; set; }
    public int? Id { get; set; }

    // Chỉ quản trị viên mới mở được liên kết này (vd trang quản lý tài khoản)
    public bool ChiQuanTri { get; set; }
}
