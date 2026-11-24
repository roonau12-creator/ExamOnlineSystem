using System.Security.Claims;
using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Models.ViewModels;
using ExamOnlineSystem.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExamOnlineSystem.Areas.Customer.Controllers;

// Danh sách môn học của sinh viên đang đăng nhập (không nhận id từ URL)
[Area("Customer")]
[Authorize(Roles = SD.Role_Student)]
public class MonHocController : Controller
{
    private readonly ISinhVienService _sinhVienService;

    public MonHocController(ISinhVienService sinhVienService)
    {
        _sinhVienService = sinhVienService;
    }

    // GET: /Customer/MonHoc?xem=tat-ca (mặc định: chỉ môn đang học trong năm học hiện tại)
    public async Task<IActionResult> Index(string? xem)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var sinhVienId = userId == null ? null : await _sinhVienService.GetSinhVienIdByUserIdAsync(userId);
        var sinhVien = sinhVienId == null ? null : await _sinhVienService.GetSinhVienByIdAsync(sinhVienId.Value);
        if (sinhVien == null)
        {
            return View("~/Areas/Customer/Views/HoSo/KhongCoHoSo.cshtml");
        }

        var tatCa = await _sinhVienService.GetMonHocCuaSinhVienAsync(sinhVien.Id);
        var xemTatCa = xem == "tat-ca";
        return View(new MonHocSinhVienVM
        {
            SinhVien = sinhVien,
            XemTatCa = xemTatCa,
            MonHocs = xemTatCa ? tatCa : tatCa.Where(m => m.DangHoc).ToList(),
            TongSoMon = tatCa.Count,
            SoMonDangHoc = tatCa.Count(m => m.DangHoc)
        });
    }
}
