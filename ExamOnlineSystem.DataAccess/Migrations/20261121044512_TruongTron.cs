using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ExamOnlineSystem.Migrations;

/// <inheritdoc />
public partial class _20261121044512_TruongTron : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TruongTrons",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Ma = table.Column<string>(type: "text", nullable: false),
                Ten = table.Column<string>(type: "text", nullable: false),
                MoTa = table.Column<string>(type: "text", nullable: true),
                Nguon = table.Column<string>(type: "text", nullable: false),
                CotNguon = table.Column<string>(type: "text", nullable: true),
                BatBuoc = table.Column<bool>(type: "boolean", nullable: false),
                HeThong = table.Column<bool>(type: "boolean", nullable: false),
                SoThuTu = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TruongTrons", x => x.Id);
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "TruongTrons");
    }
}
