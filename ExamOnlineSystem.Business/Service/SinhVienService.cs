using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Data;
using ExamOnlineSystem.Models;
using ExamOnlineSystem.Models.ViewModels;
using ExamOnlineSystem.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExamOnlineSystem.Business.Service;

public class SinhVienService : ISinhVienService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public SinhVienService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<List<SinhVien>> GetAllSinhVienAsync()
    {
        return await _context.SinhViens
            .Include(s => s.Lop)
            .Include(s => s.Khoa)
            .OrderBy(s => s.MaSinhVien)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int?> GetSinhVienIdByUserIdAsync(string userId)
    {
        return await _context.SinhViens
            .Where(s => s.ApplicationUserId == userId)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<List<MonHocCuaToiItem>> GetMonHocCuaSinhVienAsync(int sinhVienId)
    {
        // Dùng lại dữ liệu trang chi tiết: lớp học phần + lịch thi của sinh viên, gom theo môn học
        var chiTiet = await GetSinhVienDetailAsync(sinhVienId);
        if (chiTiet == null)
        {
            return new List<MonHocCuaToiItem>();
        }

        var monHocIds = chiTiet.LopHocPhans.Select(l => l.MonHocId)
            .Concat(chiTiet.LichThis.Select(l => l.MonHocId))
            .Distinct().ToList();

        var monHocs = await _context.MonHocs
            .Where(m => monHocIds.Contains(m.Id))
            .Select(m => new MonHocCuaToiItem
            {
                MonHocId = m.Id,
                MaMonHoc = m.MaMonHoc,
                TenMonHoc = m.TenMonHoc,
                TenBoMon = m.BoMon != null ? m.BoMon.TenBoMon : "",
                HinhThucThi = m.HinhThucThi,
                ThoiGianThi = m.ThoiGianThi
            })
            .AsNoTracking()
            .ToListAsync();

        foreach (var m in monHocs)
        {
            m.LopHocPhans = chiTiet.LopHocPhans.Where(l => l.MonHocId == m.MonHocId).ToList();
            m.LichThis = chiTiet.LichThis.Where(l => l.MonHocId == m.MonHocId).ToList();
        }

        // Môn đang học trước, rồi theo tên môn
        return monHocs.OrderByDescending(m => m.DangHoc).ThenBy(m => m.TenMonHoc).ToList();
    }

    public async Task<KyThiChiTietVM?> GetKyThiCuaSinhVienAsync(int sinhVienId, int lichThiId)
    {
        var vm = await _context.SinhVienDuThis
            .Where(x => x.SinhVienId == sinhVienId && x.LichThiId == lichThiId)
            .Select(x => new KyThiChiTietVM
            {
                LichThiId = x.LichThiId,
                TenLichThi = x.LichThi!.TenLichThi,
                GhiChu = x.LichThi.GhiChu,
                ThoiGianBatDau = x.LichThi.ThoiGianBatDau,
                ThoiGianKetThuc = x.LichThi.ThoiGianKetThuc,
                GiaoVien = x.LichThi.GiaoVien!.HoTen,
                SoThiSinh = _context.SinhVienDuThis.Count(s => s.LichThiId == x.LichThiId),
                MaMonHoc = x.LichThi.DeThi!.MonHoc.MaMonHoc,
                TenMonHoc = x.LichThi.DeThi.MonHoc.TenMonHoc,
                TenDeThi = x.LichThi.DeThi.TenDeThi,
                LoaiDeThi = x.LichThi.DeThi.LoaiDeThi,
                ThoiLuong = x.LichThi.DeThi.ThoiLuong,
                TongDiem = x.LichThi.DeThi.TongDiem,
                SoCauHoi = _context.CauHoiDeThis.Count(c => c.DeThiId == x.LichThi.DeThiId),
                DaPhatHanh = x.LichThi.DeThi.DaPhatHanh,
                TrangThai = x.TrangThai,
                // Điểm: lấy ở SinhVienDuThi, nếu chưa có thì lấy lượt thi mới nhất (giống trang Kết quả thi)
                Diem = x.Diem ?? _context.LanThis
                    .Where(l => l.SinhVienDuThiId == x.Id && l.Diem != null)
                    .OrderByDescending(l => l.LanThu)
                    .Select(l => l.Diem)
                    .FirstOrDefault()
            })
            .AsNoTracking()
            .FirstOrDefaultAsync();
        if (vm == null)
        {
            return null;
        }

        vm.Lops = await _context.LichThiLops
            .Where(x => x.LichThiId == lichThiId)
            .OrderBy(x => x.Lop!.MaLop)
            .Select(x => x.Lop!.MaLop)
            .ToListAsync();

        vm.LanThis = await _context.LanThis
            .Where(l => l.SinhVienDuThi!.SinhVienId == sinhVienId && l.SinhVienDuThi.LichThiId == lichThiId)
            .OrderBy(l => l.LanThu)
            .Select(l => new SinhVienLanThiItem
            {
                Id = l.Id,
                LichThiId = lichThiId,
                LanThu = l.LanThu,
                ThoiGianBatDau = l.ThoiGianBatDau,
                ThoiGianNopBai = l.ThoiGianNopBai,
                ThoiLuong = vm.ThoiLuong,
                TrangThai = l.TrangThai,
                Diem = l.Diem,
                TongDiem = vm.TongDiem
            })
            .AsNoTracking()
            .ToListAsync();

        return vm;
    }

    public async Task<bool> CapNhatSoDienThoaiAsync(int sinhVienId, string soDienThoai)
    {
        var sinhVien = await _context.SinhViens.FirstOrDefaultAsync(s => s.Id == sinhVienId);
        if (sinhVien == null)
        {
            return false;
        }

        var sdt = soDienThoai.Trim();
        sinhVien.SoDienThoai = sdt;
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == sinhVien.ApplicationUserId);
        if (user != null)
        {
            user.PhoneNumber = sdt;
        }
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<SinhVienDetailVM?> GetSinhVienDetailAsync(int id)
    {
        var sinhVien = await GetSinhVienByIdAsync(id);
        if (sinhVien == null)
        {
            return null;
        }

        var taiKhoan = await _context.Users
            .Where(u => u.Id == sinhVien.ApplicationUserId)
            .Select(u => new { u.DangHoatDong, u.LanDangNhapCuoi })
            .AsNoTracking()
            .FirstOrDefaultAsync();

        var vm = new SinhVienDetailVM
        {
            SinhVien = sinhVien,
            DangHoatDong = taiKhoan?.DangHoatDong,
            LanDangNhapCuoi = taiKhoan?.LanDangNhapCuoi
        };

        vm.LopHocPhans = await _context.LopHocPhanSinhViens
            .Where(x => x.SinhVienId == id)
            .OrderByDescending(x => x.LopHocPhan!.NamHoc!.NgayBatDau)
            .ThenByDescending(x => x.LopHocPhan!.HocKy)
            .ThenBy(x => x.LopHocPhan!.MaLopHocPhan)
            .Select(x => new SinhVienLopHocPhanItem
            {
                Id = x.LopHocPhanId,
                MonHocId = x.LopHocPhan!.HocPhan!.MonHocId,
                MaLopHocPhan = x.LopHocPhan!.MaLopHocPhan,
                TenLopHocPhan = x.LopHocPhan.TenLopHocPhan,
                MaHocPhan = x.LopHocPhan.HocPhan!.MaHocPhan,
                TenHocPhan = x.LopHocPhan.HocPhan.TenHocPhan,
                SoTinChi = x.LopHocPhan.HocPhan.SoTinChi,
                TenNamHoc = x.LopHocPhan.NamHoc!.TenNamHoc,
                LaNamHienTai = x.LopHocPhan.NamHoc.LaNamHienTai,
                HocKy = x.LopHocPhan.HocKy,
                GiaoVienPhuTrach = x.LopHocPhan.LopHocPhanGiaoViens
                    .Where(g => g.VaiTro == LopHocPhanVaiTro.PhuTrach)
                    .Select(g => g.GiaoVien!.HoTen)
                    .FirstOrDefault()
            })
            .AsNoTracking()
            .ToListAsync();

        // Điểm: lấy ở SinhVienDuThi, nếu chưa có thì lấy lượt thi mới nhất (giống trang Kết quả thi)
        vm.LichThis = await _context.SinhVienDuThis
            .Where(x => x.SinhVienId == id)
            .OrderByDescending(x => x.LichThi!.ThoiGianBatDau)
            .Select(x => new SinhVienLichThiItem
            {
                LichThiId = x.LichThiId,
                MonHocId = x.LichThi!.DeThi!.MonHocId,
                TenLichThi = x.LichThi!.TenLichThi,
                TenDeThi = x.LichThi.DeThi!.TenDeThi,
                TenMonHoc = x.LichThi.DeThi.MonHoc.TenMonHoc,
                ThoiLuong = x.LichThi.DeThi.ThoiLuong,
                GhiChu = x.LichThi.GhiChu,
                ThoiGianBatDau = x.LichThi.ThoiGianBatDau,
                ThoiGianKetThuc = x.LichThi.ThoiGianKetThuc,
                TrangThai = x.TrangThai,
                TongDiem = x.LichThi.DeThi.TongDiem,
                Diem = x.Diem ?? _context.LanThis
                    .Where(l => l.SinhVienDuThiId == x.Id && l.Diem != null)
                    .OrderByDescending(l => l.LanThu)
                    .Select(l => l.Diem)
                    .FirstOrDefault(),
                SoLuotThi = _context.LanThis.Count(l => l.SinhVienDuThiId == x.Id),
                LanThiMoiNhatId = _context.LanThis
                    .Where(l => l.SinhVienDuThiId == x.Id)
                    .OrderByDescending(l => l.LanThu)
                    .Select(l => (int?)l.Id)
                    .FirstOrDefault()
            })
            .AsNoTracking()
            .ToListAsync();

        vm.LanThis = await _context.LanThis
            .Where(l => l.SinhVienDuThi!.SinhVienId == id)
            .OrderByDescending(l => l.ThoiGianBatDau)
            .Select(l => new SinhVienLanThiItem
            {
                Id = l.Id,
                LichThiId = l.SinhVienDuThi!.LichThiId,
                TenLichThi = l.SinhVienDuThi.LichThi!.TenLichThi,
                TenMonHoc = l.SinhVienDuThi.LichThi.DeThi!.MonHoc.TenMonHoc,
                LanThu = l.LanThu,
                ThoiGianBatDau = l.ThoiGianBatDau,
                ThoiGianNopBai = l.ThoiGianNopBai,
                ThoiLuong = l.SinhVienDuThi.LichThi.DeThi.ThoiLuong,
                TrangThai = l.TrangThai,
                Diem = l.Diem,
                TongDiem = l.SinhVienDuThi.LichThi.DeThi.TongDiem
            })
            .AsNoTracking()
            .ToListAsync();

        return vm;
    }

    public async Task<SinhVien?> GetSinhVienByIdAsync(int id)
    {
        return await _context.SinhViens
            .Include(s => s.Lop)
            .Include(s => s.Khoa)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<bool> MaSinhVienExistsAsync(string maSinhVien, int? excludeId = null)
    {
        var ma = maSinhVien.Trim().ToLower();
        return await _context.SinhViens.AnyAsync(s => s.MaSinhVien.ToLower() == ma && (excludeId == null || s.Id != excludeId));
    }

    public async Task<bool> EmailExistsAsync(string email, int? excludeId = null)
    {
        var e = email.Trim().ToLower();
        if (await _context.SinhViens.AnyAsync(s => s.Email.ToLower() == e && (excludeId == null || s.Id != excludeId)))
        {
            return true;
        }

        var user = await _userManager.FindByEmailAsync(email.Trim());
        if (user == null)
        {
            return false;
        }

        // Tài khoản tìm được chính là tài khoản của sinh viên đang sửa -> không tính là trùng
        var userIdCuaSinhVien = excludeId == null
            ? null
            : await _context.SinhViens.Where(s => s.Id == excludeId).Select(s => s.ApplicationUserId).FirstOrDefaultAsync();
        return user.Id != userIdCuaSinhVien;
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> CreateSinhVienAsync(SinhVienCreateVM vm)
    {
        var lop = await _context.Lops.AsNoTracking().FirstOrDefaultAsync(l => l.Id == vm.LopId);
        if (lop == null)
        {
            return (false, new[] { "Lớp không tồn tại" });
        }

        var email = vm.Email.Trim();
        var hoTen = vm.HoTen.Trim();

        // UserManager dùng chung ApplicationDbContext (scoped) nên nằm trong transaction này
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            PhoneNumber = vm.SoDienThoai.Trim(),
            HoTen = hoTen,
            ChucDanh = "Sinh viên",
            KhoaId = lop.KhoaId,
        };

        var result = await _userManager.CreateAsync(user, vm.MatKhau);
        if (!result.Succeeded)
        {
            return (false, result.Errors.Select(e => e.Description));
        }

        result = await _userManager.AddToRoleAsync(user, SD.Role_Student);
        if (!result.Succeeded)
        {
            return (false, result.Errors.Select(e => e.Description));
        }

        _context.SinhViens.Add(new SinhVien
        {
            ApplicationUserId = user.Id,
            MaSinhVien = vm.MaSinhVien.Trim(),
            HoTen = hoTen,
            NgaySinh = vm.NgaySinh!.Value,
            GioiTinh = vm.GioiTinh!.Value,
            KhoaId = lop.KhoaId,
            LopId = lop.Id,
            Email = email,
            SoDienThoai = vm.SoDienThoai.Trim(),
        });
        await _context.SaveChangesAsync();

        await transaction.CommitAsync();
        return (true, Array.Empty<string>());
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> UpdateSinhVienAsync(SinhVienEditVM vm)
    {
        var sinhVien = await _context.SinhViens.FirstOrDefaultAsync(s => s.Id == vm.Id);
        if (sinhVien == null)
        {
            return (false, new[] { "Không tìm thấy sinh viên" });
        }

        var lop = await _context.Lops.AsNoTracking().FirstOrDefaultAsync(l => l.Id == vm.LopId);
        if (lop == null)
        {
            return (false, new[] { "Lớp không tồn tại" });
        }

        var email = vm.Email.Trim();
        var hoTen = vm.HoTen.Trim();
        var soDienThoai = vm.SoDienThoai.Trim();

        await using var transaction = await _context.Database.BeginTransactionAsync();

        sinhVien.MaSinhVien = vm.MaSinhVien.Trim();
        sinhVien.HoTen = hoTen;
        sinhVien.NgaySinh = vm.NgaySinh!.Value;
        sinhVien.GioiTinh = vm.GioiTinh!.Value;
        sinhVien.LopId = lop.Id;
        sinhVien.KhoaId = lop.KhoaId;
        sinhVien.Email = email;
        sinhVien.SoDienThoai = soDienThoai;

        // Đồng bộ tài khoản đăng nhập
        var user = await _userManager.FindByIdAsync(sinhVien.ApplicationUserId);
        if (user != null)
        {
            var doiEmail = !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase);
            user.Email = email;
            user.UserName = email;
            user.HoTen = hoTen;
            user.PhoneNumber = soDienThoai;
            user.KhoaId = lop.KhoaId;

            // UpdateAsync kiểm tra trùng tên đăng nhập/email, chuẩn hóa Normalized*, rồi SaveChanges
            // (lưu luôn các thay đổi của sinhVien vì dùng chung DbContext)
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return (false, result.Errors.Select(e => e.Description));
            }

            if (doiEmail)
            {
                // Đổi tên đăng nhập -> đăng xuất các phiên cũ của sinh viên
                await _userManager.UpdateSecurityStampAsync(user);
            }
        }
        else
        {
            await _context.SaveChangesAsync();
        }

        await transaction.CommitAsync();
        return (true, Array.Empty<string>());
    }

    public async Task<(bool Success, string Message)> DeleteSinhVienAsync(int id)
    {
        var sinhVien = await _context.SinhViens.FirstOrDefaultAsync(s => s.Id == id);
        if (sinhVien == null)
        {
            return (false, "Không tìm thấy sinh viên cần xóa");
        }

        // Xóa sinh viên sẽ xóa dây chuyền lượt thi, bài làm, điểm -> chặn nếu sinh viên đã làm bài
        var soLuotThi = await _context.LanThis.CountAsync(l => l.SinhVienDuThi!.SinhVienId == id);
        if (soLuotThi > 0)
        {
            return (false, $"Không thể xóa sinh viên {sinhVien.MaSinhVien}: sinh viên đã có {soLuotThi} lượt làm bài thi, xóa sẽ mất kết quả thi");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        _context.SinhViens.Remove(sinhVien);
        await _context.SaveChangesAsync();

        var daXoaTaiKhoan = false;
        var user = await _userManager.FindByIdAsync(sinhVien.ApplicationUserId);
        if (user != null)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var chiLaSinhVien = roles.All(r => r == SD.Role_Student);
            // Tài khoản còn gắn với hồ sơ sinh viên khác thì cũng giữ lại
            var conSinhVienKhac = await _context.SinhViens.AnyAsync(s => s.ApplicationUserId == user.Id);

            if (chiLaSinhVien && !conSinhVienKhac)
            {
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    return (false, "Không xóa được tài khoản: " + string.Join("; ", result.Errors.Select(e => e.Description)));
                }
                daXoaTaiKhoan = true;
            }
        }

        await transaction.CommitAsync();
        return (true, daXoaTaiKhoan
            ? $"Đã xóa sinh viên {sinhVien.MaSinhVien} và tài khoản {sinhVien.Email}"
            : $"Đã xóa sinh viên {sinhVien.MaSinhVien} (tài khoản đăng nhập được giữ lại)");
    }

    public async Task<SinhVienImportResult> ImportSinhVienAsync(List<SinhVienImportRow> rows, string? matKhauMacDinh)
    {
        var kq = new SinhVienImportResult();

        var lops = await _context.Lops.AsNoTracking().ToListAsync();
        var lopTheoMa = lops
            .GroupBy(l => l.MaLop.Trim().ToLower())
            .ToDictionary(g => g.Key, g => g.First());
        var maDaCo = (await _context.SinhViens.Select(s => s.MaSinhVien).ToListAsync())
            .Select(m => m.Trim().ToLower()).ToHashSet();
        var emailDaCo = (await _context.SinhViens.Select(s => s.Email).ToListAsync())
            .Concat(await _context.Users.Where(u => u.Email != null).Select(u => u.Email!).ToListAsync())
            .Select(e => e.Trim().ToLower()).ToHashSet();

        var canThem = new List<(SinhVienImportRow Row, Lop Lop, string MatKhau)>();
        foreach (var row in rows)
        {
            if (maDaCo.Contains(row.MaSinhVien.Trim().ToLower()))
            {
                kq.Skipped.Add(row.MaSinhVien);
                continue;
            }

            var ok = true;
            if (!lopTheoMa.TryGetValue(row.MaLop.Trim().ToLower(), out var lop))
            {
                kq.Errors.Add($"Dòng {row.Dong}: không tìm thấy lớp có mã \"{row.MaLop}\"");
                ok = false;
            }

            if (emailDaCo.Contains(row.Email.Trim().ToLower()))
            {
                kq.Errors.Add($"Dòng {row.Dong}: email \"{row.Email}\" đã được sử dụng");
                ok = false;
            }

            var matKhau = string.IsNullOrWhiteSpace(row.MatKhau) ? matKhauMacDinh : row.MatKhau;
            if (string.IsNullOrWhiteSpace(matKhau))
            {
                kq.Errors.Add($"Dòng {row.Dong}: thiếu mật khẩu (cột Mật khẩu trống và chưa nhập mật khẩu mặc định)");
                ok = false;
            }
            else
            {
                // Kiểm tra độ mạnh mật khẩu trước để báo lỗi theo dòng, không phải dừng giữa chừng
                foreach (var validator in _userManager.PasswordValidators)
                {
                    var r = await validator.ValidateAsync(_userManager, new ApplicationUser(), matKhau);
                    if (!r.Succeeded)
                    {
                        kq.Errors.Add($"Dòng {row.Dong}: mật khẩu không hợp lệ – " + string.Join(" ", r.Errors.Select(e => e.Description)));
                        ok = false;
                    }
                }
            }

            if (ok)
            {
                canThem.Add((row, lop!, matKhau!));
            }
        }

        if (kq.Errors.Count > 0 || canThem.Count == 0)
        {
            return kq;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        foreach (var (row, lop, matKhau) in canThem)
        {
            var email = row.Email.Trim();
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                PhoneNumber = row.SoDienThoai,
                HoTen = row.HoTen,
                ChucDanh = "Sinh viên",
                KhoaId = lop.KhoaId,
            };

            var result = await _userManager.CreateAsync(user, matKhau);
            if (result.Succeeded)
            {
                result = await _userManager.AddToRoleAsync(user, SD.Role_Student);
            }
            if (!result.Succeeded)
            {
                // Thoát mà không Commit -> transaction rollback, không nhập dòng nào
                kq.Errors.Add($"Dòng {row.Dong}: " + string.Join(" ", result.Errors.Select(e => e.Description)));
                return kq;
            }

            _context.SinhViens.Add(new SinhVien
            {
                ApplicationUserId = user.Id,
                MaSinhVien = row.MaSinhVien,
                HoTen = row.HoTen,
                NgaySinh = row.NgaySinh,
                GioiTinh = row.GioiTinh,
                KhoaId = lop.KhoaId,
                LopId = lop.Id,
                Email = email,
                SoDienThoai = row.SoDienThoai,
            });
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        kq.Added = canThem.Count;
        return kq;
    }
}
