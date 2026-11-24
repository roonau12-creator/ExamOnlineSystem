using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ExamOnlineSystem.Migrations;

/// <inheritdoc />
public partial class _20261120143703_AddTieuDeGocToTable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TieuDeGocs",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Ten = table.Column<string>(type: "text", nullable: false),
                MoTa = table.Column<string>(type: "text", nullable: true),
                LoaiDe = table.Column<string>(type: "text", nullable: false),
                TenTepGoc = table.Column<string>(type: "text", nullable: false),
                DuongDanTep = table.Column<string>(type: "text", nullable: false),
                KichThuoc = table.Column<int>(type: "integer", nullable: true),
                LaMacDinh = table.Column<bool>(type: "boolean", nullable: false),
                TrangThai = table.Column<string>(type: "text", nullable: false),
                NguoiTao = table.Column<string>(type: "text", nullable: true),
                NgayTao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                NgayCapNhat = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TieuDeGocs", x => x.Id);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "TieuDeGocs");
    }
}
