using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ExamOnlineSystem.Migrations;

/// <inheritdoc />
public partial class _20261123133216_DapAnLanThi : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DapAnLanThis",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                CauHoiLanThiId = table.Column<int>(type: "integer", nullable: false),
                DapAnGocId = table.Column<int>(type: "integer", nullable: false),
                NoiDung = table.Column<string>(type: "text", nullable: false),
                LaDapAnDung = table.Column<bool>(type: "boolean", nullable: false),
                ThuTu = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DapAnLanThis", x => x.Id);
                table.ForeignKey(
                    name: "FK_DapAnLanThis_CauHoiLanThis_CauHoiLanThiId",
                    column: x => x.CauHoiLanThiId,
                    principalTable: "CauHoiLanThis",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_DapAnLanThis_DapAns_DapAnGocId",
                    column: x => x.DapAnGocId,
                    principalTable: "DapAns",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_DapAnLanThis_CauHoiLanThiId_ThuTu",
            table: "DapAnLanThis",
            columns: new[] { "CauHoiLanThiId", "ThuTu" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_DapAnLanThis_DapAnGocId",
            table: "DapAnLanThis",
            column: "DapAnGocId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "DapAnLanThis");
    }
}
