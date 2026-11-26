using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamOnlineSystem.Migrations;

/// <inheritdoc />
public partial class _20261125024425_SeedLopHocPhan : Migration
{
    // 10 lớp học phần mẫu của năm học 2026-2027 (7 lớp học kỳ 1, 2 lớp học kỳ 2, 1 lớp học kỳ hè).
    // Mã lớp theo quy ước <MaHocPhan>.<số nhóm>, tên lớp theo quy ước "<Tên học phần> - Nhóm <n>".
    private static readonly string[] MaLopHocPhanMau =
    {
        "CSDL01.01",
        "CSDL01.02",
        "LTHDT01.01",
        "CTDL01.01",
        "MANGMT01.01",
        "TIENGANH01.01",
        "TCC01.01",
        "ATTT01.01",
        "CNPM01.01",
        "THDC01.01"
    };

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Học phần không được seed bằng migration nên không thể chèn HocPhanId cứng:
        // tra Id qua MaHocPhan / MaNamHoc và chỉ chèn lớp nào có đủ học phần + năm học.
        // NOT EXISTS để chạy lại không vi phạm unique index trên MaLopHocPhan.
        // Không chèn Id tường minh -> sequence của cột Id tự nhảy, không cần setval.
        migrationBuilder.Sql(
            """
            INSERT INTO "LopHocPhans"
                ("MaLopHocPhan", "TenLopHocPhan", "HocPhanId", "NamHocId", "HocKy", "SiSoToiDa", "GhiChu", "NgayTao")
            SELECT m.ma, m.ten, hp."Id", nh."Id", m.hocky, m.siso, m.ghichu, TIMESTAMPTZ '2026-11-25 00:00:00+00'
            FROM (VALUES
                ('CSDL01.01',     'Cơ sở dữ liệu - Nhóm 1',                  'CSDL01',     '2026-2027', 1, 60, 'Dữ liệu mẫu'),
                ('CSDL01.02',     'Cơ sở dữ liệu - Nhóm 2',                  'CSDL01',     '2026-2027', 1, 60, 'Dữ liệu mẫu'),
                ('LTHDT01.01',    'Lập trình hướng đối tượng - Nhóm 1',      'LTHDT01',    '2026-2027', 1, 50, 'Dữ liệu mẫu'),
                ('CTDL01.01',     'Cấu trúc dữ liệu và giải thuật - Nhóm 1', 'CTDL01',     '2026-2027', 1, 55, 'Dữ liệu mẫu'),
                ('MANGMT01.01',   'Mạng máy tính - Nhóm 1',                  'MANGMT01',   '2026-2027', 1, 45, 'Dữ liệu mẫu'),
                ('TIENGANH01.01', 'Tiếng Anh 1 - Nhóm 1',                    'TIENGANH01', '2026-2027', 1, 40, 'Dữ liệu mẫu'),
                ('TCC01.01',      'Toán cao cấp A1 - Nhóm 1',                'TCC01',      '2026-2027', 1, 80, 'Dữ liệu mẫu'),
                ('ATTT01.01',     'An toàn thông tin - Nhóm 1',              'ATTT01',     '2026-2027', 2, 50, 'Dữ liệu mẫu'),
                ('CNPM01.01',     'Công nghệ phần mềm - Nhóm 1',             'CNPM01',     '2026-2027', 2, 50, 'Dữ liệu mẫu'),
                ('THDC01.01',     'Tin học đại cương - Nhóm 1',              'THDC01',     '2026-2027', 3, 30, 'Lớp học kỳ hè - dữ liệu mẫu')
            ) AS m (ma, ten, mahp, manh, hocky, siso, ghichu)
            JOIN "HocPhans" hp ON hp."MaHocPhan" = m.mahp
            JOIN "NamHocs"  nh ON nh."MaNamHoc"  = m.manh
            WHERE NOT EXISTS (
                SELECT 1 FROM "LopHocPhans" l WHERE l."MaLopHocPhan" = m.ma
            );
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Phân công giảng viên / sinh viên của các lớp này bị xóa theo (FK cascade).
        foreach (var ma in MaLopHocPhanMau)
        {
            migrationBuilder.DeleteData(
                table: "LopHocPhans",
                keyColumn: "MaLopHocPhan",
                keyValue: ma);
        }
    }
}
