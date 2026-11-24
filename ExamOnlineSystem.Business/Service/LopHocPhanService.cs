using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Data;
using ExamOnlineSystem.Models;
using ExamOnlineSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ExamOnlineSystem.Business.Service;

public class LopHocPhanService : ILopHocPhanService
{
    private readonly ApplicationDbContext _context;

    public LopHocPhanService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<LopHocPhanListItemVM>> GetAllLopHocPhanAsync()
    {
        return await _context.LopHocPhans
            .OrderByDescending(l => l.NamHoc!.NgayBatDau)
            .ThenBy(l => l.HocKy)
            .ThenBy(l => l.MaLopHocPhan)
            .Select(l => new LopHocPhanListItemVM
            {
                Id = l.Id,
                MaLopHocPhan = l.MaLopHocPhan,
                TenLopHocPhan = l.TenLopHocPhan,
                HocPhanId = l.HocPhanId,
                MaHocPhan = l.HocPhan!.MaHocPhan,
                TenHocPhan = l.HocPhan!.TenHocPhan,
                TenMonHoc = l.HocPhan!.MonHoc!.TenMonHoc,
                TenKhoa = l.HocPhan!.Khoa!.TenKhoa,
                SoTinChi = l.HocPhan!.SoTinChi,
                NamHocId = l.NamHocId,
                MaNamHoc = l.NamHoc!.MaNamHoc,
                NamHienTai = l.NamHoc!.LaNamHienTai,
                HocKy = l.HocKy,
                SiSoToiDa = l.SiSoToiDa,
                SoSinhVien = l.LopHocPhanSinhViens.Count,
                SoGiaoVien = l.LopHocPhanGiaoViens.Count,
                GiaoVienPhuTrach = l.LopHocPhanGiaoViens
                    .Where(g => g.VaiTro == LopHocPhanVaiTro.PhuTrach)
                    .Select(g => g.GiaoVien!.HoTen)
                    .FirstOrDefault() ?? string.Empty,
                GhiChu = l.GhiChu ?? string.Empty
            })
            .AsNoTracking()
            .ToListAsync();
    }

