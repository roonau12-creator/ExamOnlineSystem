using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ExamOnlineSystem.Migrations;

/// <inheritdoc />
public partial class _20261123130519_SinhVienDuThi : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SinhVienDuThis",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                LichThiId = table.Column<int>(type: "integer", nullable: false),
                SinhVienId = table.Column<int>(type: "integer", nullable: false),
                ThoiGianBatDau = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ThoiGianNopBai = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                TrangThai = table.Column<int>(type: "integer", nullable: false),
                Diem = table.Column<double>(type: "double precision", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SinhVienDuThis", x => x.Id);
                table.ForeignKey(
                    name: "FK_SinhVienDuThis_LichThis_LichThiId",
                    column: x => x.LichThiId,
                    principalTable: "LichThis",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_SinhVienDuThis_SinhViens_SinhVienId",
                    column: x => x.SinhVienId,
                    principalTable: "SinhViens",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SinhVienDuThis_LichThiId_SinhVienId",
            table: "SinhVienDuThis",
            columns: new[] { "LichThiId", "SinhVienId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_SinhVienDuThis_SinhVienId",
            table: "SinhVienDuThis",
            column: "SinhVienId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "SinhVienDuThis");
    }
}
