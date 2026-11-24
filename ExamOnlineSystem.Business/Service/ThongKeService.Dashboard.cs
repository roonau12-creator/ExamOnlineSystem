using ExamOnlineSystem.Models.ViewModels;
using ExamOnlineSystem.Utility;
using Microsoft.EntityFrameworkCore;

namespace ExamOnlineSystem.Business.Service;

// Phần dashboard của ThongKeService (xem ThongKeService.cs cho các trang thống kê chi tiết).
// Tách file riêng vì đây là màn hình gộp số liệu của toàn hệ thống: tài khoản, nhân sự, danh mục,
// ngân hàng đề, lịch thi, kết quả chấm, biểu đồ theo thời gian và hoạt động gần đây.
public partial class ThongKeService
{
    // Số hoạt động lấy ở mỗi nhóm trước khi trộn và cắt còn SoHoatDongHienThi dòng mới nhất
    private const int SoHoatDongMoiNhom = 10;
    private const int SoHoatDongHienThi = 15;

    public async Task<ThongKeDashboardVM> GetDashboardAsync()
    {
        var bayGio = DateTime.UtcNow;
        var moc30Ngay = bayGio.AddDays(-30);
        var moc7Ngay = bayGio.AddDays(-7);

        var vm = new ThongKeDashboardVM { CapNhatLuc = bayGio };

        // ===== 1. Tài khoản =====
        vm.TongTaiKhoan = await _context.Users.CountAsync();
        vm.SoTaiKhoanHoatDong = await _context.Users.CountAsync(u => u.DangHoatDong);
        vm.SoTaiKhoanMoi30Ngay = await _context.Users.CountAsync(u => u.NgayTao >= moc30Ngay);
        vm.SoDangNhap7Ngay = await _context.Users
            .CountAsync(u => u.LanDangNhapCuoi != null && u.LanDangNhapCuoi >= moc7Ngay);
        // Một tài khoản có thể giữ nhiều vai trò nên phải đếm riêng số tài khoản đã phân quyền
        vm.SoTaiKhoanCoVaiTro = await _context.UserRoles.Select(x => x.UserId).Distinct().CountAsync();

        var soTheoVaiTro = await (from ur in _context.UserRoles
                join r in _context.Roles on ur.RoleId equals r.Id
                group ur by r.Name into nhom
                select new { VaiTro = nhom.Key, SoLuong = nhom.Count() })
            .AsNoTracking()
            .ToListAsync();
        vm.SoTaiKhoanQuanTri = soTheoVaiTro.FirstOrDefault(x => x.VaiTro == SD.Role_Admin)?.SoLuong ?? 0;
        vm.SoTaiKhoanGiangVien = soTheoVaiTro.FirstOrDefault(x => x.VaiTro == SD.Role_Teacher)?.SoLuong ?? 0;
        vm.SoTaiKhoanSinhVien = soTheoVaiTro.FirstOrDefault(x => x.VaiTro == SD.Role_Student)?.SoLuong ?? 0;

        // ===== 2. Giảng viên =====
        vm.TongGiaoVien = await _context.GiaoViens.CountAsync();
        vm.SoGiaoVienDaKhoa = await _context.GiaoViens
            .CountAsync(g => g.ApplicationUser != null && !g.ApplicationUser.DangHoatDong);
        vm.SoGiaoVienCoHoatDong = await _context.GiaoViens
            .CountAsync(g => _context.CauHois.Any(c => c.GiaoVienId == g.Id) ||
                             _context.DeThis.Any(d => d.GiaoVienId == g.Id));

        // ===== 3. Sinh viên =====
        vm.TongSinhVien = await _context.SinhViens.CountAsync();
        vm.SoSinhVienNam = await _context.SinhViens.CountAsync(s => s.GioiTinh);
        vm.TongLop = await _context.Lops.CountAsync();

        // ===== 4. Khoa & 5. Bộ môn =====
        vm.TongKhoa = await _context.Khoas.CountAsync();
        vm.TongBoMon = await _context.BoMons.CountAsync();

        // ===== 6. Môn học & ngân hàng câu hỏi =====
        vm.TongMonHoc = await _context.MonHocs.CountAsync();
        vm.SoMonHocHoatDong = await _context.MonHocs.CountAsync(m => m.TrangThai == "Hoạt động");
        vm.TongHocPhan = await _context.HocPhans.CountAsync();
        vm.TongLopHocPhan = await _context.LopHocPhans.CountAsync();
        vm.TongDanhMucCauHoi = await _context.DanhMucCauHois.CountAsync();
        vm.TongCauHoi = await _context.CauHois.CountAsync();
        vm.SoCauHoiTracNghiem = await _context.CauHois.CountAsync(c => c.LoaiCauHoi == 1);
        vm.SoCauHoiTuLuan = await _context.CauHois.CountAsync(c => c.LoaiCauHoi == 2);

        // ===== 7. Đề thi =====
        vm.TongDeThi = await _context.DeThis.CountAsync();
        vm.SoDeThiDaPhatHanh = await _context.DeThis.CountAsync(d => d.DaPhatHanh);

        // ===== 8. Lịch thi =====
        // Lấy khung giờ của mọi lịch thi một lượt: dùng cho cả thẻ số liệu và biểu đồ theo thời gian
        var lichThis = await _context.LichThis
            .Select(l => new { l.ThoiGianBatDau, l.ThoiGianKetThuc })
            .AsNoTracking()
            .ToListAsync();
        vm.TongLichThi = lichThis.Count;
        vm.SoLichThiDangDienRa = lichThis.Count(l => l.ThoiGianBatDau <= bayGio && l.ThoiGianKetThuc >= bayGio);
        vm.SoLichThiSapToi = lichThis.Count(l => l.ThoiGianBatDau > bayGio);
        vm.SoLichThiDaKetThuc = lichThis.Count(l => l.ThoiGianKetThuc < bayGio);

        // ===== 9. Lượt dự thi & 10. Kết quả =====
        // Điểm ưu tiên lấy ở SinhVienDuThi, nếu chưa có thì lấy lần thi gần nhất đã có điểm
        var duThis = await _context.SinhVienDuThis
            .Select(x => new DuThiDashboardRow
            {
                SinhVienId = x.SinhVienId,
                BatDauLichThi = x.LichThi!.ThoiGianBatDau,
                TrangThai = x.TrangThai,
                TongDiem = x.LichThi!.DeThi!.TongDiem,
                Diem = x.Diem ?? _context.LanThis
                    .Where(l => l.SinhVienDuThiId == x.Id && l.Diem != null)
                    .OrderByDescending(l => l.LanThu)
                    .Select(l => l.Diem)
                    .FirstOrDefault()
            })
            .AsNoTracking()
            .ToListAsync();

        vm.TongLuotDuThi = duThis.Count;
        vm.SoBaiDaNop = duThis.Count(x => x.TrangThai == 2);
        vm.SoDangLamBai = duThis.Count(x => x.TrangThai == 1);
        vm.SoChuaThi = duThis.Count(x => x.TrangThai == 0);
        vm.SoSinhVienDaThi = duThis.Where(x => x.TrangThai == 2).Select(x => x.SinhVienId).Distinct().Count();
        vm.SoBaiChuaCham = duThis.Count(x => x.TrangThai == 2 && !x.Diem.HasValue);

        var he10 = LayDiemHe10(duThis);
        vm.SoBaiDaCham = he10.Count;
        if (he10.Count > 0)
        {
            vm.SoBaiDat = he10.Count(d => d >= 5);
            vm.DiemTrungBinhHe10 = Math.Round(he10.Average(), 2);
            vm.DiemCaoNhatHe10 = he10.Max();
            vm.DiemThapNhatHe10 = he10.Min();
        }
        vm.PhanBo = TinhPhanBo(he10);

        // ===== 11. Biểu đồ theo tháng – học kỳ – năm học =====
        await TinhBieuDoAsync(vm, lichThis.Select(l => l.ThoiGianBatDau).ToList(), duThis);

        // ===== 12. Hoạt động gần đây =====
        vm.HoatDongGanDay = await LayHoatDongGanDayAsync();

        return vm;
    }

