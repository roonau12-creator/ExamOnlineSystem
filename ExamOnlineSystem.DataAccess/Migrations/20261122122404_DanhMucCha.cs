using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamOnlineSystem.Migrations;

/// <inheritdoc />
public partial class _20261122122404_DanhMucCha : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "DanhMucChaId",
            table: "DanhMucCauHois",
            type: "integer",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_DanhMucCauHois_DanhMucChaId",
            table: "DanhMucCauHois",
            column: "DanhMucChaId");

        migrationBuilder.AddForeignKey(
            name: "FK_DanhMucCauHois_DanhMucCauHois_DanhMucChaId",
            table: "DanhMucCauHois",
            column: "DanhMucChaId",
            principalTable: "DanhMucCauHois",
            principalColumn: "Id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_DanhMucCauHois_DanhMucCauHois_DanhMucChaId",
            table: "DanhMucCauHois");

        migrationBuilder.DropIndex(
            name: "IX_DanhMucCauHois_DanhMucChaId",
            table: "DanhMucCauHois");

        migrationBuilder.DropColumn(
            name: "DanhMucChaId",
            table: "DanhMucCauHois");
    }
}
