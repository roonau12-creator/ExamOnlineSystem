using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ExamOnlineSystem.Migrations;

/// <inheritdoc />
public partial class _20261121124825_SinhVien : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SinhViens",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ApplicationUserId = table.Column<string>(type: "text", nullable: false),
                MaSinhVien = table.Column<string>(type: "text", nullable: false),
                HoTen = table.Column<string>(type: "text", nullable: false),
                NgaySinh = table.Column<DateOnly>(type: "date", nullable: false),
                GioiTinh = table.Column<bool>(type: "boolean", nullable: false),
                KhoaId = table.Column<int>(type: "integer", nullable: false),
                Email = table.Column<string>(type: "text", nullable: false),
                SoDienThoai = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SinhViens", x => x.Id);
                table.ForeignKey(
                    name: "FK_SinhViens_AspNetUsers_ApplicationUserId",
                    column: x => x.ApplicationUserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_SinhViens_Khoas_KhoaId",
                    column: x => x.KhoaId,
                    principalTable: "Khoas",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SinhViens_ApplicationUserId",
            table: "SinhViens",
            column: "ApplicationUserId");

        migrationBuilder.CreateIndex(
            name: "IX_SinhViens_KhoaId",
            table: "SinhViens",
            column: "KhoaId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "SinhViens");
    }
}