    // Dựng 3 chuỗi mốc thời gian cho biểu đồ. Một lịch thi (và các lượt dự thi của nó) được quy về
    // mốc chứa ThoiGianBatDau của lịch thi, tính theo giờ địa phương của máy chủ.
    private async Task TinhBieuDoAsync(ThongKeDashboardVM vm, List<DateTime> batDauLichThis,
        List<DuThiDashboardRow> duThis)
    {
        var homNay = DateOnly.FromDateTime(GioDiaPhuong(DateTime.UtcNow));

        var lichTheoThang = batDauLichThis.GroupBy(DauThang).ToDictionary(g => g.Key, g => g.Count());
        var duThiTheoThang = duThis.ToLookup(x => DauThang(x.BatDauLichThi));

        // --- Theo tháng: 12 tháng gần nhất, kể cả tháng chưa có hoạt động (để thấy khoảng trống) ---
        var thangHienTai = new DateTime(homNay.Year, homNay.Month, 1);
        for (var i = 11; i >= 0; i--)
        {
            var thang = thangHienTai.AddMonths(-i);
            var moc = new MocThongKeVM
            {
                Nhan = "T" + thang.Month,
                NhanPhu = thang.Year.ToString(),
                NhanDayDu = $"Tháng {thang.Month}/{thang.Year}",
                LaHienTai = thang == thangHienTai,
                SoLichThi = lichTheoThang.TryGetValue(thang, out var soLich) ? soLich : 0
            };
            GopKetQuaVaoMoc(moc, duThiTheoThang[thang]);
            vm.TheoThang.Add(moc);
        }

        // --- Theo học kỳ và năm học ---
        var namHocs = await _context.NamHocs
            .OrderBy(n => n.NgayBatDau)
            .Select(n => new { n.Id, n.MaNamHoc, n.TenNamHoc, n.NgayBatDau, n.NgayKetThuc, n.LaNamHienTai })
            .AsNoTracking()
            .ToListAsync();
        if (namHocs.Count == 0)
        {
            return;
        }

        // Lớp học phần có sẵn NamHocId + HocKy nên lấy trực tiếp, không phải suy từ ngày
        var lopHocPhanTheoKy = await _context.LopHocPhans
            .GroupBy(l => new { l.NamHocId, l.HocKy })
            .Select(g => new { g.Key.NamHocId, g.Key.HocKy, SoLuong = g.Count() })
            .AsNoTracking()
            .ToListAsync();

        // Năm học hiện tại: năm học chứa hôm nay, nếu không có thì lấy năm học được đánh dấu hiện hành
        var namHocHienTai = namHocs.FirstOrDefault(n => homNay >= n.NgayBatDau && homNay <= n.NgayKetThuc)
                            ?? namHocs.FirstOrDefault(n => n.LaNamHienTai);
        var hocKyHienTai = HocKyTheoThang(homNay.Month);

        var mocNamHoc = new List<MocThongKeVM>();
        var mocHocKy = new List<MocThongKeVM>();

        foreach (var nh in namHocs)
        {
            var lichTrongNam = batDauLichThis.Where(t => TrongNamHoc(t, nh.NgayBatDau, nh.NgayKetThuc)).ToList();
            var duThiTrongNam = duThis.Where(x => TrongNamHoc(x.BatDauLichThi, nh.NgayBatDau, nh.NgayKetThuc)).ToList();
            var laNamHienTai = namHocHienTai != null && nh.Id == namHocHienTai.Id;

            var mocNam = new MocThongKeVM
            {
                Nhan = nh.MaNamHoc,
                NhanDayDu = nh.TenNamHoc,
                LaHienTai = laNamHienTai,
                SoLichThi = lichTrongNam.Count,
                SoLopHocPhan = lopHocPhanTheoKy.Where(x => x.NamHocId == nh.Id).Sum(x => x.SoLuong)
            };
            GopKetQuaVaoMoc(mocNam, duThiTrongNam);
            mocNamHoc.Add(mocNam);

            foreach (var hocKy in LopHocPhanLabel.TatCaHocKy)
            {
                var mocKy = new MocThongKeVM
                {
                    Nhan = hocKy == 3 ? "HK hè" : "HK" + hocKy,
                    NhanPhu = nh.MaNamHoc,
                    NhanDayDu = LopHocPhanLabel.TenHocKy(hocKy) + " · " + nh.TenNamHoc,
                    LaHienTai = laNamHienTai && hocKy == hocKyHienTai,
                    SoLichThi = lichTrongNam.Count(t => HocKyTheoThang(GioDiaPhuong(t).Month) == hocKy),
                    SoLopHocPhan = lopHocPhanTheoKy
                        .Where(x => x.NamHocId == nh.Id && x.HocKy == hocKy)
                        .Sum(x => x.SoLuong)
                };
                GopKetQuaVaoMoc(mocKy,
                    duThiTrongNam.Where(x => HocKyTheoThang(GioDiaPhuong(x.BatDauLichThi).Month) == hocKy));
                mocHocKy.Add(mocKy);
            }
        }

        // Chỉ vẽ mốc đã có hoạt động (kèm mốc hiện tại) để biểu đồ không toàn cột rỗng
        vm.TheoNamHoc = mocNamHoc.Where(m => m.CoDuLieu || m.LaHienTai).TakeLast(6).ToList();
        vm.TheoHocKy = mocHocKy.Where(m => m.CoDuLieu || m.LaHienTai).TakeLast(8).ToList();
    }

