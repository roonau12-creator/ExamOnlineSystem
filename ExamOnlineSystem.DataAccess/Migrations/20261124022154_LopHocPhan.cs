using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ExamOnlineSystem.Migrations;

/// <inheritdoc />
public partial class _20261124022154_LopHocPhan : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "LopHocPhans",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                MaLopHocPhan = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                TenLopHocPhan = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                HocPhanId = table.Column<int>(type: "integer", nullable: false),
                NamHocId = table.Column<int>(type: "integer", nullable: false),
                HocKy = table.Column<int>(type: "integer", nullable: false),
                SiSoToiDa = table.Column<int>(type: "integer", nullable: false),
                GhiChu = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: ""),
                NgayTao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LopHocPhans", x => x.Id);
                table.ForeignKey(
                    name: "FK_LopHocPhans_HocPhans_HocPhanId",
                    column: x => x.HocPhanId,
                    principalTable: "HocPhans",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_LopHocPhans_NamHocs_NamHocId",
                    column: x => x.NamHocId,
                    principalTable: "NamHocs",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "LopHocPhanGiaoViens",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                LopHocPhanId = table.Column<int>(type: "integer", nullable: false),
                GiaoVienId = table.Column<int>(type: "integer", nullable: false),
                VaiTro = table.Column<int>(type: "integer", nullable: false),
                NgayPhanCong = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LopHocPhanGiaoViens", x => x.Id);
                table.ForeignKey(
                    name: "FK_LopHocPhanGiaoViens_GiaoViens_GiaoVienId",
                    column: x => x.GiaoVienId,
                    principalTable: "GiaoViens",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_LopHocPhanGiaoViens_LopHocPhans_LopHocPhanId",
                    column: x => x.LopHocPhanId,
                    principalTable: "LopHocPhans",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "LopHocPhanSinhViens",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                LopHocPhanId = table.Column<int>(type: "integer", nullable: false),
                SinhVienId = table.Column<int>(type: "integer", nullable: false),
                NgayDangKy = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LopHocPhanSinhViens", x => x.Id);
                table.ForeignKey(
                    name: "FK_LopHocPhanSinhViens_LopHocPhans_LopHocPhanId",
                    column: x => x.LopHocPhanId,
                    principalTable: "LopHocPhans",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_LopHocPhanSinhViens_SinhViens_SinhVienId",
                    column: x => x.SinhVienId,
                    principalTable: "SinhViens",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_LopHocPhanGiaoViens_GiaoVienId",
            table: "LopHocPhanGiaoViens",
            column: "GiaoVienId");

        migrationBuilder.CreateIndex(
            name: "IX_LopHocPhanGiaoViens_LopHocPhanId_GiaoVienId",
            table: "LopHocPhanGiaoViens",
            columns: new[] { "LopHocPhanId", "GiaoVienId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_LopHocPhans_HocPhanId",
            table: "LopHocPhans",
            column: "HocPhanId");

        migrationBuilder.CreateIndex(
            name: "IX_LopHocPhans_MaLopHocPhan",
            table: "LopHocPhans",
            column: "MaLopHocPhan",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_LopHocPhans_NamHocId_HocKy",
            table: "LopHocPhans",
            columns: new[] { "NamHocId", "HocKy" });

        migrationBuilder.CreateIndex(
            name: "IX_LopHocPhanSinhViens_LopHocPhanId_SinhVienId",
            table: "LopHocPhanSinhViens",
            columns: new[] { "LopHocPhanId", "SinhVienId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_LopHocPhanSinhViens_SinhVienId",
            table: "LopHocPhanSinhViens",
            column: "SinhVienId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "LopHocPhanGiaoViens");

        migrationBuilder.DropTable(
            name: "LopHocPhanSinhViens");

        migrationBuilder.DropTable(
            name: "LopHocPhans");
    }
}
