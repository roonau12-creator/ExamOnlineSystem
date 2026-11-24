using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ExamOnlineSystem.Migrations;

/// <inheritdoc />
public partial class _20261119140305_MonHoc : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MonHocs",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                KhoaId = table.Column<int>(type: "integer", nullable: false),
                BoMonId = table.Column<int>(type: "integer", nullable: false),
                MaMonHoc = table.Column<string>(type: "text", nullable: false),
                TenMonHoc = table.Column<string>(type: "text", nullable: false),
                MoTa = table.Column<string>(type: "text", nullable: false),
                HinhThucThi = table.Column<string>(type: "text", nullable: false),
                ThoiGianThi = table.Column<int>(type: "integer", nullable: false),
                SoThuTu = table.Column<int>(type: "integer", nullable: false),
                TrangThai = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MonHocs", x => x.Id);
                table.ForeignKey(
                    name: "FK_MonHocs_BoMons_BoMonId",
                    column: x => x.BoMonId,
                    principalTable: "BoMons",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_MonHocs_Khoas_KhoaId",
                    column: x => x.KhoaId,
                    principalTable: "Khoas",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MonHocs_BoMonId",
            table: "MonHocs",
            column: "BoMonId");

        migrationBuilder.CreateIndex(
            name: "IX_MonHocs_KhoaId",
            table: "MonHocs",
            column: "KhoaId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "MonHocs");
    }
}
