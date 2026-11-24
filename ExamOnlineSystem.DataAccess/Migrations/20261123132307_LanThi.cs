using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ExamOnlineSystem.Migrations;

/// <inheritdoc />
public partial class _20261123132307_LanThi : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "LanThis",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                SinhVienDuThiId = table.Column<int>(type: "integer", nullable: false),
                LanThu = table.Column<int>(type: "integer", nullable: false),
                ThoiGianBatDau = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ThoiGianNopBai = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                TrangThai = table.Column<int>(type: "integer", nullable: false),
                Diem = table.Column<double>(type: "double precision", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LanThis", x => x.Id);
                table.ForeignKey(
                    name: "FK_LanThis_SinhVienDuThis_SinhVienDuThiId",
                    column: x => x.SinhVienDuThiId,
                    principalTable: "SinhVienDuThis",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_LanThis_SinhVienDuThiId_LanThu",
            table: "LanThis",
            columns: new[] { "SinhVienDuThiId", "LanThu" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "LanThis");
    }
}
