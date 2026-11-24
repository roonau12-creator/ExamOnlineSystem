using System.Security.Claims;
using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Models;
using ExamOnlineSystem.Models.ViewModels;
using ExamOnlineSystem.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ExamOnlineSystem.Areas.Customer.Controllers;

// Lịch thi của sinh viên đang đăng nhập (không nhận id từ URL)
[Area("Customer")]
[Authorize(Roles = SD.Role_Student)]
public class LichThiController : Controller
{
    private readonly ISinhVienService _sinhVienService;
    private readonly UserManager<ApplicationUser> _userManager;

    public LichThiController(ISinhVienService sinhVienService, UserManager<ApplicationUser> userManager)
    {
        _sinhVienService = sinhVienService;
        _userManager = userManager;
    }

    // GET: /Customer/LichThi?xem=da-qua (mặc định: các kỳ thi đang và sắp diễn ra)
    public async Task<IActionResult> Index(string? xem)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var sinhVienId = userId == null ? null : await _sinhVienService.GetSinhVienIdByUserIdAsync(userId);
        var vm = sinhVienId == null ? null : await _sinhVienService.GetSinhVienDetailAsync(sinhVienId.Value);
        if (vm == null)
        {
            return View("~/Areas/Customer/Views/HoSo/KhongCoHoSo.cshtml");
        }

        ViewData["XemDaQua"] = xem == "da-qua";
        return View(vm);
    }

    // GET: /Customer/LichThi/ChiTiet/5 – chỉ mở được kỳ thi mà sinh viên có tên trong danh sách dự thi
    public async Task<IActionResult> ChiTiet(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var sinhVienId = userId == null ? null : await _sinhVienService.GetSinhVienIdByUserIdAsync(userId);
        if (sinhVienId == null)
        {
            return View("~/Areas/Customer/Views/HoSo/KhongCoHoSo.cshtml");
        }

        var vm = await _sinhVienService.GetKyThiCuaSinhVienAsync(sinhVienId.Value, id.Value);
        return vm == null ? NotFound() : View(vm);
    }

    // GET: /Customer/LichThi/XacNhan/5 – kiểm tra điều kiện trước khi vào thi (không ghi gì vào CSDL)
    public async Task<IActionResult> XacNhan(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var sinhVienId = userId == null ? null : await _sinhVienService.GetSinhVienIdByUserIdAsync(userId);
        if (sinhVienId == null)
        {
            return View("~/Areas/Customer/Views/HoSo/KhongCoHoSo.cshtml");
        }

        var kyThi = await _sinhVienService.GetKyThiCuaSinhVienAsync(sinhVienId.Value, id.Value);
        if (kyThi == null)
        {
            return NotFound(); // không có tên trong danh sách dự thi
        }

        var sinhVien = (await _sinhVienService.GetSinhVienByIdAsync(sinhVienId.Value))!;
        var user = await _userManager.GetUserAsync(User);
        return View(new XacNhanVaoThiVM
        {
            KyThi = kyThi,
            SinhVien = sinhVien,
            KiemTras = KiemTraDieuKien(kyThi, user?.DangHoatDong == true, DateTime.UtcNow),
            CoTrangLamBai = false
        });
    }

    // Các điều kiện vào thi, theo thứ tự hiển thị
    private static List<KiemTraVaoThi> KiemTraDieuKien(KyThiChiTietVM k, bool taiKhoanHoatDong, DateTime now)
    {
        var ds = new List<KiemTraVaoThi>
        {
            new() { Ten = "Có tên trong danh sách dự thi", MoTa = "Bạn được xếp vào kỳ thi này.", MucDo = MucDoKiemTra.Dat },
            taiKhoanHoatDong
                ? new() { Ten = "Tài khoản đang hoạt động", MoTa = "Tài khoản không bị khóa.", MucDo = MucDoKiemTra.Dat }
                : new() { Ten = "Tài khoản đang hoạt động", MoTa = "Tài khoản của bạn đang bị khóa. Vui lòng liên hệ phòng đào tạo.", MucDo = MucDoKiemTra.KhongDat }
        };

        var bd = k.ThoiGianBatDau.ToLocalTime();
        var kt = k.ThoiGianKetThuc.ToLocalTime();
        if (now < k.ThoiGianBatDau)
        {
            ds.Add(new() { Ten = "Trong giờ thi", MoTa = $"Chưa đến giờ thi. Bài thi mở lúc {bd:HH:mm dd/MM/yyyy}.", MucDo = MucDoKiemTra.KhongDat });
        }
        else if (now >= k.ThoiGianKetThuc)
        {
            ds.Add(new() { Ten = "Trong giờ thi", MoTa = $"Kỳ thi đã kết thúc lúc {kt:HH:mm dd/MM/yyyy}.", MucDo = MucDoKiemTra.KhongDat });
        }
        else
        {
            ds.Add(new() { Ten = "Trong giờ thi", MoTa = $"Bài thi đang mở, đóng lúc {kt:HH:mm}.", MucDo = MucDoKiemTra.Dat });

            // Còn ít thời gian hơn thời lượng làm bài: vẫn vào được nhưng phải nộp trước giờ đóng
            // (đã nộp bài thì không cần kiểm tra mục này)
            var conLai = (int)Math.Floor((k.ThoiGianKetThuc - now).TotalMinutes);
            if (k.TrangThai != 2)
            {
                ds.Add(conLai >= k.ThoiLuong
                    ? new() { Ten = "Đủ thời gian làm bài", MoTa = $"Còn {conLai} phút đến giờ đóng, đủ cho {k.ThoiLuong} phút làm bài.", MucDo = MucDoKiemTra.Dat }
                    : new() { Ten = "Đủ thời gian làm bài", MoTa = $"Chỉ còn {conLai} phút đến giờ đóng, ít hơn {k.ThoiLuong} phút làm bài. Bạn phải nộp trước {kt:HH:mm}.", MucDo = MucDoKiemTra.CanhBao });
            }
        }

        ds.Add(k.TrangThai switch
        {
            2 => new() { Ten = "Chưa nộp bài", MoTa = "Bạn đã nộp bài cho kỳ thi này.", MucDo = MucDoKiemTra.KhongDat },
            1 => new() { Ten = "Chưa nộp bài", MoTa = "Bạn đang có bài làm dở; vào thi sẽ tiếp tục bài đó.", MucDo = MucDoKiemTra.CanhBao },
            _ => new() { Ten = "Chưa nộp bài", MoTa = "Bạn chưa làm bài kỳ thi này.", MucDo = MucDoKiemTra.Dat }
        });

        ds.Add(k.DaPhatHanh && k.SoCauHoi > 0
            ? new() { Ten = "Đề thi sẵn sàng", MoTa = $"Đề đã phát hành, gồm {k.SoCauHoi} câu.", MucDo = MucDoKiemTra.Dat }
            : new() { Ten = "Đề thi sẵn sàng", MoTa = "Đề thi chưa được phát hành hoặc chưa có câu hỏi. Vui lòng báo giảng viên.", MucDo = MucDoKiemTra.KhongDat });

        return ds;
    }
}
