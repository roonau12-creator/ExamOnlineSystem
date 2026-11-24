using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Data;
using ExamOnlineSystem.Models;
using ExamOnlineSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ExamOnlineSystem.Business.Service;

public class CauHoiService : ICauHoiService
{
    private readonly ApplicationDbContext _context;

    public CauHoiService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<CauHoiListItemVM>> GetAllCauHoiAsync()
    {
        // Chiếu thẳng sang VM để chỉ truy vấn các cột cần dùng; số đáp án đếm bằng truy vấn con
        return await _context.CauHois
            .OrderBy(c => c.DanhMucCauHoi.MonHoc!.TenMonHoc)
            .ThenBy(c => c.Id)
            .Select(c => new CauHoiListItemVM
            {
                Id = c.Id,
                NoiDung = c.NoiDung,
                DanhMucCauHoiId = c.DanhMucCauHoiId,
                TenDanhMuc = c.DanhMucCauHoi.TenDanhMuc,
                MonHocId = c.DanhMucCauHoi.MonHocId,
                TenMonHoc = c.DanhMucCauHoi.MonHoc!.TenMonHoc,
                LoaiCauHoi = c.LoaiCauHoi,
                MucDo = c.MucDo,
                DiemMacDinh = c.DiemMacDinh,
                TenGiaoVien = c.GiaoVien.HoTen,
                SoDapAn = _context.DapAns.Count(d => d.CauHoiId == c.Id),
                CoHinhAnh = c.HinhAnh != null
            })
            .ToListAsync();
    }

    public async Task<CauHoi> CreateCauHoiAsync(CauHoi cauHoi, IEnumerable<DapAn> dapAns)
    {
        _context.CauHois.Add(cauHoi);
        var thuTu = 1;
        foreach (var dapAn in dapAns)
        {
            dapAn.CauHoi = cauHoi; // EF gán CauHoiId sau khi thêm câu hỏi
            dapAn.ThuTu = thuTu++;
            _context.DapAns.Add(dapAn);
        }
        await _context.SaveChangesAsync();
        return cauHoi;
    }

