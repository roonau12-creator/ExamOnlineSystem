using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Data;
using ExamOnlineSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ExamOnlineSystem.Business.Service;

public class NamHocService : INamHocService
{
    private readonly ApplicationDbContext _context;

    public NamHocService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<NamHoc>> GetAllNamHocAsync()
    {
        return await _context.NamHocs
            .OrderByDescending(n => n.NgayBatDau)
            .ThenBy(n => n.MaNamHoc)
            .AsNoTracking()
            .ToListAsync();
    }

    // AsNoTracking: tránh lỗi "already being tracked" khi Edit đọc rồi cập nhật object khác cùng Id
    public async Task<NamHoc?> GetNamHocByIdAsync(int id)
    {
        return await _context.NamHocs
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == id);
    }

    public async Task<NamHoc?> GetNamHocHienTaiAsync()
    {
        return await _context.NamHocs
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.LaNamHienTai);
    }

    public async Task<NamHoc> CreateNamHocAsync(NamHoc namHoc)
    {
        namHoc.MaNamHoc = namHoc.MaNamHoc.Trim();
        namHoc.TenNamHoc = namHoc.TenNamHoc.Trim();
        namHoc.GhiChu = (namHoc.GhiChu ?? string.Empty).Trim();

        if (namHoc.LaNamHienTai)
        {
            await BoCoHienTaiAsync();
        }

        _context.NamHocs.Add(namHoc);
        await _context.SaveChangesAsync();
        return namHoc;
    }

    public async Task<bool> UpdateNamHocAsync(NamHoc namHoc)
    {
        var namHocDb = await _context.NamHocs.FirstOrDefaultAsync(n => n.Id == namHoc.Id);
        if (namHocDb == null)
        {
            return false;
        }

        if (namHoc.LaNamHienTai)
        {
            await BoCoHienTaiAsync(namHoc.Id);
        }

        namHocDb.MaNamHoc = namHoc.MaNamHoc.Trim();
        namHocDb.TenNamHoc = namHoc.TenNamHoc.Trim();
        namHocDb.NgayBatDau = namHoc.NgayBatDau;
        namHocDb.NgayKetThuc = namHoc.NgayKetThuc;
        namHocDb.LaNamHienTai = namHoc.LaNamHienTai;
        namHocDb.GhiChu = (namHoc.GhiChu ?? string.Empty).Trim();

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<(bool Success, string Message)> DatNamHienTaiAsync(int id)
    {
        var namHoc = await _context.NamHocs.FirstOrDefaultAsync(n => n.Id == id);
        if (namHoc == null)
        {
            return (false, "Không tìm thấy năm học");
        }

        if (namHoc.LaNamHienTai)
        {
            return (true, $"\"{namHoc.TenNamHoc}\" đang là năm học hiện tại");
        }

        await BoCoHienTaiAsync(id);
        namHoc.LaNamHienTai = true;
        await _context.SaveChangesAsync();
        return (true, $"Đã đặt \"{namHoc.TenNamHoc}\" làm năm học hiện tại");
    }

    public async Task<(bool Success, string Message)> DeleteNamHocAsync(int id)
    {
        var namHoc = await _context.NamHocs.FirstOrDefaultAsync(n => n.Id == id);
        if (namHoc == null)
        {
            return (false, "Không tìm thấy năm học cần xóa");
        }

        _context.NamHocs.Remove(namHoc);
        await _context.SaveChangesAsync();
        return (true, $"Đã xóa năm học \"{namHoc.TenNamHoc}\"");
    }

    public async Task<bool> MaNamHocExistsAsync(string maNamHoc, int? excludeId = null)
    {
        var ma = maNamHoc.Trim().ToLower();
        return await _context.NamHocs
            .AnyAsync(n => n.MaNamHoc.ToLower() == ma && (excludeId == null || n.Id != excludeId));
    }

    // Hai khoảng [a1,a2] và [b1,b2] giao nhau khi a1 <= b2 và b1 <= a2
    public async Task<NamHoc?> GetNamHocTrungThoiGianAsync(DateOnly batDau, DateOnly ketThuc, int? excludeId = null)
    {
        return await _context.NamHocs
            .AsNoTracking()
            .FirstOrDefaultAsync(n => (excludeId == null || n.Id != excludeId)
                                      && n.NgayBatDau <= ketThuc
                                      && batDau <= n.NgayKetThuc);
    }

    // Bỏ cờ "năm học hiện tại" ở tất cả năm học (trừ boQuaId) – chưa SaveChanges
    private async Task BoCoHienTaiAsync(int? boQuaId = null)
    {
        var dangHienTai = await _context.NamHocs
            .Where(n => n.LaNamHienTai && (boQuaId == null || n.Id != boQuaId))
            .ToListAsync();
        foreach (var item in dangHienTai)
        {
            item.LaNamHienTai = false;
        }
    }
}
