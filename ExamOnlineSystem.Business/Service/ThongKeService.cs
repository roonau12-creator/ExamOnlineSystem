using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Data;
using ExamOnlineSystem.Models;
using ExamOnlineSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ExamOnlineSystem.Business.Service;

// Thống kê cho khu vực quản trị. Phần dashboard tổng quan nằm ở ThongKeService.Dashboard.cs.
public partial class ThongKeService : IThongKeService
{
    private readonly ApplicationDbContext _context;

    public ThongKeService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ThongKeGiaoVienTongQuanVM> GetThongKeGiaoVienAsync()
    {
        var bayGio = DateTime.UtcNow;
        var ds = await TruyVanGiaoVien(bayGio).ToListAsync();
        var ketQua = (await LayKetQuaThiAsync(null)).ToLookup(x => x.GiaoVienId);
        foreach (var g in ds)
        {
            ChuanHoaGio(g);
            ApDungKetQuaThi(g, ketQua[g.Id]);
        }

        return new ThongKeGiaoVienTongQuanVM
        {
            GiaoViens = ds,
            TheoKhoa = GomNhom(ds, g => g.TenKhoa, _ => string.Empty),
            TheoBoMon = GomNhom(ds, g => g.TenBoMon, g => g.TenKhoa)
        };
    }

    public async Task<ThongKeGiaoVienChiTietVM?> GetThongKeGiaoVienByIdAsync(int giaoVienId)
    {
        var bayGio = DateTime.UtcNow;
        var gv = await TruyVanGiaoVien(bayGio).FirstOrDefaultAsync(g => g.Id == giaoVienId);
        if (gv == null)
        {
            return null;
        }
        ChuanHoaGio(gv);

        var ketQua = await LayKetQuaThiAsync(giaoVienId);
        ApDungKetQuaThi(gv, ketQua);

        var deThis = await _context.DeThis
            .Where(d => d.GiaoVienId == giaoVienId)
            .OrderByDescending(d => d.DaPhatHanh)
            .ThenBy(d => d.MaDeThi)
            .Select(d => new DeThiCuaGiaoVienVM
            {
                Id = d.Id,
                MaDeThi = d.MaDeThi,
                TenDeThi = d.TenDeThi,
                TenMonHoc = d.MonHoc.TenMonHoc,
                LoaiDeThi = d.LoaiDeThi,
                ThoiLuong = d.ThoiLuong,
                TongDiem = d.TongDiem,
                SoCau = _context.CauHoiDeThis.Count(c => c.DeThiId == d.Id),
                DaPhatHanh = d.DaPhatHanh,
                NgayPhatHanh = d.NgayPhatHanh,
                SoLichThi = _context.LichThis.Count(l => l.DeThiId == d.Id)
            })
            .AsNoTracking()
            .ToListAsync();
        foreach (var d in deThis.Where(x => x.NgayPhatHanh.HasValue))
        {
            d.NgayPhatHanh = DateTime.SpecifyKind(d.NgayPhatHanh!.Value, DateTimeKind.Utc);
        }

        var lichThis = await _context.LichThis
            .Where(l => l.GiaoVienId == giaoVienId)
            .OrderByDescending(l => l.ThoiGianBatDau)
            .Select(l => new LichThiCuaGiaoVienVM
            {
                Id = l.Id,
                TenLichThi = l.TenLichThi,
                MaDeThi = l.DeThi!.MaDeThi,
                TenMonHoc = l.DeThi!.MonHoc.TenMonHoc,
                ThoiGianBatDau = l.ThoiGianBatDau,
                ThoiGianKetThuc = l.ThoiGianKetThuc
            })
            .AsNoTracking()
            .ToListAsync();

        // Số liệu làm bài của từng lịch thi lấy từ cùng một lần truy vấn kết quả ở trên
        var theoLich = ketQua.ToLookup(x => x.LichThiId);
        foreach (var l in lichThis)
        {
            l.ThoiGianBatDau = DateTime.SpecifyKind(l.ThoiGianBatDau, DateTimeKind.Utc);
            l.ThoiGianKetThuc = DateTime.SpecifyKind(l.ThoiGianKetThuc, DateTimeKind.Utc);
            l.TrangThai = TrangThaiLichThi.Tinh(l.ThoiGianBatDau, l.ThoiGianKetThuc, bayGio);

            var rows = theoLich[l.Id].ToList();
            l.SoSinhVien = rows.Count;
            l.SoDaNop = rows.Count(x => x.TrangThai == 2);
            l.SoChuaCham = rows.Count(x => x.TrangThai == 2 && !x.Diem.HasValue);
            var he10 = rows.Where(x => x.Diem.HasValue)
                .Select(x => QuyDoiDiem.He10(x.Diem, x.TongDiem)!.Value)
                .ToList();
            l.DiemTrungBinhHe10 = he10.Count == 0 ? null : Math.Round(he10.Average(), 2);
        }

        var lopHocPhans = await _context.LopHocPhanGiaoViens
            .Where(x => x.GiaoVienId == giaoVienId)
            .OrderByDescending(x => x.LopHocPhan!.NamHoc!.MaNamHoc)
            .ThenBy(x => x.LopHocPhan!.HocKy)
            .ThenBy(x => x.LopHocPhan!.MaLopHocPhan)
            .Select(x => new LopHocPhanCuaGiaoVienVM
            {
                Id = x.LopHocPhanId,
                MaLopHocPhan = x.LopHocPhan!.MaLopHocPhan,
                TenLopHocPhan = x.LopHocPhan!.TenLopHocPhan,
                TenHocPhan = x.LopHocPhan!.HocPhan!.MonHoc!.TenMonHoc,
                TenNamHoc = x.LopHocPhan!.NamHoc!.MaNamHoc,
                HocKy = x.LopHocPhan!.HocKy,
                VaiTro = x.VaiTro,
                SiSoToiDa = x.LopHocPhan!.SiSoToiDa,
                SoSinhVien = _context.LopHocPhanSinhViens.Count(s => s.LopHocPhanId == x.LopHocPhanId)
            })
            .AsNoTracking()
            .ToListAsync();

        var danhMucs = await _context.CauHois
            .Where(c => c.GiaoVienId == giaoVienId)
            .GroupBy(c => new { c.DanhMucCauHoiId, c.DanhMucCauHoi.TenDanhMuc, TenMonHoc = c.DanhMucCauHoi.MonHoc!.TenMonHoc })
            .Select(grp => new DanhMucCauHoiCuaGiaoVienVM
            {
                Id = grp.Key.DanhMucCauHoiId,
                TenDanhMuc = grp.Key.TenDanhMuc,
                TenMonHoc = grp.Key.TenMonHoc,
                SoCauHoi = grp.Count(),
                SoCauHoiTracNghiem = grp.Count(c => c.LoaiCauHoi == 1),
                SoCauHoiTuLuan = grp.Count(c => c.LoaiCauHoi == 2)
            })
            .AsNoTracking()
            .ToListAsync();

        return new ThongKeGiaoVienChiTietVM
        {
            GiaoVien = gv,
            DeThis = deThis,
            LichThis = lichThis,
            LopHocPhans = lopHocPhans,
            DanhMucs = danhMucs.OrderByDescending(d => d.SoCauHoi).ThenBy(d => d.TenDanhMuc).ToList()
        };
    }

