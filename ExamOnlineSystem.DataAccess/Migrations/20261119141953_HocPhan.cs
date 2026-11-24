using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ExamOnlineSystem.Migrations;

/// <inheritdoc />
public partial class _20261119141953_HocPhan : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "HocPhans",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                KhoaId = table.Column<int>(type: "integer", nullable: false),
                MonHocId = table.Column<int>(type: "integer", nullable: false),
                MaHocPhan = table.Column<string>(type: "text", nullable: false),
                TenHocPhan = table.Column<string>(type: "text", nullable: false),
                SoTinChi = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_HocPhans", x => x.Id);
                table.ForeignKey(
                    name: "FK_HocPhans_Khoas_KhoaId",
                    column: x => x.KhoaId,
                    principalTable: "Khoas",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_HocPhans_MonHocs_MonHocId",
                    column: x => x.MonHocId,
                    principalTable: "MonHocs",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_HocPhans_KhoaId",
            table: "HocPhans",
            column: "KhoaId");

        migrationBuilder.CreateIndex(
            name: "IX_HocPhans_MonHocId",
            table: "HocPhans",
            column: "MonHocId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "HocPhans");
    }
}
