using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Data;
using ExamOnlineSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ExamOnlineSystem.Business.Service;

public class HocPhanService : IHocPhanService
{
    private readonly ApplicationDbContext _context;

    public HocPhanService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<HocPhan>> GetAllHocPhanAsync()
    {
        return await _context.HocPhans
            .Include(h => h.Khoa)
            .Include(h => h.MonHoc)
            .OrderBy(h => h.Khoa!.SoThuTu)
            .ThenBy(h => h.MonHoc!.SoThuTu)
            .ThenBy(h => h.MaHocPhan)
            .AsNoTracking()
            .ToListAsync();
    }

    // AsNoTracking: tránh lỗi "already being tracked" khi Edit gọi hàm này rồi UpdateHocPhanAsync với object khác cùng Id
    public async Task<HocPhan?> GetHocPhanByIdAsync(int id)
    {
        return await _context.HocPhans
            .Include(h => h.Khoa)
            .Include(h => h.MonHoc)
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == id);
    }

    public async Task<HocPhan> CreateHocPhanAsync(HocPhan hocPhan)
    {
        _context.HocPhans.Add(hocPhan);
        await _context.SaveChangesAsync();
        return hocPhan;
    }

    public async Task UpdateHocPhanAsync(HocPhan hocPhan)
    {
        // chỉ cập nhật học phần, không đụng tới bản ghi khoa / môn học
        hocPhan.Khoa = null;
        hocPhan.MonHoc = null;
        _context.HocPhans.Update(hocPhan);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteHocPhanAsync(int id)
    {
        var hocPhan = await _context.HocPhans.FirstOrDefaultAsync(h => h.Id == id);
        if (hocPhan == null)
        {
            throw new KeyNotFoundException("Không tìm thấy học phần");
        }
        _context.HocPhans.Remove(hocPhan);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.HocPhans.AnyAsync(h => h.Id == id);
    }

    public async Task<bool> IsMaHocPhanDuplicateAsync(string maHocPhan, int excludeId = 0)
    {
        var ma = maHocPhan.Trim().ToLower();
        return await _context.HocPhans.AnyAsync(h => h.Id != excludeId && h.MaHocPhan.ToLower() == ma);
    }

    public async Task<IEnumerable<MonHoc>> GetMonHocByKhoaAsync(int khoaId)
    {
        return await _context.MonHocs
            .Where(m => m.KhoaId == khoaId)
            .OrderBy(m => m.SoThuTu)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<MonHoc>> GetAllMonHocAsync()
    {
        return await _context.MonHocs
            .Include(m => m.Khoa)
            .OrderBy(m => m.Khoa!.SoThuTu)
            .ThenBy(m => m.SoThuTu)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> MonHocThuocKhoaAsync(int monHocId, int khoaId)
    {
        return await _context.MonHocs.AnyAsync(m => m.Id == monHocId && m.KhoaId == khoaId);
    }

    public async Task<(int Added, int Updated)> ImportHocPhanAsync(IEnumerable<HocPhan> hocPhans)
    {
        var existing = await _context.HocPhans.ToListAsync();
        int added = 0, updated = 0;

        foreach (var item in hocPhans)
        {
            var hocPhan = existing.FirstOrDefault(h => h.MaHocPhan.Equals(item.MaHocPhan, StringComparison.OrdinalIgnoreCase));
            if (hocPhan == null)
            {
                hocPhan = new HocPhan { MaHocPhan = item.MaHocPhan };
                _context.HocPhans.Add(hocPhan);
                added++;
            }
            else
            {
                updated++;
            }

            hocPhan.TenHocPhan = item.TenHocPhan;
            hocPhan.KhoaId = item.KhoaId;
            hocPhan.MonHocId = item.MonHocId;
            hocPhan.SoTinChi = item.SoTinChi;
        }

        await _context.SaveChangesAsync();
        return (added, updated);
    }
}