    // ===== Dùng chung =====

    // Thống kê câu hỏi / đề thi / lịch thi / lớp học phần của từng giảng viên (chưa gồm kết quả thi)
    private IQueryable<ThongKeGiaoVienVM> TruyVanGiaoVien(DateTime bayGio) =>
        _context.GiaoViens
            .OrderBy(g => g.BoMon!.Khoa!.SoThuTu)
            .ThenBy(g => g.BoMon!.SoThuTu)
            .ThenBy(g => g.MaGiaoVien)
            .Select(g => new ThongKeGiaoVienVM
            {
                Id = g.Id,
                MaGiaoVien = g.MaGiaoVien,
                HoTen = g.HoTen,
                ChucDanh = g.ApplicationUser!.ChucDanh,
                Email = g.ApplicationUser!.Email ?? string.Empty,
                SoDienThoai = g.SoDienThoai,
                BoMonId = g.BoMonId,
                TenBoMon = g.BoMon!.TenBoMon,
                TenKhoa = g.BoMon!.Khoa!.TenKhoa,
                DangHoatDong = g.ApplicationUser!.DangHoatDong,
                LanDangNhapCuoi = g.ApplicationUser!.LanDangNhapCuoi,
                NgayTaoTaiKhoan = g.ApplicationUser!.NgayTao,

                SoCauHoi = _context.CauHois.Count(c => c.GiaoVienId == g.Id),
                SoCauHoiTracNghiem = _context.CauHois.Count(c => c.GiaoVienId == g.Id && c.LoaiCauHoi == 1),
                SoCauHoiTuLuan = _context.CauHois.Count(c => c.GiaoVienId == g.Id && c.LoaiCauHoi == 2),
                SoCauHoiDe = _context.CauHois.Count(c => c.GiaoVienId == g.Id && c.MucDo == 1),
                SoCauHoiTrungBinh = _context.CauHois.Count(c => c.GiaoVienId == g.Id && c.MucDo == 2),
                SoCauHoiKho = _context.CauHois.Count(c => c.GiaoVienId == g.Id && c.MucDo == 3),
                SoCauHoiRatKho = _context.CauHois.Count(c => c.GiaoVienId == g.Id && c.MucDo == 4),
                SoDanhMucCauHoi = _context.CauHois.Where(c => c.GiaoVienId == g.Id)
                    .Select(c => c.DanhMucCauHoiId).Distinct().Count(),

                SoDeThi = _context.DeThis.Count(d => d.GiaoVienId == g.Id),
                SoDeThiDaPhatHanh = _context.DeThis.Count(d => d.GiaoVienId == g.Id && d.DaPhatHanh),
                SoMonHocSoanDe = _context.DeThis.Where(d => d.GiaoVienId == g.Id)
                    .Select(d => d.MonHocId).Distinct().Count(),

                SoLichThi = _context.LichThis.Count(l => l.GiaoVienId == g.Id),
                SoLichThiSapDienRa = _context.LichThis.Count(l => l.GiaoVienId == g.Id && l.ThoiGianBatDau > bayGio),
                SoLichThiDangDienRa = _context.LichThis.Count(l => l.GiaoVienId == g.Id &&
                    l.ThoiGianBatDau <= bayGio && l.ThoiGianKetThuc >= bayGio),
                SoLichThiDaKetThuc = _context.LichThis.Count(l => l.GiaoVienId == g.Id && l.ThoiGianKetThuc < bayGio),

                SoLopHocPhan = _context.LopHocPhanGiaoViens.Count(x => x.GiaoVienId == g.Id),
                SoLopHocPhanPhuTrach = _context.LopHocPhanGiaoViens
                    .Count(x => x.GiaoVienId == g.Id && x.VaiTro == LopHocPhanVaiTro.PhuTrach),
                SoSinhVienLopHocPhan = _context.LopHocPhanSinhViens
                    .Count(s => s.LopHocPhan!.LopHocPhanGiaoViens.Any(x => x.GiaoVienId == g.Id))
            })
            .AsNoTracking();

