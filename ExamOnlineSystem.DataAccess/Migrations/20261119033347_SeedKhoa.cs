using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ExamOnlineSystem.Migrations;

/// <inheritdoc />
public partial class _20261119033347_SeedKhoa : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertData(
            table: "Khoas",
            columns: new[] { "Id", "MaKhoa", "SoThuTu", "TenKhoa" },
            values: new object[,]
            {
                { 1, "12345", 1, "Hạ tầng kỹ thuật" },
                { 2, "45678", 2, "Kỹ thuật xây dựng" }
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DeleteData(
            table: "Khoas",
            keyColumn: "Id",
            keyValue: 1);

        migrationBuilder.DeleteData(
            table: "Khoas",
            keyColumn: "Id",
            keyValue: 2);
    }
}
