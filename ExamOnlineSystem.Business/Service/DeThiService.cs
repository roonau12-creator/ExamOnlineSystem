using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Data;
using ExamOnlineSystem.Models;
using ExamOnlineSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ExamOnlineSystem.Business.Service;

public class DeThiService : IDeThiService
{
    private readonly ApplicationDbContext _context;

    public DeThiService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<DeThiListItemVM>> GetAllDeThiAsync()
    {
        // Chiếu thẳng sang VM; số câu và tổng điểm các câu tính bằng truy vấn con
        return await _context.DeThis
            .OrderBy(d => d.MonHoc.TenMonHoc)
            .ThenBy(d => d.MaDeThi)
            .Select(d => new DeThiListItemVM
            {
                Id = d.Id,
                MaDeThi = d.MaDeThi,
                TenDeThi = d.TenDeThi,
                MonHocId = d.MonHocId,
                TenMonHoc = d.MonHoc.TenMonHoc,
                LoaiDeThi = d.LoaiDeThi,
                ThoiLuong = d.ThoiLuong,
                TongDiem = d.TongDiem,
                SoCauHoi = _context.CauHoiDeThis.Count(c => c.DeThiId == d.Id),
                TongDiemCauHoi = _context.CauHoiDeThis.Where(c => c.DeThiId == d.Id).Sum(c => (double?)c.Diem) ?? 0,
                GiaoVienId = d.GiaoVienId,
                TenGiaoVien = d.GiaoVien.HoTen,
                DaPhatHanh = d.DaPhatHanh,
                NgayPhatHanh = d.NgayPhatHanh
            })
            .ToListAsync();
    }

    public async Task<bool> MaDeThiExistsAsync(string maDeThi, int? boQuaId = null)
    {
        var ma = maDeThi.Trim().ToLower();
        return await _context.DeThis.AnyAsync(d => d.MaDeThi.ToLower() == ma && d.Id != boQuaId);
    }

    public Task<List<CauHoiChonVaoDeVM>> GetCauHoiTheoMonHocAsync(int monHocId) =>
        ChieuCauHoi(_context.CauHois.Where(c => c.DanhMucCauHoi.MonHocId == monHocId));

    public Task<List<CauHoiChonVaoDeVM>> GetCauHoiTheoIdsAsync(IEnumerable<int> ids)
    {
        var ds = ids.Distinct().ToList();
        return ChieuCauHoi(_context.CauHois.Where(c => ds.Contains(c.Id)));
    }

    private async Task<List<CauHoiChonVaoDeVM>> ChieuCauHoi(IQueryable<CauHoi> query)
    {
        return await query
            .OrderBy(c => c.Id)
            .Select(c => new CauHoiChonVaoDeVM
            {
                Id = c.Id,
                NoiDung = c.NoiDung,
                MonHocId = c.DanhMucCauHoi.MonHocId,
                DanhMucCauHoiId = c.DanhMucCauHoiId,
                LoaiCauHoi = c.LoaiCauHoi,
                MucDo = c.MucDo,
                DiemMacDinh = c.DiemMacDinh,
                CoHinhAnh = c.HinhAnh != null,
                GiaiThich = c.GiaiThich,
                SoDapAn = _context.DapAns.Count(d => d.CauHoiId == c.Id)
            })
            .ToListAsync();
    }

    public async Task<DeThi> CreateDeThiAsync(DeThi deThi, List<CauHoiDeThi> cauHois)
    {
        _context.DeThis.Add(deThi);
        var thuTu = 1;
        foreach (var c in cauHois)
        {
            c.DeThi = deThi; // EF gán DeThiId sau khi thêm đề
            c.ThuTu = thuTu++;
            _context.CauHoiDeThis.Add(c);
        }
        await _context.SaveChangesAsync();
        return deThi;
    }

