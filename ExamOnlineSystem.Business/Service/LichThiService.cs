using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Data;
using ExamOnlineSystem.Models;
using ExamOnlineSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ExamOnlineSystem.Business.Service;

public class LichThiService : ILichThiService
{
    private readonly ApplicationDbContext _context;

    public LichThiService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<LichThiListItemVM>> GetAllLichThiAsync()
    {
        var ds = await _context.LichThis
            .OrderBy(l => l.ThoiGianBatDau)
            .Select(l => new LichThiListItemVM
            {
                Id = l.Id,
                TenLichThi = l.TenLichThi,
                DeThiId = l.DeThiId,
                MaDeThi = l.DeThi!.MaDeThi,
                TenDeThi = l.DeThi!.TenDeThi,
                TenMonHoc = l.DeThi!.MonHoc.TenMonHoc,
                ThoiLuong = l.DeThi!.ThoiLuong,
                ThoiGianBatDau = l.ThoiGianBatDau,
                ThoiGianKetThuc = l.ThoiGianKetThuc,
                GiaoVienId = l.GiaoVienId,
                TenGiaoVien = l.GiaoVien!.HoTen,
                GhiChu = l.GhiChu,
                Lops = l.LichThiLops.OrderBy(x => x.Lop!.TenLop).Select(x => x.Lop!.TenLop).ToList(),
                SoSinhVien = _context.SinhViens.Count(s => l.LichThiLops.Any(x => x.LopId == s.LopId))
            })
            .AsNoTracking()
            .ToListAsync();

        // Npgsql trả DateTime Kind=Utc cho timestamptz; đảm bảo chắc chắn trước khi đổi giờ hiển thị
        foreach (var l in ds)
        {
            l.ThoiGianBatDau = DateTime.SpecifyKind(l.ThoiGianBatDau, DateTimeKind.Utc);
            l.ThoiGianKetThuc = DateTime.SpecifyKind(l.ThoiGianKetThuc, DateTimeKind.Utc);
        }
        return ds;
    }

    public async Task<int> CountLichThiTheoDeAsync(int deThiId)
    {
        return await _context.LichThis.CountAsync(l => l.DeThiId == deThiId);
    }

    public async Task<List<LopChonVM>> GetLopChoLichThiAsync()
    {
        return await _context.Lops
            .OrderBy(l => l.Khoa!.SoThuTu).ThenBy(l => l.TenLop)
            .Select(l => new LopChonVM
            {
                Id = l.Id,
                MaLop = l.MaLop,
                TenLop = l.TenLop,
                TenKhoa = l.Khoa!.TenKhoa,
                SoSinhVien = _context.SinhViens.Count(s => s.LopId == l.Id)
            })
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<TrungLichVM>> GetLichTrungGioAsync(IEnumerable<int> lopIds, DateTime batDau, DateTime ketThuc, int? boQuaId = null)
    {
        var ids = lopIds.Distinct().ToList();
        // Hai khung giờ giao nhau khi: bắt đầu A < kết thúc B và bắt đầu B < kết thúc A
        var ds = await _context.LichThiLops
            .Where(x => ids.Contains(x.LopId) && x.LichThiId != boQuaId &&
                        x.LichThi!.ThoiGianBatDau < ketThuc && batDau < x.LichThi.ThoiGianKetThuc)
            .OrderBy(x => x.LichThi!.ThoiGianBatDau)
            .Select(x => new TrungLichVM
            {
                TenLop = x.Lop!.TenLop,
                TenLichThi = x.LichThi!.TenLichThi,
                ThoiGianBatDau = x.LichThi.ThoiGianBatDau,
                ThoiGianKetThuc = x.LichThi.ThoiGianKetThuc
            })
            .AsNoTracking()
            .ToListAsync();
        foreach (var t in ds)
        {
            t.ThoiGianBatDau = DateTime.SpecifyKind(t.ThoiGianBatDau, DateTimeKind.Utc);
            t.ThoiGianKetThuc = DateTime.SpecifyKind(t.ThoiGianKetThuc, DateTimeKind.Utc);
        }
        return ds;
    }

    public async Task<LichThi?> GetLichThiByIdAsync(int id)
    {
        var lt = await _context.LichThis
            .Include(l => l.LichThiLops)
            .Include(l => l.DeThi)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id);
        if (lt != null)
        {
            lt.ThoiGianBatDau = DateTime.SpecifyKind(lt.ThoiGianBatDau, DateTimeKind.Utc);
            lt.ThoiGianKetThuc = DateTime.SpecifyKind(lt.ThoiGianKetThuc, DateTimeKind.Utc);
        }
        return lt;
    }