    // Trộn 5 nhóm hoạt động có mốc thời gian trong hệ thống rồi lấy các dòng mới nhất
    private async Task<List<HoatDongGanDayVM>> LayHoatDongGanDayAsync()
    {
        var hoatDong = new List<HoatDongGanDayVM>();

        // Tài khoản mới tạo
        hoatDong.AddRange(await _context.Users
            .OrderByDescending(u => u.NgayTao)
            .Take(SoHoatDongMoiNhom)
            .Select(u => new HoatDongGanDayVM
            {
                ThoiDiem = u.NgayTao,
                Loai = "Tài khoản",
                Icon = "person-plus",
                Mau = "secondary",
                TieuDe = "Tạo tài khoản " + u.HoTen,
                MoTa = u.Email ?? string.Empty,
                Controller = "User",
                Action = "Index",
                ChiQuanTri = true
            })
            .AsNoTracking()
            .ToListAsync());

        // Đề thi được phát hành
        hoatDong.AddRange(await _context.DeThis
            .Where(d => d.DaPhatHanh && d.NgayPhatHanh != null)
            .OrderByDescending(d => d.NgayPhatHanh)
            .Take(SoHoatDongMoiNhom)
            .Select(d => new HoatDongGanDayVM
            {
                ThoiDiem = d.NgayPhatHanh!.Value,
                Loai = "Đề thi",
                Icon = "send-check",
                Mau = "primary",
                TieuDe = "Phát hành đề " + d.MaDeThi,
                MoTa = d.TenDeThi + " · " + d.MonHoc.TenMonHoc,
                Controller = "DeThi",
                Action = "Details",
                Id = d.Id
            })
            .AsNoTracking()
            .ToListAsync());

        // Lịch thi được tạo
        hoatDong.AddRange(await _context.LichThis
            .OrderByDescending(l => l.NgayTao)
            .Take(SoHoatDongMoiNhom)
            .Select(l => new HoatDongGanDayVM
            {
                ThoiDiem = l.NgayTao,
                Loai = "Lịch thi",
                Icon = "calendar-plus",
                Mau = "info",
                TieuDe = "Tạo lịch thi " + l.TenLichThi,
                MoTa = "Đề " + l.DeThi!.MaDeThi + " · GV " + l.GiaoVien!.HoTen,
                Controller = "LichThi",
                Action = "Details",
                Id = l.Id
            })
            .AsNoTracking()
            .ToListAsync());

        // Lớp học phần được mở (ghép nhãn học kỳ ở bộ nhớ vì LopHocPhanLabel không dịch được sang SQL)
        var lopHocPhans = await _context.LopHocPhans
            .OrderByDescending(l => l.NgayTao)
            .Take(SoHoatDongMoiNhom)
            .Select(l => new
            {
                l.Id,
                l.NgayTao,
                l.MaLopHocPhan,
                l.TenLopHocPhan,
                l.HocKy,
                MaNamHoc = l.NamHoc!.MaNamHoc
            })
            .AsNoTracking()
            .ToListAsync();
        hoatDong.AddRange(lopHocPhans.Select(l => new HoatDongGanDayVM
        {
            ThoiDiem = l.NgayTao,
            Loai = "Lớp học phần",
            Icon = "person-check",
            Mau = "success",
            TieuDe = "Mở lớp học phần " + l.MaLopHocPhan,
            MoTa = l.TenLopHocPhan + " · " + LopHocPhanLabel.TenHocKy(l.HocKy) + " năm học " + l.MaNamHoc,
            Controller = "LopHocPhan",
            Action = "Details",
            Id = l.Id
        }));

        // Sinh viên nộp bài
        var baiDaNop = await _context.SinhVienDuThis
            .Where(x => x.ThoiGianNopBai != null)
            .OrderByDescending(x => x.ThoiGianNopBai)
            .Take(SoHoatDongMoiNhom)
            .Select(x => new
            {
                x.LichThiId,
                ThoiGianNopBai = x.ThoiGianNopBai!.Value,
                HoTen = x.SinhVien!.HoTen,
                MaSinhVien = x.SinhVien!.MaSinhVien,
                TenLichThi = x.LichThi!.TenLichThi,
                TongDiem = x.LichThi!.DeThi!.TongDiem,
                Diem = x.Diem ?? _context.LanThis
                    .Where(l => l.SinhVienDuThiId == x.Id && l.Diem != null)
                    .OrderByDescending(l => l.LanThu)
                    .Select(l => l.Diem)
                    .FirstOrDefault()
            })
            .AsNoTracking()
            .ToListAsync();
        hoatDong.AddRange(baiDaNop.Select(x =>
        {
            var diemHe10 = QuyDoiDiem.He10(x.Diem, x.TongDiem);
            return new HoatDongGanDayVM
            {
                ThoiDiem = x.ThoiGianNopBai,
                Loai = "Bài thi",
                Icon = "upload",
                Mau = "warning",
                TieuDe = x.HoTen + " (" + x.MaSinhVien + ") nộp bài",
                MoTa = x.TenLichThi + " · " +
                       (diemHe10.HasValue ? diemHe10.Value.ToString("0.##") + "/10" : "chưa chấm"),
                Controller = "KetQuaThi",
                Action = "Details",
                Id = x.LichThiId
            };
        }));

        // Npgsql có thể trả về Kind = Unspecified: gắn lại UTC để view gọi ToLocalTime() đúng
        foreach (var h in hoatDong)
        {
            h.ThoiDiem = DateTime.SpecifyKind(h.ThoiDiem, DateTimeKind.Utc);
        }

        return hoatDong
            .OrderByDescending(h => h.ThoiDiem)
            .Take(SoHoatDongHienThi)
            .ToList();
    }