    public async Task<CauHoi?> GetCauHoiByIdAsync(int id)
    {
        return await _context.CauHois
            .Include(c => c.GiaoVien)
            .Include(c => c.DanhMucCauHoi)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<DapAn>> GetDapAnsAsync(int cauHoiId)
    {
        return await _context.DapAns
            .Where(d => d.CauHoiId == cauHoiId)
            .OrderBy(d => d.ThuTu)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<DapAnListItemVM>> GetAllDapAnAsync()
    {
        return await _context.DapAns
            .OrderBy(d => d.CauHoi.DanhMucCauHoi.MonHoc!.TenMonHoc)
            .ThenBy(d => d.CauHoiId)
            .ThenBy(d => d.ThuTu)
            .Select(d => new DapAnListItemVM
            {
                Id = d.Id,
                NoiDung = d.NoiDung,
                DapAnDung = d.DapAnDung,
                ThuTu = d.ThuTu,
                CauHoiId = d.CauHoiId,
                NoiDungCauHoi = d.CauHoi.NoiDung,
                MucDo = d.CauHoi.MucDo,
                DanhMucCauHoiId = d.CauHoi.DanhMucCauHoiId,
                TenMonHoc = d.CauHoi.DanhMucCauHoi.MonHoc!.TenMonHoc,
                TenGiaoVien = d.CauHoi.GiaoVien.HoTen
            })
            .ToListAsync();
    }

    public async Task<List<CauHoiLuaChonVM>> GetCauHoiTracNghiemAsync(int? giaoVienId)
    {
        return await _context.CauHois
            .Where(c => c.LoaiCauHoi == 1 && (giaoVienId == null || c.GiaoVienId == giaoVienId))
            .OrderBy(c => c.DanhMucCauHoi.MonHoc!.TenMonHoc)
            .ThenBy(c => c.Id)
            .Select(c => new CauHoiLuaChonVM
            {
                Id = c.Id,
                NoiDung = c.NoiDung,
                MonHocId = c.DanhMucCauHoi.MonHocId,
                SoDapAn = _context.DapAns.Count(d => d.CauHoiId == c.Id)
            })
            .ToListAsync();
    }

    public async Task<DapAn> AddDapAnAsync(DapAn dapAn)
    {
        var thuTuLonNhat = await _context.DapAns
            .Where(d => d.CauHoiId == dapAn.CauHoiId)
            .MaxAsync(d => (int?)d.ThuTu) ?? 0;
        dapAn.ThuTu = thuTuLonNhat + 1;
        _context.DapAns.Add(dapAn);
        await _context.SaveChangesAsync();
        return dapAn;
    }

    public async Task<DapAn?> GetDapAnByIdAsync(int id)
    {
        return await _context.DapAns.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task UpdateDapAnAsync(int id, string noiDung, bool dapAnDung)
    {
        var dapAn = await _context.DapAns.FirstOrDefaultAsync(d => d.Id == id)
                    ?? throw new KeyNotFoundException("Không tìm thấy đáp án");
        dapAn.NoiDung = noiDung;
        dapAn.DapAnDung = dapAnDung;
        await _context.SaveChangesAsync();
    }

    public async Task<Dictionary<int, List<DapAn>>> GetDapAnsTheoCauHoiAsync(IEnumerable<int> cauHoiIds)
    {
        var ids = cauHoiIds.Distinct().ToList();
        var ds = await _context.DapAns
            .Where(d => ids.Contains(d.CauHoiId))
            .OrderBy(d => d.ThuTu)
            .AsNoTracking()
            .ToListAsync();
        return ids.ToDictionary(id => id, id => ds.Where(d => d.CauHoiId == id).ToList());
    }

    public async Task<int> ImportDapAnAsync(List<DapAn> dapAns)
    {
        var ids = dapAns.Select(d => d.CauHoiId).Distinct().ToList();
        var thuTuLonNhat = await _context.DapAns
            .Where(d => ids.Contains(d.CauHoiId))
            .GroupBy(d => d.CauHoiId)
            .Select(g => new { CauHoiId = g.Key, Max = g.Max(d => d.ThuTu) })
            .ToDictionaryAsync(x => x.CauHoiId, x => x.Max);

        foreach (var d in dapAns)
        {
            var tiepTheo = thuTuLonNhat.GetValueOrDefault(d.CauHoiId) + 1;
            thuTuLonNhat[d.CauHoiId] = tiepTheo;
            d.ThuTu = tiepTheo;
            _context.DapAns.Add(d);
        }
        await _context.SaveChangesAsync();
        return dapAns.Count;
    }

    public async Task DeleteDapAnAsync(int id)
    {
        var dapAn = await _context.DapAns.FirstOrDefaultAsync(d => d.Id == id)
                    ?? throw new KeyNotFoundException("Không tìm thấy đáp án");
        _context.DapAns.Remove(dapAn);

        // Đánh lại thứ tự để nhãn A, B, C... liên tục
        var conLai = await _context.DapAns
            .Where(d => d.CauHoiId == dapAn.CauHoiId && d.Id != id)
            .OrderBy(d => d.ThuTu).ThenBy(d => d.Id)
            .ToListAsync();
        for (var i = 0; i < conLai.Count; i++)
        {
            conLai[i].ThuTu = i + 1;
        }
        await _context.SaveChangesAsync();
    }

    public async Task<Dictionary<int, List<string>>> GetDePhatHanhChuaCauAsync(IEnumerable<int> cauHoiIds)
    {
        var ids = cauHoiIds.Distinct().ToList();
        var ds = await _context.CauHoiDeThis
            .Where(c => ids.Contains(c.CauHoiId) && c.DeThi.DaPhatHanh)
            .Select(c => new { c.CauHoiId, c.DeThi.MaDeThi })
            .ToListAsync();
        return ds.GroupBy(x => x.CauHoiId)
                 .ToDictionary(g => g.Key, g => g.Select(x => x.MaDeThi).Distinct().OrderBy(m => m).ToList());
    }

    public async Task<int> CountDeThiDangDungAsync(int cauHoiId)
    {
        return await _context.CauHoiDeThis
            .Where(c => c.CauHoiId == cauHoiId)
            .Select(c => c.DeThiId)
            .Distinct()
            .CountAsync();
    }

    public async Task<List<CauHoiDeThi>> GetCauHoiDeThisAsync(int cauHoiId)
    {
        return await _context.CauHoiDeThis
            .Include(c => c.DeThi)
            .Where(c => c.CauHoiId == cauHoiId)
            .OrderBy(c => c.DeThi.MaDeThi)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task UpdateCauHoiAsync(CauHoi cauHoi, List<DapAn> dapAns)
    {
        var entity = await _context.CauHois.FirstOrDefaultAsync(c => c.Id == cauHoi.Id)
                     ?? throw new KeyNotFoundException("Không tìm thấy câu hỏi");

        // Giáo viên tạo câu hỏi giữ nguyên
        entity.DanhMucCauHoiId = cauHoi.DanhMucCauHoiId;
        entity.NoiDung = cauHoi.NoiDung;
        entity.LoaiCauHoi = cauHoi.LoaiCauHoi;
        entity.MucDo = cauHoi.MucDo;
        entity.DiemMacDinh = cauHoi.DiemMacDinh;
        entity.GiaiThich = cauHoi.GiaiThich;
        entity.HinhAnh = cauHoi.HinhAnh;

        var dapAnCu = await _context.DapAns.Where(d => d.CauHoiId == entity.Id).ToListAsync();
        var daDung = new HashSet<int>(); // đáp án cũ đã được cập nhật (bỏ qua Id lặp lại / không thuộc câu này)

        var thuTu = 1;
        foreach (var d in dapAns)
        {
            var cu = d.Id > 0 ? dapAnCu.FirstOrDefault(x => x.Id == d.Id) : null;
            if (cu != null && daDung.Add(cu.Id))
            {
                cu.NoiDung = d.NoiDung;
                cu.DapAnDung = d.DapAnDung;
                cu.ThuTu = thuTu++;
            }
            else
            {
                _context.DapAns.Add(new DapAn
                {
                    CauHoiId = entity.Id, NoiDung = d.NoiDung, DapAnDung = d.DapAnDung, ThuTu = thuTu++
                });
            }
        }

        // Đáp án cũ không còn trên form (hoặc chuyển sang tự luận) -> xóa
        _context.DapAns.RemoveRange(dapAnCu.Where(x => !daDung.Contains(x.Id)));

        await _context.SaveChangesAsync();
    }

    public async Task<HashSet<string>> GetCauHoiKeysAsync()
    {
        var ds = await _context.CauHois
            .Select(c => new { c.DanhMucCauHoiId, c.NoiDung })
            .ToListAsync();
        return ds.Select(c => ICauHoiService.TaoKhoa(c.DanhMucCauHoiId, c.NoiDung)).ToHashSet();
    }

    public async Task<int> ImportCauHoiAsync(List<CauHoiImportRow> rows, int giaoVienId)
    {
        foreach (var row in rows)
        {
            var cauHoi = new CauHoi
            {
                DanhMucCauHoiId = row.DanhMucCauHoiId,
                GiaoVienId = giaoVienId,
                NoiDung = row.NoiDung,
                LoaiCauHoi = row.LoaiCauHoi,
                MucDo = row.MucDo,
                DiemMacDinh = row.DiemMacDinh,
                GiaiThich = row.GiaiThich
            };
            _context.CauHois.Add(cauHoi);

            var thuTu = 1;
            foreach (var d in row.DapAns)
            {
                _context.DapAns.Add(new DapAn
                {
                    CauHoi = cauHoi, NoiDung = d.NoiDung, DapAnDung = d.DapAnDung, ThuTu = thuTu++
                });
            }
        }
        await _context.SaveChangesAsync();
        return rows.Count;
    }

    public async Task DeleteCauHoiAsync(int id)
    {
        var cauHoi = await _context.CauHois.FirstOrDefaultAsync(c => c.Id == id)
                     ?? throw new KeyNotFoundException("Không tìm thấy câu hỏi");

        // Xóa đáp án tường minh (không phụ thuộc vào cấu hình cascade của DB)
        _context.DapAns.RemoveRange(await _context.DapAns.Where(d => d.CauHoiId == id).ToListAsync());
        _context.CauHois.Remove(cauHoi);
        await _context.SaveChangesAsync();
    }

    public async Task<GiaoVien?> GetGiaoVienByUserIdAsync(string userId)
    {
        return await _context.GiaoViens
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.ApplicationUserId == userId);
    }

    public async Task<List<GiaoVien>> GetAllGiaoVienAsync()
    {
        return await _context.GiaoViens
            .OrderBy(g => g.HoTen)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> GiaoVienExistsAsync(int id)
    {
        return await _context.GiaoViens.AnyAsync(g => g.Id == id);
    }
}
