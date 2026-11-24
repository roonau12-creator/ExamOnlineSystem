using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamOnlineSystem.Migrations;

/// <inheritdoc />
public partial class _20261121125057_AddLopToSinhVien : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "LopId",
            table: "SinhViens",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.CreateIndex(
            name: "IX_SinhViens_LopId",
            table: "SinhViens",
            column: "LopId");

        migrationBuilder.AddForeignKey(
            name: "FK_SinhViens_Lops_LopId",
            table: "SinhViens",
            column: "LopId",
            principalTable: "Lops",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_SinhViens_Lops_LopId",
            table: "SinhViens");

        migrationBuilder.DropIndex(
            name: "IX_SinhViens_LopId",
            table: "SinhViens");

        migrationBuilder.DropColumn(
            name: "LopId",
            table: "SinhViens");
    }
}
