using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ExamOnlineSystem.Migrations;

/// <inheritdoc />
public partial class _20261121140639_GiaoVien : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "GiaoViens",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ApplicationUserId = table.Column<string>(type: "text", nullable: false),
                MaGiaoVien = table.Column<string>(type: "text", nullable: false),
                HoTen = table.Column<string>(type: "text", nullable: false),
                BoMonId = table.Column<int>(type: "integer", nullable: false),
                SoDienThoai = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GiaoViens", x => x.Id);
                table.ForeignKey(
                    name: "FK_GiaoViens_AspNetUsers_ApplicationUserId",
                    column: x => x.ApplicationUserId,
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_GiaoViens_BoMons_BoMonId",
                    column: x => x.BoMonId,
                    principalTable: "BoMons",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_GiaoViens_ApplicationUserId",
            table: "GiaoViens",
            column: "ApplicationUserId");

        migrationBuilder.CreateIndex(
            name: "IX_GiaoViens_BoMonId",
            table: "GiaoViens",
            column: "BoMonId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "GiaoViens");
    }
}