    public async Task UpdateLichThiAsync(LichThi lichThi, IEnumerable<int> lopIds)
    {
        var entity = await _context.LichThis.Include(l => l.LichThiLops).FirstOrDefaultAsync(l => l.Id == lichThi.Id)
                     ?? throw new KeyNotFoundException("Không tìm thấy lịch thi");
        entity.TenLichThi = lichThi.TenLichThi;
        entity.DeThiId = lichThi.DeThiId;
        entity.ThoiGianBatDau = lichThi.ThoiGianBatDau;
        entity.ThoiGianKetThuc = lichThi.ThoiGianKetThuc;
        entity.GhiChu = lichThi.GhiChu;

        // Đồng bộ lớp: giữ lớp còn chọn, thêm lớp mới, bỏ lớp không còn chọn
        var moi = lopIds.Distinct().ToHashSet();
        _context.LichThiLops.RemoveRange(entity.LichThiLops.Where(x => !moi.Contains(x.LopId)).ToList());
        foreach (var lopId in moi.Where(id => entity.LichThiLops.All(x => x.LopId != id)))
        {
            entity.LichThiLops.Add(new LichThiLop { LopId = lopId });
        }

        // Đồng bộ SinhVienDuThi theo danh sách lớp mới: thêm sinh viên mới, xóa sinh viên thuộc lớp bị bỏ (nếu chưa thi)
        var svTrongLopMoi = await _context.SinhViens
            .Where(s => moi.Contains(s.LopId))
            .Select(s => s.Id)
            .ToListAsync();
        var svHienCo = await _context.SinhVienDuThis
            .Include(x => x.SinhVien)
            .Where(x => x.LichThiId == entity.Id)
            .ToListAsync();

        var svHienCoIds = svHienCo.Select(x => x.SinhVienId).ToHashSet();
        foreach (var svId in svTrongLopMoi.Where(id => !svHienCoIds.Contains(id)))
        {
            _context.SinhVienDuThis.Add(new SinhVienDuThi
            {
                LichThiId = entity.Id,
                SinhVienId = svId,
                TrangThai = 0
            });
        }

        var svCanXoa = svHienCo
            .Where(x => x.TrangThai == 0 && (x.SinhVien == null || !moi.Contains(x.SinhVien.LopId)))
            .ToList();
        if (svCanXoa.Count > 0)
        {
            _context.SinhVienDuThis.RemoveRange(svCanXoa);
        }

        await _context.SaveChangesAsync();
    }

    public async Task<LichThi> CreateLichThiAsync(LichThi lichThi, IEnumerable<int> lopIds)
    {
        var distinctLopIds = lopIds.Distinct().ToList();
        foreach (var lopId in distinctLopIds)
        {
            lichThi.LichThiLops.Add(new LichThiLop { LopId = lopId });
        }
        _context.LichThis.Add(lichThi);

        var svIds = await _context.SinhViens
            .Where(s => distinctLopIds.Contains(s.LopId))
            .Select(s => s.Id)
            .ToListAsync();
        foreach (var svId in svIds)
        {
            _context.SinhVienDuThis.Add(new SinhVienDuThi
            {
                LichThi = lichThi,
                SinhVienId = svId,
                TrangThai = 0
            });
        }

        await _context.SaveChangesAsync();
        return lichThi;
    }

    public async Task<List<SinhVienDuThi>> GetSinhVienDuThiByLichThiAsync(int lichThiId)
    {
        // Đảm bảo đồng bộ sinh viên còn thiếu (cho lịch thi tạo trước khi có bảng SinhVienDuThis hoặc sinh viên mới vào lớp)
        var lopIds = await _context.LichThiLops
            .Where(x => x.LichThiId == lichThiId)
            .Select(x => x.LopId)
            .ToListAsync();

        if (lopIds.Count > 0)
        {
            var svTrongLop = await _context.SinhViens
                .Where(s => lopIds.Contains(s.LopId))
                .Select(s => s.Id)
                .ToListAsync();
            var daCoIds = (await _context.SinhVienDuThis
                .Where(x => x.LichThiId == lichThiId)
                .Select(x => x.SinhVienId)
                .ToListAsync()).ToHashSet();

            var thieuIds = svTrongLop.Where(id => !daCoIds.Contains(id)).ToList();
            if (thieuIds.Count > 0)
            {
                foreach (var svId in thieuIds)
                {
                    _context.SinhVienDuThis.Add(new SinhVienDuThi
                    {
                        LichThiId = lichThiId,
                        SinhVienId = svId,
                        TrangThai = 0
                    });
                }
                await _context.SaveChangesAsync();
            }
        }

        var ds = await _context.SinhVienDuThis
            .Include(x => x.SinhVien)!.ThenInclude(s => s!.Lop)
            .Where(x => x.LichThiId == lichThiId)
            .OrderBy(x => x.SinhVien!.Lop!.MaLop)
            .ThenBy(x => x.SinhVien!.MaSinhVien)
            .AsNoTracking()
            .ToListAsync();

        foreach (var item in ds)
        {
            if (item.ThoiGianBatDau.HasValue)
                item.ThoiGianBatDau = DateTime.SpecifyKind(item.ThoiGianBatDau.Value, DateTimeKind.Utc);
            if (item.ThoiGianNopBai.HasValue)
                item.ThoiGianNopBai = DateTime.SpecifyKind(item.ThoiGianNopBai.Value, DateTimeKind.Utc);
        }

        return ds;
    }

    public async Task DeleteLichThiAsync(int id)
    {
        var entity = await _context.LichThis.FindAsync(id)
                     ?? throw new KeyNotFoundException("Không tìm thấy lịch thi");
        _context.LichThis.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