    private static void GopKetQuaVaoMoc(MocThongKeVM moc, IEnumerable<DuThiDashboardRow> ds)
    {
        var rows = ds as IList<DuThiDashboardRow> ?? ds.ToList();
        moc.SoLuotDuThi = rows.Count;
        moc.SoBaiDaNop = rows.Count(x => x.TrangThai == 2);

        var he10 = LayDiemHe10(rows);
        moc.SoBaiDaCham = he10.Count;
        moc.SoBaiDat = he10.Count(d => d >= 5);
        moc.DiemTrungBinhHe10 = he10.Count == 0 ? null : Math.Round(he10.Average(), 2);
    }

    private static List<double> LayDiemHe10(IEnumerable<DuThiDashboardRow> rows) =>
        rows.Where(x => x.Diem.HasValue)
            .Select(x => QuyDoiDiem.He10(x.Diem, x.TongDiem)!.Value)
            .ToList();

    // Lịch thi không gắn trực tiếp với học kỳ nên suy ra từ tháng:
    // tháng 9–12 và tháng 1 là học kỳ 1, tháng 2–6 là học kỳ 2, tháng 7–8 là học kỳ hè
    private static int HocKyTheoThang(int thang) => thang switch
    {
        >= 9 => 1,
        1 => 1,
        >= 2 and <= 6 => 2,
        _ => 3
    };

    private static DateTime GioDiaPhuong(DateTime utc) =>
        DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToLocalTime();

    private static DateTime DauThang(DateTime utc)
    {
        var gio = GioDiaPhuong(utc);
        return new DateTime(gio.Year, gio.Month, 1);
    }

    private static bool TrongNamHoc(DateTime utc, DateOnly tu, DateOnly den)
    {
        var ngay = DateOnly.FromDateTime(GioDiaPhuong(utc));
        return ngay >= tu && ngay <= den;
    }

    // Một lượt dự thi kèm mốc thời gian của lịch thi và điểm đã quy đổi được
    private class DuThiDashboardRow
    {
        public int SinhVienId { get; set; }
        public DateTime BatDauLichThi { get; set; }
        public int TrangThai { get; set; }
        public double TongDiem { get; set; }
        public double? Diem { get; set; }
    }
}
