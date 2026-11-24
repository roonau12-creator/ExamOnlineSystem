using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Data;
using ExamOnlineSystem.Models;
using ExamOnlineSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ExamOnlineSystem.Business.Service;

public class KhoaService : IKhoaService
{
    private readonly ApplicationDbContext _context;

    public KhoaService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Khoa>> GetAllKhoaByAsync()
    {
        return await _context.Khoas
            .OrderBy(k => k.SoThuTu)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Khoa> CreateKhoaByAsync(Khoa khoa)
    {
        _context.Khoas.Add(khoa);
        await _context.SaveChangesAsync();
        return khoa;
    }

    public async Task UpdateKhoaAsync(Khoa khoa)
    {
        _context.Khoas.Update(khoa);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteKhoaAsync(int id)
    {
        var khoa = await _context.Khoas.FirstOrDefaultAsync(k => k.Id == id);
        if (khoa == null)
        {
            throw new KeyNotFoundException("Không tìm thấy khoa");
        }
        _context.Khoas.Remove(khoa);
        await _context.SaveChangesAsync();
    }

    // AsNoTracking: tránh lỗi "already being tracked" khi Edit gọi hàm này rồi UpdateKhoaAsync với object khác cùng Id
    public async Task<Khoa?> GetKhoaByIdAsync(int id)
    {
        return await _context.Khoas
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.Id == id);
    }

    public async Task<KhoaDetailVM?> GetKhoaDetailAsync(int id)
    {
        var khoa = await GetKhoaByIdAsync(id);
        if (khoa == null)
        {
            return null;
        }

        var vm = new KhoaDetailVM { Khoa = khoa };

        vm.BoMons = await _context.BoMons
            .Where(b => b.KhoaId == id)
            .OrderBy(b => b.SoThuTu).ThenBy(b => b.MaBoMon)
            .Select(b => new KhoaBoMonItem
            {
                Id = b.Id,
                MaBoMon = b.MaBoMon,
                TenBoMon = b.TenBoMon,
                SoThuTu = b.SoThuTu,
                SoGiaoVien = _context.GiaoViens.Count(g => g.BoMonId == b.Id),
                SoMonHoc = _context.MonHocs.Count(m => m.BoMonId == b.Id)
            })
            .ToListAsync();

        vm.Lops = await _context.Lops
            .Where(l => l.KhoaId == id)
            .OrderBy(l => l.MaLop)
            .Select(l => new KhoaLopItem
            {
                Id = l.Id,
                MaLop = l.MaLop,
                TenLop = l.TenLop,
                SiSo = _context.SinhViens.Count(s => s.LopId == l.Id)
            })
            .ToListAsync();

        vm.MonHocs = await _context.MonHocs
            .Where(m => m.KhoaId == id)
            .OrderBy(m => m.SoThuTu).ThenBy(m => m.MaMonHoc)
            .Select(m => new KhoaMonHocItem
            {
                Id = m.Id,
                MaMonHoc = m.MaMonHoc,
                TenMonHoc = m.TenMonHoc,
                TenBoMon = m.BoMon != null ? m.BoMon.TenBoMon : "",
                HinhThucThi = m.HinhThucThi,
                ThoiGianThi = m.ThoiGianThi,
                TrangThai = m.TrangThai
            })
            .ToListAsync();

        // Giảng viên thuộc khoa = giảng viên của các bộ môn trong khoa
        vm.GiaoViens = await _context.GiaoViens
            .Where(g => g.BoMon != null && g.BoMon.KhoaId == id)
            .OrderBy(g => g.MaGiaoVien)
            .Select(g => new KhoaGiaoVienItem
            {
                Id = g.Id,
                MaGiaoVien = g.MaGiaoVien,
                HoTen = g.HoTen,
                TenBoMon = g.BoMon!.TenBoMon,
                Email = g.ApplicationUser != null ? g.ApplicationUser.Email : null,
                SoDienThoai = g.SoDienThoai
            })
            .ToListAsync();

        vm.SoHocPhan = await _context.HocPhans.CountAsync(h => h.KhoaId == id);
        vm.SoSinhVien = await _context.SinhViens.CountAsync(s => s.KhoaId == id);
        return vm;
    }

    public async Task<(int Added, int Updated)> ImportKhoaAsync(IEnumerable<Khoa> khoas)
    {
        var existing = await _context.Khoas.ToListAsync();
        int added = 0, updated = 0;

        foreach (var item in khoas)
        {
            var khoa = existing.FirstOrDefault(k => k.MaKhoa.Equals(item.MaKhoa, StringComparison.OrdinalIgnoreCase));
            if (khoa == null)
            {
                _context.Khoas.Add(new Khoa { MaKhoa = item.MaKhoa, TenKhoa = item.TenKhoa, SoThuTu = item.SoThuTu });
                added++;
            }
            else
            {
                khoa.TenKhoa = item.TenKhoa;
                khoa.SoThuTu = item.SoThuTu;
                updated++;
            }
        }

        await _context.SaveChangesAsync();
        return (added, updated);
    }
}
