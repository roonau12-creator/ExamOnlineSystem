using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Data;
using ExamOnlineSystem.Models;
using ExamOnlineSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ExamOnlineSystem.Business.Service;

// Làm bài thi: đề được sao chép sang CauHoiLanThi / DapAnLanThi khi bắt đầu nên sửa đề sau đó
// không ảnh hưởng bài đang làm. Bài làm lưu từng câu (BaiLam + DapAnDaChon), nộp bài thì
// chấm tự động phần trắc nghiệm; câu tự luận để giảng viên chấm.
public class LamBaiService : ILamBaiService
{
    // Cho phép lưu / nộp trễ vài giây so với giờ hết hạn (lệch đồng hồ, độ trễ mạng)
    private const int GiaySaiSo = 10;

    // Giới hạn độ dài một bài tự luận (chặn gửi dữ liệu quá lớn)
    private const int KyTuToiDaTuLuan = 20000;

    private readonly ApplicationDbContext _context;

    public LamBaiService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, string Message, int LanThiId)> BatDauAsync(int sinhVienId, int lichThiId)
    {
        var duThi = await _context.SinhVienDuThis
            .Include(x => x.LichThi)!.ThenInclude(l => l!.DeThi)
            .FirstOrDefaultAsync(x => x.SinhVienId == sinhVienId && x.LichThiId == lichThiId);
        if (duThi?.LichThi?.DeThi == null)
        {
            return (false, "Bạn không có tên trong danh sách dự thi của kỳ thi này", 0);
        }

        var lichThi = duThi.LichThi;
        var deThi = lichThi.DeThi;
        var now = DateTime.UtcNow;
        var moLuc = DateTime.SpecifyKind(lichThi.ThoiGianBatDau, DateTimeKind.Utc);
        var dongLuc = DateTime.SpecifyKind(lichThi.ThoiGianKetThuc, DateTimeKind.Utc);

        if (now < moLuc)
        {
            return (false, $"Chưa đến giờ thi. Bài thi mở lúc {moLuc.ToLocalTime():HH:mm dd/MM/yyyy}", 0);
        }
        if (now >= dongLuc)
        {
            return (false, $"Kỳ thi đã kết thúc lúc {dongLuc.ToLocalTime():HH:mm dd/MM/yyyy}", 0);
        }
        if (!deThi.DaPhatHanh)
        {
            return (false, "Đề thi chưa được phát hành. Vui lòng báo giảng viên", 0);
        }

        // Đang làm dở -> tiếp tục đúng lượt đó (hết giờ thì nộp luôn, không mở lại)
        var dangLam = await _context.LanThis
            .Where(l => l.SinhVienDuThiId == duThi.Id && l.TrangThai == 0)
            .OrderByDescending(l => l.LanThu)
            .FirstOrDefaultAsync();
        if (dangLam != null)
        {
            if (await DongNeuHetGioAsync(dangLam, duThi, deThi, dongLuc, now))
            {
                return (false, "Lượt thi trước của bạn đã hết giờ và được nộp tự động", 0);
            }
            return (true, string.Empty, dangLam.Id);
        }

        if (duThi.TrangThai == 2)
        {
            var biDinhChi = await _context.LanThis.AnyAsync(l => l.SinhVienDuThiId == duThi.Id && l.TrangThai == 3);
            return (false, biDinhChi
                ? "Bạn đã bị đình chỉ thi do mở tab khác trong lúc làm bài"
                : "Bạn đã nộp bài cho kỳ thi này", 0);
        }

        // ===== Sao chép đề cho lượt thi mới =====
        var cauTrongDe = await _context.CauHoiDeThis
            .Where(c => c.DeThiId == lichThi.DeThiId)
            .OrderBy(c => c.ThuTu).ThenBy(c => c.Id)
            .AsNoTracking()
            .ToListAsync();
        if (cauTrongDe.Count == 0)
        {
            return (false, "Đề thi chưa có câu hỏi nào. Vui lòng báo giảng viên", 0);
        }

        var cauHoiIds = cauTrongDe.Select(c => c.CauHoiId).Distinct().ToList();
        var cauHoiGoc = await _context.CauHois
            .Where(c => cauHoiIds.Contains(c.Id))
            .AsNoTracking()
            .ToDictionaryAsync(c => c.Id);
        var dapAnGoc = (await _context.DapAns
                .Where(d => cauHoiIds.Contains(d.CauHoiId))
                .AsNoTracking()
                .ToListAsync())
            .GroupBy(d => d.CauHoiId)
            .ToDictionary(g => g.Key, g => g.ToList());

        await using var transaction = await _context.Database.BeginTransactionAsync();

        var lanThuCuoi = await _context.LanThis
            .Where(l => l.SinhVienDuThiId == duThi.Id)
            .MaxAsync(l => (int?)l.LanThu) ?? 0;

        var lanThi = new LanThi
        {
            SinhVienDuThiId = duThi.Id,
            LanThu = lanThuCuoi + 1,
            ThoiGianBatDau = now,
            TrangThai = 0
        };
        _context.LanThis.Add(lanThi);

        var thuTu = 0;
        foreach (var c in cauTrongDe)
        {
            if (!cauHoiGoc.TryGetValue(c.CauHoiId, out var goc))
            {
                continue; // câu hỏi đã bị xóa khỏi ngân hàng
            }

            thuTu++;
            var cauSnapshot = new CauHoiLanThi
            {
                LanThi = lanThi,
                CauHoiGocId = goc.Id,
                NoiDung = goc.NoiDung,
                LoaiCauHoi = (short)goc.LoaiCauHoi,
                Diem = (decimal)c.Diem,
                ThuTu = thuTu
            };
            _context.CauHoiLanThis.Add(cauSnapshot);

            if (goc.LoaiCauHoi == 2)
            {
                continue; // tự luận không có đáp án sẵn
            }

            // Giữ đúng thứ tự đáp án của đề (đề có thể đã trộn đáp án)
            var theoDe = ThuTuDapAnHelper.SapXep(dapAnGoc.GetValueOrDefault(goc.Id) ?? new List<DapAn>(), c.ThuTuDapAn);
            for (var i = 0; i < theoDe.Count; i++)
            {
                _context.DapAnLanThis.Add(new DapAnLanThi
                {
                    CauHoiLanThi = cauSnapshot,
                    DapAnGocId = theoDe[i].Id,
                    NoiDung = theoDe[i].NoiDung,
                    LaDapAnDung = theoDe[i].DapAnDung,
                    ThuTu = i + 1
                });
            }
        }

        duThi.TrangThai = 1;
        duThi.ThoiGianBatDau ??= now;
        duThi.ThoiGianNopBai = null;

        try
        {
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException)
        {
            // Gửi trùng yêu cầu bắt đầu (bấm hai lần): chỉ mục LanThu duy nhất chặn lượt thứ hai,
            // quay về lượt đã mở thay vì báo lỗi giữa giờ thi.
            await transaction.RollbackAsync();
            _context.ChangeTracker.Clear();
            var daMo = await _context.LanThis
                .Where(l => l.SinhVienDuThiId == duThi.Id && l.TrangThai == 0)
                .OrderByDescending(l => l.LanThu)
                .Select(l => (int?)l.Id)
                .FirstOrDefaultAsync();
            return daMo == null
                ? (false, "Không mở được đề thi. Vui lòng thử lại hoặc báo giảng viên", 0)
                : (true, string.Empty, daMo.Value);
        }
        return (true, string.Empty, lanThi.Id);
    }

    public async Task<LamBaiVM?> GetBaiLamAsync(int sinhVienId, int lanThiId)
    {
        var bo = await LayLanThiAsync(sinhVienId, lanThiId);
        if (bo == null)
        {
            return null;
        }
        var (lanThi, duThi, lichThi, deThi) = bo.Value;
        var now = DateTime.UtcNow;
        await DongNeuHetGioAsync(lanThi, duThi, deThi, DateTime.SpecifyKind(lichThi.ThoiGianKetThuc, DateTimeKind.Utc), now);

        var sinhVien = await _context.SinhViens
            .Where(s => s.Id == sinhVienId)
            .Select(s => new { s.MaSinhVien, s.HoTen, MaLop = s.Lop != null ? s.Lop.MaLop : "" })
            .AsNoTracking()
            .FirstOrDefaultAsync();

        var vm = new LamBaiVM
        {
            LanThiId = lanThi.Id,
            LanThu = lanThi.LanThu,
            SoLanThi = await _context.LanThis.CountAsync(l => l.SinhVienDuThiId == duThi.Id),
            LichThiId = lichThi.Id,
            TenLichThi = lichThi.TenLichThi,
            TenMonHoc = await _context.MonHocs.Where(m => m.Id == deThi.MonHocId).Select(m => m.TenMonHoc).FirstOrDefaultAsync() ?? "",
            MaDeThi = deThi.MaDeThi,
            TenDeThi = deThi.TenDeThi,
            TenGiaoVien = await _context.GiaoViens.Where(g => g.Id == lichThi.GiaoVienId).Select(g => g.HoTen).FirstOrDefaultAsync() ?? "",
            MaSinhVien = sinhVien?.MaSinhVien ?? "",
            HoTen = sinhVien?.HoTen ?? "",
            MaLop = sinhVien?.MaLop ?? "",
            ThoiLuong = deThi.ThoiLuong,
            TongDiem = deThi.TongDiem,
            TrangThai = lanThi.TrangThai,
            ThoiGianBatDau = DateTime.SpecifyKind(lanThi.ThoiGianBatDau, DateTimeKind.Utc),
            LichThiKetThuc = DateTime.SpecifyKind(lichThi.ThoiGianKetThuc, DateTimeKind.Utc),
            HetHan = HetHan(lanThi, deThi, DateTime.SpecifyKind(lichThi.ThoiGianKetThuc, DateTimeKind.Utc))
        };

        // Đáp án đúng KHÔNG ra khỏi tầng này: chỉ dùng để biết câu có nhiều đáp án đúng hay không
        vm.CauHois = await _context.CauHoiLanThis
            .Where(c => c.LanThiId == lanThi.Id)
            .OrderBy(c => c.ThuTu)
            .Select(c => new CauLamBaiVM
            {
                CauHoiLanThiId = c.Id,
                ThuTu = c.ThuTu,
                NoiDung = c.NoiDung,
                LoaiCauHoi = c.LoaiCauHoi,
                Diem = c.Diem,
                CoHinhAnh = c.CauHoiGoc!.HinhAnh != null,
                ChonNhieu = _context.DapAnLanThis.Count(d => d.CauHoiLanThiId == c.Id && d.LaDapAnDung) > 1,
                NoiDungTraLoi = _context.BaiLams
                    .Where(b => b.CauHoiLanThiId == c.Id)
                    .Select(b => b.NoiDungTraLoi)
                    .FirstOrDefault(),
                DapAns = _context.DapAnLanThis
                    .Where(d => d.CauHoiLanThiId == c.Id)
                    .OrderBy(d => d.ThuTu)
                    .Select(d => new DapAnLamBaiVM
                    {
                        Id = d.Id,
                        ThuTu = d.ThuTu,
                        NoiDung = d.NoiDung,
                        DaChon = _context.DapAnDaChons.Any(x => x.DapAnLanThiId == d.Id && x.BaiLam!.LanThiId == lanThi.Id)
                    })
                    .ToList()
            })
            .AsNoTracking()
            .ToListAsync();

        return vm;
    }

    public async Task<(bool Success, string Message, int SoCauDaLam)> LuuCauAsync(int sinhVienId, int lanThiId,
        int cauHoiLanThiId, List<int> dapAnLanThiIds, string? noiDungTraLoi)
    {
        var bo = await LayLanThiAsync(sinhVienId, lanThiId);
        if (bo == null)
        {
            return (false, "Không tìm thấy lượt thi của bạn", 0);
        }
        var (lanThi, duThi, lichThi, deThi) = bo.Value;
        var now = DateTime.UtcNow;
        var dongLuc = DateTime.SpecifyKind(lichThi.ThoiGianKetThuc, DateTimeKind.Utc);

        if (await DongNeuHetGioAsync(lanThi, duThi, deThi, dongLuc, now))
        {
            return (false, "Đã hết giờ làm bài, bài của bạn được nộp tự động", await DemCauDaLamAsync(lanThi.Id));
        }
        if (lanThi.TrangThai != 0)
        {
            return (false, "Bài thi đã nộp nên không lưu được nữa", await DemCauDaLamAsync(lanThi.Id));
        }

        var cauHoi = await _context.CauHoiLanThis
            .FirstOrDefaultAsync(c => c.Id == cauHoiLanThiId && c.LanThiId == lanThi.Id);
        if (cauHoi == null)
        {
            return (false, "Câu hỏi không thuộc bài thi này", await DemCauDaLamAsync(lanThi.Id));
        }

        var baiLam = await _context.BaiLams
            .FirstOrDefaultAsync(b => b.LanThiId == lanThi.Id && b.CauHoiLanThiId == cauHoi.Id);
        if (baiLam == null)
        {
            baiLam = new BaiLam { LanThiId = lanThi.Id, CauHoiLanThiId = cauHoi.Id };
            _context.BaiLams.Add(baiLam);
        }
        baiLam.NgayCapNhat = now;

        if (cauHoi.LoaiCauHoi == 2)
        {
            var noiDung = noiDungTraLoi?.Trim();
            if (noiDung != null && noiDung.Length > KyTuToiDaTuLuan)
            {
                return (false, $"Bài tự luận của một câu tối đa {KyTuToiDaTuLuan:N0} ký tự", await DemCauDaLamAsync(lanThi.Id));
            }
            baiLam.NoiDungTraLoi = string.IsNullOrEmpty(noiDung) ? null : noiDung;
        }
        else
        {
            // Chỉ nhận đáp án thuộc đúng câu này; danh sách rỗng = sinh viên bỏ chọn
            var hopLe = await _context.DapAnLanThis
                .Where(d => d.CauHoiLanThiId == cauHoi.Id && dapAnLanThiIds.Contains(d.Id))
                .Select(d => d.Id)
                .ToListAsync();
            if (dapAnLanThiIds.Count > 0 && hopLe.Count == 0)
            {
                // Dữ liệu gửi lên sai: không ghi gì để không xóa mất lựa chọn đang có
                return (false, "Đáp án không thuộc câu hỏi này", await DemCauDaLamAsync(lanThi.Id));
            }

            // Câu chỉ có một đáp án đúng thì chỉ giữ một lựa chọn
            var soDapAnDung = await _context.DapAnLanThis.CountAsync(d => d.CauHoiLanThiId == cauHoi.Id && d.LaDapAnDung);
            if (soDapAnDung <= 1 && hopLe.Count > 1)
            {
                hopLe = hopLe.Take(1).ToList();
            }

            await _context.SaveChangesAsync(); // cần BaiLam.Id trước khi ghi đáp án đã chọn

            var daChon = await _context.DapAnDaChons.Where(x => x.BaiLamId == baiLam.Id).ToListAsync();
            _context.DapAnDaChons.RemoveRange(daChon.Where(x => !hopLe.Contains(x.DapAnLanThiId)));
            foreach (var id in hopLe.Where(id => daChon.All(x => x.DapAnLanThiId != id)))
            {
                _context.DapAnDaChons.Add(new DapAnDaChon { BaiLamId = baiLam.Id, DapAnLanThiId = id });
            }
        }

        await _context.SaveChangesAsync();
        return (true, string.Empty, await DemCauDaLamAsync(lanThi.Id));
    }

    public async Task<(bool Success, string Message)> NopBaiAsync(int sinhVienId, int lanThiId, bool tuDong)
    {
        var bo = await LayLanThiAsync(sinhVienId, lanThiId);
        if (bo == null)
        {
            return (false, "Không tìm thấy lượt thi của bạn");
        }
        var (lanThi, duThi, lichThi, deThi) = bo.Value;
        var now = DateTime.UtcNow;
        var dongLuc = DateTime.SpecifyKind(lichThi.ThoiGianKetThuc, DateTimeKind.Utc);

        if (lanThi.TrangThai != 0)
        {
            return (true, "Bài thi của bạn đã được nộp trước đó");
        }
        if (await DongNeuHetGioAsync(lanThi, duThi, deThi, dongLuc, now))
        {
            return (true, "Đã hết giờ làm bài, bài của bạn được nộp tự động");
        }

        await ChamVaDongAsync(lanThi, duThi, deThi, tuDong ? 2 : 1, now);
        return (true, tuDong ? "Đã hết giờ, bài của bạn được nộp tự động" : "Nộp bài thành công");
    }

    public async Task<(bool Success, string Message)> DinhChiAsync(int sinhVienId, int lanThiId)
    {
        var bo = await LayLanThiAsync(sinhVienId, lanThiId);
        if (bo == null)
        {
            return (false, "Không tìm thấy lượt thi của bạn");
        }
        var (lanThi, duThi, lichThi, deThi) = bo.Value;
        var now = DateTime.UtcNow;
        var dongLuc = DateTime.SpecifyKind(lichThi.ThoiGianKetThuc, DateTimeKind.Utc);

        if (lanThi.TrangThai != 0)
        {
            return (true, "Bài thi của bạn đã được đóng trước đó");
        }
        if (await DongNeuHetGioAsync(lanThi, duThi, deThi, dongLuc, now))
        {
            return (true, "Đã hết giờ làm bài, bài của bạn được nộp tự động");
        }

        await ChamVaDongAsync(lanThi, duThi, deThi, 3, now);
        return (true, "Bạn đã bị đình chỉ thi do mở tab khác trong lúc làm bài");
    }

    public async Task<KetQuaLamBaiVM?> GetKetQuaAsync(int sinhVienId, int lanThiId)
    {
        var bo = await LayLanThiAsync(sinhVienId, lanThiId);
        if (bo == null)
        {
            return null;
        }
        var (lanThi, duThi, lichThi, deThi) = bo.Value;
        await DongNeuHetGioAsync(lanThi, duThi, deThi, DateTime.SpecifyKind(lichThi.ThoiGianKetThuc, DateTimeKind.Utc), DateTime.UtcNow);
        if (lanThi.TrangThai == 0)
        {
            return null; // vẫn đang làm bài
        }

        return new KetQuaLamBaiVM
        {
            LanThiId = lanThi.Id,
            LanThu = lanThi.LanThu,
            LichThiId = lichThi.Id,
            TenLichThi = lichThi.TenLichThi,
            TenMonHoc = await _context.MonHocs.Where(m => m.Id == deThi.MonHocId).Select(m => m.TenMonHoc).FirstOrDefaultAsync() ?? "",
            MaDeThi = deThi.MaDeThi,
            TenDeThi = deThi.TenDeThi,
            MaSinhVien = await _context.SinhViens.Where(s => s.Id == sinhVienId).Select(s => s.MaSinhVien).FirstOrDefaultAsync() ?? "",
            HoTen = await _context.SinhViens.Where(s => s.Id == sinhVienId).Select(s => s.HoTen).FirstOrDefaultAsync() ?? "",
            TrangThai = lanThi.TrangThai,
            ThoiGianBatDau = DateTime.SpecifyKind(lanThi.ThoiGianBatDau, DateTimeKind.Utc),
            ThoiGianNopBai = lanThi.ThoiGianNopBai.HasValue
                ? DateTime.SpecifyKind(lanThi.ThoiGianNopBai.Value, DateTimeKind.Utc)
                : null,
            ThoiLuong = deThi.ThoiLuong,
            TongDiem = deThi.TongDiem,
            SoCau = await _context.CauHoiLanThis.CountAsync(c => c.LanThiId == lanThi.Id),
            SoCauDaLam = await DemCauDaLamAsync(lanThi.Id),
            SoCauTuLuan = await _context.CauHoiLanThis.CountAsync(c => c.LanThiId == lanThi.Id && c.LoaiCauHoi == 2),
            Diem = lanThi.Diem
        };
    }

    public async Task<string?> GetTenFileHinhAnhAsync(int sinhVienId, int cauHoiLanThiId)
    {
        return await _context.CauHoiLanThis
            .Where(c => c.Id == cauHoiLanThiId && c.LanThi!.SinhVienDuThi!.SinhVienId == sinhVienId)
            .Select(c => c.CauHoiGoc!.HinhAnh)
            .FirstOrDefaultAsync();
    }

    // ===== Dùng chung =====

    // Lượt thi của chính sinh viên kèm lịch thi và đề thi (có theo dõi để cập nhật được)
    private async Task<(LanThi LanThi, SinhVienDuThi DuThi, LichThi LichThi, DeThi DeThi)?> LayLanThiAsync(int sinhVienId, int lanThiId)
    {
        var lanThi = await _context.LanThis
            .Include(l => l.SinhVienDuThi)!.ThenInclude(d => d!.LichThi)!.ThenInclude(l => l!.DeThi)
            .FirstOrDefaultAsync(l => l.Id == lanThiId);
        var duThi = lanThi?.SinhVienDuThi;
        if (duThi?.SinhVienId != sinhVienId || duThi.LichThi?.DeThi == null)
        {
            return null;
        }
        return (lanThi!, duThi, duThi.LichThi, duThi.LichThi.DeThi);
    }

    // Giờ phải nộp bài: hết thời lượng làm bài, hoặc giờ đóng kỳ thi nếu vào thi muộn
    private static DateTime HetHan(LanThi lanThi, DeThi deThi, DateTime dongLuc)
    {
        var theoThoiLuong = DateTime.SpecifyKind(lanThi.ThoiGianBatDau, DateTimeKind.Utc).AddMinutes(deThi.ThoiLuong);
        return theoThoiLuong < dongLuc ? theoThoiLuong : dongLuc;
    }

    // Hết giờ mà lượt thi còn mở -> nộp và chấm tự động. Trả về true nếu vừa đóng lượt thi.
    private async Task<bool> DongNeuHetGioAsync(LanThi lanThi, SinhVienDuThi duThi, DeThi deThi, DateTime dongLuc, DateTime now)
    {
        if (lanThi.TrangThai != 0 || now <= HetHan(lanThi, deThi, dongLuc).AddSeconds(GiaySaiSo))
        {
            return false;
        }
        await ChamVaDongAsync(lanThi, duThi, deThi, 2, HetHan(lanThi, deThi, dongLuc));
        return true;
    }

    // Chấm tự động phần trắc nghiệm rồi đóng lượt thi.
    // trangThai: 1 = sinh viên tự nộp, 2 = hết giờ, 3 = bị đình chỉ. Đề có câu tự luận thì chưa có điểm tổng
    // (giảng viên chấm sau), nhưng điểm từng câu trắc nghiệm vẫn được ghi lại.
    private async Task ChamVaDongAsync(LanThi lanThi, SinhVienDuThi duThi, DeThi deThi, int trangThai, DateTime thoiDiemNop)
    {
        var cauHois = await _context.CauHoiLanThis
            .Where(c => c.LanThiId == lanThi.Id)
            .ToListAsync();
        var dapAnDung = await _context.DapAnLanThis
            .Where(d => d.CauHoiLanThi!.LanThiId == lanThi.Id && d.LaDapAnDung)
            .Select(d => new { d.CauHoiLanThiId, d.Id })
            .ToListAsync();
        var baiLams = await _context.BaiLams
            .Where(b => b.LanThiId == lanThi.Id)
            .ToListAsync();
        var daChon = await _context.DapAnDaChons
            .Where(x => x.BaiLam!.LanThiId == lanThi.Id)
            .Select(x => new { x.BaiLamId, x.DapAnLanThiId })
            .ToListAsync();

        var coTuLuan = false;
        decimal tongDiem = 0;
        foreach (var cauHoi in cauHois)
        {
            if (cauHoi.LoaiCauHoi == 2)
            {
                coTuLuan = true;
                continue; // chờ giảng viên chấm
            }

            var dung = dapAnDung.Where(d => d.CauHoiLanThiId == cauHoi.Id).Select(d => d.Id).ToHashSet();
            var baiLam = baiLams.FirstOrDefault(b => b.CauHoiLanThiId == cauHoi.Id);
            var chon = baiLam == null
                ? new HashSet<int>()
                : daChon.Where(x => x.BaiLamId == baiLam.Id).Select(x => x.DapAnLanThiId).ToHashSet();

            // Đúng khi chọn đủ và chỉ các đáp án đúng (câu nhiều đáp án phải chọn hết)
            var diem = dung.Count > 0 && chon.SetEquals(dung) ? cauHoi.Diem : 0m;
            tongDiem += diem;
            if (baiLam != null)
            {
                baiLam.Diem = diem;
                baiLam.NgayCapNhat = thoiDiemNop;
            }
        }

        lanThi.TrangThai = trangThai;
        lanThi.ThoiGianNopBai = thoiDiemNop;
        lanThi.Diem = coTuLuan ? null : Math.Round((double)tongDiem, 2);

        duThi.TrangThai = 2;
        duThi.ThoiGianNopBai = thoiDiemNop;
        duThi.Diem = lanThi.Diem;

        await _context.SaveChangesAsync();
    }

    // Số câu đã trả lời: tự luận có nội dung, trắc nghiệm có ít nhất một đáp án được chọn
    private async Task<int> DemCauDaLamAsync(int lanThiId)
    {
        return await _context.BaiLams
            .CountAsync(b => b.LanThiId == lanThiId &&
                             (b.CauHoiLanThi!.LoaiCauHoi == 2
                                 ? b.NoiDungTraLoi != null && b.NoiDungTraLoi != ""
                                 : _context.DapAnDaChons.Any(x => x.BaiLamId == b.Id)));
    }
}
