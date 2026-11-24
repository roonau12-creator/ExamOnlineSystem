using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamOnlineSystem.Migrations;

/// <inheritdoc />
public partial class _20261123001040_UniqueMaDeThi : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_DeThis_MaDeThi",
            table: "DeThis",
            column: "MaDeThi",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_DeThis_MaDeThi",
            table: "DeThis");
    }
}
