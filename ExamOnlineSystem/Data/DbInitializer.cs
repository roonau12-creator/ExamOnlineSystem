using ExamOnlineSystem.Models;
using ExamOnlineSystem.Utility;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExamOnlineSystem.Data;

// Chạy 1 lần lúc khởi động: tạo các vai trò và tài khoản Admin mặc định nếu chưa có.
// Thông tin tài khoản lấy từ mục "AdminSeed" trong appsettings.json.
public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

        foreach (var role in new[] { SD.Role_Admin, SD.Role_Teacher, SD.Role_Student })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Dữ liệu mẫu chỉ tạo khi chạy môi trường Development
        if (scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await SeedGiaoVienMauAsync(context, userManager, logger);
        }

        var email = config["AdminSeed:Email"];
        var password = config["AdminSeed:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var admin = await userManager.FindByEmailAsync(email);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                HoTen = config["AdminSeed:HoTen"] ?? "Quản trị viên",
                ChucDanh = "Quản trị hệ thống",
            };

            var result = await userManager.CreateAsync(admin, password);
            if (!result.Succeeded)
            {
                logger.LogError("Không tạo được tài khoản admin: {Loi}",
                    string.Join("; ", result.Errors.Select(e => e.Description)));
                return;
            }
            logger.LogInformation("Đã tạo tài khoản admin {Email}", email);
        }

        if (!await userManager.IsInRoleAsync(admin, SD.Role_Admin))
        {
            await userManager.AddToRoleAsync(admin, SD.Role_Admin);
        }
    }

    // Mật khẩu chung của các tài khoản giảng viên mẫu (đăng nhập bằng email)
    private const string MatKhauGiaoVienMau = "GiaoVien@123";

    // 10 giảng viên mẫu: (mã GV, họ tên, mã bộ môn, chức danh, số điện thoại)
    private static readonly (string Ma, string HoTen, string MaBoMon, string ChucDanh, string Sdt)[] GiaoVienMau =
    {
        ("GV002", "Trần Thị Bình",      "KHMT",  "Giảng viên",          "0912345602"),
        ("GV003", "Lê Hoàng Cường",     "KTPM",  "Giảng viên chính",    "0912345603"),
        ("GV004", "Phạm Minh Đức",      "MMT",   "Trưởng bộ môn",       "0912345604"),
        ("GV005", "Hoàng Thị Thu Hà",   "HTTT",  "Giảng viên",          "0912345605"),
        ("GV006", "Vũ Ngọc Lan",        "TA",    "Giảng viên",          "0912345606"),
        ("GV007", "Đặng Quốc Huy",      "TOAN",  "Giảng viên cao cấp",  "0912345607"),
        ("GV008", "Bùi Thanh Mai",      "VL",    "Giảng viên",          "0912345608"),
        ("GV009", "Ngô Văn Nam",        "DT",    "Giảng viên chính",    "0912345609"),
        ("GV010", "Đỗ Thị Phương",      "KTKT",  "Giảng viên",          "0912345610"),
        ("GV011", "Trịnh Văn Quang",    "TRIET", "Trưởng bộ môn",       "0912345611"),
    };

    // Tạo tài khoản (vai trò Teacher) + hồ sơ giảng viên. Bỏ qua giảng viên đã có (theo mã GV hoặc email)
    // và giảng viên có mã bộ môn không tồn tại, nên chạy lại nhiều lần vẫn an toàn.
    private static async Task SeedGiaoVienMauAsync(ApplicationDbContext context,
        UserManager<ApplicationUser> userManager, ILogger logger)
    {
        var boMons = await context.BoMons.AsNoTracking().ToDictionaryAsync(b => b.MaBoMon, StringComparer.OrdinalIgnoreCase);
        var maDaCo = (await context.GiaoViens.Select(g => g.MaGiaoVien).ToListAsync())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var soDaTao = 0;

        foreach (var gv in GiaoVienMau)
        {
            var email = $"giaovien{gv.Ma[2..].TrimStart('0').PadLeft(2, '0')}@examonline.vn"; // GV002 -> giaovien02
            if (maDaCo.Contains(gv.Ma) || await userManager.FindByEmailAsync(email) != null)
            {
                continue;
            }
            if (!boMons.TryGetValue(gv.MaBoMon, out var boMon))
            {
                logger.LogWarning("Bỏ qua giảng viên mẫu {Ma}: không có bộ môn {MaBoMon}", gv.Ma, gv.MaBoMon);
                continue;
            }

            await using var transaction = await context.Database.BeginTransactionAsync();

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                PhoneNumber = gv.Sdt,
                HoTen = gv.HoTen,
                ChucDanh = gv.ChucDanh,
                KhoaId = boMon.KhoaId,
                DangHoatDong = true
            };

            var result = await userManager.CreateAsync(user, MatKhauGiaoVienMau);
            if (result.Succeeded)
            {
                result = await userManager.AddToRoleAsync(user, SD.Role_Teacher);
            }
            if (!result.Succeeded)
            {
                logger.LogError("Không tạo được giảng viên mẫu {Ma}: {Loi}", gv.Ma,
                    string.Join("; ", result.Errors.Select(e => e.Description)));
                continue; // transaction chưa commit -> tự rollback
            }

            context.GiaoViens.Add(new GiaoVien
            {
                ApplicationUserId = user.Id,
                MaGiaoVien = gv.Ma,
                HoTen = gv.HoTen,
                BoMonId = boMon.Id,
                SoDienThoai = gv.Sdt
            });
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            soDaTao++;
        }

        if (soDaTao > 0)
        {
            logger.LogInformation("Đã tạo {SoLuong} giảng viên mẫu (mật khẩu: {MatKhau})", soDaTao, MatKhauGiaoVienMau);
        }
    }
}
