using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Data;
using ExamOnlineSystem.Models;
using ExamOnlineSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ExamOnlineSystem.Business.Service;

public class BoMonService : IBoMonService
{
    private readonly ApplicationDbContext _context;

    public BoMonService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<BoMon>> GetAllBoMonAsync(bool includeKhoa = false)
    {
        if (includeKhoa)
        {
            return await _context.BoMons
                .Include(b => b.Khoa)
                .OrderBy(b => b.Khoa!.SoThuTu)
                .ThenBy(b => b.SoThuTu)
                .AsNoTracking()
                .ToListAsync();
        }
        return await _context.BoMons
            .OrderBy(b => b.Khoa!.SoThuTu)
            .ThenBy(b => b.SoThuTu)
            .AsNoTracking()
            .ToListAsync();
    }

    // AsNoTracking: tránh lỗi "already being tracked" khi Edit gọi hàm này rồi UpdateBoMonAsync với object khác cùng Id
    public async Task<BoMon?> GetBoMonByIdAsync(int id,bool includeKhoa = false)
    {
        if (includeKhoa)
        {
            return await _context.BoMons
                .Include(b => b.Khoa)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id);
        }
        return await _context.BoMons
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<BoMon> CreateBoMonAsync(BoMon boMon)
    {
        _context.BoMons.Add(boMon);
        await _context.SaveChangesAsync();
        return boMon;
    }

    public async Task UpdateBoMonAsync(BoMon boMon)
    {
        boMon.Khoa = null; // chỉ cập nhật bộ môn, không đụng tới bản ghi khoa
        _context.BoMons.Update(boMon);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteBoMonAsync(int id)
    {
        var boMon = await _context.BoMons.FirstOrDefaultAsync(b => b.Id == id);
        if (boMon == null)
        {
            throw new KeyNotFoundException("Không tìm thấy bộ môn");
        }
        _context.BoMons.Remove(boMon);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.BoMons.AnyAsync(b => b.Id == id);
    }

    public async Task<BoMonDetailVM?> GetBoMonDetailAsync(int id)
    {
        var boMon = await GetBoMonByIdAsync(id, includeKhoa: true);
        if (boMon == null)
        {
            return null;
        }

        var vm = new BoMonDetailVM { BoMon = boMon };

        vm.MonHocs = await _context.MonHocs
            .Where(m => m.BoMonId == id)
            .OrderBy(m => m.SoThuTu).ThenBy(m => m.MaMonHoc)
            .Select(m => new BoMonMonHocItem
            {
                Id = m.Id,
                MaMonHoc = m.MaMonHoc,
                TenMonHoc = m.TenMonHoc,
                HinhThucThi = m.HinhThucThi,
                ThoiGianThi = m.ThoiGianThi,
                TrangThai = m.TrangThai,
                SoHocPhan = _context.HocPhans.Count(h => h.MonHocId == m.Id),
                SoDeThi = _context.DeThis.Count(d => d.MonHocId == m.Id)
            })
            .ToListAsync();

        // Học phần thuộc bộ môn = học phần của các môn học trong bộ môn
        vm.HocPhans = await _context.HocPhans
            .Where(h => h.MonHoc != null && h.MonHoc.BoMonId == id)
            .OrderBy(h => h.MaHocPhan)
            .Select(h => new BoMonHocPhanItem
            {
                Id = h.Id,
                MaHocPhan = h.MaHocPhan,
                TenHocPhan = h.TenHocPhan,
                TenMonHoc = h.MonHoc!.TenMonHoc,
                SoTinChi = h.SoTinChi
            })
            .ToListAsync();

        vm.GiaoViens = await _context.GiaoViens
            .Where(g => g.BoMonId == id)
            .OrderBy(g => g.MaGiaoVien)
            .Select(g => new BoMonGiaoVienItem
            {
                Id = g.Id,
                MaGiaoVien = g.MaGiaoVien,
                HoTen = g.HoTen,
                Email = g.ApplicationUser != null ? g.ApplicationUser.Email : null,
                SoDienThoai = g.SoDienThoai
            })
            .ToListAsync();

        vm.SoDeThi = vm.MonHocs.Sum(m => m.SoDeThi);
        return vm;
    }

    public async Task<(int Added, int Updated)> ImportBoMonAsync(IEnumerable<BoMon> boMons)
    {
        var existing = await _context.BoMons.ToListAsync();
        int added = 0, updated = 0;

        foreach (var item in boMons)
        {
            var boMon = existing.FirstOrDefault(b => b.MaBoMon.Equals(item.MaBoMon, StringComparison.OrdinalIgnoreCase));
            if (boMon == null)
            {
                _context.BoMons.Add(new BoMon
                {
                    MaBoMon = item.MaBoMon, TenBoMon = item.TenBoMon, KhoaId = item.KhoaId, SoThuTu = item.SoThuTu
                });
                added++;
            }
            else
            {
                boMon.TenBoMon = item.TenBoMon;
                boMon.KhoaId = item.KhoaId;
                boMon.SoThuTu = item.SoThuTu;
                updated++;
            }
        }

        await _context.SaveChangesAsync();
        return (added, updated);
    }
}
