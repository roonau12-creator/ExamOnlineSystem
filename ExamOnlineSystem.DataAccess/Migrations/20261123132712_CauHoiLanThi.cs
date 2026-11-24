using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ExamOnlineSystem.Migrations;

/// <inheritdoc />
public partial class _20261123132712_CauHoiLanThi : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CauHoiLanThis",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                LanThiId = table.Column<int>(type: "integer", nullable: false),
                CauHoiGocId = table.Column<int>(type: "integer", nullable: false),
                NoiDung = table.Column<string>(type: "text", nullable: false),
                LoaiCauHoi = table.Column<short>(type: "smallint", nullable: false),
                Diem = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                ThuTu = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CauHoiLanThis", x => x.Id);
                table.ForeignKey(
                    name: "FK_CauHoiLanThis_CauHois_CauHoiGocId",
                    column: x => x.CauHoiGocId,
                    principalTable: "CauHois",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CauHoiLanThis_LanThis_LanThiId",
                    column: x => x.LanThiId,
                    principalTable: "LanThis",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CauHoiLanThis_CauHoiGocId",
            table: "CauHoiLanThis",
            column: "CauHoiGocId");

        migrationBuilder.CreateIndex(
            name: "IX_CauHoiLanThis_LanThiId_ThuTu",
            table: "CauHoiLanThis",
            columns: new[] { "LanThiId", "ThuTu" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CauHoiLanThis");
    }
}
