using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Data;
using ExamOnlineSystem.Models;
using ExamOnlineSystem.Models.ViewModels;
using ExamOnlineSystem.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExamOnlineSystem.Business.Service;

// Quản lý tài khoản đăng nhập (bảng AspNetUsers) và vai trò của tài khoản.
// Tài khoản không bị xóa – chỉ khóa (DangHoatDong = false) để giữ nguyên dữ liệu đã tạo.
public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UserService(ApplicationDbContext context, UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<List<UserListItemVM>> GetAllUserAsync()
    {
        // Chiếu thẳng sang VM: tên khoa và mã hồ sơ lấy bằng truy vấn con
        var users = await _context.Users
            .OrderBy(u => u.HoTen)
            .Select(u => new UserListItemVM
            {
                Id = u.Id,
                HoTen = u.HoTen,
                Email = u.Email ?? "",
                SoDienThoai = u.PhoneNumber ?? "",
                ChucDanh = u.ChucDanh,
                KhoaId = u.KhoaId,
                TenKhoa = _context.Khoas.Where(k => k.Id == u.KhoaId).Select(k => k.TenKhoa).FirstOrDefault() ?? "",
                DangHoatDong = u.DangHoatDong,
                NgayTao = u.NgayTao,
                LanDangNhapCuoi = u.LanDangNhapCuoi,
                MaSinhVien = _context.SinhViens.Where(s => s.ApplicationUserId == u.Id).Select(s => s.MaSinhVien).FirstOrDefault(),
                MaGiaoVien = _context.GiaoViens.Where(g => g.ApplicationUserId == u.Id).Select(g => g.MaGiaoVien).FirstOrDefault()
            })
            .AsNoTracking()
            .ToListAsync();

        // Vai trò lấy một lượt rồi ghép vào danh sách (tránh gọi UserManager cho từng tài khoản)
        var vaiTroTheoUser = await (from ur in _context.UserRoles
                join r in _context.Roles on ur.RoleId equals r.Id
                select new { ur.UserId, TenVaiTro = r.Name ?? "" })
            .AsNoTracking()
            .ToListAsync();

        var map = vaiTroTheoUser
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(x => x.TenVaiTro).OrderBy(x => x)));

        foreach (var u in users)
        {
            u.VaiTro = map.TryGetValue(u.Id, out var vt) ? vt : "";
        }

        return users;
    }

    public async Task<UserEditVM?> GetUserForEditAsync(string id)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            return null;
        }

        var vaiTro = (await _userManager.GetRolesAsync(user)).FirstOrDefault() ?? "";

        return new UserEditVM
        {
            Id = user.Id,
            HoTen = user.HoTen,
            Email = user.Email ?? "",
            SoDienThoai = user.PhoneNumber,
            ChucDanh = user.ChucDanh,
            KhoaId = user.KhoaId ?? 0,
            VaiTro = vaiTro,
            DangHoatDong = user.DangHoatDong,
            MaSinhVien = await _context.SinhViens.Where(s => s.ApplicationUserId == id).Select(s => s.MaSinhVien).FirstOrDefaultAsync(),
            MaGiaoVien = await _context.GiaoViens.Where(g => g.ApplicationUserId == id).Select(g => g.MaGiaoVien).FirstOrDefaultAsync()
        };
    }

    public async Task<bool> EmailExistsAsync(string email, string? excludeUserId = null)
    {
        var user = await _userManager.FindByEmailAsync(email.Trim());
        return user != null && user.Id != excludeUserId;
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> CreateUserAsync(UserCreateVM vm)
    {
        if (!VaiTroLabel.TatCa.Contains(vm.VaiTro))
        {
            return (false, new[] { "Vai trò không hợp lệ" });
        }

        var (khoaId, loiKhoa) = await DocKhoaIdAsync(vm.KhoaId);
        if (loiKhoa != null)
        {
            return (false, new[] { loiKhoa });
        }

        await DamBaoVaiTroTonTaiAsync(vm.VaiTro);

        var email = vm.Email.Trim();

        // UserManager dùng chung ApplicationDbContext (scoped) nên nằm trong transaction này
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            PhoneNumber = string.IsNullOrWhiteSpace(vm.SoDienThoai) ? null : vm.SoDienThoai.Trim(),
            HoTen = vm.HoTen.Trim(),
            ChucDanh = string.IsNullOrWhiteSpace(vm.ChucDanh) ? VaiTroLabel.Ten(vm.VaiTro) : vm.ChucDanh.Trim(),
            KhoaId = khoaId,
            DangHoatDong = true
        };

        var result = await _userManager.CreateAsync(user, vm.MatKhau);
        if (!result.Succeeded)
        {
            return (false, result.Errors.Select(e => e.Description));
        }

        result = await _userManager.AddToRoleAsync(user, vm.VaiTro);
        if (!result.Succeeded)
        {
            return (false, result.Errors.Select(e => e.Description));
        }

        await transaction.CommitAsync();
        return (true, Array.Empty<string>());
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> UpdateUserAsync(UserEditVM vm, string currentUserId)
    {
        if (!VaiTroLabel.TatCa.Contains(vm.VaiTro))
        {
            return (false, new[] { "Vai trò không hợp lệ" });
        }

        var user = await _userManager.FindByIdAsync(vm.Id);
        if (user == null)
        {
            return (false, new[] { "Không tìm thấy tài khoản" });
        }

        var (khoaId, loiKhoa) = await DocKhoaIdAsync(vm.KhoaId);
        if (loiKhoa != null)
        {
            return (false, new[] { loiKhoa });
        }

        var vaiTroHienTai = (await _userManager.GetRolesAsync(user)).ToList();
        var doiVaiTro = !(vaiTroHienTai.Count == 1 && vaiTroHienTai[0] == vm.VaiTro);

        if (doiVaiTro)
        {
            if (user.Id == currentUserId)
            {
                return (false, new[] { "Không thể tự đổi vai trò của tài khoản đang đăng nhập" });
            }

            if (vaiTroHienTai.Contains(SD.Role_Admin) && !await ConQuanTriKhacAsync(user.Id, chiDangHoatDong: false))
            {
                return (false, new[] { "Hệ thống phải còn ít nhất một tài khoản quản trị viên" });
            }
        }

        var email = vm.Email.Trim();
        var doiEmail = !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase);

        await using var transaction = await _context.Database.BeginTransactionAsync();

        await DamBaoVaiTroTonTaiAsync(vm.VaiTro);

        user.Email = email;
        user.UserName = email;
        user.HoTen = vm.HoTen.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(vm.SoDienThoai) ? null : vm.SoDienThoai.Trim();
        user.ChucDanh = string.IsNullOrWhiteSpace(vm.ChucDanh) ? null : vm.ChucDanh.Trim();
        user.KhoaId = khoaId;

        // UpdateAsync kiểm tra trùng tên đăng nhập/email, chuẩn hóa Normalized*, rồi SaveChanges
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return (false, result.Errors.Select(e => e.Description));
        }

        if (doiVaiTro)
        {
            if (vaiTroHienTai.Count > 0)
            {
                result = await _userManager.RemoveFromRolesAsync(user, vaiTroHienTai);
                if (!result.Succeeded)
                {
                    return (false, result.Errors.Select(e => e.Description));
                }
            }

            result = await _userManager.AddToRoleAsync(user, vm.VaiTro);
            if (!result.Succeeded)
            {
                return (false, result.Errors.Select(e => e.Description));
            }
        }

        // Đặt lại mật khẩu: để trống = giữ nguyên mật khẩu cũ
        if (!string.IsNullOrWhiteSpace(vm.MatKhauMoi))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            result = await _userManager.ResetPasswordAsync(user, token, vm.MatKhauMoi);
            if (!result.Succeeded)
            {
                return (false, result.Errors.Select(e => e.Description));
            }
        }

        await DongBoHoSoAsync(user);

        // Đổi email (tên đăng nhập), vai trò hoặc mật khẩu -> đăng xuất các phiên cũ
        if (doiEmail || doiVaiTro || !string.IsNullOrWhiteSpace(vm.MatKhauMoi))
        {
            await _userManager.UpdateSecurityStampAsync(user);
        }

        await transaction.CommitAsync();
        return (true, Array.Empty<string>());
    }

    public async Task<(bool Success, string Message)> SetTrangThaiAsync(string id, bool khoa, string currentUserId)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            return (false, "Không tìm thấy tài khoản");
        }

        if (khoa && user.Id == currentUserId)
        {
            return (false, "Không thể khóa tài khoản đang đăng nhập");
        }

        if (khoa && await _userManager.IsInRoleAsync(user, SD.Role_Admin)
                 && !await ConQuanTriKhacAsync(user.Id, chiDangHoatDong: true))
        {
            return (false, "Hệ thống phải còn ít nhất một quản trị viên đang hoạt động");
        }

        if (user.DangHoatDong == !khoa)
        {
            return (true, khoa ? $"Tài khoản {user.Email} đã bị khóa trước đó" : $"Tài khoản {user.Email} đang hoạt động");
        }

        user.DangHoatDong = !khoa;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return (false, string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        if (khoa)
        {
            // Đổi security stamp để cookie đang đăng nhập của tài khoản bị khóa mất hiệu lực
            await _userManager.UpdateSecurityStampAsync(user);
        }

        return (true, khoa
            ? $"Đã khóa tài khoản {user.Email}"
            : $"Đã mở khóa tài khoản {user.Email}");
    }

    // 0 (hoặc khoa không tồn tại) -> null: tài khoản không gắn khoa nào
    private async Task<(int? KhoaId, string? Error)> DocKhoaIdAsync(int khoaIdForm)
    {
        if (khoaIdForm <= 0)
        {
            return (null, null);
        }

        return await _context.Khoas.AnyAsync(k => k.Id == khoaIdForm)
            ? (khoaIdForm, null)
            : (null, "Khoa không tồn tại");
    }

    private async Task DamBaoVaiTroTonTaiAsync(string vaiTro)
    {
        if (!await _roleManager.RoleExistsAsync(vaiTro))
        {
            await _roleManager.CreateAsync(new IdentityRole(vaiTro));
        }
    }

    // Còn tài khoản quản trị nào khác ngoài userId không (dùng để không khóa/hạ quyền admin cuối cùng)
    private async Task<bool> ConQuanTriKhacAsync(string userId, bool chiDangHoatDong)
    {
        var adminRoleId = await _context.Roles
            .Where(r => r.Name == SD.Role_Admin)
            .Select(r => r.Id)
            .FirstOrDefaultAsync();

        if (adminRoleId == null)
        {
            return false;
        }

        var query = from ur in _context.UserRoles
            join u in _context.Users on ur.UserId equals u.Id
            where ur.RoleId == adminRoleId && u.Id != userId
            select u;

        if (chiDangHoatDong)
        {
            query = query.Where(u => u.DangHoatDong);
        }

        return await query.AnyAsync();
    }

    // Hồ sơ sinh viên / giảng viên lưu trùng họ tên, email, SĐT -> cập nhật theo tài khoản để không lệch dữ liệu
    private async Task DongBoHoSoAsync(ApplicationUser user)
    {
        var sinhVien = await _context.SinhViens.FirstOrDefaultAsync(s => s.ApplicationUserId == user.Id);
        if (sinhVien != null)
        {
            sinhVien.HoTen = user.HoTen;
            sinhVien.Email = user.Email ?? sinhVien.Email;
            sinhVien.SoDienThoai = user.PhoneNumber ?? sinhVien.SoDienThoai;
        }

        var giaoVien = await _context.GiaoViens.FirstOrDefaultAsync(g => g.ApplicationUserId == user.Id);
        if (giaoVien != null)
        {
            giaoVien.HoTen = user.HoTen;
            giaoVien.SoDienThoai = user.PhoneNumber ?? giaoVien.SoDienThoai;
        }

        if (sinhVien != null || giaoVien != null)
        {
            await _context.SaveChangesAsync();
        }
    }
}
