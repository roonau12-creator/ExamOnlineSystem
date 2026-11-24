using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Data;
using ExamOnlineSystem.Models;
using ExamOnlineSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ExamOnlineSystem.Business.Service;

public class DanhMucCauHoiService : IDanhMucCauHoiService
{
    private readonly ApplicationDbContext _context;

    public DanhMucCauHoiService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DanhMucCauHoi>> GetAllDanhMucCauHoiAsync()
    {
        return await _context.DanhMucCauHois
            .Include(d => d.MonHoc)
            .Include(d => d.DanhMucCha)
            .OrderBy(d => d.MonHoc!.TenMonHoc)
            .ThenBy(d => d.TenDanhMuc)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<DanhMucCauHoi?> GetDanhMucCauHoiByIdAsync(int id)
    {
        return await _context.DanhMucCauHois
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<List<DanhMucCauHoiNode>> GetDanhMucTreeAsync()
    {
        // Đã sắp theo môn học rồi tên danh mục nên các nút con giữ thứ tự tên
        var all = (await GetAllDanhMucCauHoiAsync()).ToList();
        var ids = all.Select(d => d.Id).ToHashSet();
        var childrenOf = all.Where(d => d.DanhMucChaId != null).ToLookup(d => d.DanhMucChaId!.Value);

        var result = new List<DanhMucCauHoiNode>();
        var visited = new HashSet<int>(); // phòng dữ liệu lỗi có vòng lặp cha-con

        void Duyet(DanhMucCauHoi dm, List<DanhMucCauHoi> toTien)
        {
            if (!visited.Add(dm.Id))
            {
                return;
            }
            result.Add(new DanhMucCauHoiNode { DanhMuc = dm, Cap = toTien.Count + 1, ToTien = toTien });
            var toTienCuaCon = new List<DanhMucCauHoi>(toTien) { dm };
            foreach (var con in childrenOf[dm.Id])
            {
                Duyet(con, toTienCuaCon);
            }
        }

        foreach (var goc in all.Where(d => d.DanhMucChaId == null || !ids.Contains(d.DanhMucChaId.Value)))
        {
            Duyet(goc, new List<DanhMucCauHoi>());
        }
        return result;
    }

    public async Task<DanhMucCauHoi?> GetDanhMucCauHoiDetailAsync(int id)
    {
        return await _context.DanhMucCauHois
            .Include(d => d.MonHoc)
            .Include(d => d.DanhMucCha)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<List<DanhMucCauHoi>> GetDanhMucConAsync(int id)
    {
        return await _context.DanhMucCauHois
            .Where(d => d.DanhMucChaId == id)
            .OrderBy(d => d.TenDanhMuc)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<CauHoi>> GetCauHoisByDanhMucAsync(int id)
    {
        return await _context.CauHois
            .Include(c => c.GiaoVien)
            .Where(c => c.DanhMucCauHoiId == id)
            .OrderBy(c => c.Id)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<DanhMucCauHoi> CreateDanhMucCauHoiAsync(DanhMucCauHoi danhMuc)
    {
        // chỉ thêm danh mục, không đụng tới bản ghi môn học / danh mục cha
        danhMuc.MonHoc = null;
        danhMuc.DanhMucCha = null;
        danhMuc.MoTa ??= string.Empty;
        _context.DanhMucCauHois.Add(danhMuc);
        await _context.SaveChangesAsync();
        return danhMuc;
    }

    public async Task UpdateDanhMucCauHoiAsync(DanhMucCauHoi danhMuc)
    {
        // chỉ cập nhật danh mục, không đụng tới bản ghi môn học / danh mục cha
        danhMuc.MonHoc = null;
        danhMuc.DanhMucCha = null;
        danhMuc.MoTa ??= string.Empty;
        _context.DanhMucCauHois.Update(danhMuc);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.DanhMucCauHois.AnyAsync(d => d.Id == id);
    }

    public async Task<bool> HasChildrenAsync(int id)
    {
        return await _context.DanhMucCauHois.AnyAsync(d => d.DanhMucChaId == id);
    }

    public async Task<bool> HasCauHoiAsync(int id)
    {
        return await _context.CauHois.AnyAsync(c => c.DanhMucCauHoiId == id);
    }

    public async Task DeleteDanhMucCauHoiAsync(int id)
    {
        var danhMuc = await _context.DanhMucCauHois.FirstOrDefaultAsync(d => d.Id == id);
        if (danhMuc == null)
        {
            throw new KeyNotFoundException("Không tìm thấy danh mục câu hỏi");
        }
        _context.DanhMucCauHois.Remove(danhMuc);
        await _context.SaveChangesAsync();
    }

    public async Task<HashSet<int>> GetSelfAndDescendantIdsAsync(int id)
    {
        // Lấy toàn bộ cặp (Id, ChaId) một lần rồi duyệt trong bộ nhớ
        var links = await _context.DanhMucCauHois
            .Where(d => d.DanhMucChaId != null)
            .Select(d => new { d.Id, ChaId = d.DanhMucChaId!.Value })
            .ToListAsync();
        var childrenOf = links.ToLookup(l => l.ChaId, l => l.Id);

        var result = new HashSet<int> { id };
        var queue = new Queue<int>();
        queue.Enqueue(id);
        while (queue.Count > 0)
        {
            foreach (var child in childrenOf[queue.Dequeue()])
            {
                if (result.Add(child))
                {
                    queue.Enqueue(child);
                }
            }
        }
        return result;
    }

    public async Task<DanhMucCauHoiImportResult> ImportDanhMucCauHoiAsync(List<DanhMucCauHoiImportRow> rows)
    {
        var result = new DanhMucCauHoiImportResult();

        // pool = danh mục đã có (được theo dõi để cập nhật) + danh mục mới thêm trong lần nhập này
        var pool = await _context.DanhMucCauHois.ToListAsync();
        var moiThem = new HashSet<DanhMucCauHoi>();

        // d có phải con trực tiếp của cha không (cha == null: d là danh mục gốc).
        // Danh mục mới chưa có Id nên so sánh qua navigation DanhMucCha.
        static bool LaCon(DanhMucCauHoi d, DanhMucCauHoi? cha) =>
            cha == null
                ? d.DanhMucChaId == null && d.DanhMucCha == null
                : ReferenceEquals(d.DanhMucCha, cha) || (cha.Id != 0 && d.DanhMucChaId == cha.Id);

        DanhMucCauHoi? Tim(int monHocId, DanhMucCauHoi? cha, string ten) =>
            pool.FirstOrDefault(d => d.MonHocId == monHocId && LaCon(d, cha) &&
                                     d.TenDanhMuc.Equals(ten, StringComparison.OrdinalIgnoreCase));

        // Xử lý cấp nông trước để danh mục cha có trong file được tạo trước danh mục con của nó
        foreach (var row in rows.OrderBy(r => r.DuongDanCha.Count).ThenBy(r => r.Dong))
        {
            DanhMucCauHoi? cha = null;
            var timThayCha = true;
            foreach (var ten in row.DuongDanCha)
            {
                cha = Tim(row.MonHocId, cha, ten);
                if (cha == null)
                {
                    result.Errors.Add($"Dòng {row.Dong}: không tìm thấy danh mục cha \"{string.Join(" > ", row.DuongDanCha)}\" trong môn học này");
                    timThayCha = false;
                    break;
                }
            }
            if (!timThayCha)
            {
                continue;
            }

            var danhMuc = Tim(row.MonHocId, cha, row.TenDanhMuc);
            if (danhMuc == null)
            {
                danhMuc = new DanhMucCauHoi
                {
                    TenDanhMuc = row.TenDanhMuc,
                    MonHocId = row.MonHocId,
                    MoTa = row.MoTa,
                    DanhMucCha = cha
                };
                _context.DanhMucCauHois.Add(danhMuc);
                pool.Add(danhMuc);
                moiThem.Add(danhMuc);
                result.Added++;
            }
            else
            {
                danhMuc.MoTa = row.MoTa;
                if (!moiThem.Contains(danhMuc))
                {
                    result.Updated++;
                }
            }
        }

        if (result.Errors.Count > 0)
        {
            _context.ChangeTracker.Clear(); // bỏ mọi thay đổi, không nhập dòng nào
            result.Added = result.Updated = 0;
            return result;
        }

        await _context.SaveChangesAsync();
        return result;
    }

    public async Task<IEnumerable<MonHoc>> GetAllMonHocAsync()
    {
        return await _context.MonHocs
            .OrderBy(m => m.SoThuTu)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> MonHocExistsAsync(int monHocId)
    {
        return await _context.MonHocs.AnyAsync(m => m.Id == monHocId);
    }
}
