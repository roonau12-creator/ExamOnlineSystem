using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamOnlineSystem.Migrations;

/// <inheritdoc />
public partial class _20261123013731_PhatHanhDeThi : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "DaPhatHanh",
            table: "DeThis",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTime>(
            name: "NgayPhatHanh",
            table: "DeThis",
            type: "timestamp with time zone",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "DaPhatHanh",
            table: "DeThis");

        migrationBuilder.DropColumn(
            name: "NgayPhatHanh",
            table: "DeThis");
    }
}
