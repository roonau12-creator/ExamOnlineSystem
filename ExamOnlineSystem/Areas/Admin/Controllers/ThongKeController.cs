using ClosedXML.Excel;
using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Models.ViewModels;
using ExamOnlineSystem.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamOnlineSystem.Areas.Admin.Controllers;

// Thống kê, báo cáo cho khu vực quản trị
[Area("Admin")]
[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Teacher)]
public class ThongKeController : Controller
{
    private readonly IThongKeService _thongKeService;

    public ThongKeController(IThongKeService thongKeService)
    {
        _thongKeService = thongKeService;
    }

    // GET: /Admin/ThongKe - dashboard gộp toàn bộ số liệu của hệ thống
    public async Task<IActionResult> Index()
    {
        return View(await _thongKeService.GetDashboardAsync());
    }

    // GET: /Admin/ThongKe/GiaoVien
    public async Task<IActionResult> GiaoVien()
    {
        return View(await _thongKeService.GetThongKeGiaoVienAsync());
    }

    // GET: /Admin/ThongKe/GiaoVienChiTiet/5
    public async Task<IActionResult> GiaoVienChiTiet(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }
        var vm = await _thongKeService.GetThongKeGiaoVienByIdAsync(id.Value);
        if (vm == null)
        {
            return NotFound();
        }
        return View(vm);
    }

    // GET: /Admin/ThongKe/XuatExcelGiaoVien - bảng thống kê giảng viên
    public async Task<IActionResult> XuatExcelGiaoVien()
    {
        var vm = await _thongKeService.GetThongKeGiaoVienAsync();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("ThongKeGiaoVien");

        sheet.Cell(1, 1).Value = "THỐNG KÊ GIẢNG VIÊN";
        sheet.Range(1, 1, 1, 18).Merge().Style.Font.SetBold().Font.SetFontSize(14);
        sheet.Cell(2, 1).Value = $"Xuất lúc {DateTime.Now:dd/MM/yyyy HH:mm} · {vm.TongGiaoVien} giảng viên " +
                                 $"({vm.SoDangHoatDong} đang hoạt động, {vm.SoDaKhoa} đã khóa)";
        sheet.Range(2, 1, 2, 18).Merge();

        const int dongTieuDe = 4;
        string[] tieuDe =
        {
            "STT", "Mã GV", "Họ tên", "Chức danh", "Bộ môn", "Khoa", "Trạng thái",
            "Câu hỏi", "Trắc nghiệm", "Tự luận", "Đề thi", "Đã phát hành",
            "Lịch thi", "Lớp học phần", "Lượt dự thi", "Đã nộp", "Điểm TB (hệ 10)", "Mức độ hoạt động"
        };
        for (var i = 0; i < tieuDe.Length; i++)
        {
            sheet.Cell(dongTieuDe, i + 1).Value = tieuDe[i];
        }
        var header = sheet.Range(dongTieuDe, 1, dongTieuDe, tieuDe.Length);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.LightBlue;
        sheet.Column(2).Style.NumberFormat.Format = "@";   // giữ số 0 đầu của mã GV

        for (var i = 0; i < vm.GiaoViens.Count; i++)
        {
            var g = vm.GiaoViens[i];
            var dong = dongTieuDe + 1 + i;
            sheet.Cell(dong, 1).Value = i + 1;
            sheet.Cell(dong, 2).Value = g.MaGiaoVien;
            sheet.Cell(dong, 3).Value = g.HoTen;
            sheet.Cell(dong, 4).Value = g.ChucDanh ?? "";
            sheet.Cell(dong, 5).Value = g.TenBoMon;
            sheet.Cell(dong, 6).Value = g.TenKhoa;
            sheet.Cell(dong, 7).Value = g.DangHoatDong ? "Đang hoạt động" : "Đã khóa";
            sheet.Cell(dong, 8).Value = g.SoCauHoi;
            sheet.Cell(dong, 9).Value = g.SoCauHoiTracNghiem;
            sheet.Cell(dong, 10).Value = g.SoCauHoiTuLuan;
            sheet.Cell(dong, 11).Value = g.SoDeThi;
            sheet.Cell(dong, 12).Value = g.SoDeThiDaPhatHanh;
            sheet.Cell(dong, 13).Value = g.SoLichThi;
            sheet.Cell(dong, 14).Value = g.SoLopHocPhan;
            sheet.Cell(dong, 15).Value = g.SoLuotDuThi;
            sheet.Cell(dong, 16).Value = g.SoBaiDaNop;
            if (g.DiemTrungBinhHe10.HasValue)
            {
                sheet.Cell(dong, 17).Value = g.DiemTrungBinhHe10.Value;
            }
            sheet.Cell(dong, 18).Value = g.MucDoHoatDong;
        }

        var dongTong = dongTieuDe + vm.GiaoViens.Count + 1;
        sheet.Cell(dongTong, 3).Value = "TỔNG";
        sheet.Cell(dongTong, 8).Value = vm.TongCauHoi;
        sheet.Cell(dongTong, 11).Value = vm.TongDeThi;
        sheet.Cell(dongTong, 12).Value = vm.TongDeThiDaPhatHanh;
        sheet.Cell(dongTong, 13).Value = vm.TongLichThi;
        sheet.Cell(dongTong, 14).Value = vm.TongLopHocPhan;
        sheet.Cell(dongTong, 15).Value = vm.TongLuotDuThi;
        sheet.Cell(dongTong, 16).Value = vm.TongBaiDaNop;
        sheet.Range(dongTong, 1, dongTong, tieuDe.Length).Style.Font.Bold = true;

        // Sheet gộp theo khoa
        var sheetKhoa = workbook.Worksheets.Add("TheoKhoa");
        string[] tieuDeKhoa = { "Khoa", "Số giảng viên", "Câu hỏi", "Đề thi", "Lịch thi", "Lớp học phần" };
        for (var i = 0; i < tieuDeKhoa.Length; i++)
        {
            sheetKhoa.Cell(1, i + 1).Value = tieuDeKhoa[i];
        }
        var headerKhoa = sheetKhoa.Range(1, 1, 1, tieuDeKhoa.Length);
        headerKhoa.Style.Font.Bold = true;
        headerKhoa.Style.Fill.BackgroundColor = XLColor.LightBlue;
        for (var i = 0; i < vm.TheoKhoa.Count; i++)
        {
            var k = vm.TheoKhoa[i];
            sheetKhoa.Cell(i + 2, 1).Value = k.Ten;
            sheetKhoa.Cell(i + 2, 2).Value = k.SoGiaoVien;
            sheetKhoa.Cell(i + 2, 3).Value = k.SoCauHoi;
            sheetKhoa.Cell(i + 2, 4).Value = k.SoDeThi;
            sheetKhoa.Cell(i + 2, 5).Value = k.SoLichThi;
            sheetKhoa.Cell(i + 2, 6).Value = k.SoLopHocPhan;
        }

        sheet.Columns().AdjustToContents();
        sheetKhoa.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"ThongKeGiangVien_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
    }

    // GET: /Admin/ThongKe/SinhVien
    public async Task<IActionResult> SinhVien()
    {
        return View(await _thongKeService.GetThongKeSinhVienAsync());
    }

    // GET: /Admin/ThongKe/SinhVienChiTiet/5
    public async Task<IActionResult> SinhVienChiTiet(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }
        var vm = await _thongKeService.GetThongKeSinhVienByIdAsync(id.Value);
        if (vm == null)
        {
            return NotFound();
        }
        return View(vm);
    }

    // GET: /Admin/ThongKe/XuatExcelSinhVien - bảng thống kê sinh viên
    public async Task<IActionResult> XuatExcelSinhVien()
    {
        var vm = await _thongKeService.GetThongKeSinhVienAsync();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("ThongKeSinhVien");

        sheet.Cell(1, 1).Value = "THỐNG KÊ SINH VIÊN";
        sheet.Range(1, 1, 1, 20).Merge().Style.Font.SetBold().Font.SetFontSize(14);
        sheet.Cell(2, 1).Value = $"Xuất lúc {DateTime.Now:dd/MM/yyyy HH:mm} · {vm.TongSinhVien} sinh viên " +
                                 $"({vm.SoNam} nam, {vm.SoNu} nữ) · {vm.SoDaThi} đã dự thi";
        sheet.Range(2, 1, 2, 20).Merge();

        const int dongTieuDe = 4;
        string[] tieuDe =
        {
            "STT", "Mã SV", "Họ tên", "Ngày sinh", "Giới tính", "Mã lớp", "Lớp", "Khoa", "Trạng thái",
            "Lịch thi", "Đã nộp", "Chưa thi", "Đã chấm", "Chưa chấm", "Đạt",
            "Điểm TB (hệ 10)", "Cao nhất", "Thấp nhất", "Xếp loại", "Lớp học phần"
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
        sheet.Column(6).Style.NumberFormat.Format = "@";

        for (var i = 0; i < vm.SinhViens.Count; i++)
        {
            var s = vm.SinhViens[i];
            var dong = dongTieuDe + 1 + i;
            sheet.Cell(dong, 1).Value = i + 1;
            sheet.Cell(dong, 2).Value = s.MaSinhVien;
            sheet.Cell(dong, 3).Value = s.HoTen;
            sheet.Cell(dong, 4).Value = s.NgaySinh.ToString("dd/MM/yyyy");
            sheet.Cell(dong, 5).Value = s.GioiTinh ? "Nam" : "Nữ";
            sheet.Cell(dong, 6).Value = s.MaLop;
            sheet.Cell(dong, 7).Value = s.TenLop;
            sheet.Cell(dong, 8).Value = s.TenKhoa;
            sheet.Cell(dong, 9).Value = s.DangHoatDong ? "Đang hoạt động" : "Đã khóa";
            sheet.Cell(dong, 10).Value = s.SoLichThi;
            sheet.Cell(dong, 11).Value = s.SoBaiDaNop;
            sheet.Cell(dong, 12).Value = s.SoChuaThi;
            sheet.Cell(dong, 13).Value = s.SoBaiCoDiem;
            sheet.Cell(dong, 14).Value = s.SoBaiChuaCham;
            sheet.Cell(dong, 15).Value = s.SoBaiDat;
            if (s.DiemTrungBinhHe10.HasValue)
            {
                sheet.Cell(dong, 16).Value = s.DiemTrungBinhHe10.Value;
                sheet.Cell(dong, 17).Value = s.DiemCaoNhatHe10!.Value;
                sheet.Cell(dong, 18).Value = s.DiemThapNhatHe10!.Value;
            }
            sheet.Cell(dong, 19).Value = s.XepLoai;
            sheet.Cell(dong, 20).Value = s.SoLopHocPhan;
        }

        var dongTong = dongTieuDe + vm.SinhViens.Count + 1;
        sheet.Cell(dongTong, 3).Value = "TỔNG";
        sheet.Cell(dongTong, 10).Value = vm.TongLuotDuThi;
        sheet.Cell(dongTong, 11).Value = vm.TongBaiDaNop;
        sheet.Cell(dongTong, 13).Value = vm.TongBaiCoDiem;
        sheet.Cell(dongTong, 14).Value = vm.TongBaiChuaCham;
        sheet.Cell(dongTong, 15).Value = vm.TongBaiDat;
        if (vm.DiemTrungBinhHe10.HasValue)
        {
            sheet.Cell(dongTong, 16).Value = vm.DiemTrungBinhHe10.Value;
        }
        sheet.Cell(dongTong, 20).Value = vm.TongLopHocPhan;
        sheet.Range(dongTong, 1, dongTong, tieuDe.Length).Style.Font.Bold = true;

        // Sheet gộp theo lớp
        var sheetLop = workbook.Worksheets.Add("TheoLop");
        string[] tieuDeLop =
        {
            "Lớp", "Khoa", "Sĩ số", "Nam", "Nữ", "Đã dự thi",
            "Lượt dự thi", "Đã nộp", "Đã chấm", "Đạt", "Tỉ lệ đạt (%)", "Điểm TB (hệ 10)"
        };
        for (var i = 0; i < tieuDeLop.Length; i++)
        {
            sheetLop.Cell(1, i + 1).Value = tieuDeLop[i];
        }
        var headerLop = sheetLop.Range(1, 1, 1, tieuDeLop.Length);
        headerLop.Style.Font.Bold = true;
        headerLop.Style.Fill.BackgroundColor = XLColor.LightBlue;
        sheetLop.Column(1).Style.NumberFormat.Format = "@";
        for (var i = 0; i < vm.TheoLop.Count; i++)
        {
            var l = vm.TheoLop[i];
            sheetLop.Cell(i + 2, 1).Value = l.Ten;
            sheetLop.Cell(i + 2, 2).Value = l.TenCha;
            sheetLop.Cell(i + 2, 3).Value = l.SoSinhVien;
            sheetLop.Cell(i + 2, 4).Value = l.SoNam;
            sheetLop.Cell(i + 2, 5).Value = l.SoNu;
            sheetLop.Cell(i + 2, 6).Value = l.SoDaThi;
            sheetLop.Cell(i + 2, 7).Value = l.SoLuotDuThi;
            sheetLop.Cell(i + 2, 8).Value = l.SoBaiDaNop;
            sheetLop.Cell(i + 2, 9).Value = l.SoBaiCoDiem;
            sheetLop.Cell(i + 2, 10).Value = l.SoBaiDat;
            sheetLop.Cell(i + 2, 11).Value = l.TiLeDat;
            if (l.DiemTrungBinhHe10.HasValue)
            {
                sheetLop.Cell(i + 2, 12).Value = l.DiemTrungBinhHe10.Value;
            }
        }

        sheet.Columns().AdjustToContents();
        sheetLop.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"ThongKeSinhVien_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
    }

    // GET: /Admin/ThongKe/Khoa
    public async Task<IActionResult> Khoa()
    {
        return View(await _thongKeService.GetThongKeKhoaAsync());
    }

    // GET: /Admin/ThongKe/KhoaChiTiet/5
    public async Task<IActionResult> KhoaChiTiet(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }
        var vm = await _thongKeService.GetThongKeKhoaByIdAsync(id.Value);
        if (vm == null)
        {
            return NotFound();
        }
        return View(vm);
    }

    // GET: /Admin/ThongKe/XuatExcelKhoa - bảng thống kê khoa
    public async Task<IActionResult> XuatExcelKhoa()
    {
        var vm = await _thongKeService.GetThongKeKhoaAsync();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("ThongKeKhoa");

        sheet.Cell(1, 1).Value = "THỐNG KÊ KHOA";
        sheet.Range(1, 1, 1, 24).Merge().Style.Font.SetBold().Font.SetFontSize(14);
        sheet.Cell(2, 1).Value = $"Xuất lúc {DateTime.Now:dd/MM/yyyy HH:mm} · {vm.TongKhoa} khoa · " +
                                 $"{vm.TongBoMon} bộ môn · {vm.TongGiaoVien} giảng viên · {vm.TongSinhVien} sinh viên";
        sheet.Range(2, 1, 2, 24).Merge();
        sheet.Cell(3, 1).Value = "Câu hỏi / đề thi quy về khoa theo môn học; lịch thi theo giảng viên phụ trách; " +
                                 "kết quả thi theo sinh viên thuộc khoa. Điểm quy về thang 10.";
        sheet.Range(3, 1, 3, 24).Merge().Style.Font.SetItalic();

        const int dongTieuDe = 5;
        string[] tieuDe =
        {
            "STT", "Mã khoa", "Tên khoa",
            "Bộ môn", "Giảng viên", "Lớp", "Sinh viên", "Nam", "Nữ",
            "Môn học", "Học phần", "Danh mục CH", "Câu hỏi", "Trắc nghiệm", "Tự luận",
            "Đề thi", "Đã phát hành", "Lịch thi",
            "Lượt dự thi", "Đã nộp", "Đã chấm", "Đạt", "Tỉ lệ đạt (%)", "Điểm TB (hệ 10)"
        };
        for (var i = 0; i < tieuDe.Length; i++)
        {
            sheet.Cell(dongTieuDe, i + 1).Value = tieuDe[i];
        }
        var header = sheet.Range(dongTieuDe, 1, dongTieuDe, tieuDe.Length);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.LightBlue;
        sheet.Column(2).Style.NumberFormat.Format = "@";   // giữ số 0 đầu của mã khoa

        for (var i = 0; i < vm.Khoas.Count; i++)
        {
            var k = vm.Khoas[i];
            var dong = dongTieuDe + 1 + i;
            sheet.Cell(dong, 1).Value = i + 1;
            sheet.Cell(dong, 2).Value = k.MaKhoa;
            sheet.Cell(dong, 3).Value = k.TenKhoa;
            sheet.Cell(dong, 4).Value = k.SoBoMon;
            sheet.Cell(dong, 5).Value = k.SoGiaoVien;
            sheet.Cell(dong, 6).Value = k.SoLop;
            sheet.Cell(dong, 7).Value = k.SoSinhVien;
            sheet.Cell(dong, 8).Value = k.SoSinhVienNam;
            sheet.Cell(dong, 9).Value = k.SoSinhVienNu;
            sheet.Cell(dong, 10).Value = k.SoMonHoc;
            sheet.Cell(dong, 11).Value = k.SoHocPhan;
            sheet.Cell(dong, 12).Value = k.SoDanhMucCauHoi;
            sheet.Cell(dong, 13).Value = k.SoCauHoi;
            sheet.Cell(dong, 14).Value = k.SoCauHoiTracNghiem;
            sheet.Cell(dong, 15).Value = k.SoCauHoiTuLuan;
            sheet.Cell(dong, 16).Value = k.SoDeThi;
            sheet.Cell(dong, 17).Value = k.SoDeThiDaPhatHanh;
            sheet.Cell(dong, 18).Value = k.SoLichThi;
            sheet.Cell(dong, 19).Value = k.SoLuotDuThi;
            sheet.Cell(dong, 20).Value = k.SoBaiDaNop;
            sheet.Cell(dong, 21).Value = k.SoBaiCoDiem;
            sheet.Cell(dong, 22).Value = k.SoBaiDat;
            sheet.Cell(dong, 23).Value = k.TiLeDat;
            if (k.DiemTrungBinhHe10.HasValue)
            {
                sheet.Cell(dong, 24).Value = k.DiemTrungBinhHe10.Value;
            }
        }

        var dongTong = dongTieuDe + vm.Khoas.Count + 1;
        sheet.Cell(dongTong, 3).Value = "TỔNG";
        sheet.Cell(dongTong, 4).Value = vm.TongBoMon;
        sheet.Cell(dongTong, 5).Value = vm.TongGiaoVien;
        sheet.Cell(dongTong, 6).Value = vm.TongLop;
        sheet.Cell(dongTong, 7).Value = vm.TongSinhVien;
        sheet.Cell(dongTong, 10).Value = vm.TongMonHoc;
        sheet.Cell(dongTong, 11).Value = vm.TongHocPhan;
        sheet.Cell(dongTong, 13).Value = vm.TongCauHoi;
        sheet.Cell(dongTong, 16).Value = vm.TongDeThi;
        sheet.Cell(dongTong, 18).Value = vm.TongLichThi;
        sheet.Cell(dongTong, 19).Value = vm.TongLuotDuThi;
        sheet.Cell(dongTong, 20).Value = vm.TongBaiDaNop;
        sheet.Cell(dongTong, 21).Value = vm.TongBaiCoDiem;
        sheet.Cell(dongTong, 22).Value = vm.TongBaiDat;
        sheet.Cell(dongTong, 23).Value = vm.TiLeDat;
        if (vm.DiemTrungBinhHe10.HasValue)
        {
            sheet.Cell(dongTong, 24).Value = vm.DiemTrungBinhHe10.Value;
        }
        sheet.Range(dongTong, 1, dongTong, tieuDe.Length).Style.Font.Bold = true;

        sheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"ThongKeKhoa_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
    }

    // GET: /Admin/ThongKe/BoMon
    public async Task<IActionResult> BoMon()
    {
        return View(await _thongKeService.GetThongKeBoMonAsync());
    }

    // GET: /Admin/ThongKe/BoMonChiTiet/5
    public async Task<IActionResult> BoMonChiTiet(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }
        var vm = await _thongKeService.GetThongKeBoMonByIdAsync(id.Value);
        if (vm == null)
        {
            return NotFound();
        }
        return View(vm);
    }

    // GET: /Admin/ThongKe/XuatExcelBoMon - bảng thống kê bộ môn
    public async Task<IActionResult> XuatExcelBoMon()
    {
        var vm = await _thongKeService.GetThongKeBoMonAsync();

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("ThongKeBoMon");

        string[] tieuDe =
        {
            "STT", "Mã bộ môn", "Tên bộ môn", "Khoa",
            "Giảng viên", "Môn học", "Học phần", "Lớp học phần", "Danh mục CH",
            "Câu hỏi", "Trắc nghiệm", "Tự luận", "Dễ", "Trung bình", "Khó", "Rất khó",
            "Đề thi", "Đã phát hành", "Lịch thi",
            "Lượt dự thi", "Đã nộp", "Đã chấm", "Đạt", "Tỉ lệ đạt (%)", "Điểm TB (hệ 10)"
        };

        sheet.Cell(1, 1).Value = "THỐNG KÊ BỘ MÔN";
        sheet.Range(1, 1, 1, tieuDe.Length).Merge().Style.Font.SetBold().Font.SetFontSize(14);
        sheet.Cell(2, 1).Value = $"Xuất lúc {DateTime.Now:dd/MM/yyyy HH:mm} · {vm.TongBoMon} bộ môn thuộc " +
                                 $"{vm.SoKhoa} khoa · {vm.TongGiaoVien} giảng viên";
        sheet.Range(2, 1, 2, tieuDe.Length).Merge();
        sheet.Cell(3, 1).Value = "Môn học, học phần, câu hỏi, đề thi quy về bộ môn theo môn học; " +
                                 "lịch thi và kết quả thi theo giảng viên của bộ môn phụ trách. Điểm quy về thang 10.";
        sheet.Range(3, 1, 3, tieuDe.Length).Merge().Style.Font.SetItalic();

        const int dongTieuDe = 5;
        for (var i = 0; i < tieuDe.Length; i++)
        {
            sheet.Cell(dongTieuDe, i + 1).Value = tieuDe[i];
        }
        var header = sheet.Range(dongTieuDe, 1, dongTieuDe, tieuDe.Length);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.LightBlue;
        sheet.Column(2).Style.NumberFormat.Format = "@";   // giữ số 0 đầu của mã bộ môn

        for (var i = 0; i < vm.BoMons.Count; i++)
        {
            var b = vm.BoMons[i];
            var dong = dongTieuDe + 1 + i;
            sheet.Cell(dong, 1).Value = i + 1;
            sheet.Cell(dong, 2).Value = b.MaBoMon;
            sheet.Cell(dong, 3).Value = b.TenBoMon;
            sheet.Cell(dong, 4).Value = b.TenKhoa;
            sheet.Cell(dong, 5).Value = b.SoGiaoVien;
            sheet.Cell(dong, 6).Value = b.SoMonHoc;
            sheet.Cell(dong, 7).Value = b.SoHocPhan;
            sheet.Cell(dong, 8).Value = b.SoLopHocPhan;
            sheet.Cell(dong, 9).Value = b.SoDanhMucCauHoi;
            sheet.Cell(dong, 10).Value = b.SoCauHoi;
            sheet.Cell(dong, 11).Value = b.SoCauHoiTracNghiem;
            sheet.Cell(dong, 12).Value = b.SoCauHoiTuLuan;
            sheet.Cell(dong, 13).Value = b.SoCauHoiDe;
            sheet.Cell(dong, 14).Value = b.SoCauHoiTrungBinh;
            sheet.Cell(dong, 15).Value = b.SoCauHoiKho;
            sheet.Cell(dong, 16).Value = b.SoCauHoiRatKho;
            sheet.Cell(dong, 17).Value = b.SoDeThi;
            sheet.Cell(dong, 18).Value = b.SoDeThiDaPhatHanh;
            sheet.Cell(dong, 19).Value = b.SoLichThi;
            sheet.Cell(dong, 20).Value = b.SoLuotDuThi;
            sheet.Cell(dong, 21).Value = b.SoBaiDaNop;
            sheet.Cell(dong, 22).Value = b.SoBaiCoDiem;
            sheet.Cell(dong, 23).Value = b.SoBaiDat;
            sheet.Cell(dong, 24).Value = b.TiLeDat;
            if (b.DiemTrungBinhHe10.HasValue)
            {
                sheet.Cell(dong, 25).Value = b.DiemTrungBinhHe10.Value;
            }
        }

        var dongTong = dongTieuDe + vm.BoMons.Count + 1;
        sheet.Cell(dongTong, 3).Value = "TỔNG";
        sheet.Cell(dongTong, 5).Value = vm.TongGiaoVien;
        sheet.Cell(dongTong, 6).Value = vm.TongMonHoc;
        sheet.Cell(dongTong, 7).Value = vm.TongHocPhan;
        sheet.Cell(dongTong, 10).Value = vm.TongCauHoi;
        sheet.Cell(dongTong, 17).Value = vm.TongDeThi;
        sheet.Cell(dongTong, 18).Value = vm.TongDeThiDaPhatHanh;
        sheet.Cell(dongTong, 19).Value = vm.TongLichThi;
        sheet.Cell(dongTong, 20).Value = vm.TongLuotDuThi;
        sheet.Cell(dongTong, 21).Value = vm.TongBaiDaNop;
        sheet.Cell(dongTong, 22).Value = vm.TongBaiCoDiem;
        sheet.Cell(dongTong, 23).Value = vm.TongBaiDat;
        sheet.Cell(dongTong, 24).Value = vm.TiLeDat;
        if (vm.DiemTrungBinhHe10.HasValue)
        {
            sheet.Cell(dongTong, 25).Value = vm.DiemTrungBinhHe10.Value;
        }
        sheet.Range(dongTong, 1, dongTong, tieuDe.Length).Style.Font.Bold = true;

        sheet.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"ThongKeBoMon_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
    }
}
