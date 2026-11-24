using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ExamOnlineSystem.Migrations;

/// <inheritdoc />
public partial class _20261124015852_NamHoc : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "NamHocs",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                MaNamHoc = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                TenNamHoc = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                NgayBatDau = table.Column<DateOnly>(type: "date", nullable: false),
                NgayKetThuc = table.Column<DateOnly>(type: "date", nullable: false),
                LaNamHienTai = table.Column<bool>(type: "boolean", nullable: false),
                GhiChu = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, defaultValue: "")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_NamHocs", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_NamHocs_MaNamHoc",
            table: "NamHocs",
            column: "MaNamHoc",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "NamHocs");
    }
}
