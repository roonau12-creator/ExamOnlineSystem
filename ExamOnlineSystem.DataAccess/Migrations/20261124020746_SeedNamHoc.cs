using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ExamOnlineSystem.Migrations;

/// <inheritdoc />
public partial class _20261124020746_SeedNamHoc : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertData(
            table: "NamHocs",
            columns: new[] { "Id", "GhiChu", "LaNamHienTai", "MaNamHoc", "NgayBatDau", "NgayKetThuc", "TenNamHoc" },
            values: new object[,]
            {
                { 1, "Đã kết thúc", false, "2020-2021", new DateOnly(2020, 9, 1), new DateOnly(2021, 8, 31), "Năm học 2020 - 2021" },
                { 2, "Đã kết thúc", false, "2021-2022", new DateOnly(2021, 9, 1), new DateOnly(2022, 8, 31), "Năm học 2021 - 2022" },
                { 3, "Đã kết thúc", false, "2022-2023", new DateOnly(2022, 9, 1), new DateOnly(2023, 8, 31), "Năm học 2022 - 2023" },
                { 4, "Đã kết thúc", false, "2023-2024", new DateOnly(2023, 9, 1), new DateOnly(2024, 8, 31), "Năm học 2023 - 2024" },
                { 5, "Đã kết thúc", false, "2024-2025", new DateOnly(2024, 9, 1), new DateOnly(2025, 8, 31), "Năm học 2024 - 2025" },
                { 6, "Đã kết thúc", false, "2025-2026", new DateOnly(2025, 9, 1), new DateOnly(2026, 8, 31), "Năm học 2025 - 2026" },
                { 7, "Năm học hiện hành", true, "2026-2027", new DateOnly(2026, 9, 1), new DateOnly(2027, 8, 31), "Năm học 2026 - 2027" },
                { 8, "Dự kiến", false, "2027-2028", new DateOnly(2027, 9, 1), new DateOnly(2028, 8, 31), "Năm học 2027 - 2028" },
                { 9, "Dự kiến", false, "2028-2029", new DateOnly(2028, 9, 1), new DateOnly(2029, 8, 31), "Năm học 2028 - 2029" },
                { 10, "Dự kiến", false, "2029-2030", new DateOnly(2029, 9, 1), new DateOnly(2030, 8, 31), "Năm học 2029 - 2030" }
            });

        // Seed chèn Id tường minh nên sequence của cột Id không tự nhảy theo.
        // Đẩy sequence lên quá Id lớn nhất để thêm năm học từ giao diện không bị trùng khóa chính.
        migrationBuilder.Sql(
            """
            SELECT setval(pg_get_serial_sequence('"NamHocs"', 'Id'), (SELECT COALESCE(MAX("Id"), 1) FROM "NamHocs"));
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DeleteData(
            table: "NamHocs",
            keyColumn: "Id",
            keyValue: 1);

        migrationBuilder.DeleteData(
            table: "NamHocs",
            keyColumn: "Id",
            keyValue: 2);

        migrationBuilder.DeleteData(
            table: "NamHocs",
            keyColumn: "Id",
            keyValue: 3);

        migrationBuilder.DeleteData(
            table: "NamHocs",
            keyColumn: "Id",
            keyValue: 4);

        migrationBuilder.DeleteData(
            table: "NamHocs",
            keyColumn: "Id",
            keyValue: 5);

        migrationBuilder.DeleteData(
            table: "NamHocs",
            keyColumn: "Id",
            keyValue: 6);

        migrationBuilder.DeleteData(
            table: "NamHocs",
            keyColumn: "Id",
            keyValue: 7);

        migrationBuilder.DeleteData(
            table: "NamHocs",
            keyColumn: "Id",
            keyValue: 8);

        migrationBuilder.DeleteData(
            table: "NamHocs",
            keyColumn: "Id",
            keyValue: 9);

        migrationBuilder.DeleteData(
            table: "NamHocs",
            keyColumn: "Id",
            keyValue: 10);
    }
}
