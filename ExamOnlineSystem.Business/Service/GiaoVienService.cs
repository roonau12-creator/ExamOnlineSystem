using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Data;
using ExamOnlineSystem.Models;
using ExamOnlineSystem.Models.ViewModels;
using ExamOnlineSystem.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExamOnlineSystem.Business.Service;

public class GiaoVienService : IGiaoVienService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public GiaoVienService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<bool> MaGiaoVienExistsAsync(string maGiaoVien, int? excludeId = null)
    {
        var ma = maGiaoVien.Trim().ToLower();
        return await _context.GiaoViens.AnyAsync(g => g.MaGiaoVien.ToLower() == ma && (excludeId == null || g.Id != excludeId));
    }

    public async Task<bool> EmailExistsAsync(string email, int? excludeId = null)
    {
        var user = await _userManager.FindByEmailAsync(email.Trim());
        if (user == null)
        {
            return false;
        }
        if (excludeId == null)
        {
            return true;
        }
        // Email thuộc chính tài khoản của giảng viên đang sửa thì không tính là trùng
        return !await _context.GiaoViens.AnyAsync(g => g.Id == excludeId && g.ApplicationUserId == user.Id);
    }

    public async Task<GiaoVienEditVM?> GetGiaoVienForEditAsync(int id)
    {
        return await _context.GiaoViens
            .Where(g => g.Id == id)
            .Select(g => new GiaoVienEditVM
            {
                Id = g.Id,
                MaGiaoVien = g.MaGiaoVien,
                HoTen = g.HoTen,
                ChucDanh = g.ApplicationUser!.ChucDanh,
                BoMonId = g.BoMonId,
                Email = g.ApplicationUser!.Email ?? "",
                SoDienThoai = g.SoDienThoai
            })
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> UpdateGiaoVienAsync(GiaoVienEditVM vm)
    {
        var giaoVien = await _context.GiaoViens.FirstOrDefaultAsync(g => g.Id == vm.Id);
        if (giaoVien == null)
        {
            return (false, new[] { "Không tìm thấy giảng viên" });
        }

        var boMon = await _context.BoMons.AsNoTracking().FirstOrDefaultAsync(b => b.Id == vm.BoMonId);
        if (boMon == null)
        {
            return (false, new[] { "Bộ môn không tồn tại" });
        }

        var email = vm.Email.Trim();
        var hoTen = vm.HoTen.Trim();
        var sdt = vm.SoDienThoai.Trim();

        await using var transaction = await _context.Database.BeginTransactionAsync();

        giaoVien.MaGiaoVien = vm.MaGiaoVien.Trim();
        giaoVien.HoTen = hoTen;
        giaoVien.BoMonId = boMon.Id;
        giaoVien.SoDienThoai = sdt;

        // Đồng bộ tài khoản đăng nhập
        var user = await _userManager.FindByIdAsync(giaoVien.ApplicationUserId);
        if (user == null)
        {
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return (true, Array.Empty<string>());
        }

        var doiEmail = !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase);
        user.Email = email;
        user.UserName = email;
        user.HoTen = hoTen;
        user.PhoneNumber = sdt;
        user.ChucDanh = string.IsNullOrWhiteSpace(vm.ChucDanh) ? "Giảng viên" : vm.ChucDanh.Trim();
        user.KhoaId = boMon.KhoaId;

        // UpdateAsync kiểm tra trùng tên đăng nhập/email, chuẩn hóa Normalized*, rồi SaveChanges
        // (lưu luôn các thay đổi của giaoVien vì dùng chung DbContext)
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return (false, result.Errors.Select(e => e.Description));
        }

        // Đặt lại mật khẩu: để trống = giữ nguyên mật khẩu cũ
        var doiMatKhau = !string.IsNullOrWhiteSpace(vm.MatKhauMoi);
        if (doiMatKhau)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            result = await _userManager.ResetPasswordAsync(user, token, vm.MatKhauMoi!);
            if (!result.Succeeded)
            {
                return (false, result.Errors.Select(e => e.Description));
            }
        }
        else if (doiEmail)
        {
            // Đổi tên đăng nhập -> đăng xuất các phiên cũ (ResetPassword đã tự làm việc này)
            await _userManager.UpdateSecurityStampAsync(user);
        }

        await transaction.CommitAsync();
        return (true, Array.Empty<string>());
    }

    public async Task<(bool Success, int GiaoVienId, IEnumerable<string> Errors)> CreateGiaoVienAsync(GiaoVienCreateVM vm)
    {
        var boMon = await _context.BoMons.AsNoTracking().FirstOrDefaultAsync(b => b.Id == vm.BoMonId);
        if (boMon == null)
        {
            return (false, 0, new[] { "Bộ môn không tồn tại" });
        }

        var email = vm.Email.Trim();
        var hoTen = vm.HoTen.Trim();
        var sdt = vm.SoDienThoai.Trim();

        // UserManager dùng chung ApplicationDbContext (scoped) nên nằm trong transaction này
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            PhoneNumber = sdt,
            HoTen = hoTen,
            ChucDanh = string.IsNullOrWhiteSpace(vm.ChucDanh) ? "Giảng viên" : vm.ChucDanh.Trim(),
            KhoaId = boMon.KhoaId,
            DangHoatDong = true
        };

        var result = await _userManager.CreateAsync(user, vm.MatKhau);
        if (!result.Succeeded)
        {
            return (false, 0, result.Errors.Select(e => e.Description));
        }

        result = await _userManager.AddToRoleAsync(user, SD.Role_Teacher);
        if (!result.Succeeded)
        {
            return (false, 0, result.Errors.Select(e => e.Description));
        }

        var giaoVien = new GiaoVien
        {
            ApplicationUserId = user.Id,
            MaGiaoVien = vm.MaGiaoVien.Trim(),
            HoTen = hoTen,
            BoMonId = boMon.Id,
            SoDienThoai = sdt
        };
        _context.GiaoViens.Add(giaoVien);
        await _context.SaveChangesAsync();

        await transaction.CommitAsync();
        return (true, giaoVien.Id, Array.Empty<string>());
    }

    public async Task<List<GiaoVienListItemVM>> GetAllGiaoVienAsync()
    {
        return await ProjectToListItem(_context.GiaoViens
                .OrderBy(g => g.BoMon!.Khoa!.SoThuTu)
                .ThenBy(g => g.BoMon!.SoThuTu)
                .ThenBy(g => g.MaGiaoVien))
            .ToListAsync();
    }

    public async Task<GiaoVienDetailVM?> GetGiaoVienDetailAsync(int id)
    {
        var gv = await ProjectToListItem(_context.GiaoViens.Where(g => g.Id == id)).FirstOrDefaultAsync();
        if (gv == null)
        {
            return null;
        }

        var boMon = await _context.BoMons
            .Where(b => b.Id == gv.BoMonId)
            .Select(b => new { b.MaBoMon, b.KhoaId, MaKhoa = b.Khoa!.MaKhoa })
            .AsNoTracking()
            .FirstOrDefaultAsync();

        return new GiaoVienDetailVM
        {
            GiaoVien = gv,
            MaBoMon = boMon?.MaBoMon ?? "",
            KhoaId = boMon?.KhoaId ?? 0,
            MaKhoa = boMon?.MaKhoa ?? "",
            DongNghieps = await ProjectToListItem(_context.GiaoViens
                    .Where(g => g.BoMonId == gv.BoMonId && g.Id != id)
                    .OrderBy(g => g.MaGiaoVien))
                .ToListAsync(),
            LopHocPhans = await _context.LopHocPhanGiaoViens
                .Where(x => x.GiaoVienId == id)
                .OrderByDescending(x => x.LopHocPhan!.NamHoc!.NgayBatDau)
                .ThenByDescending(x => x.LopHocPhan!.HocKy)
                .ThenBy(x => x.LopHocPhan!.MaLopHocPhan)
                .Select(x => new GiaoVienLopHocPhanItem
                {
                    Id = x.LopHocPhanId,
                    MaLopHocPhan = x.LopHocPhan!.MaLopHocPhan,
                    TenLopHocPhan = x.LopHocPhan.TenLopHocPhan,
                    MaHocPhan = x.LopHocPhan.HocPhan!.MaHocPhan,
                    TenHocPhan = x.LopHocPhan.HocPhan.TenHocPhan,
                    SoTinChi = x.LopHocPhan.HocPhan.SoTinChi,
                    TenNamHoc = x.LopHocPhan.NamHoc!.TenNamHoc,
                    LaNamHienTai = x.LopHocPhan.NamHoc.LaNamHienTai,
                    HocKy = x.LopHocPhan.HocKy,
                    VaiTro = x.VaiTro,
                    SoSinhVien = x.LopHocPhan.LopHocPhanSinhViens.Count,
                    SiSoToiDa = x.LopHocPhan.SiSoToiDa
                })
                .AsNoTracking()
                .ToListAsync()
        };
    }

    public async Task<(bool Success, string Message)> GanGiaoVienVaoBoMonAsync(int boMonId, IReadOnlyCollection<int> giaoVienIds)
    {
        if (giaoVienIds.Count == 0)
        {
            return (false, "Vui lòng chọn ít nhất một giảng viên");
        }

        var boMon = await _context.BoMons.AsNoTracking().FirstOrDefaultAsync(b => b.Id == boMonId);
        if (boMon == null)
        {
            return (false, "Bộ môn không tồn tại");
        }

        var ids = giaoVienIds.Distinct().ToList();
        var giaoViens = await _context.GiaoViens
            .Include(g => g.ApplicationUser)
            .Where(g => ids.Contains(g.Id))
            .ToListAsync();
        if (giaoViens.Count != ids.Count)
        {
            return (false, "Có giảng viên không tồn tại, vui lòng tải lại trang");
        }

        var canDoi = giaoViens.Where(g => g.BoMonId != boMonId).ToList();
        foreach (var gv in canDoi)
        {
            gv.BoMonId = boMonId;
            if (gv.ApplicationUser != null)
            {
                gv.ApplicationUser.KhoaId = boMon.KhoaId; // khoa công tác đi theo bộ môn
            }
        }
        await _context.SaveChangesAsync();

        var daOSan = giaoViens.Count - canDoi.Count;
        return (true, $"Đã gán {canDoi.Count} giảng viên vào {boMon.TenBoMon}" +
                      (daOSan > 0 ? $" ({daOSan} giảng viên đã ở bộ môn này từ trước)" : ""));
    }

    public async Task<(bool Success, string Message)> DeleteGiaoVienAsync(int id, string? currentUserId)
    {
        var giaoVien = await _context.GiaoViens.FirstOrDefaultAsync(g => g.Id == id);
        if (giaoVien == null)
        {
            return (false, "Không tìm thấy giảng viên cần xóa");
        }

        if (giaoVien.ApplicationUserId == currentUserId)
        {
            return (false, "Không thể xóa giảng viên gắn với tài khoản đang đăng nhập");
        }

        // Câu hỏi, đề thi sẽ bị xóa dây chuyền; lịch thi, phân công thì DB chặn -> kiểm tra trước, báo rõ lý do
        var lienQuan = new List<string>();
        void Them(int soLuong, string ten)
        {
            if (soLuong > 0) lienQuan.Add($"{soLuong} {ten}");
        }
        Them(await _context.CauHois.CountAsync(c => c.GiaoVienId == id), "câu hỏi");
        Them(await _context.DeThis.CountAsync(d => d.GiaoVienId == id), "đề thi");
        Them(await _context.LichThis.CountAsync(l => l.GiaoVienId == id), "lịch thi");
        Them(await _context.LopHocPhanGiaoViens.CountAsync(x => x.GiaoVienId == id), "lớp học phần được phân công");
        if (lienQuan.Count > 0)
        {
            return (false, $"Không thể xóa giảng viên {giaoVien.MaGiaoVien}: giảng viên còn {string.Join(", ", lienQuan)}. " +
                           "Hãy chuyển hoặc xóa các dữ liệu này trước, hoặc khóa tài khoản thay vì xóa.");
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        _context.GiaoViens.Remove(giaoVien);
        await _context.SaveChangesAsync();

        var daXoaTaiKhoan = false;
        var user = await _userManager.FindByIdAsync(giaoVien.ApplicationUserId);
        if (user != null)
        {
            // Chỉ xóa tài khoản thuần giảng viên; tài khoản có vai trò khác (vd Admin) được giữ lại
            var roles = await _userManager.GetRolesAsync(user);
            var chiLaGiaoVien = roles.All(r => r == SD.Role_Teacher);
            var conHoSoKhac = await _context.GiaoViens.AnyAsync(g => g.ApplicationUserId == user.Id)
                              || await _context.SinhViens.AnyAsync(sv => sv.ApplicationUserId == user.Id);

            if (chiLaGiaoVien && !conHoSoKhac)
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
            ? $"Đã xóa giảng viên {giaoVien.MaGiaoVien} và tài khoản {user!.Email}"
            : $"Đã xóa giảng viên {giaoVien.MaGiaoVien} (tài khoản đăng nhập được giữ lại)");
    }

    // Chiếu thẳng sang VM; số câu hỏi / đề thi tính bằng truy vấn con
    private IQueryable<GiaoVienListItemVM> ProjectToListItem(IQueryable<GiaoVien> query)
    {
        return query
            .Select(g => new GiaoVienListItemVM
            {
                Id = g.Id,
                MaGiaoVien = g.MaGiaoVien,
                HoTen = g.HoTen,
                Email = g.ApplicationUser!.Email ?? "",
                SoDienThoai = g.SoDienThoai,
                ChucDanh = g.ApplicationUser!.ChucDanh,
                BoMonId = g.BoMonId,
                TenBoMon = g.BoMon!.TenBoMon,
                KhoaId = g.BoMon!.KhoaId,
                TenKhoa = g.BoMon!.Khoa!.TenKhoa,
                DangHoatDong = g.ApplicationUser!.DangHoatDong,
                LanDangNhapCuoi = g.ApplicationUser!.LanDangNhapCuoi,
                SoCauHoi = _context.CauHois.Count(c => c.GiaoVienId == g.Id),
                SoDeThi = _context.DeThis.Count(d => d.GiaoVienId == g.Id)
            })
            .AsNoTracking();
    }
}
