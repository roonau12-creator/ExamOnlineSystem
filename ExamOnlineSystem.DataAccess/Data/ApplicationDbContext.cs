using ExamOnlineSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ExamOnlineSystem.Data;

// Dùng ApplicationUser làm user của Identity: các cột mới nằm trong bảng AspNetUsers, truy cập qua DbSet Users
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
        
    }
    public DbSet<Khoa> Khoas { get; set; }
    public DbSet<NamHoc> NamHocs { get; set; }
    public DbSet<LopHocPhan> LopHocPhans { get; set; }
    public DbSet<LopHocPhanGiaoVien> LopHocPhanGiaoViens { get; set; }
    public DbSet<LopHocPhanSinhVien> LopHocPhanSinhViens { get; set; }
    public DbSet<BoMon> BoMons { get; set; }
    public DbSet<MonHoc> MonHocs { get; set; }
    public DbSet<HocPhan> HocPhans { get; set; }
    public DbSet<Lop> Lops { get; set; }
    public DbSet<SinhVien> SinhViens { get; set; }
    public DbSet<GiaoVien>GiaoViens { get; set; }
    public DbSet<DanhMucCauHoi>  DanhMucCauHois { get; set; }
    public DbSet<CauHoi> CauHois { get; set; }
    public DbSet<DapAn>  DapAns { get; set; }
    public DbSet<DeThi> DeThis { get; set; }
    public DbSet<CauHoiDeThi>  CauHoiDeThis { get; set; }
    public DbSet<LichThi> LichThis { get; set; }
    public DbSet<LichThiLop> LichThiLops { get; set; }
    public DbSet<SinhVienDuThi> SinhVienDuThis { get; set; }
    public DbSet<LanThi> LanThis { get; set; }
    public DbSet<CauHoiLanThi> CauHoiLanThis { get; set; }
    public DbSet<DapAnLanThi> DapAnLanThis { get; set; }
    public DbSet<BaiLam> BaiLams { get; set; }
    public DbSet<DapAnDaChon> DapAnDaChons { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // MoTa không bắt buộc trên form nhưng cột DB giữ NOT NULL như migration đã tạo
        modelBuilder.Entity<MonHoc>().Property(m => m.MoTa).IsRequired();
        modelBuilder.Entity<MonHoc>().Property(m => m.TenTiengAnh).IsRequired().HasDefaultValue(string.Empty);
        modelBuilder.Entity<DanhMucCauHoi>().Property(d => d.MoTa).IsRequired();

        // Mã đề thi không được trùng
        modelBuilder.Entity<DeThi>().HasIndex(d => d.MaDeThi).IsUnique();

        // Năm học: mã năm học không được trùng, GhiChu không bắt buộc nhưng cột giữ NOT NULL
        modelBuilder.Entity<NamHoc>().HasIndex(n => n.MaNamHoc).IsUnique();
        modelBuilder.Entity<NamHoc>().Property(n => n.GhiChu).IsRequired().HasDefaultValue(string.Empty);

        // Lớp học phần: mã không được trùng; không xóa được học phần / năm học đang có lớp học phần.
        // Phân công giảng viên và sinh viên bị xóa theo khi xóa lớp học phần, nhưng không xóa
        // bản ghi giảng viên / sinh viên gốc. Một giảng viên / sinh viên chỉ xuất hiện 1 lần trong 1 lớp.
        modelBuilder.Entity<LopHocPhan>().HasIndex(l => l.MaLopHocPhan).IsUnique();
        modelBuilder.Entity<LopHocPhan>().Property(l => l.GhiChu).IsRequired().HasDefaultValue(string.Empty);
        modelBuilder.Entity<LopHocPhan>().HasOne(l => l.HocPhan).WithMany().HasForeignKey(l => l.HocPhanId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<LopHocPhan>().HasOne(l => l.NamHoc).WithMany().HasForeignKey(l => l.NamHocId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<LopHocPhan>().HasIndex(l => new { l.NamHocId, l.HocKy });

        modelBuilder.Entity<LopHocPhanGiaoVien>().HasOne(x => x.LopHocPhan).WithMany(l => l.LopHocPhanGiaoViens).HasForeignKey(x => x.LopHocPhanId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<LopHocPhanGiaoVien>().HasOne(x => x.GiaoVien).WithMany().HasForeignKey(x => x.GiaoVienId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<LopHocPhanGiaoVien>().HasIndex(x => new { x.LopHocPhanId, x.GiaoVienId }).IsUnique();

        modelBuilder.Entity<LopHocPhanSinhVien>().HasOne(x => x.LopHocPhan).WithMany(l => l.LopHocPhanSinhViens).HasForeignKey(x => x.LopHocPhanId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<LopHocPhanSinhVien>().HasOne(x => x.SinhVien).WithMany().HasForeignKey(x => x.SinhVienId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<LopHocPhanSinhVien>().HasIndex(x => new { x.LopHocPhanId, x.SinhVienId }).IsUnique();

        // Lịch thi: không xóa được đề thi / giáo viên đang có lịch thi (tránh mất lịch thi ngầm);
        // xóa lịch thi hoặc lớp thì xóa luôn dòng lớp tham gia. Một lớp chỉ xuất hiện 1 lần trong 1 lịch thi.
        modelBuilder.Entity<LichThi>().HasOne(l => l.DeThi).WithMany().HasForeignKey(l => l.DeThiId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<LichThi>().HasOne(l => l.GiaoVien).WithMany().HasForeignKey(l => l.GiaoVienId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<LichThi>().HasIndex(l => l.ThoiGianBatDau);
        modelBuilder.Entity<LichThiLop>().HasOne(x => x.LichThi).WithMany(l => l.LichThiLops).HasForeignKey(x => x.LichThiId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<LichThiLop>().HasIndex(x => new { x.LichThiId, x.LopId }).IsUnique();

        // Sinh viên dự thi: mỗi sinh viên chỉ có một bản ghi trong một lịch thi
        modelBuilder.Entity<SinhVienDuThi>().HasOne(x => x.LichThi).WithMany().HasForeignKey(x => x.LichThiId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SinhVienDuThi>().HasOne(x => x.SinhVien).WithMany().HasForeignKey(x => x.SinhVienId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<SinhVienDuThi>().HasIndex(x => new { x.LichThiId, x.SinhVienId }).IsUnique();

        // Lần thi: gắn với SinhVienDuThi, mỗi lần thi có LanThu duy nhất trong cùng SinhVienDuThi
        modelBuilder.Entity<LanThi>().HasOne(x => x.SinhVienDuThi).WithMany().HasForeignKey(x => x.SinhVienDuThiId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<LanThi>().HasIndex(x => new { x.SinhVienDuThiId, x.LanThu }).IsUnique();

        // Câu hỏi lần thi: gắn với LanThi (Cascade) và CauHoiGoc (Restrict)
        modelBuilder.Entity<CauHoiLanThi>().HasOne(x => x.LanThi).WithMany().HasForeignKey(x => x.LanThiId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CauHoiLanThi>().HasOne(x => x.CauHoiGoc).WithMany().HasForeignKey(x => x.CauHoiGocId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CauHoiLanThi>().HasIndex(x => new { x.LanThiId, x.ThuTu }).IsUnique();

        // Đáp án lần thi: gắn với CauHoiLanThi (Cascade) và DapAnGoc (Restrict)
        modelBuilder.Entity<DapAnLanThi>().HasOne(x => x.CauHoiLanThi).WithMany().HasForeignKey(x => x.CauHoiLanThiId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<DapAnLanThi>().HasOne(x => x.DapAnGoc).WithMany().HasForeignKey(x => x.DapAnGocId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DapAnLanThi>().HasIndex(x => new { x.CauHoiLanThiId, x.ThuTu }).IsUnique();

        // Bài làm: mỗi câu hỏi trong một lần thi chỉ có một bài làm
        modelBuilder.Entity<BaiLam>().HasOne(x => x.LanThi).WithMany().HasForeignKey(x => x.LanThiId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<BaiLam>().HasOne(x => x.CauHoiLanThi).WithMany().HasForeignKey(x => x.CauHoiLanThiId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<BaiLam>().HasIndex(x => new { x.LanThiId, x.CauHoiLanThiId }).IsUnique();

        // Đáp án đã chọn: gắn với BaiLam và DapAnLanThi, không chọn trùng cùng một đáp án
        modelBuilder.Entity<DapAnDaChon>().HasOne(x => x.BaiLam).WithMany().HasForeignKey(x => x.BaiLamId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<DapAnDaChon>().HasOne(x => x.DapAnLanThi).WithMany().HasForeignKey(x => x.DapAnLanThiId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<DapAnDaChon>().HasIndex(x => new { x.BaiLamId, x.DapAnLanThiId }).IsUnique();

        // Khoa công tác của người dùng: xóa khoa thì người dùng chỉ mất liên kết, không bị xóa theo
        modelBuilder.Entity<ApplicationUser>()
            .HasOne<Khoa>()
            .WithMany()
            .HasForeignKey(u => u.KhoaId)
            .OnDelete(DeleteBehavior.SetNull);
        // 10 năm học mẫu: 2020-2021 .. 2029-2030; năm học hiện tại là 2026-2027
        modelBuilder.Entity<NamHoc>().HasData(
            Enumerable.Range(0, 10).Select(i =>
            {
                var namBatDau = 2020 + i;
                return new NamHoc
                {
                    Id = i + 1,
                    MaNamHoc = $"{namBatDau}-{namBatDau + 1}",
                    TenNamHoc = $"Năm học {namBatDau} - {namBatDau + 1}",
                    NgayBatDau = new DateOnly(namBatDau, 9, 1),
                    NgayKetThuc = new DateOnly(namBatDau + 1, 8, 31),
                    LaNamHienTai = namBatDau == 2026,
                    GhiChu = namBatDau < 2026
                        ? "Đã kết thúc"
                        : namBatDau == 2026
                            ? "Năm học hiện hành"
                            : "Dự kiến"
                };
            }).ToArray());

        modelBuilder.Entity<Khoa>().HasData(
            new Khoa
            {
                Id = 1, MaKhoa = "12345", TenKhoa = "Hạ tầng kỹ thuật", SoThuTu = 1
            },
            new Khoa
            {
                Id = 2,MaKhoa = "45678",TenKhoa = "Kỹ thuật xây dựng", SoThuTu = 2
            }
        );
    }
}
