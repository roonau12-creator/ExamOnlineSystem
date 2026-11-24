using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamOnlineSystem.Migrations;

/// <inheritdoc />
public partial class _20261120134733_AddTenTiengAnhMonHoc : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "TenTiengAnh",
            table: "MonHocs",
            type: "text",
            nullable: false,
            defaultValue: "");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "TenTiengAnh",
            table: "MonHocs");
    }
}