    // AsNoTracking: tránh lỗi "already being tracked" khi Edit đọc rồi cập nhật object khác cùng Id
    public async Task<LopHocPhan?> GetLopHocPhanByIdAsync(int id)
    {
        return await _context.LopHocPhans
            .Include(l => l.HocPhan)!.ThenInclude(h => h!.MonHoc)
            .Include(l => l.HocPhan)!.ThenInclude(h => h!.Khoa)
            .Include(l => l.NamHoc)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<LopHocPhanDetailVM?> GetChiTietAsync(int id)
    {
        var lopHocPhan = await GetLopHocPhanByIdAsync(id);
        if (lopHocPhan == null)
        {
            return null;
        }

        return new LopHocPhanDetailVM
        {
            LopHocPhan = lopHocPhan,
            GiaoViens = await GetGiaoVienTrongLopAsync(id),
            SinhViens = await GetSinhVienTrongLopAsync(id)
        };
    }

    public async Task<LopHocPhan> CreateLopHocPhanAsync(LopHocPhan lopHocPhan)
    {
        lopHocPhan.MaLopHocPhan = lopHocPhan.MaLopHocPhan.Trim();
        lopHocPhan.TenLopHocPhan = lopHocPhan.TenLopHocPhan.Trim();
        lopHocPhan.GhiChu = (lopHocPhan.GhiChu ?? string.Empty).Trim();
        lopHocPhan.NgayTao = DateTime.UtcNow;
        // Chỉ lưu theo khóa ngoại, không để EF thêm học phần / năm học mới
        lopHocPhan.HocPhan = null;
        lopHocPhan.NamHoc = null;

        _context.LopHocPhans.Add(lopHocPhan);
        await _context.SaveChangesAsync();
        return lopHocPhan;
    }

    public async Task<bool> UpdateLopHocPhanAsync(LopHocPhan lopHocPhan)
    {
        var db = await _context.LopHocPhans.FirstOrDefaultAsync(l => l.Id == lopHocPhan.Id);
        if (db == null)
        {
            return false;
        }

        db.MaLopHocPhan = lopHocPhan.MaLopHocPhan.Trim();
        db.TenLopHocPhan = lopHocPhan.TenLopHocPhan.Trim();
        db.HocPhanId = lopHocPhan.HocPhanId;
        db.NamHocId = lopHocPhan.NamHocId;
        db.HocKy = lopHocPhan.HocKy;
        db.SiSoToiDa = lopHocPhan.SiSoToiDa;
        db.GhiChu = (lopHocPhan.GhiChu ?? string.Empty).Trim();

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<(bool Success, string Message)> DeleteLopHocPhanAsync(int id)
    {
        var lopHocPhan = await _context.LopHocPhans.FirstOrDefaultAsync(l => l.Id == id);
        if (lopHocPhan == null)
        {
            return (false, "Không tìm thấy lớp học phần cần xóa");
        }

        var soSinhVien = await DemSinhVienAsync(id);
        if (soSinhVien > 0)
        {
            return (false, $"Không thể xóa lớp học phần \"{lopHocPhan.MaLopHocPhan}\" vì còn {soSinhVien} sinh viên. " +
                           "Hãy bỏ sinh viên khỏi lớp trước.");
        }

        _context.LopHocPhans.Remove(lopHocPhan);
        await _context.SaveChangesAsync();
        return (true, $"Đã xóa lớp học phần \"{lopHocPhan.MaLopHocPhan}\"");
    }

    public async Task<bool> MaLopHocPhanExistsAsync(string maLopHocPhan, int? excludeId = null)
    {
        var ma = maLopHocPhan.Trim().ToLower();
        return await _context.LopHocPhans
            .AnyAsync(l => l.MaLopHocPhan.ToLower() == ma && (excludeId == null || l.Id != excludeId));
    }

    public async Task<int> DemSinhVienAsync(int lopHocPhanId)
    {
        return await _context.LopHocPhanSinhViens.CountAsync(x => x.LopHocPhanId == lopHocPhanId);
    }

    // ===== Phân công giảng viên =====

    public async Task<List<GiaoVienPhanCongVM>> GetGiaoVienTrongLopAsync(int lopHocPhanId)
    {
        return await _context.LopHocPhanGiaoViens
            .Where(x => x.LopHocPhanId == lopHocPhanId)
            .OrderBy(x => x.VaiTro)
            .ThenBy(x => x.GiaoVien!.MaGiaoVien)
            .Select(x => new GiaoVienPhanCongVM
            {
                Id = x.GiaoVienId,
                MaGiaoVien = x.GiaoVien!.MaGiaoVien,
                HoTen = x.GiaoVien!.HoTen,
                Email = x.GiaoVien!.ApplicationUser!.Email ?? "",
                BoMonId = x.GiaoVien!.BoMonId,
                TenBoMon = x.GiaoVien!.BoMon!.TenBoMon,
                KhoaId = x.GiaoVien!.BoMon!.KhoaId,
                TenKhoa = x.GiaoVien!.BoMon!.Khoa!.TenKhoa,
                VaiTro = x.VaiTro,
                SoLopHocPhan = _context.LopHocPhanGiaoViens.Count(g => g.GiaoVienId == x.GiaoVienId)
            })
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<GiaoVienPhanCongVM>> GetGiaoVienNgoaiLopAsync(int lopHocPhanId)
    {
        var daPhanCong = _context.LopHocPhanGiaoViens
            .Where(x => x.LopHocPhanId == lopHocPhanId)
            .Select(x => x.GiaoVienId);

        return await _context.GiaoViens
            .Where(g => !daPhanCong.Contains(g.Id))
            .OrderBy(g => g.BoMon!.Khoa!.SoThuTu)
            .ThenBy(g => g.BoMon!.SoThuTu)
            .ThenBy(g => g.MaGiaoVien)
            .Select(g => new GiaoVienPhanCongVM
            {
                Id = g.Id,
                MaGiaoVien = g.MaGiaoVien,
                HoTen = g.HoTen,
                Email = g.ApplicationUser!.Email ?? "",
                BoMonId = g.BoMonId,
                TenBoMon = g.BoMon!.TenBoMon,
                KhoaId = g.BoMon!.KhoaId,
                TenKhoa = g.BoMon!.Khoa!.TenKhoa,
                VaiTro = 0,
                SoLopHocPhan = _context.LopHocPhanGiaoViens.Count(x => x.GiaoVienId == g.Id)
            })
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<PhanCongResult> PhanCongGiaoVienAsync(int lopHocPhanId, IEnumerable<int> giaoVienIds, int vaiTro)
    {
        if (vaiTro != LopHocPhanVaiTro.PhuTrach && vaiTro != LopHocPhanVaiTro.TroGiang)
        {
            return PhanCongResult.Loi("Vai trò không hợp lệ");
        }

        var lopHocPhan = await _context.LopHocPhans.FirstOrDefaultAsync(l => l.Id == lopHocPhanId);
        if (lopHocPhan == null)
        {
            return PhanCongResult.Loi("Không tìm thấy lớp học phần");
        }

        var ids = giaoVienIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return PhanCongResult.Loi("Chưa chọn giảng viên nào");
        }

        var idHopLe = await _context.GiaoViens.Where(g => ids.Contains(g.Id)).Select(g => g.Id).ToListAsync();
        var daCo = await _context.LopHocPhanGiaoViens
            .Where(x => x.LopHocPhanId == lopHocPhanId && ids.Contains(x.GiaoVienId))
            .Select(x => x.GiaoVienId)
            .ToListAsync();

        var canThem = idHopLe.Except(daCo).ToList();
        if (canThem.Count == 0)
        {
            return PhanCongResult.Loi("Các giảng viên đã chọn đều đã có trong lớp học phần");
        }

        // Tối đa 1 người phụ trách
        if (vaiTro == LopHocPhanVaiTro.PhuTrach)
        {
            if (canThem.Count > 1)
            {
                return PhanCongResult.Loi("Mỗi lớp học phần chỉ có 1 giảng viên phụ trách. " +
                                          "Hãy phân công 1 người phụ trách, những người còn lại là trợ giảng.");
            }

            var phuTrachCu = await _context.LopHocPhanGiaoViens
                .Include(x => x.GiaoVien)
                .FirstOrDefaultAsync(x => x.LopHocPhanId == lopHocPhanId && x.VaiTro == LopHocPhanVaiTro.PhuTrach);
            if (phuTrachCu != null)
            {
                return PhanCongResult.Loi(
                    $"Lớp học phần đã có giảng viên phụ trách \"{phuTrachCu.GiaoVien?.HoTen}\". " +
                    "Hãy đổi vai trò của giảng viên đó thành trợ giảng trước, hoặc phân công người mới làm trợ giảng.");
            }
        }

        foreach (var giaoVienId in canThem)
        {
            _context.LopHocPhanGiaoViens.Add(new LopHocPhanGiaoVien
            {
                LopHocPhanId = lopHocPhanId,
                GiaoVienId = giaoVienId,
                VaiTro = vaiTro,
                NgayPhanCong = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        var kq = PhanCongResult.Ok($"Đã phân công {canThem.Count} giảng viên " +
                                   $"({(vaiTro == LopHocPhanVaiTro.PhuTrach ? "phụ trách" : "trợ giảng")})");
        var boQua = ids.Count - canThem.Count;
        if (boQua > 0)
        {
            kq.CanhBao.Add($"Bỏ qua {boQua} giảng viên vì đã có trong lớp học phần hoặc không còn tồn tại.");
        }
        return kq;
    }

    public async Task<PhanCongResult> DoiVaiTroGiaoVienAsync(int lopHocPhanId, int giaoVienId, int vaiTro)
    {
        if (vaiTro != LopHocPhanVaiTro.PhuTrach && vaiTro != LopHocPhanVaiTro.TroGiang)
        {
            return PhanCongResult.Loi("Vai trò không hợp lệ");
        }

        var dong = await _context.LopHocPhanGiaoViens
            .Include(x => x.GiaoVien)
            .FirstOrDefaultAsync(x => x.LopHocPhanId == lopHocPhanId && x.GiaoVienId == giaoVienId);
        if (dong == null)
        {
            return PhanCongResult.Loi("Giảng viên này chưa được phân công vào lớp học phần");
        }

        if (dong.VaiTro == vaiTro)
        {
            return PhanCongResult.Ok($"\"{dong.GiaoVien?.HoTen}\" đang là {TenVaiTro(vaiTro)}");
        }

        var kq = PhanCongResult.Ok($"Đã đặt \"{dong.GiaoVien?.HoTen}\" làm {TenVaiTro(vaiTro)}");

        // Đặt làm phụ trách: người phụ trách cũ chuyển thành trợ giảng để luôn chỉ có 1 người phụ trách
        if (vaiTro == LopHocPhanVaiTro.PhuTrach)
        {
            var phuTrachCu = await _context.LopHocPhanGiaoViens
                .Include(x => x.GiaoVien)
                .Where(x => x.LopHocPhanId == lopHocPhanId
                            && x.VaiTro == LopHocPhanVaiTro.PhuTrach
                            && x.GiaoVienId != giaoVienId)
                .ToListAsync();
            foreach (var cu in phuTrachCu)
            {
                cu.VaiTro = LopHocPhanVaiTro.TroGiang;
                kq.CanhBao.Add($"\"{cu.GiaoVien?.HoTen}\" đã chuyển thành trợ giảng.");
            }
        }

        dong.VaiTro = vaiTro;
        await _context.SaveChangesAsync();
        return kq;
    }

    public async Task<PhanCongResult> BoGiaoVienAsync(int lopHocPhanId, IEnumerable<int> giaoVienIds)
    {
        var ids = giaoVienIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return PhanCongResult.Loi("Chưa chọn giảng viên nào");
        }

        var dongs = await _context.LopHocPhanGiaoViens
            .Include(x => x.GiaoVien)
            .Where(x => x.LopHocPhanId == lopHocPhanId && ids.Contains(x.GiaoVienId))
            .ToListAsync();
        if (dongs.Count == 0)
        {
            return PhanCongResult.Loi("Các giảng viên đã chọn không có trong lớp học phần");
        }

        var coPhuTrach = dongs.Any(x => x.VaiTro == LopHocPhanVaiTro.PhuTrach);
        _context.LopHocPhanGiaoViens.RemoveRange(dongs);
        await _context.SaveChangesAsync();

        var kq = PhanCongResult.Ok($"Đã bỏ phân công {dongs.Count} giảng viên");
        if (coPhuTrach)
        {
            kq.CanhBao.Add("Lớp học phần hiện chưa có giảng viên phụ trách.");
        }
        return kq;
    }

    // ===== Phân sinh viên =====

    public async Task<List<SinhVienChonVM>> GetSinhVienTrongLopAsync(int lopHocPhanId)
    {
        var daCo = _context.LopHocPhanSinhViens
            .Where(x => x.LopHocPhanId == lopHocPhanId)
            .Select(x => x.SinhVienId);

        return await DongChon(_context.SinhViens.Where(s => daCo.Contains(s.Id)))
            .OrderBy(s => s.MaSinhVien)
            .ToListAsync();
    }

    public async Task<List<SinhVienChonVM>> GetSinhVienNgoaiLopAsync(int lopHocPhanId)
    {
        var daCo = _context.LopHocPhanSinhViens
            .Where(x => x.LopHocPhanId == lopHocPhanId)
            .Select(x => x.SinhVienId);

        return await DongChon(_context.SinhViens.Where(s => !daCo.Contains(s.Id)))
            .OrderBy(s => s.TenKhoa)
            .ThenBy(s => s.MaLop)
            .ThenBy(s => s.MaSinhVien)
            .ToListAsync();
    }

    public async Task<PhanCongResult> ThemSinhVienAsync(int lopHocPhanId, IEnumerable<int> sinhVienIds)
    {
        var lopHocPhan = await _context.LopHocPhans.FirstOrDefaultAsync(l => l.Id == lopHocPhanId);
        if (lopHocPhan == null)
        {
            return PhanCongResult.Loi("Không tìm thấy lớp học phần");
        }

        var ids = sinhVienIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return PhanCongResult.Loi("Chưa chọn sinh viên nào");
        }

        var idHopLe = await _context.SinhViens.Where(s => ids.Contains(s.Id)).Select(s => s.Id).ToListAsync();
        var daCo = await _context.LopHocPhanSinhViens
            .Where(x => x.LopHocPhanId == lopHocPhanId && ids.Contains(x.SinhVienId))
            .Select(x => x.SinhVienId)
            .ToListAsync();

        var canThem = idHopLe.Except(daCo).ToList();
        if (canThem.Count == 0)
        {
            return PhanCongResult.Loi("Các sinh viên đã chọn đều đã có trong lớp học phần");
        }

        // Không vượt sĩ số tối đa
        var hienCo = await DemSinhVienAsync(lopHocPhanId);
        var conLai = lopHocPhan.SiSoToiDa - hienCo;
        if (canThem.Count > conLai)
        {
            return PhanCongResult.Loi(
                $"Vượt sĩ số tối đa: lớp học phần \"{lopHocPhan.MaLopHocPhan}\" có {hienCo}/{lopHocPhan.SiSoToiDa} sinh viên, " +
                $"chỉ còn {Math.Max(conLai, 0)} suất nhưng đang chọn {canThem.Count} sinh viên. " +
                "Hãy chọn ít hơn hoặc tăng sĩ số tối đa của lớp học phần.");
        }

        foreach (var sinhVienId in canThem)
        {
            _context.LopHocPhanSinhViens.Add(new LopHocPhanSinhVien
            {
                LopHocPhanId = lopHocPhanId,
                SinhVienId = sinhVienId,
                NgayDangKy = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();

        var kq = PhanCongResult.Ok($"Đã thêm {canThem.Count} sinh viên vào lớp học phần " +
                                   $"({hienCo + canThem.Count}/{lopHocPhan.SiSoToiDa})");
        var boQua = ids.Count - canThem.Count;
        if (boQua > 0)
        {
            kq.CanhBao.Add($"Bỏ qua {boQua} sinh viên vì đã có trong lớp học phần hoặc không còn tồn tại.");
        }
        return kq;
    }

    public async Task<PhanCongResult> BoSinhVienAsync(int lopHocPhanId, IEnumerable<int> sinhVienIds)
    {
        var ids = sinhVienIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return PhanCongResult.Loi("Chưa chọn sinh viên nào");
        }

        var dongs = await _context.LopHocPhanSinhViens
            .Where(x => x.LopHocPhanId == lopHocPhanId && ids.Contains(x.SinhVienId))
            .ToListAsync();
        if (dongs.Count == 0)
        {
            return PhanCongResult.Loi("Các sinh viên đã chọn không có trong lớp học phần");
        }

        _context.LopHocPhanSinhViens.RemoveRange(dongs);
        await _context.SaveChangesAsync();
        return PhanCongResult.Ok($"Đã bỏ {dongs.Count} sinh viên khỏi lớp học phần");
    }

    private static string TenVaiTro(int vaiTro) =>
        vaiTro == LopHocPhanVaiTro.PhuTrach ? "giảng viên phụ trách" : "trợ giảng";

    // Chiếu sinh viên sang dòng chọn (kèm lớp, khoa) – giữ nguyên dạng IQueryable để EF dịch được sang SQL
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
