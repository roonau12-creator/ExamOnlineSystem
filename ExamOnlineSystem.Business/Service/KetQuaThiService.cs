using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Data;
using ExamOnlineSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ExamOnlineSystem.Business.Service;

public class KetQuaThiService : IKetQuaThiService
{
    private readonly ApplicationDbContext _context;

    public KetQuaThiService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<KetQuaThiListItemVM>> GetTongHopKetQuaAsync()
    {
        var ds = await TruyVanTongHop().ToListAsync();
        var diem = (await LayDiemAsync(null)).ToLookup(x => x.LichThiId);
        foreach (var l in ds)
        {
            ChuanHoaGio(l);
            ApDungThongKe(l, diem[l.Id]);
        }
        return ds;
    }

    public async Task<KetQuaThiChiTietVM?> GetKetQuaByLichThiAsync(int lichThiId)
    {
        var tongHop = await TruyVanTongHop().FirstOrDefaultAsync(l => l.Id == lichThiId);
        if (tongHop == null)
        {
            return null;
        }
        ChuanHoaGio(tongHop);
        ApDungThongKe(tongHop, await LayDiemAsync(lichThiId));

        var sinhViens = await _context.SinhVienDuThis
            .Where(x => x.LichThiId == lichThiId)
            .OrderBy(x => x.SinhVien!.Lop!.MaLop)
            .ThenBy(x => x.SinhVien!.MaSinhVien)
            .Select(x => new KetQuaSinhVienVM
            {
                Id = x.Id,
                SinhVienId = x.SinhVienId,
                MaSinhVien = x.SinhVien!.MaSinhVien,
                HoTen = x.SinhVien!.HoTen,
                MaLop = x.SinhVien!.Lop!.MaLop,
                TenLop = x.SinhVien!.Lop!.TenLop,
                Email = x.SinhVien!.Email,
                TrangThai = x.TrangThai,
                TongDiem = x.LichThi!.DeThi!.TongDiem,
                // Thời gian / điểm lấy ở SinhVienDuThi; chưa có thì lấy theo lần thi gần nhất
                ThoiGianBatDau = x.ThoiGianBatDau ?? _context.LanThis
                    .Where(l => l.SinhVienDuThiId == x.Id)
                    .OrderByDescending(l => l.LanThu)
                    .Select(l => (DateTime?)l.ThoiGianBatDau)
                    .FirstOrDefault(),
                ThoiGianNopBai = x.ThoiGianNopBai ?? _context.LanThis
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

        foreach (var sv in sinhViens)
        {
            if (sv.ThoiGianBatDau.HasValue)
                sv.ThoiGianBatDau = DateTime.SpecifyKind(sv.ThoiGianBatDau.Value, DateTimeKind.Utc);
            if (sv.ThoiGianNopBai.HasValue)
                sv.ThoiGianNopBai = DateTime.SpecifyKind(sv.ThoiGianNopBai.Value, DateTimeKind.Utc);
        }

        return new KetQuaThiChiTietVM
        {
            TongHop = tongHop,
            TrangThai = TrangThaiLichThi.Tinh(tongHop.ThoiGianBatDau, tongHop.ThoiGianKetThuc, DateTime.UtcNow),
            SinhViens = sinhViens,
            PhanBo = TinhPhanBo(sinhViens)
        };
    }

    public async Task<BaiThiChiTietVM?> GetBaiThiAsync(int lanThiId)
    {
        var bai = await _context.LanThis
            .Where(l => l.Id == lanThiId)
            .Select(l => new BaiThiChiTietVM
            {
                LanThiId = l.Id,
                LanThu = l.LanThu,
                TrangThai = l.TrangThai,
                ThoiGianBatDau = l.ThoiGianBatDau,
                ThoiGianNopBai = l.ThoiGianNopBai,
                Diem = l.Diem ?? l.SinhVienDuThi!.Diem,
                SinhVienDuThiId = l.SinhVienDuThiId,
                MaSinhVien = l.SinhVienDuThi!.SinhVien!.MaSinhVien,
                HoTen = l.SinhVienDuThi!.SinhVien!.HoTen,
                MaLop = l.SinhVienDuThi!.SinhVien!.Lop!.MaLop,
                TenLop = l.SinhVienDuThi!.SinhVien!.Lop!.TenLop,
                LichThiId = l.SinhVienDuThi!.LichThiId,
                TenLichThi = l.SinhVienDuThi!.LichThi!.TenLichThi,
                DeThiId = l.SinhVienDuThi!.LichThi!.DeThiId,
                MaDeThi = l.SinhVienDuThi!.LichThi!.DeThi!.MaDeThi,
                TenDeThi = l.SinhVienDuThi!.LichThi!.DeThi!.TenDeThi,
                TenMonHoc = l.SinhVienDuThi!.LichThi!.DeThi!.MonHoc.TenMonHoc,
                ThoiLuong = l.SinhVienDuThi!.LichThi!.DeThi!.ThoiLuong,
                TongDiem = l.SinhVienDuThi!.LichThi!.DeThi!.TongDiem
            })
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (bai == null)
        {
            return null;
        }
        bai.ThoiGianBatDau = DateTime.SpecifyKind(bai.ThoiGianBatDau, DateTimeKind.Utc);
        if (bai.ThoiGianNopBai.HasValue)
            bai.ThoiGianNopBai = DateTime.SpecifyKind(bai.ThoiGianNopBai.Value, DateTimeKind.Utc);

        // Mỗi câu hỏi có nhiều nhất một bài làm trong cùng lần thi (chỉ mục duy nhất LanThiId + CauHoiLanThiId)
        bai.CauHois = await _context.CauHoiLanThis
            .Where(c => c.LanThiId == lanThiId)
            .OrderBy(c => c.ThuTu)
            .Select(c => new CauHoiBaiLamVM
            {
                CauHoiLanThiId = c.Id,
                ThuTu = c.ThuTu,
                NoiDung = c.NoiDung,
                LoaiCauHoi = c.LoaiCauHoi,
                DiemToiDa = c.Diem,
                NoiDungTraLoi = _context.BaiLams
                    .Where(b => b.CauHoiLanThiId == c.Id)
                    .Select(b => b.NoiDungTraLoi)
                    .FirstOrDefault(),
                DiemCham = _context.BaiLams
                    .Where(b => b.CauHoiLanThiId == c.Id)
                    .Select(b => b.Diem)
                    .FirstOrDefault(),
                NhanXet = _context.BaiLams
                    .Where(b => b.CauHoiLanThiId == c.Id)
                    .Select(b => b.NhanXet)
                    .FirstOrDefault(),
                DapAns = _context.DapAnLanThis
                    .Where(d => d.CauHoiLanThiId == c.Id)
                    .OrderBy(d => d.ThuTu)
                    .Select(d => new DapAnBaiLamVM
                    {
                        Id = d.Id,
                        ThuTu = d.ThuTu,
                        NoiDung = d.NoiDung,
                        LaDapAnDung = d.LaDapAnDung,
                        DaChon = _context.DapAnDaChons.Any(x => x.DapAnLanThiId == d.Id)
                    })
                    .ToList()
            })
            .AsNoTracking()
            .ToListAsync();

        return bai;
    }

    // ===== Dùng chung =====

    // Phần đầu của một dòng kết quả (thông tin lịch thi / đề thi), chưa gồm thống kê điểm
    private IQueryable<KetQuaThiListItemVM> TruyVanTongHop() =>
        _context.LichThis
            .OrderByDescending(l => l.ThoiGianBatDau)
            .Select(l => new KetQuaThiListItemVM
            {
                Id = l.Id,
                TenLichThi = l.TenLichThi,
                DeThiId = l.DeThiId,
                MaDeThi = l.DeThi!.MaDeThi,
                TenDeThi = l.DeThi!.TenDeThi,
                TenMonHoc = l.DeThi!.MonHoc.TenMonHoc,
                ThoiLuong = l.DeThi!.ThoiLuong,
                TongDiem = l.DeThi!.TongDiem,
                ThoiGianBatDau = l.ThoiGianBatDau,
                ThoiGianKetThuc = l.ThoiGianKetThuc,
                GiaoVienId = l.GiaoVienId,
                TenGiaoVien = l.GiaoVien!.HoTen,
                Lops = l.LichThiLops.OrderBy(x => x.Lop!.TenLop).Select(x => x.Lop!.TenLop).ToList()
            })
            .AsNoTracking();

    // Trạng thái + điểm của từng sinh viên dự thi. lichThiId = null: lấy tất cả lịch thi.
    // Điểm ưu tiên trên SinhVienDuThi; chưa chấm thì lấy điểm của lần thi gần nhất đã có điểm.
    private async Task<List<DiemDuThi>> LayDiemAsync(int? lichThiId)
    {
        var q = _context.SinhVienDuThis.AsQueryable();
        if (lichThiId.HasValue)
        {
            q = q.Where(x => x.LichThiId == lichThiId.Value);
        }
        return await q
            .Select(x => new DiemDuThi
            {
                LichThiId = x.LichThiId,
                TrangThai = x.TrangThai,
                Diem = x.Diem ?? _context.LanThis
                    .Where(l => l.SinhVienDuThiId == x.Id && l.Diem != null)
                    .OrderByDescending(l => l.LanThu)
                    .Select(l => l.Diem)
                    .FirstOrDefault()
            })
            .AsNoTracking()
            .ToListAsync();
    }

    private static void ApDungThongKe(KetQuaThiListItemVM vm, IEnumerable<DiemDuThi> ds)
    {
        var rows = ds as IList<DiemDuThi> ?? ds.ToList();
        vm.SoSinhVien = rows.Count;
        vm.SoChuaThi = rows.Count(x => x.TrangThai == 0);
        vm.SoDangThi = rows.Count(x => x.TrangThai == 1);
        vm.SoDaNop = rows.Count(x => x.TrangThai == 2);

        vm.SoChuaCham = rows.Count(x => x.TrangThai == 2 && !x.Diem.HasValue);

        var diem = rows.Where(x => x.Diem.HasValue).Select(x => x.Diem!.Value).ToList();
        vm.SoCoDiem = diem.Count;
        if (diem.Count == 0)
        {
            return;
        }
        vm.DiemTrungBinh = Math.Round(diem.Average(), 2);
        vm.DiemCaoNhat = diem.Max();
        vm.DiemThapNhat = diem.Min();
        vm.SoDat = diem.Count(d => QuyDoiDiem.He10(d, vm.TongDiem) >= 5);
    }

    private static List<PhanBoDiemKhungVM> TinhPhanBo(List<KetQuaSinhVienVM> sinhViens)
    {
        var diem = sinhViens.Where(s => s.DiemHe10.HasValue).Select(s => s.DiemHe10!.Value).ToList();
        return KetQuaLabel.KhungDiem
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
    }

    // Npgsql trả timestamptz với Kind=Utc; đặt lại cho chắc trước khi đổi sang giờ địa phương
    private static void ChuanHoaGio(KetQuaThiListItemVM vm)
    {
        vm.ThoiGianBatDau = DateTime.SpecifyKind(vm.ThoiGianBatDau, DateTimeKind.Utc);
        vm.ThoiGianKetThuc = DateTime.SpecifyKind(vm.ThoiGianKetThuc, DateTimeKind.Utc);
    }

    private class DiemDuThi
    {
        public int LichThiId { get; set; }
        public int TrangThai { get; set; }
        public double? Diem { get; set; }
    }
}
