using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ExamOnlineSystem.Migrations;

/// <inheritdoc />
public partial class _20261122121917_DeThiCauHoiDeThi : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DeThis",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                MaDeThi = table.Column<string>(type: "text", nullable: false),
                TenDeThi = table.Column<string>(type: "text", nullable: false),
                MonHocId = table.Column<int>(type: "integer", nullable: false),
                GiaoVienId = table.Column<int>(type: "integer", nullable: false),
                ThoiLuong = table.Column<int>(type: "integer", nullable: false),
                TongDiem = table.Column<double>(type: "double precision", nullable: false),
                LoaiDeThi = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DeThis", x => x.Id);
                table.ForeignKey(
                    name: "FK_DeThis_GiaoViens_GiaoVienId",
                    column: x => x.GiaoVienId,
                    principalTable: "GiaoViens",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_DeThis_MonHocs_MonHocId",
                    column: x => x.MonHocId,
                    principalTable: "MonHocs",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CauHoiDeThis",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                DeThiId = table.Column<int>(type: "integer", nullable: false),
                CauHoiId = table.Column<int>(type: "integer", nullable: false),
                Diem = table.Column<double>(type: "double precision", nullable: false),
                ThuTu = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CauHoiDeThis", x => x.Id);
                table.ForeignKey(
                    name: "FK_CauHoiDeThis_CauHois_CauHoiId",
                    column: x => x.CauHoiId,
                    principalTable: "CauHois",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CauHoiDeThis_DeThis_DeThiId",
                    column: x => x.DeThiId,
                    principalTable: "DeThis",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CauHoiDeThis_CauHoiId",
            table: "CauHoiDeThis",
            column: "CauHoiId");

        migrationBuilder.CreateIndex(
            name: "IX_CauHoiDeThis_DeThiId",
            table: "CauHoiDeThis",
            column: "DeThiId");

        migrationBuilder.CreateIndex(
            name: "IX_DeThis_GiaoVienId",
            table: "DeThis",
            column: "GiaoVienId");

        migrationBuilder.CreateIndex(
            name: "IX_DeThis_MonHocId",
            table: "DeThis",
            column: "MonHocId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CauHoiDeThis");

        migrationBuilder.DropTable(
            name: "DeThis");
    }
}
