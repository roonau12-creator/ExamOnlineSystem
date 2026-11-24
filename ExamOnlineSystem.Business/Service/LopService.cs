using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Data;
using ExamOnlineSystem.Models;
using ExamOnlineSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ExamOnlineSystem.Business.Service;

public class LopService : ILopService
{
    private readonly ApplicationDbContext _context;

    public LopService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Lop>> GetAllLopAsync()
    {
        return await _context.Lops
            .Include(l => l.Khoa)
            .OrderBy(l => l.Khoa!.SoThuTu)
            .ThenBy(l => l.MaLop)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Lop?> GetLopByIdAsync(int id)
    {
        return await _context.Lops
            .Include(l => l.Khoa)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<List<SinhVien>> GetSinhViensByLopAsync(int lopId)
    {
        return await _context.SinhViens
            .Where(s => s.LopId == lopId)
            .OrderBy(s => s.MaSinhVien)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Lop> CreateLopAsync(Lop lop)
    {
        lop.MaLop = lop.MaLop.Trim();
        lop.TenLop = lop.TenLop.Trim();
        lop.Khoa = null; // chỉ lưu theo KhoaId, không để EF thêm Khoa mới
        _context.Lops.Add(lop);
        await _context.SaveChangesAsync();
        return lop;
    }

    public async Task<bool> UpdateLopAsync(Lop lop)
    {
        var lopDb = await _context.Lops.FirstOrDefaultAsync(l => l.Id == lop.Id);
        if (lopDb == null)
        {
            return false;
        }

        var doiKhoa = lopDb.KhoaId != lop.KhoaId;
        lopDb.MaLop = lop.MaLop.Trim();
        lopDb.TenLop = lop.TenLop.Trim();
        lopDb.KhoaId = lop.KhoaId;

        if (doiKhoa)
        {
            // Giữ khoa của sinh viên khớp với khoa của lớp
            var sinhViens = await _context.SinhViens.Where(s => s.LopId == lop.Id).ToListAsync();
            foreach (var sv in sinhViens)
            {
                sv.KhoaId = lop.KhoaId;
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<(bool Success, string Message)> DeleteLopAsync(int id)
    {
        var lop = await _context.Lops.FirstOrDefaultAsync(l => l.Id == id);
        if (lop == null)
        {
            return (false, "Không tìm thấy lớp cần xóa");
        }

        var soSinhVien = await _context.SinhViens.CountAsync(s => s.LopId == id);
        if (soSinhVien > 0)
        {
            return (false, $"Không thể xóa lớp \"{lop.MaLop}\" vì lớp còn {soSinhVien} sinh viên. Hãy chuyển hoặc xóa sinh viên trước.");
        }

        _context.Lops.Remove(lop);
        await _context.SaveChangesAsync();
        return (true, $"Đã xóa lớp \"{lop.MaLop}\"");
    }

    public async Task<bool> MaLopExistsAsync(string maLop, int? excludeId = null)
    {
        var ma = maLop.Trim().ToLower();
        return await _context.Lops.AnyAsync(l => l.MaLop.ToLower() == ma && (excludeId == null || l.Id != excludeId));
    }

    public async Task<Dictionary<int, int>> CountSinhVienByLopAsync()
    {
        return await _context.SinhViens
            .GroupBy(s => s.LopId)
            .Select(g => new { LopId = g.Key, SoLuong = g.Count() })
            .ToDictionaryAsync(x => x.LopId, x => x.SoLuong);
    }

    public async Task<List<SinhVienChonVM>> GetSinhVienChonByLopAsync(int lopId)
    {
        return await DongChon(_context.SinhViens.Where(s => s.LopId == lopId))
            .OrderBy(s => s.MaSinhVien)
            .ToListAsync();
    }

    public async Task<List<SinhVienChonVM>> GetSinhVienNgoaiLopAsync(int lopId)
    {
        return await DongChon(_context.SinhViens.Where(s => s.LopId != lopId))
            .OrderBy(s => s.TenKhoa)
            .ThenBy(s => s.MaLop)
            .ThenBy(s => s.MaSinhVien)
            .ToListAsync();
    }

    public async Task<List<LopChonVM>> GetLopKhacAsync(int lopId)
    {
        return await _context.Lops
            .Where(l => l.Id != lopId)
            .OrderBy(l => l.Khoa!.SoThuTu)
            .ThenBy(l => l.MaLop)
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

    public async Task<PhanLopResult> ChuyenSinhVienVaoLopAsync(int lopDichId, IEnumerable<int> sinhVienIds)
    {
        var ids = (sinhVienIds ?? Enumerable.Empty<int>()).Where(i => i > 0).Distinct().ToList();
        if (ids.Count == 0)
        {
            return PhanLopResult.Loi("Chưa chọn sinh viên nào");
        }

        var lopDich = await _context.Lops
            .Include(l => l.Khoa)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == lopDichId);
        if (lopDich == null)
        {
            return PhanLopResult.Loi("Lớp cần phân sinh viên vào không còn tồn tại");
        }

        var sinhViens = await _context.SinhViens.Where(s => ids.Contains(s.Id)).ToListAsync();
        if (sinhViens.Count == 0)
        {
            return PhanLopResult.Loi("Không tìm thấy sinh viên nào trong số đã chọn");
        }

        var canChuyen = sinhViens.Where(s => s.LopId != lopDichId).ToList();
        if (canChuyen.Count == 0)
        {
            return PhanLopResult.Loi($"Các sinh viên đã chọn đều đã thuộc lớp \"{lopDich.MaLop}\"");
        }

        var kq = new PhanLopResult { Success = true };

        // Sinh viên không tìm thấy / đã ở trong lớp đích -> báo lại cho người dùng
        var khongThay = ids.Count - sinhViens.Count;
        if (khongThay > 0)
        {
            kq.CanhBao.Add($"Bỏ qua {khongThay} sinh viên không còn tồn tại (có thể vừa bị xóa).");
        }

        var daOTrongLop = sinhViens.Count - canChuyen.Count;
        if (daOTrongLop > 0)
        {
            kq.CanhBao.Add($"Bỏ qua {daOTrongLop} sinh viên đã thuộc lớp \"{lopDich.MaLop}\".");
        }

        // Khoa của sinh viên luôn đi theo khoa của lớp (giống UpdateLopAsync / SinhVienService)
        var doiKhoa = canChuyen.Where(s => s.KhoaId != lopDich.KhoaId).ToList();

        // Suất dự thi đã tạo theo lớp cũ không tự mất khi đổi lớp -> cảnh báo để người dùng kiểm tra lịch thi
        var idsChuyen = canChuyen.Select(s => s.Id).ToList();
        var bayGio = DateTime.UtcNow;
        var vuongLichThi = await _context.SinhVienDuThis
            .Where(d => idsChuyen.Contains(d.SinhVienId)
                        && d.LichThi!.ThoiGianKetThuc >= bayGio
                        && !d.LichThi.LichThiLops.Any(x => x.LopId == lopDichId))
            .Select(d => d.SinhVienId)
            .Distinct()
            .CountAsync();

        await using var transaction = await _context.Database.BeginTransactionAsync();

        foreach (var sv in canChuyen)
        {
            sv.LopId = lopDich.Id;
            sv.KhoaId = lopDich.KhoaId;
        }

        if (doiKhoa.Count > 0)
        {
            // Giữ khoa công tác trên tài khoản đăng nhập khớp với khoa mới
            var userIds = doiKhoa
                .Select(s => s.ApplicationUserId)
                .Where(uid => !string.IsNullOrEmpty(uid))
                .ToList();
            var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
            foreach (var u in users)
            {
                u.KhoaId = lopDich.KhoaId;
            }
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        kq.Message = $"Đã phân {canChuyen.Count} sinh viên vào lớp \"{lopDich.MaLop}\"";
        if (doiKhoa.Count > 0)
        {
            kq.CanhBao.Add($"{doiKhoa.Count} sinh viên đã đổi khoa sang \"{lopDich.Khoa?.TenKhoa}\" theo khoa của lớp mới.");
        }
        if (vuongLichThi > 0)
        {
            kq.CanhBao.Add($"{vuongLichThi} sinh viên vẫn còn suất dự thi ở lịch thi chưa kết thúc của lớp cũ. " +
                           "Hãy mở lại các lịch thi đó để kiểm tra danh sách dự thi.");
        }
        return kq;
    }

    // Chiếu SinhVien sang dòng chọn (kèm lớp + khoa) – dùng chung cho hai danh sách của trang phân lớp
    private static IQueryable<SinhVienChonVM> DongChon(IQueryable<SinhVien> query)
    {
        return query
            .AsNoTracking()
            .Select(s => new SinhVienChonVM
            {
                Id = s.Id,
                MaSinhVien = s.MaSinhVien,
                HoTen = s.HoTen,
                NgaySinh = s.NgaySinh,
                GioiTinh = s.GioiTinh,
                Email = s.Email,
                LopId = s.LopId,
                MaLop = s.Lop!.MaLop,
                TenLop = s.Lop.TenLop,
                KhoaId = s.KhoaId,
                TenKhoa = s.Khoa!.TenKhoa
            });
    }
}