    // Lượt dự thi của các lịch thi do giảng viên phụ trách. giaoVienId = null: tất cả giảng viên.
    // Điểm ưu tiên trên SinhVienDuThi, chưa chấm thì lấy điểm lần thi gần nhất đã có điểm.
    private async Task<List<KetQuaDuThi>> LayKetQuaThiAsync(int? giaoVienId)
    {
        var q = _context.SinhVienDuThis.AsQueryable();
        if (giaoVienId.HasValue)
        {
            q = q.Where(x => x.LichThi!.GiaoVienId == giaoVienId.Value);
        }
        return await q
            .Select(x => new KetQuaDuThi
            {
                GiaoVienId = x.LichThi!.GiaoVienId,
                LichThiId = x.LichThiId,
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
    }

    private static void ApDungKetQuaThi(ThongKeGiaoVienVM vm, IEnumerable<KetQuaDuThi> ds)
    {
        var rows = ds as IList<KetQuaDuThi> ?? ds.ToList();
        vm.SoLuotDuThi = rows.Count;
        vm.SoBaiDaNop = rows.Count(x => x.TrangThai == 2);
        vm.SoBaiChuaCham = rows.Count(x => x.TrangThai == 2 && !x.Diem.HasValue);

        var he10 = rows.Where(x => x.Diem.HasValue)
            .Select(x => QuyDoiDiem.He10(x.Diem, x.TongDiem)!.Value)
            .ToList();
        vm.SoBaiCoDiem = he10.Count;
        if (he10.Count == 0)
        {
            return;
        }
        vm.DiemTrungBinhHe10 = Math.Round(he10.Average(), 2);
        vm.SoBaiDat = he10.Count(d => d >= 5);
    }

    private static List<ThongKeNhomVM> GomNhom(List<ThongKeGiaoVienVM> ds,
        Func<ThongKeGiaoVienVM, string> khoa, Func<ThongKeGiaoVienVM, string> cha) =>
        ds.GroupBy(khoa)
            .Select(grp => new ThongKeNhomVM
            {
                Ten = string.IsNullOrWhiteSpace(grp.Key) ? "(chưa phân)" : grp.Key,
                TenCha = cha(grp.First()),
                SoGiaoVien = grp.Count(),
                SoCauHoi = grp.Sum(g => g.SoCauHoi),
                SoDeThi = grp.Sum(g => g.SoDeThi),
                SoLichThi = grp.Sum(g => g.SoLichThi),
                SoLopHocPhan = grp.Sum(g => g.SoLopHocPhan)
            })
            .OrderByDescending(n => n.SoCauHoi)
            .ThenBy(n => n.Ten)
            .ToList();

    // Npgsql trả timestamptz với Kind=Utc; đặt lại cho chắc trước khi đổi sang giờ địa phương
    private static void ChuanHoaGio(ThongKeGiaoVienVM vm)
    {
        vm.NgayTaoTaiKhoan = DateTime.SpecifyKind(vm.NgayTaoTaiKhoan, DateTimeKind.Utc);
        if (vm.LanDangNhapCuoi.HasValue)
        {
            vm.LanDangNhapCuoi = DateTime.SpecifyKind(vm.LanDangNhapCuoi.Value, DateTimeKind.Utc);
        }
    }


    // ===== Thống kê sinh viên =====

    public async Task<ThongKeSinhVienTongQuanVM> GetThongKeSinhVienAsync()
    {
        var ds = await TruyVanSinhVien().ToListAsync();
        var ketQua = (await LayKetQuaSinhVienAsync(null)).ToLookup(x => x.SinhVienId);
        foreach (var s in ds)
        {
            ChuanHoaGio(s);
            ApDungKetQuaSinhVien(s, ketQua[s.Id]);
        }

        return new ThongKeSinhVienTongQuanVM
        {
            SinhViens = ds,
            PhanBoXepLoai = TinhPhanBo(ds.Where(s => s.DiemTrungBinhHe10.HasValue)
                .Select(s => s.DiemTrungBinhHe10!.Value).ToList()),
            TheoKhoa = GomNhomSinhVien(ds, s => s.TenKhoa, _ => string.Empty),
            TheoLop = GomNhomSinhVien(ds, s => s.MaLop, s => s.TenKhoa)
        };
    }

    public async Task<ThongKeSinhVienChiTietVM?> GetThongKeSinhVienByIdAsync(int sinhVienId)
    {
        var sv = await TruyVanSinhVien().FirstOrDefaultAsync(s => s.Id == sinhVienId);
        if (sv == null)
        {
            return null;
        }
        ChuanHoaGio(sv);
        ApDungKetQuaSinhVien(sv, await LayKetQuaSinhVienAsync(sinhVienId));

        var bayGio = DateTime.UtcNow;
        var lichThis = await _context.SinhVienDuThis
            .Where(x => x.SinhVienId == sinhVienId)
            .OrderByDescending(x => x.LichThi!.ThoiGianBatDau)
            .Select(x => new KetQuaLichThiCuaSinhVienVM
            {
                Id = x.Id,
                LichThiId = x.LichThiId,
                TenLichThi = x.LichThi!.TenLichThi,
                MaDeThi = x.LichThi!.DeThi!.MaDeThi,
                TenMonHoc = x.LichThi!.DeThi!.MonHoc.TenMonHoc,
                TenGiaoVien = x.LichThi!.GiaoVien!.HoTen,
                ThoiGianBatDau = x.LichThi!.ThoiGianBatDau,
                ThoiGianKetThuc = x.LichThi!.ThoiGianKetThuc,
                TongDiem = x.LichThi!.DeThi!.TongDiem,
                TrangThai = x.TrangThai,
                // Thời gian / điểm lấy ở SinhVienDuThi; chưa có thì lấy theo lần thi gần nhất
                BatDauLamBai = x.ThoiGianBatDau ?? _context.LanThis
                    .Where(l => l.SinhVienDuThiId == x.Id)
                    .OrderByDescending(l => l.LanThu)
                    .Select(l => (DateTime?)l.ThoiGianBatDau)
                    .FirstOrDefault(),
                NopBai = x.ThoiGianNopBai ?? _context.LanThis
                    .Where(l => l.SinhVienDuThiId == x.Id && l.ThoiGianNopBai != null)
                    .OrderByDescending(l => l.LanThu)
                    .Select(l => l.ThoiGianNopBai)
                    .FirstOrDefault(),
                Diem = x.Diem ?? _context.LanThis
                    .Where(l => l.SinhVienDuThiId == x.Id && l.Diem != null)
                    .OrderByDescending(l => l.LanThu)
                    .Select(l => l.Diem)
                    .FirstOrDefault(),
                SoLanThi = _context.LanThis.Count(l => l.SinhVienDuThiId == x.Id),
                LanThiId = _context.LanThis
                    .Where(l => l.SinhVienDuThiId == x.Id)
                    .OrderByDescending(l => l.LanThu)
                    .Select(l => (int?)l.Id)
                    .FirstOrDefault()
            })
            .AsNoTracking()
            .ToListAsync();

        foreach (var l in lichThis)
        {
            l.ThoiGianBatDau = DateTime.SpecifyKind(l.ThoiGianBatDau, DateTimeKind.Utc);
            l.ThoiGianKetThuc = DateTime.SpecifyKind(l.ThoiGianKetThuc, DateTimeKind.Utc);
            if (l.BatDauLamBai.HasValue)
                l.BatDauLamBai = DateTime.SpecifyKind(l.BatDauLamBai.Value, DateTimeKind.Utc);
            if (l.NopBai.HasValue)
                l.NopBai = DateTime.SpecifyKind(l.NopBai.Value, DateTimeKind.Utc);
            l.TrangThaiLich = TrangThaiLichThi.Tinh(l.ThoiGianBatDau, l.ThoiGianKetThuc, bayGio);
        }

        var lopHocPhans = await _context.LopHocPhanSinhViens
            .Where(x => x.SinhVienId == sinhVienId)
            .OrderByDescending(x => x.LopHocPhan!.NamHoc!.MaNamHoc)
            .ThenBy(x => x.LopHocPhan!.HocKy)
            .ThenBy(x => x.LopHocPhan!.MaLopHocPhan)
            .Select(x => new LopHocPhanCuaSinhVienVM
            {
                Id = x.LopHocPhanId,
                MaLopHocPhan = x.LopHocPhan!.MaLopHocPhan,
                TenLopHocPhan = x.LopHocPhan!.TenLopHocPhan,
                TenMonHoc = x.LopHocPhan!.HocPhan!.MonHoc!.TenMonHoc,
                TenNamHoc = x.LopHocPhan!.NamHoc!.MaNamHoc,
                HocKy = x.LopHocPhan!.HocKy,
                NgayDangKy = x.NgayDangKy,
                // Giảng viên phụ trách lớp học phần (VaiTro = 1); không có thì để trống
                GiaoVienPhuTrach = x.LopHocPhan!.LopHocPhanGiaoViens
                    .Where(g => g.VaiTro == LopHocPhanVaiTro.PhuTrach)
                    .Select(g => g.GiaoVien!.HoTen)
                    .FirstOrDefault() ?? string.Empty
            })
            .AsNoTracking()
            .ToListAsync();
        foreach (var l in lopHocPhans)
        {
            l.NgayDangKy = DateTime.SpecifyKind(l.NgayDangKy, DateTimeKind.Utc);
        }

        return new ThongKeSinhVienChiTietVM
        {
            SinhVien = sv,
            LichThis = lichThis,
            LopHocPhans = lopHocPhans,
            PhanBo = TinhPhanBo(lichThis.Where(l => l.DiemHe10.HasValue).Select(l => l.DiemHe10!.Value).ToList())
        };
    }

    // Thông tin sinh viên + số lớp học phần (chưa gồm kết quả thi)
    private IQueryable<ThongKeSinhVienVM> TruyVanSinhVien() =>
        _context.SinhViens
            .OrderBy(s => s.Lop!.MaLop)
            .ThenBy(s => s.MaSinhVien)
            .Select(s => new ThongKeSinhVienVM
            {
                Id = s.Id,
                MaSinhVien = s.MaSinhVien,
                HoTen = s.HoTen,
                NgaySinh = s.NgaySinh,
                GioiTinh = s.GioiTinh,
                LopId = s.LopId,
                MaLop = s.Lop!.MaLop,
                TenLop = s.Lop!.TenLop,
                TenKhoa = s.Khoa!.TenKhoa,
                Email = s.Email,
                SoDienThoai = s.SoDienThoai,
                // Sinh viên chưa có tài khoản (dữ liệu nhập lỗi) vẫn coi là đang hoạt động
                DangHoatDong = s.ApplicationUser == null || s.ApplicationUser.DangHoatDong,
                LanDangNhapCuoi = s.ApplicationUser!.LanDangNhapCuoi,
                NgayTaoTaiKhoan = s.ApplicationUser!.NgayTao,
                SoLopHocPhan = _context.LopHocPhanSinhViens.Count(x => x.SinhVienId == s.Id)
            })
            .AsNoTracking();

    // Trạng thái + điểm từng lịch thi của sinh viên. sinhVienId = null: tất cả sinh viên.
    private async Task<List<KetQuaSinhVienRow>> LayKetQuaSinhVienAsync(int? sinhVienId)
    {
        var q = _context.SinhVienDuThis.AsQueryable();
        if (sinhVienId.HasValue)
        {
            q = q.Where(x => x.SinhVienId == sinhVienId.Value);
        }
        return await q
            .Select(x => new KetQuaSinhVienRow
            {
                SinhVienId = x.SinhVienId,
                TrangThai = x.TrangThai,
                TongDiem = x.LichThi!.DeThi!.TongDiem,
                SoLanThi = _context.LanThis.Count(l => l.SinhVienDuThiId == x.Id),
                Diem = x.Diem ?? _context.LanThis
                    .Where(l => l.SinhVienDuThiId == x.Id && l.Diem != null)
                    .OrderByDescending(l => l.LanThu)
                    .Select(l => l.Diem)
                    .FirstOrDefault()
            })
            .AsNoTracking()
            .ToListAsync();
    }

    private static void ApDungKetQuaSinhVien(ThongKeSinhVienVM vm, IEnumerable<KetQuaSinhVienRow> ds)
    {
        var rows = ds as IList<KetQuaSinhVienRow> ?? ds.ToList();
        vm.SoLichThi = rows.Count;
        vm.SoChuaThi = rows.Count(x => x.TrangThai == 0);
        vm.SoDangLamBai = rows.Count(x => x.TrangThai == 1);
        vm.SoBaiDaNop = rows.Count(x => x.TrangThai == 2);
        vm.SoLanThi = rows.Sum(x => x.SoLanThi);
        vm.SoBaiChuaCham = rows.Count(x => x.TrangThai == 2 && !x.Diem.HasValue);

        var he10 = rows.Where(x => x.Diem.HasValue)
            .Select(x => QuyDoiDiem.He10(x.Diem, x.TongDiem)!.Value)
            .ToList();
        vm.SoBaiCoDiem = he10.Count;
        if (he10.Count == 0)
        {
            return;
        }
        vm.DiemTrungBinhHe10 = Math.Round(he10.Average(), 2);
        vm.DiemCaoNhatHe10 = he10.Max();
        vm.DiemThapNhatHe10 = he10.Min();
        vm.SoBaiDat = he10.Count(d => d >= 5);
    }

    private static List<ThongKeNhomSinhVienVM> GomNhomSinhVien(List<ThongKeSinhVienVM> ds,
        Func<ThongKeSinhVienVM, string> khoa, Func<ThongKeSinhVienVM, string> cha) =>
        ds.GroupBy(khoa)
            .Select(grp =>
            {
                var items = grp.ToList();
                var coDiem = items.Where(s => s.SoBaiCoDiem > 0 && s.DiemTrungBinhHe10.HasValue).ToList();
                var tongBai = coDiem.Sum(s => s.SoBaiCoDiem);
                return new ThongKeNhomSinhVienVM
                {
                    Ten = string.IsNullOrWhiteSpace(grp.Key) ? "(chưa phân)" : grp.Key,
                    TenCha = cha(items[0]),
                    SoSinhVien = items.Count,
                    SoNam = items.Count(s => s.GioiTinh),
                    SoNu = items.Count(s => !s.GioiTinh),
                    SoDaThi = items.Count(s => s.SoBaiDaNop > 0),
                    SoLuotDuThi = items.Sum(s => s.SoLichThi),
                    SoBaiDaNop = items.Sum(s => s.SoBaiDaNop),
                    SoBaiCoDiem = items.Sum(s => s.SoBaiCoDiem),
                    SoBaiDat = items.Sum(s => s.SoBaiDat),
                    // Bình quân có trọng số theo số bài đã chấm của từng sinh viên
                    DiemTrungBinhHe10 = tongBai == 0
                        ? null
                        : Math.Round(coDiem.Sum(s => s.DiemTrungBinhHe10!.Value * s.SoBaiCoDiem) / tongBai, 2)
                };
            })
            .OrderBy(n => n.Ten)
            .ToList();

    // Phân bố điểm (thang 10) theo các khung xếp loại
    private static List<PhanBoDiemKhungVM> TinhPhanBo(List<double> diem) =>
        KetQuaLabel.KhungDiem
            .Select(k =>
            {
                var soLuong = diem.Count(d => d >= k.Tu && d < k.Den);
                return new PhanBoDiemKhungVM
                {
                    Nhan = k.Nhan,
                    XepLoai = k.XepLoai,
                    Mau = k.Mau,
                    SoLuong = soLuong,
                    TiLe = diem.Count == 0 ? 0 : (int)Math.Round(soLuong * 100.0 / diem.Count)
                };
            })
            .ToList();

    private static void ChuanHoaGio(ThongKeSinhVienVM vm)
    {
        vm.NgayTaoTaiKhoan = DateTime.SpecifyKind(vm.NgayTaoTaiKhoan, DateTimeKind.Utc);
        if (vm.LanDangNhapCuoi.HasValue)
        {
            vm.LanDangNhapCuoi = DateTime.SpecifyKind(vm.LanDangNhapCuoi.Value, DateTimeKind.Utc);
        }
    }

    private class KetQuaSinhVienRow
    {
        public int SinhVienId { get; set; }
        public int TrangThai { get; set; }
        public double TongDiem { get; set; }
        public int SoLanThi { get; set; }
        public double? Diem { get; set; }
    }


    // ===== Thống kê khoa =====

    public async Task<ThongKeKhoaTongQuanVM> GetThongKeKhoaAsync()
    {
        var ds = await TruyVanKhoa(DateTime.UtcNow).ToListAsync();
        var ketQua = (await LayKetQuaKhoaAsync(null)).ToLookup(x => x.KhoaId);
        foreach (var k in ds)
        {
            ApDungKetQuaKhoa(k, ketQua[k.Id]);
        }
        return new ThongKeKhoaTongQuanVM { Khoas = ds };
    }

    public async Task<ThongKeKhoaChiTietVM?> GetThongKeKhoaByIdAsync(int khoaId)
    {
        var khoa = await TruyVanKhoa(DateTime.UtcNow).FirstOrDefaultAsync(k => k.Id == khoaId);
        if (khoa == null)
        {
            return null;
        }
        var ketQua = await LayKetQuaKhoaAsync(khoaId);
        ApDungKetQuaKhoa(khoa, ketQua);

        var boMons = await _context.BoMons
            .Where(b => b.KhoaId == khoaId)
            .OrderBy(b => b.SoThuTu)
            .Select(b => new BoMonCuaKhoaVM
            {
                Id = b.Id,
                MaBoMon = b.MaBoMon,
                TenBoMon = b.TenBoMon,
                SoGiaoVien = _context.GiaoViens.Count(g => g.BoMonId == b.Id),
                SoMonHoc = _context.MonHocs.Count(m => m.BoMonId == b.Id),
                SoCauHoi = _context.CauHois.Count(c => c.DanhMucCauHoi.MonHoc!.BoMonId == b.Id),
                SoDeThi = _context.DeThis.Count(d => d.MonHoc.BoMonId == b.Id)
            })
            .AsNoTracking()
            .ToListAsync();

        var monHocs = await _context.MonHocs
            .Where(m => m.KhoaId == khoaId)
            .OrderBy(m => m.SoThuTu)
            .ThenBy(m => m.MaMonHoc)
            .Select(m => new MonHocCuaKhoaVM
            {
                Id = m.Id,
                MaMonHoc = m.MaMonHoc,
                TenMonHoc = m.TenMonHoc,
                TenBoMon = m.BoMon!.TenBoMon,
                TrangThai = m.TrangThai,
                SoHocPhan = _context.HocPhans.Count(h => h.MonHocId == m.Id),
                SoDanhMucCauHoi = _context.DanhMucCauHois.Count(d => d.MonHocId == m.Id),
                SoCauHoi = _context.CauHois.Count(c => c.DanhMucCauHoi.MonHocId == m.Id),
                SoDeThi = _context.DeThis.Count(d => d.MonHocId == m.Id),
                SoDeThiDaPhatHanh = _context.DeThis.Count(d => d.MonHocId == m.Id && d.DaPhatHanh)
            })
            .AsNoTracking()
            .ToListAsync();

        var lops = await _context.Lops
            .Where(l => l.KhoaId == khoaId)
            .OrderBy(l => l.MaLop)
            .Select(l => new LopCuaKhoaVM
            {
                Id = l.Id,
                MaLop = l.MaLop,
                TenLop = l.TenLop,
                SoSinhVien = _context.SinhViens.Count(s => s.LopId == l.Id)
            })
            .AsNoTracking()
            .ToListAsync();

        // Kết quả từng lớp lấy từ cùng một lần truy vấn kết quả của khoa
        var theoLop = ketQua.ToLookup(x => x.LopId);
        foreach (var l in lops)
        {
            var rows = theoLop[l.Id].ToList();
            l.SoLuotDuThi = rows.Count;
            l.SoBaiDaNop = rows.Count(x => x.TrangThai == 2);
            l.SoSinhVienDaThi = rows.Where(x => x.TrangThai == 2).Select(x => x.SinhVienId).Distinct().Count();
            var he10 = rows.Where(x => x.Diem.HasValue)
                .Select(x => QuyDoiDiem.He10(x.Diem, x.TongDiem)!.Value)
                .ToList();
            l.SoBaiCoDiem = he10.Count;
            l.SoBaiDat = he10.Count(d => d >= 5);
            l.DiemTrungBinhHe10 = he10.Count == 0 ? null : Math.Round(he10.Average(), 2);
        }

        // Phân bố xếp loại: mỗi sinh viên một điểm trung bình
        var diemTheoSinhVien = ketQua
            .Where(x => x.Diem.HasValue)
            .GroupBy(x => x.SinhVienId)
            .Select(grp => grp.Average(x => QuyDoiDiem.He10(x.Diem, x.TongDiem)!.Value))
            .ToList();

        return new ThongKeKhoaChiTietVM
        {
            Khoa = khoa,
            BoMons = boMons,
            Lops = lops,
            MonHocs = monHocs,
            PhanBo = TinhPhanBo(diemTheoSinhVien)
        };
    }

    // Số liệu tổ chức / chương trình của từng khoa (chưa gồm kết quả thi).
    // Câu hỏi, đề thi quy về khoa theo môn học; lịch thi theo giảng viên phụ trách.
    private IQueryable<ThongKeKhoaVM> TruyVanKhoa(DateTime bayGio) =>
        _context.Khoas
            .OrderBy(k => k.SoThuTu)
            .Select(k => new ThongKeKhoaVM
            {
                Id = k.Id,
                MaKhoa = k.MaKhoa,
                TenKhoa = k.TenKhoa,
                SoThuTu = k.SoThuTu,

                SoBoMon = _context.BoMons.Count(b => b.KhoaId == k.Id),
                SoGiaoVien = _context.GiaoViens.Count(g => g.BoMon!.KhoaId == k.Id),
                SoGiaoVienDaKhoa = _context.GiaoViens
                    .Count(g => g.BoMon!.KhoaId == k.Id && g.ApplicationUser != null && !g.ApplicationUser.DangHoatDong),
                SoLop = _context.Lops.Count(l => l.KhoaId == k.Id),
                SoSinhVien = _context.SinhViens.Count(s => s.KhoaId == k.Id),
                SoSinhVienNam = _context.SinhViens.Count(s => s.KhoaId == k.Id && s.GioiTinh),

                SoMonHoc = _context.MonHocs.Count(m => m.KhoaId == k.Id),
                SoMonHocHoatDong = _context.MonHocs.Count(m => m.KhoaId == k.Id && m.TrangThai == "Hoạt động"),
                SoHocPhan = _context.HocPhans.Count(h => h.KhoaId == k.Id),
                SoLopHocPhan = _context.LopHocPhans.Count(l => l.HocPhan!.KhoaId == k.Id),
                SoDanhMucCauHoi = _context.DanhMucCauHois.Count(d => d.MonHoc!.KhoaId == k.Id),
                SoCauHoi = _context.CauHois.Count(c => c.DanhMucCauHoi.MonHoc!.KhoaId == k.Id),
                SoCauHoiTracNghiem = _context.CauHois
                    .Count(c => c.DanhMucCauHoi.MonHoc!.KhoaId == k.Id && c.LoaiCauHoi == 1),
                SoCauHoiTuLuan = _context.CauHois
                    .Count(c => c.DanhMucCauHoi.MonHoc!.KhoaId == k.Id && c.LoaiCauHoi == 2),
                SoDeThi = _context.DeThis.Count(d => d.MonHoc.KhoaId == k.Id),
                SoDeThiDaPhatHanh = _context.DeThis.Count(d => d.MonHoc.KhoaId == k.Id && d.DaPhatHanh),

                SoLichThi = _context.LichThis.Count(l => l.GiaoVien!.BoMon!.KhoaId == k.Id),
                SoLichThiDangDienRa = _context.LichThis.Count(l => l.GiaoVien!.BoMon!.KhoaId == k.Id &&
                    l.ThoiGianBatDau <= bayGio && l.ThoiGianKetThuc >= bayGio)
            })
            .AsNoTracking();

    // Lượt dự thi của sinh viên thuộc khoa. khoaId = null: tất cả khoa.
    private async Task<List<KetQuaKhoaRow>> LayKetQuaKhoaAsync(int? khoaId)
    {
        var q = _context.SinhVienDuThis.AsQueryable();
        if (khoaId.HasValue)
        {
            q = q.Where(x => x.SinhVien!.KhoaId == khoaId.Value);
        }
        return await q
            .Select(x => new KetQuaKhoaRow
            {
                KhoaId = x.SinhVien!.KhoaId,
                LopId = x.SinhVien!.LopId,
                SinhVienId = x.SinhVienId,
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
    }

    private static void ApDungKetQuaKhoa(ThongKeKhoaVM vm, IEnumerable<KetQuaKhoaRow> ds)
    {
        var rows = ds as IList<KetQuaKhoaRow> ?? ds.ToList();
        vm.SoLuotDuThi = rows.Count;
        vm.SoBaiDaNop = rows.Count(x => x.TrangThai == 2);
        vm.SoSinhVienDaThi = rows.Where(x => x.TrangThai == 2).Select(x => x.SinhVienId).Distinct().Count();
        vm.SoBaiChuaCham = rows.Count(x => x.TrangThai == 2 && !x.Diem.HasValue);

        var he10 = rows.Where(x => x.Diem.HasValue)
            .Select(x => QuyDoiDiem.He10(x.Diem, x.TongDiem)!.Value)
            .ToList();
        vm.SoBaiCoDiem = he10.Count;
        if (he10.Count == 0)
        {
            return;
        }
        vm.DiemTrungBinhHe10 = Math.Round(he10.Average(), 2);
        vm.DiemCaoNhatHe10 = he10.Max();
        vm.DiemThapNhatHe10 = he10.Min();
        vm.SoBaiDat = he10.Count(d => d >= 5);
    }

    private class KetQuaKhoaRow
    {
        public int KhoaId { get; set; }
        public int LopId { get; set; }
        public int SinhVienId { get; set; }
        public int TrangThai { get; set; }
        public double TongDiem { get; set; }
        public double? Diem { get; set; }
    }


    // ===== Thống kê bộ môn =====

    public async Task<ThongKeBoMonTongQuanVM> GetThongKeBoMonAsync()
    {
        var ds = await TruyVanBoMon(DateTime.UtcNow).ToListAsync();
        var ketQua = (await LayKetQuaBoMonAsync(null)).ToLookup(x => x.BoMonId);
        foreach (var b in ds)
        {
            ApDungKetQuaBoMon(b, ketQua[b.Id]);
        }
        return new ThongKeBoMonTongQuanVM { BoMons = ds };
    }

    public async Task<ThongKeBoMonChiTietVM?> GetThongKeBoMonByIdAsync(int boMonId)
    {
        var bm = await TruyVanBoMon(DateTime.UtcNow).FirstOrDefaultAsync(b => b.Id == boMonId);
        if (bm == null)
        {
            return null;
        }
        var ketQua = await LayKetQuaBoMonAsync(boMonId);
        ApDungKetQuaBoMon(bm, ketQua);

        var giaoViens = await _context.GiaoViens
            .Where(g => g.BoMonId == boMonId)
            .OrderBy(g => g.MaGiaoVien)
            .Select(g => new GiaoVienCuaBoMonVM
            {
                Id = g.Id,
                MaGiaoVien = g.MaGiaoVien,
                HoTen = g.HoTen,
                ChucDanh = g.ApplicationUser!.ChucDanh,
                Email = g.ApplicationUser!.Email ?? string.Empty,
                DangHoatDong = g.ApplicationUser == null || g.ApplicationUser.DangHoatDong,
                LanDangNhapCuoi = g.ApplicationUser!.LanDangNhapCuoi,
                SoCauHoi = _context.CauHois.Count(c => c.GiaoVienId == g.Id),
                SoDeThi = _context.DeThis.Count(d => d.GiaoVienId == g.Id),
                SoLichThi = _context.LichThis.Count(l => l.GiaoVienId == g.Id),
                SoLopHocPhan = _context.LopHocPhanGiaoViens.Count(x => x.GiaoVienId == g.Id)
            })
            .AsNoTracking()
            .ToListAsync();
        foreach (var g in giaoViens.Where(x => x.LanDangNhapCuoi.HasValue))
        {
            g.LanDangNhapCuoi = DateTime.SpecifyKind(g.LanDangNhapCuoi!.Value, DateTimeKind.Utc);
        }

        var monHocs = await _context.MonHocs
            .Where(m => m.BoMonId == boMonId)
            .OrderBy(m => m.SoThuTu)
            .ThenBy(m => m.MaMonHoc)
            .Select(m => new MonHocCuaKhoaVM
            {
                Id = m.Id,
                MaMonHoc = m.MaMonHoc,
                TenMonHoc = m.TenMonHoc,
                TenBoMon = m.BoMon!.TenBoMon,
                TrangThai = m.TrangThai,
                SoHocPhan = _context.HocPhans.Count(h => h.MonHocId == m.Id),
                SoDanhMucCauHoi = _context.DanhMucCauHois.Count(d => d.MonHocId == m.Id),
                SoCauHoi = _context.CauHois.Count(c => c.DanhMucCauHoi.MonHocId == m.Id),
                SoDeThi = _context.DeThis.Count(d => d.MonHocId == m.Id),
                SoDeThiDaPhatHanh = _context.DeThis.Count(d => d.MonHocId == m.Id && d.DaPhatHanh)
            })
            .AsNoTracking()
            .ToListAsync();

        return new ThongKeBoMonChiTietVM
        {
            BoMon = bm,
            GiaoViens = giaoViens,
            MonHocs = monHocs,
            PhanBo = TinhPhanBo(ketQua
                .Where(x => x.Diem.HasValue)
                .Select(x => QuyDoiDiem.He10(x.Diem, x.TongDiem)!.Value)
                .ToList())
        };
    }

    // Số liệu nhân sự / chương trình của từng bộ môn (chưa gồm kết quả thi)
    private IQueryable<ThongKeBoMonVM> TruyVanBoMon(DateTime bayGio) =>
        _context.BoMons
            .OrderBy(b => b.Khoa!.SoThuTu)
            .ThenBy(b => b.SoThuTu)
            .Select(b => new ThongKeBoMonVM
            {
                Id = b.Id,
                MaBoMon = b.MaBoMon,
                TenBoMon = b.TenBoMon,
                SoThuTu = b.SoThuTu,
                KhoaId = b.KhoaId,
                MaKhoa = b.Khoa!.MaKhoa,
                TenKhoa = b.Khoa!.TenKhoa,

                SoGiaoVien = _context.GiaoViens.Count(g => g.BoMonId == b.Id),
                SoGiaoVienDaKhoa = _context.GiaoViens
                    .Count(g => g.BoMonId == b.Id && g.ApplicationUser != null && !g.ApplicationUser.DangHoatDong),

                SoMonHoc = _context.MonHocs.Count(m => m.BoMonId == b.Id),
                SoMonHocHoatDong = _context.MonHocs.Count(m => m.BoMonId == b.Id && m.TrangThai == "Hoạt động"),
                SoHocPhan = _context.HocPhans.Count(h => h.MonHoc!.BoMonId == b.Id),
                SoLopHocPhan = _context.LopHocPhans.Count(l => l.HocPhan!.MonHoc!.BoMonId == b.Id),
                SoDanhMucCauHoi = _context.DanhMucCauHois.Count(d => d.MonHoc!.BoMonId == b.Id),
                SoCauHoi = _context.CauHois.Count(c => c.DanhMucCauHoi.MonHoc!.BoMonId == b.Id),
                SoCauHoiTracNghiem = _context.CauHois
                    .Count(c => c.DanhMucCauHoi.MonHoc!.BoMonId == b.Id && c.LoaiCauHoi == 1),
                SoCauHoiTuLuan = _context.CauHois
                    .Count(c => c.DanhMucCauHoi.MonHoc!.BoMonId == b.Id && c.LoaiCauHoi == 2),
                SoCauHoiDe = _context.CauHois
                    .Count(c => c.DanhMucCauHoi.MonHoc!.BoMonId == b.Id && c.MucDo == 1),
                SoCauHoiTrungBinh = _context.CauHois
                    .Count(c => c.DanhMucCauHoi.MonHoc!.BoMonId == b.Id && c.MucDo == 2),
                SoCauHoiKho = _context.CauHois
                    .Count(c => c.DanhMucCauHoi.MonHoc!.BoMonId == b.Id && c.MucDo == 3),
                SoCauHoiRatKho = _context.CauHois
                    .Count(c => c.DanhMucCauHoi.MonHoc!.BoMonId == b.Id && c.MucDo == 4),
                SoDeThi = _context.DeThis.Count(d => d.MonHoc.BoMonId == b.Id),
                SoDeThiDaPhatHanh = _context.DeThis.Count(d => d.MonHoc.BoMonId == b.Id && d.DaPhatHanh),

                SoLichThi = _context.LichThis.Count(l => l.GiaoVien!.BoMonId == b.Id),
                SoLichThiDangDienRa = _context.LichThis.Count(l => l.GiaoVien!.BoMonId == b.Id &&
                    l.ThoiGianBatDau <= bayGio && l.ThoiGianKetThuc >= bayGio)
            })
            .AsNoTracking();

    // Lượt dự thi của các lịch thi do giảng viên bộ môn phụ trách. boMonId = null: tất cả bộ môn.
    private async Task<List<KetQuaBoMonRow>> LayKetQuaBoMonAsync(int? boMonId)
    {
        var q = _context.SinhVienDuThis.AsQueryable();
        if (boMonId.HasValue)
        {
            q = q.Where(x => x.LichThi!.GiaoVien!.BoMonId == boMonId.Value);
        }
        return await q
            .Select(x => new KetQuaBoMonRow
            {
                BoMonId = x.LichThi!.GiaoVien!.BoMonId,
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
    }

    private static void ApDungKetQuaBoMon(ThongKeBoMonVM vm, IEnumerable<KetQuaBoMonRow> ds)
    {
        var rows = ds as IList<KetQuaBoMonRow> ?? ds.ToList();
        vm.SoLuotDuThi = rows.Count;
        vm.SoBaiDaNop = rows.Count(x => x.TrangThai == 2);
        vm.SoBaiChuaCham = rows.Count(x => x.TrangThai == 2 && !x.Diem.HasValue);

        var he10 = rows.Where(x => x.Diem.HasValue)
            .Select(x => QuyDoiDiem.He10(x.Diem, x.TongDiem)!.Value)
            .ToList();
        vm.SoBaiCoDiem = he10.Count;
        if (he10.Count == 0)
        {
            return;
        }
        vm.DiemTrungBinhHe10 = Math.Round(he10.Average(), 2);
        vm.DiemCaoNhatHe10 = he10.Max();
        vm.DiemThapNhatHe10 = he10.Min();
        vm.SoBaiDat = he10.Count(d => d >= 5);
    }

    private class KetQuaBoMonRow
    {
        public int BoMonId { get; set; }
        public int TrangThai { get; set; }
        public double TongDiem { get; set; }
        public double? Diem { get; set; }
    }

    private class KetQuaDuThi
    {
        public int GiaoVienId { get; set; }
        public int LichThiId { get; set; }
        public int TrangThai { get; set; }
        public double TongDiem { get; set; }
        public double? Diem { get; set; }
    }
}
