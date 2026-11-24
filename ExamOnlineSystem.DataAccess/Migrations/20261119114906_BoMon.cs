using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ExamOnlineSystem.Migrations;

/// <inheritdoc />
public partial class _20261119114906_BoMon : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "BoMons",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                KhoaId = table.Column<int>(type: "integer", nullable: false),
                MaBoMon = table.Column<string>(type: "text", nullable: false),
                TenBoMon = table.Column<string>(type: "text", nullable: false),
                SoThuTu = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_BoMons", x => x.Id);
                table.ForeignKey(
                    name: "FK_BoMons_Khoas_KhoaId",
                    column: x => x.KhoaId,
                    principalTable: "Khoas",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_BoMons_KhoaId",
            table: "BoMons",
            column: "KhoaId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "BoMons");
    }
}
