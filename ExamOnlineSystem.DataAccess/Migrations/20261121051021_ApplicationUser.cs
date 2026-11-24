using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExamOnlineSystem.Migrations;

/// <inheritdoc />
public partial class _20261121051021_ApplicationUser : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ChucDanh",
            table: "AspNetUsers",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "DangHoatDong",
            table: "AspNetUsers",
            type: "boolean",
            nullable: false,
            defaultValue: true); // tài khoản đã có vẫn hoạt động

        migrationBuilder.AddColumn<string>(
            name: "HoTen",
            table: "AspNetUsers",
            type: "text",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<int>(
            name: "KhoaId",
            table: "AspNetUsers",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "LanDangNhapCuoi",
            table: "AspNetUsers",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "NgayTao",
            table: "AspNetUsers",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "now()"); // chỉ để điền cho tài khoản đã có

        migrationBuilder.CreateIndex(
            name: "IX_AspNetUsers_KhoaId",
            table: "AspNetUsers",
            column: "KhoaId");

        migrationBuilder.AddForeignKey(
            name: "FK_AspNetUsers_Khoas_KhoaId",
            table: "AspNetUsers",
            column: "KhoaId",
            principalTable: "Khoas",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_AspNetUsers_Khoas_KhoaId",
            table: "AspNetUsers");

        migrationBuilder.DropIndex(
            name: "IX_AspNetUsers_KhoaId",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "ChucDanh",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "DangHoatDong",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "HoTen",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "KhoaId",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "LanDangNhapCuoi",
            table: "AspNetUsers");

        migrationBuilder.DropColumn(
            name: "NgayTao",
            table: "AspNetUsers");
    }
}
