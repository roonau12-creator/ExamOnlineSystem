using ExamOnlineSystem.Business.Service;
using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Data;
using ExamOnlineSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IKhoaService, KhoaService>();
builder.Services.AddScoped<INamHocService, NamHocService>();
builder.Services.AddScoped<ILopHocPhanService, LopHocPhanService>();
builder.Services.AddScoped<IBoMonService, BoMonService>();
builder.Services.AddScoped<IHocPhanService, HocPhanService>();
builder.Services.AddScoped<ILopService, LopService>();
builder.Services.AddScoped<ISinhVienService, SinhVienService>();
builder.Services.AddScoped<IDanhMucCauHoiService, DanhMucCauHoiService>();
builder.Services.AddScoped<ICauHoiService, CauHoiService>();
builder.Services.AddScoped<IDeThiService, DeThiService>();
builder.Services.AddScoped<IGiaoVienService, GiaoVienService>();
builder.Services.AddScoped<ILichThiService, LichThiService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IKetQuaThiService, KetQuaThiService>();
builder.Services.AddScoped<IThongKeService, ThongKeService>();
builder.Services.AddScoped<ILamBaiService, LamBaiService>();
builder.Services.AddSingleton<ExamOnlineSystem.Services.HinhAnhCauHoiStorage>();
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequiredUniqueChars = 1;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
// Kiểm tra lại security stamp mỗi phút: khóa tài khoản / đổi vai trò có hiệu lực gần như ngay
// (mặc định 30 phút, tài khoản bị khóa vẫn dùng được trang trong suốt thời gian đó)
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.FromMinutes(1));
// Trang đăng nhập nằm trong Area Identity (mặc định của Identity là /Account/Login)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Customer/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


// Tạo vai trò + tài khoản Admin mặc định nếu chưa có
await DbInitializer.SeedAsync(app.Services);

app.Run();