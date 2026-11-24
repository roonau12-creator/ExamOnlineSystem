using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ExamOnlineSystem.Migrations;

/// <inheritdoc />
public partial class _20261121124236_Lop : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "TieuDeGocs");

        migrationBuilder.DropTable(
            name: "TruongTrons");

        migrationBuilder.CreateTable(
            name: "Lops",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                MaLop = table.Column<string>(type: "text", nullable: false),
                TenLop = table.Column<string>(type: "text", nullable: false),
                KhoaId = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Lops", x => x.Id);
                table.ForeignKey(
                    name: "FK_Lops_Khoas_KhoaId",
                    column: x => x.KhoaId,
                    principalTable: "Khoas",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Lops_KhoaId",
            table: "Lops",
            column: "KhoaId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Lops");

        migrationBuilder.CreateTable(
            name: "TieuDeGocs",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                DuongDanTep = table.Column<string>(type: "text", nullable: false),
                KichThuoc = table.Column<int>(type: "integer", nullable: true),
                LaMacDinh = table.Column<bool>(type: "boolean", nullable: false),
                LoaiDe = table.Column<string>(type: "text", nullable: false),
                MoTa = table.Column<string>(type: "text", nullable: true),
                NgayCapNhat = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                NgayTao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                NguoiTao = table.Column<string>(type: "text", nullable: true),
                Ten = table.Column<string>(type: "text", nullable: false),
                TenTepGoc = table.Column<string>(type: "text", nullable: false),
                TrangThai = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TieuDeGocs", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "TruongTrons",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                BatBuoc = table.Column<bool>(type: "boolean", nullable: false),
                CotNguon = table.Column<string>(type: "text", nullable: true),
                HeThong = table.Column<bool>(type: "boolean", nullable: false),
                Ma = table.Column<string>(type: "text", nullable: false),
                MoTa = table.Column<string>(type: "text", nullable: true),
                Nguon = table.Column<string>(type: "text", nullable: false),
                SoThuTu = table.Column<int>(type: "integer", nullable: false),
                Ten = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TruongTrons", x => x.Id);
            });
    }
}
