using ClosedXML.Excel;
using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Models.ViewModels;
using ExamOnlineSystem.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamOnlineSystem.Areas.Admin.Controllers;

// Quản lý kết quả: theo dõi tiến độ làm bài và kết quả thi của từng lịch thi
[Area("Admin")]
[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Teacher)]
public class KetQuaThiController : Controller
{
    private readonly IKetQuaThiService _ketQuaThiService;

    public KetQuaThiController(IKetQuaThiService ketQuaThiService)
    {
        _ketQuaThiService = ketQuaThiService;
    }

    // GET: /Admin/KetQuaThi
    public IActionResult Index()
    {
        return View();
    }

    // GET: /Admin/KetQuaThi/Details/5 - Theo dõi kết quả của một lịch thi
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }
        var vm = await _ketQuaThiService.GetKetQuaByLichThiAsync(id.Value);
        if (vm == null)
        {
            return NotFound();
        }
        return View(vm);
    }

    // GET: /Admin/KetQuaThi/BaiLam/5 - Bài làm chi tiết của một lần thi
    public async Task<IActionResult> BaiLam(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }
        var vm = await _ketQuaThiService.GetBaiThiAsync(id.Value);
        if (vm == null)
        {
            return NotFound();
        }
        return View(vm);
    }

    #region API CALLS

    // Dữ liệu cho bảng danh sách (DataTables)
    public async Task<IActionResult> GetAllKetQua()
    {
        var bayGio = DateTime.UtcNow;
        var data = (await _ketQuaThiService.GetTongHopKetQuaAsync()).Select(l => new
        {
            l.Id,
            l.TenLichThi,
            l.DeThiId,
            l.MaDeThi,
            l.TenDeThi,
            l.TenMonHoc,
            l.TongDiem,
            BatDau = l.ThoiGianBatDau.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
            KetThuc = l.ThoiGianKetThuc.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
            BatDauSort = l.ThoiGianBatDau.ToString("yyyyMMddHHmm"),
            CungNgay = l.ThoiGianBatDau.ToLocalTime().Date == l.ThoiGianKetThuc.ToLocalTime().Date,
            l.TenGiaoVien,
            Lops = string.Join(", ", l.Lops),
            l.SoSinhVien,
            l.SoChuaThi,
            l.SoDangThi,
            l.SoDaNop,
            l.SoCoDiem,
            l.SoChuaCham,
            l.SoDat,
            l.TiLeNop,
            l.TiLeDat,
            DiemTrungBinh = l.DiemTrungBinhHe10,
            DiemCaoNhat = l.DiemCaoNhatHe10,
            DiemThapNhat = l.DiemThapNhatHe10,
            TrangThai = TrangThaiLichThi.Tinh(l.ThoiGianBatDau, l.ThoiGianKetThuc, bayGio)
        });
        return Json(new { data });
    }

    // Dữ liệu làm mới trang Theo dõi khi lịch thi đang diễn ra (không tải lại cả trang)
    public async Task<IActionResult> TheoDoi(int id)
    {
        var vm = await _ketQuaThiService.GetKetQuaByLichThiAsync(id);
        if (vm == null)
        {
            return Json(new { success = false, message = "Không tìm thấy lịch thi" });
        }
        return Json(new
        {
            success = true,
            trangThai = vm.TrangThai,
            tongHop = new
            {
                vm.TongHop.SoSinhVien,
                vm.TongHop.SoChuaThi,
                vm.TongHop.SoDangThi,
                vm.TongHop.SoDaNop,
                vm.TongHop.SoCoDiem,
                vm.TongHop.SoChuaCham,
                vm.TongHop.SoDat,
                vm.TongHop.TiLeNop,
                vm.TongHop.TiLeDat,
                DiemTrungBinh = vm.TongHop.DiemTrungBinhHe10,
                DiemCaoNhat = vm.TongHop.DiemCaoNhatHe10,
                DiemThapNhat = vm.TongHop.DiemThapNhatHe10
            },
            phanBo = vm.PhanBo.Select(p => new { p.Nhan, p.XepLoai, p.Mau, p.SoLuong, p.TiLe }),
            sinhViens = vm.SinhViens.Select(s => new
            {
                s.Id,
                s.MaSinhVien,
                s.HoTen,
                s.MaLop,
                s.TenLop,
                s.TrangThai,
                TenTrangThai = KetQuaLabel.TenTrangThai(s.TrangThai),
                MauTrangThai = KetQuaLabel.MauTrangThai(s.TrangThai),
                s.SoLanThi,
                s.LanThiId,
                BatDau = s.ThoiGianBatDau?.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                NopBai = s.ThoiGianNopBai?.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                BatDauSort = s.ThoiGianBatDau?.ToString("yyyyMMddHHmm") ?? "",
                NopBaiSort = s.ThoiGianNopBai?.ToString("yyyyMMddHHmm") ?? "",
                s.SoPhutLamBai,
                s.Diem,
                s.DiemHe10,
                s.XepLoai,
                MauXepLoai = KetQuaLabel.MauXepLoai(s.XepLoai)
            })
        });
    }

    // GET: /Admin/KetQuaThi/XuatExcel/5 - Bảng điểm của một lịch thi
    public async Task<IActionResult> XuatExcel(int id)
    {
        var vm = await _ketQuaThiService.GetKetQuaByLichThiAsync(id);
        if (vm == null)
        {
            return NotFound();
        }

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("BangDiem");

        sheet.Cell(1, 1).Value = $"BẢNG ĐIỂM - {vm.TongHop.TenLichThi}";
        sheet.Range(1, 1, 1, 12).Merge().Style.Font.SetBold().Font.SetFontSize(14);
        sheet.Cell(2, 1).Value = $"Môn học: {vm.TongHop.TenMonHoc} | Đề thi: {vm.TongHop.MaDeThi} - {vm.TongHop.TenDeThi} " +
                                 $"| Thang điểm: {vm.TongHop.TongDiem:0.##}";
        sheet.Range(2, 1, 2, 12).Merge();
        sheet.Cell(3, 1).Value = $"Thời gian: {vm.TongHop.ThoiGianBatDau.ToLocalTime():dd/MM/yyyy HH:mm} – " +
                                 $"{vm.TongHop.ThoiGianKetThuc.ToLocalTime():dd/MM/yyyy HH:mm} | " +
                                 $"Giảng viên: {vm.TongHop.TenGiaoVien}";
        sheet.Range(3, 1, 3, 12).Merge();

        const int dongTieuDe = 5;
        string[] tieuDe =
        {
            "STT", "Mã sinh viên", "Họ tên", "Mã lớp", "Tên lớp", "Trạng thái",
            "Số lần thi", "Bắt đầu", "Nộp bài", "Thời gian làm bài (phút)", "Điểm", "Điểm hệ 10", "Xếp loại"
        };
        for (var i = 0; i < tieuDe.Length; i++)
        {
            sheet.Cell(dongTieuDe, i + 1).Value = tieuDe[i];
        }
        var header = sheet.Range(dongTieuDe, 1, dongTieuDe, tieuDe.Length);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.LightBlue;

        // Giữ số 0 đầu của mã sinh viên / mã lớp
        sheet.Column(2).Style.NumberFormat.Format = "@";
        sheet.Column(4).Style.NumberFormat.Format = "@";

        for (var i = 0; i < vm.SinhViens.Count; i++)
        {
            var sv = vm.SinhViens[i];
            var dong = dongTieuDe + 1 + i;
            sheet.Cell(dong, 1).Value = i + 1;
            sheet.Cell(dong, 2).Value = sv.MaSinhVien;
            sheet.Cell(dong, 3).Value = sv.HoTen;
            sheet.Cell(dong, 4).Value = sv.MaLop;
            sheet.Cell(dong, 5).Value = sv.TenLop;
            sheet.Cell(dong, 6).Value = KetQuaLabel.TenTrangThai(sv.TrangThai);
            sheet.Cell(dong, 7).Value = sv.SoLanThi;
            sheet.Cell(dong, 8).Value = sv.ThoiGianBatDau?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "";
            sheet.Cell(dong, 9).Value = sv.ThoiGianNopBai?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "";
            if (sv.SoPhutLamBai.HasValue)
            {
                sheet.Cell(dong, 10).Value = sv.SoPhutLamBai.Value;
            }
            if (sv.Diem.HasValue)
            {
                sheet.Cell(dong, 11).Value = sv.Diem.Value;
                sheet.Cell(dong, 12).Value = sv.DiemHe10!.Value;
            }
            sheet.Cell(dong, 13).Value = sv.XepLoai;
        }

        var dongTongKet = dongTieuDe + vm.SinhViens.Count + 2;
        sheet.Cell(dongTongKet, 1).Value = "Tổng kết";
        sheet.Cell(dongTongKet, 1).Style.Font.Bold = true;
        sheet.Cell(dongTongKet, 2).Value =
            $"Sĩ số {vm.TongHop.SoSinhVien} | Đã nộp {vm.TongHop.SoDaNop} | Đang làm {vm.TongHop.SoDangThi} | " +
            $"Chưa thi {vm.TongHop.SoChuaThi} | Chưa chấm {vm.TongHop.SoChuaCham}";
        sheet.Cell(dongTongKet + 1, 2).Value =
            $"Điểm TB (hệ 10): {vm.TongHop.DiemTrungBinhHe10?.ToString("0.##") ?? "—"} | " +
            $"Cao nhất: {vm.TongHop.DiemCaoNhatHe10?.ToString("0.##") ?? "—"} | " +
            $"Thấp nhất: {vm.TongHop.DiemThapNhatHe10?.ToString("0.##") ?? "—"} | " +
            $"Đạt: {vm.TongHop.SoDat}/{vm.TongHop.SoCoDiem} ({vm.TongHop.TiLeDat}%)";

        sheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        var ten = $"BangDiem_{vm.TongHop.MaDeThi}_{vm.TongHop.ThoiGianBatDau.ToLocalTime():yyyyMMdd}.xlsx";
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ten);
    }

    #endregion
}