    public async Task<HashSet<string>> GetAllMaDeThiAsync()
    {
        var ds = await _context.DeThis.Select(d => d.MaDeThi).ToListAsync();
        return ds.Select(m => m.Trim().ToLowerInvariant()).ToHashSet();
    }

    public async Task<int> ImportDeThiAsync(List<(DeThi DeThi, List<CauHoiDeThi> CauHois)> deThis)
    {
        foreach (var (deThi, cauHois) in deThis)
        {
            _context.DeThis.Add(deThi);
            var thuTu = 1;
            foreach (var c in cauHois)
            {
                c.DeThi = deThi;
                c.ThuTu = thuTu++;
                _context.CauHoiDeThis.Add(c);
            }
        }
        await _context.SaveChangesAsync();
        return deThis.Count;
    }

    public async Task<DeThi?> GetDeThiByIdAsync(int id)
    {
        return await _context.DeThis
            .Include(d => d.GiaoVien)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<List<CauHoiDeThi>> GetCauHoiDeThisAsync(int deThiId)
    {
        return await _context.CauHoiDeThis
            .Where(c => c.DeThiId == deThiId)
            .OrderBy(c => c.ThuTu)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task UpdateDeThiAsync(DeThi deThi, List<CauHoiDeThi> cauHois)
    {
        var entity = await _context.DeThis.FirstOrDefaultAsync(d => d.Id == deThi.Id)
                     ?? throw new KeyNotFoundException("Không tìm thấy đề thi");

        // Giáo viên tạo đề giữ nguyên
        entity.MaDeThi = deThi.MaDeThi;
        entity.TenDeThi = deThi.TenDeThi;
        entity.MonHocId = deThi.MonHocId;
        entity.ThoiLuong = deThi.ThoiLuong;
        entity.TongDiem = deThi.TongDiem;
        entity.LoaiDeThi = deThi.LoaiDeThi;

        var hienCo = await _context.CauHoiDeThis.Where(c => c.DeThiId == entity.Id).ToListAsync();
        var giuLai = new HashSet<int>();
        var thuTu = 1;
        foreach (var c in cauHois)
        {
            var cu = hienCo.FirstOrDefault(x => x.CauHoiId == c.CauHoiId);
            if (cu != null)
            {
                cu.Diem = c.Diem;
                cu.ThuTu = thuTu++;
                giuLai.Add(cu.Id);
            }
            else
            {
                _context.CauHoiDeThis.Add(new CauHoiDeThi
                {
                    DeThiId = entity.Id, CauHoiId = c.CauHoiId, Diem = c.Diem, ThuTu = thuTu++
                });
            }
        }
        _context.CauHoiDeThis.RemoveRange(hienCo.Where(x => !giuLai.Contains(x.Id)));

        await _context.SaveChangesAsync();
    }

    public async Task<List<CauHoiDeThiListItemVM>> GetAllCauHoiDeThiAsync()
    {
        return await _context.CauHoiDeThis
            .OrderBy(c => c.DeThi.MaDeThi)
            .ThenBy(c => c.ThuTu)
            .Select(c => new CauHoiDeThiListItemVM
            {
                Id = c.Id,
                DeThiId = c.DeThiId,
                MaDeThi = c.DeThi.MaDeThi,
                TenDeThi = c.DeThi.TenDeThi,
                DaPhatHanh = c.DeThi.DaPhatHanh,
                TenMonHoc = c.DeThi.MonHoc.TenMonHoc,
                ThuTu = c.ThuTu,
                CauHoiId = c.CauHoiId,
                NoiDung = c.CauHoi.NoiDung,
                LoaiCauHoi = c.CauHoi.LoaiCauHoi,
                MucDo = c.CauHoi.MucDo,
                Diem = c.Diem,
                ThuTuDapAn = c.ThuTuDapAn
            })
            .ToListAsync();
    }

    public async Task<int> AddCauHoiVaoDeAsync(int deThiId, int cauHoiId, double diem, int? viTri)
    {
        var hienCo = await _context.CauHoiDeThis.Where(c => c.DeThiId == deThiId).OrderBy(c => c.ThuTu).ToListAsync();

        // Chuẩn hóa thứ tự 1..n (phòng dữ liệu cũ bị nhảy cóc) rồi chèn vào vị trí
        var soCau = hienCo.Count;
        var cauSo = viTri is > 0 && viTri <= soCau ? viTri.Value : soCau + 1;
        for (var i = 0; i < soCau; i++)
        {
            hienCo[i].ThuTu = i + 1 >= cauSo ? i + 2 : i + 1;
        }
        _context.CauHoiDeThis.Add(new CauHoiDeThi { DeThiId = deThiId, CauHoiId = cauHoiId, Diem = diem, ThuTu = cauSo });
        await _context.SaveChangesAsync();
        return cauSo;
    }

    public async Task UpdateCauHoiDeThiAsync(int id, int cauHoiId, double diem, int viTri)
    {
        var dong = await _context.CauHoiDeThis.FirstOrDefaultAsync(c => c.Id == id)
                   ?? throw new KeyNotFoundException("Không tìm thấy câu hỏi trong đề");

        if (dong.CauHoiId != cauHoiId)
        {
            dong.CauHoiId = cauHoiId;
            dong.ThuTuDapAn = null; // thứ tự đáp án cũ thuộc câu khác
        }
        dong.Diem = diem;

        // Đưa câu tới vị trí mới rồi đánh lại thứ tự 1..n
        var ds = await _context.CauHoiDeThis
            .Where(c => c.DeThiId == dong.DeThiId)
            .OrderBy(c => c.ThuTu).ThenBy(c => c.Id)
            .ToListAsync();
        ds.Remove(dong);
        ds.Insert(Math.Clamp(viTri, 1, ds.Count + 1) - 1, dong);
        for (var i = 0; i < ds.Count; i++)
        {
            ds[i].ThuTu = i + 1;
        }
        await _context.SaveChangesAsync();
    }

    public async Task<bool> SapXepCauHoiAsync(int deThiId, List<int> cauHoiDeThiIds)
    {
        var ds = await _context.CauHoiDeThis.Where(c => c.DeThiId == deThiId).ToListAsync();
        // Phải khớp chính xác tập câu đang có (đề có thể vừa bị người khác thêm/bỏ câu)
        if (cauHoiDeThiIds.Count != ds.Count || cauHoiDeThiIds.Distinct().Count() != ds.Count ||
            !ds.All(c => cauHoiDeThiIds.Contains(c.Id)))
        {
            return false;
        }
        var theoId = ds.ToDictionary(c => c.Id);
        for (var i = 0; i < cauHoiDeThiIds.Count; i++)
        {
            theoId[cauHoiDeThiIds[i]].ThuTu = i + 1;
        }
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> PhanBoDiemAsync(int deThiId, double tongDiem, Dictionary<int, double> diemTheoDong)
    {
        var de = await _context.DeThis.FirstOrDefaultAsync(d => d.Id == deThiId);
        if (de == null)
        {
            return false;
        }
        var ds = await _context.CauHoiDeThis.Where(c => c.DeThiId == deThiId).ToListAsync();
        if (ds.Count != diemTheoDong.Count || !ds.All(c => diemTheoDong.ContainsKey(c.Id)))
        {
            return false; // đề vừa bị thêm/bỏ câu ở nơi khác
        }
        de.TongDiem = tongDiem;
        foreach (var c in ds)
        {
            c.Diem = diemTheoDong[c.Id];
        }
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<CauHoiDeThi?> GetCauHoiDeThiByIdAsync(int id)
    {
        return await _context.CauHoiDeThis
            .Include(c => c.DeThi)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task RemoveCauHoiKhoiDeAsync(int cauHoiDeThiId)
    {
        var dong = await _context.CauHoiDeThis.FirstOrDefaultAsync(c => c.Id == cauHoiDeThiId)
                   ?? throw new KeyNotFoundException("Không tìm thấy câu hỏi trong đề");
        _context.CauHoiDeThis.Remove(dong);

        var conLai = await _context.CauHoiDeThis
            .Where(c => c.DeThiId == dong.DeThiId && c.Id != dong.Id)
            .OrderBy(c => c.ThuTu).ThenBy(c => c.Id)
            .ToListAsync();
        for (var i = 0; i < conLai.Count; i++)
        {
            conLai[i].ThuTu = i + 1;
        }
        await _context.SaveChangesAsync();
    }

    public async Task<int> AddNhieuCauHoiVaoDeAsync(int deThiId, List<(int CauHoiId, double Diem)> cauHois, int? viTri)
    {
        var hienCo = await _context.CauHoiDeThis.Where(c => c.DeThiId == deThiId).OrderBy(c => c.ThuTu).ThenBy(c => c.Id).ToListAsync();
        var soCau = hienCo.Count;
        var batDau = viTri is > 0 && viTri <= soCau ? viTri.Value : soCau + 1;
        var n = cauHois.Count;

        // Câu cũ trước vị trí chèn giữ số, từ vị trí chèn trở đi lùi n vị trí
        for (var i = 0; i < soCau; i++)
        {
            hienCo[i].ThuTu = i + 1 >= batDau ? i + 1 + n : i + 1;
        }
        for (var k = 0; k < n; k++)
        {
            _context.CauHoiDeThis.Add(new CauHoiDeThi
            {
                DeThiId = deThiId, CauHoiId = cauHois[k].CauHoiId, Diem = cauHois[k].Diem, ThuTu = batDau + k
            });
        }
        await _context.SaveChangesAsync();
        return batDau;
    }

    public async Task<double?> DatTongDiemTheoCauAsync(int deThiId)
    {
        var de = await _context.DeThis.FirstOrDefaultAsync(d => d.Id == deThiId)
                 ?? throw new KeyNotFoundException("Không tìm thấy đề thi");
        var diem = await _context.CauHoiDeThis.Where(c => c.DeThiId == deThiId).Select(c => c.Diem).ToListAsync();
        if (diem.Count == 0)
        {
            return null;
        }
        de.TongDiem = Math.Round(diem.Sum(), 2);
        await _context.SaveChangesAsync();
        return de.TongDiem;
    }

    public async Task<bool> CoGianDiemCauAsync(int deThiId)
    {
        var de = await _context.DeThis.FirstOrDefaultAsync(d => d.Id == deThiId)
                 ?? throw new KeyNotFoundException("Không tìm thấy đề thi");
        var cau = await _context.CauHoiDeThis.Where(c => c.DeThiId == deThiId).OrderBy(c => c.ThuTu).ToListAsync();
        if (cau.Count == 0)
        {
            return false;
        }
        var moi = PhanBoDiemHelper.PhanBo(cau.Select(c => c.Diem).ToList(), de.TongDiem,
            PhanBoDiemHelper.BuocPhuHop(de.TongDiem, cau.Count));
        for (var i = 0; i < cau.Count; i++)
        {
            cau[i].Diem = moi[i];
        }
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<string>> KiemTraPhatHanhAsync(int deThiId)
    {
        var loi = new List<string>();
        var de = await _context.DeThis.AsNoTracking().FirstOrDefaultAsync(d => d.Id == deThiId);
        if (de == null)
        {
            loi.Add("Không tìm thấy đề thi");
            return loi;
        }

        var cauHois = await _context.CauHoiDeThis
            .Where(c => c.DeThiId == deThiId)
            .OrderBy(c => c.ThuTu)
            .Select(c => new
            {
                c.ThuTu,
                c.CauHoiId,
                c.Diem,
                c.CauHoi.LoaiCauHoi,
                c.CauHoi.DanhMucCauHoi.MonHocId,
                SoDapAn = _context.DapAns.Count(d => d.CauHoiId == c.CauHoiId),
                SoDung = _context.DapAns.Count(d => d.CauHoiId == c.CauHoiId && d.DapAnDung)
            })
            .ToListAsync();

        if (cauHois.Count == 0)
        {
            loi.Add("Đề chưa có câu hỏi nào");
            return loi;
        }

        var tong = Math.Round(cauHois.Sum(c => c.Diem), 2);
        if (Math.Abs(tong - de.TongDiem) >= 0.001)
        {
            loi.Add($"Tổng điểm các câu ({tong}) chưa khớp tổng điểm đề ({de.TongDiem})");
        }
        foreach (var c in cauHois)
        {
            var ten = $"Câu {c.ThuTu} (#{c.CauHoiId})";
            if (c.Diem <= 0)
                loi.Add($"{ten}: điểm phải lớn hơn 0");
            if (c.MonHocId != de.MonHocId)
                loi.Add($"{ten}: không thuộc môn học của đề");
            if (de.LoaiDeThi != 3 && de.LoaiDeThi != c.LoaiCauHoi)
                loi.Add($"{ten}: loại câu không phù hợp loại đề");
            if (c.LoaiCauHoi == 1 && c.SoDapAn < 2)
                loi.Add($"{ten}: câu trắc nghiệm chưa đủ 2 đáp án");
            else if (c.LoaiCauHoi == 1 && c.SoDung == 0)
                loi.Add($"{ten}: câu trắc nghiệm chưa có đáp án đúng");
        }
        return loi;
    }

    public async Task DatPhatHanhAsync(int deThiId, bool phatHanh)
    {
        var de = await _context.DeThis.FirstOrDefaultAsync(d => d.Id == deThiId)
                 ?? throw new KeyNotFoundException("Không tìm thấy đề thi");
        de.DaPhatHanh = phatHanh;
        de.NgayPhatHanh = phatHanh ? DateTime.UtcNow : null;
        await _context.SaveChangesAsync();
    }

    public async Task<List<DeThi>> GetDeThiDaPhatHanhAsync(int? monHocId = null)
    {
        return await _context.DeThis
            .Where(d => d.DaPhatHanh && (monHocId == null || d.MonHocId == monHocId))
            .OrderBy(d => d.MaDeThi)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> TronDapAnAsync(int deThiId)
    {
        var cauTrongDe = await _context.CauHoiDeThis
            .Where(c => c.DeThiId == deThiId && c.CauHoi.LoaiCauHoi == 1)
            .ToListAsync();
        var ids = cauTrongDe.Select(c => c.CauHoiId).ToList();
        var dapAnTheoCau = (await _context.DapAns.Where(d => ids.Contains(d.CauHoiId)).AsNoTracking().ToListAsync())
            .ToLookup(d => d.CauHoiId);

        var soCau = 0;
        foreach (var c in cauTrongDe)
        {
            var hienTai = ThuTuDapAnHelper.SapXep(dapAnTheoCau[c.CauHoiId], c.ThuTuDapAn);
            var moi = ThuTuDapAnHelper.Tron(hienTai);
            if (moi != null)
            {
                c.ThuTuDapAn = moi;
                soCau++;
            }
        }
        await _context.SaveChangesAsync();
        return soCau;
    }

    public async Task DeleteDeThiAsync(int id)
    {
        var deThi = await _context.DeThis.FirstOrDefaultAsync(d => d.Id == id)
                    ?? throw new KeyNotFoundException("Không tìm thấy đề thi");

        // Xóa liên kết tường minh (không phụ thuộc vào cấu hình cascade của DB)
        _context.CauHoiDeThis.RemoveRange(await _context.CauHoiDeThis.Where(c => c.DeThiId == id).ToListAsync());
        _context.DeThis.Remove(deThi);
        await _context.SaveChangesAsync();
    }
}
