using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ExamOnlineSystem.Migrations;

/// <inheritdoc />
public partial class _20261123024745_LichThi : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "LichThis",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                TenLichThi = table.Column<string>(type: "text", nullable: false),
                DeThiId = table.Column<int>(type: "integer", nullable: false),
                ThoiGianBatDau = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ThoiGianKetThuc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                GiaoVienId = table.Column<int>(type: "integer", nullable: false),
                GhiChu = table.Column<string>(type: "text", nullable: false),
                NgayTao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LichThis", x => x.Id);
                table.ForeignKey(
                    name: "FK_LichThis_DeThis_DeThiId",
                    column: x => x.DeThiId,
                    principalTable: "DeThis",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_LichThis_GiaoViens_GiaoVienId",
                    column: x => x.GiaoVienId,
                    principalTable: "GiaoViens",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "LichThiLops",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                LichThiId = table.Column<int>(type: "integer", nullable: false),
                LopId = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LichThiLops", x => x.Id);
                table.ForeignKey(
                    name: "FK_LichThiLops_LichThis_LichThiId",
                    column: x => x.LichThiId,
                    principalTable: "LichThis",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_LichThiLops_Lops_LopId",
                    column: x => x.LopId,
                    principalTable: "Lops",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_LichThiLops_LichThiId_LopId",
            table: "LichThiLops",
            columns: new[] { "LichThiId", "LopId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_LichThiLops_LopId",
            table: "LichThiLops",
            column: "LopId");

        migrationBuilder.CreateIndex(
            name: "IX_LichThis_DeThiId",
            table: "LichThis",
            column: "DeThiId");

        migrationBuilder.CreateIndex(
            name: "IX_LichThis_GiaoVienId",
            table: "LichThis",
            column: "GiaoVienId");

        migrationBuilder.CreateIndex(
            name: "IX_LichThis_ThoiGianBatDau",
            table: "LichThis",
            column: "ThoiGianBatDau");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "LichThiLops");

        migrationBuilder.DropTable(
            name: "LichThis");
    }
}
