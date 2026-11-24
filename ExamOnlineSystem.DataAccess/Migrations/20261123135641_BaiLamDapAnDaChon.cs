using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ExamOnlineSystem.Migrations;

/// <inheritdoc />
public partial class _20261123135641_BaiLamDapAnDaChon : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "BaiLams",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                LanThiId = table.Column<int>(type: "integer", nullable: false),
                CauHoiLanThiId = table.Column<int>(type: "integer", nullable: false),
                NoiDungTraLoi = table.Column<string>(type: "text", nullable: true),
                Diem = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                NhanXet = table.Column<string>(type: "text", nullable: true),
                NgayCapNhat = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BaiLams", x => x.Id);
                table.ForeignKey(
                    name: "FK_BaiLams_CauHoiLanThis_CauHoiLanThiId",
                    column: x => x.CauHoiLanThiId,
                    principalTable: "CauHoiLanThis",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_BaiLams_LanThis_LanThiId",
                    column: x => x.LanThiId,
                    principalTable: "LanThis",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "DapAnDaChons",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                BaiLamId = table.Column<int>(type: "integer", nullable: false),
                DapAnLanThiId = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DapAnDaChons", x => x.Id);
                table.ForeignKey(
                    name: "FK_DapAnDaChons_BaiLams_BaiLamId",
                    column: x => x.BaiLamId,
                    principalTable: "BaiLams",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_DapAnDaChons_DapAnLanThis_DapAnLanThiId",
                    column: x => x.DapAnLanThiId,
                    principalTable: "DapAnLanThis",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_BaiLams_CauHoiLanThiId",
            table: "BaiLams",
            column: "CauHoiLanThiId");

        migrationBuilder.CreateIndex(
            name: "IX_BaiLams_LanThiId_CauHoiLanThiId",
            table: "BaiLams",
            columns: new[] { "LanThiId", "CauHoiLanThiId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DapAnDaChons_BaiLamId_DapAnLanThiId",
            table: "DapAnDaChons",
            columns: new[] { "BaiLamId", "DapAnLanThiId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DapAnDaChons_DapAnLanThiId",
            table: "DapAnDaChons",
            column: "DapAnLanThiId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "DapAnDaChons");

        migrationBuilder.DropTable(
            name: "BaiLams");
    }
}
