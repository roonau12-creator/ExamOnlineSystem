using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ExamOnlineSystem.Migrations;

/// <inheritdoc />
public partial class _20261122120440_DanhMucCauHoiCauHoiDapAn : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "DanhMucCauHois",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                TenDanhMuc = table.Column<string>(type: "text", nullable: false),
                MonHocId = table.Column<int>(type: "integer", nullable: false),
                MoTa = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DanhMucCauHois", x => x.Id);
                table.ForeignKey(
                    name: "FK_DanhMucCauHois_MonHocs_MonHocId",
                    column: x => x.MonHocId,
                    principalTable: "MonHocs",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CauHois",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                DanhMucCauHoiId = table.Column<int>(type: "integer", nullable: false),
                GiaoVienId = table.Column<int>(type: "integer", nullable: false),
                NoiDung = table.Column<string>(type: "text", nullable: false),
                LoaiCauHoi = table.Column<int>(type: "integer", nullable: false),
                MucDo = table.Column<int>(type: "integer", nullable: false),
                DiemMacDinh = table.Column<double>(type: "double precision", nullable: false),
                GiaiThich = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CauHois", x => x.Id);
                table.ForeignKey(
                    name: "FK_CauHois_DanhMucCauHois_DanhMucCauHoiId",
                    column: x => x.DanhMucCauHoiId,
                    principalTable: "DanhMucCauHois",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CauHois_GiaoViens_GiaoVienId",
                    column: x => x.GiaoVienId,
                    principalTable: "GiaoViens",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "DapAns",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                CauHoiId = table.Column<int>(type: "integer", nullable: false),
                NoiDung = table.Column<string>(type: "text", nullable: false),
                DapAnDung = table.Column<bool>(type: "boolean", nullable: false),
                ThuTu = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DapAns", x => x.Id);
                table.ForeignKey(
                    name: "FK_DapAns_CauHois_CauHoiId",
                    column: x => x.CauHoiId,
                    principalTable: "CauHois",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CauHois_DanhMucCauHoiId",
            table: "CauHois",
            column: "DanhMucCauHoiId");

        migrationBuilder.CreateIndex(
            name: "IX_CauHois_GiaoVienId",
            table: "CauHois",
            column: "GiaoVienId");

        migrationBuilder.CreateIndex(
            name: "IX_DanhMucCauHois_MonHocId",
            table: "DanhMucCauHois",
            column: "MonHocId");

        migrationBuilder.CreateIndex(
            name: "IX_DapAns_CauHoiId",
            table: "DapAns",
            column: "CauHoiId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "DapAns");

        migrationBuilder.DropTable(
            name: "CauHois");

        migrationBuilder.DropTable(
            name: "DanhMucCauHois");
    }
}
