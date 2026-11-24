using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Models;
using ExamOnlineSystem.Models.ViewModels;
using ExamOnlineSystem.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ExamOnlineSystem.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Teacher)]
public class LopHocPhanController : Controller
{
    private readonly ILopHocPhanService _lopHocPhanService;
    private readonly IHocPhanService _hocPhanService;
    private readonly INamHocService _namHocService;

    public LopHocPhanController(ILopHocPhanService lopHocPhanService,
        IHocPhanService hocPhanService,
        INamHocService namHocService)
    {
        _lopHocPhanService = lopHocPhanService;
        _hocPhanService = hocPhanService;
        _namHocService = namHocService;
    }

    // GET: /Admin/LopHocPhan – dữ liệu bảng được lophocphan.js tải qua GetAllLopHocPhan
    // GET: /Admin/LopHocPhan?namHocId=7&hocKy=1
    // Không có namHocId -> chọn sẵn năm học hiện tại; namHocId=0 -> tất cả năm học
    public async Task<IActionResult> Index(int? namHocId, int? hocKy)
    {
        var namHocs = (await _namHocService.GetAllNamHocAsync()).ToList();
        namHocId ??= namHocs.FirstOrDefault(n => n.LaNamHienTai)?.Id ?? 0;

        ViewData["NamHocList"] = namHocs.Select(n => new SelectListItem
        {
            Value = n.Id.ToString(),
            Text = n.MaNamHoc + (n.LaNamHienTai ? " (hiện tại)" : ""),
            Selected = n.Id == namHocId
        }).ToList();
        ViewData["HocKyList"] = LopHocPhanLabel.TatCaHocKy.Select(k => new SelectListItem
        {
            Value = k.ToString(),
            Text = LopHocPhanLabel.TenHocKy(k),
            Selected = k == hocKy
        }).ToList();
        return View();
    }

    // GET: /Admin/LopHocPhan/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var vm = await _lopHocPhanService.GetChiTietAsync(id.Value);
        if (vm == null)
        {
            return NotFound();
        }

        return View(vm);
    }

    // GET: /Admin/LopHocPhan/Create
    public async Task<IActionResult> Create()
    {
        var namHocHienTai = await _namHocService.GetNamHocHienTaiAsync();
        var lopHocPhan = new LopHocPhan
        {
            NamHocId = namHocHienTai?.Id ?? 0,
            HocKy = 1,
            SiSoToiDa = 60
        };

        await LoadDanhMuc(lopHocPhan);
        return View(lopHocPhan);
    }

    // POST: /Admin/LopHocPhan/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LopHocPhan lopHocPhan)
    {
        await KiemTraAsync(lopHocPhan);

        if (!ModelState.IsValid)
        {
            await LoadDanhMuc(lopHocPhan);
            return View(lopHocPhan);
        }

        var moi = await _lopHocPhanService.CreateLopHocPhanAsync(lopHocPhan);
        TempData["success"] = "Thêm lớp học phần thành công. Hãy phân công giảng viên và sinh viên.";
        return RedirectToAction(nameof(PhanCong), new { id = moi.Id });
    }

    // GET: /Admin/LopHocPhan/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var lopHocPhan = await _lopHocPhanService.GetLopHocPhanByIdAsync(id.Value);
        if (lopHocPhan == null)
        {
            return NotFound();
        }

        await LoadDanhMuc(lopHocPhan);
        return View(lopHocPhan);
    }

    // POST: /Admin/LopHocPhan/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([FromRoute] int id, LopHocPhan lopHocPhan)
    {
        if (id != lopHocPhan.Id)
        {
            return NotFound();
        }

        await KiemTraAsync(lopHocPhan, lopHocPhan.Id);

        // Không hạ sĩ số tối đa xuống dưới số sinh viên đang có
        var soSinhVien = await _lopHocPhanService.DemSinhVienAsync(lopHocPhan.Id);
        if (lopHocPhan.SiSoToiDa < soSinhVien)
        {
            ModelState.AddModelError(nameof(LopHocPhan.SiSoToiDa),
                $"Lớp học phần đang có {soSinhVien} sinh viên nên sĩ số tối đa không được nhỏ hơn {soSinhVien}");
        }

        if (!ModelState.IsValid)
        {
            await LoadDanhMuc(lopHocPhan);
            return View(lopHocPhan);
        }

        if (!await _lopHocPhanService.UpdateLopHocPhanAsync(lopHocPhan))
        {
            return NotFound();
        }

        TempData["success"] = "Cập nhật lớp học phần thành công";
        return RedirectToAction(nameof(Index));
    }

    // GET: /Admin/LopHocPhan/PhanCong/5 – phân công giảng viên, sinh viên
    // (danh sách ứng viên do lophocphan-phancong.js tải qua Ajax)
    public async Task<IActionResult> PhanCong(int? id, string? tab)
    {
        if (id == null)
        {
            return NotFound();
        }

        var lopHocPhan = await _lopHocPhanService.GetLopHocPhanByIdAsync(id.Value);
        if (lopHocPhan == null)
        {
            return NotFound();
        }

        var vm = new PhanCongLopHocPhanVM
        {
            LopHocPhan = lopHocPhan,
            GiaoVienTrongLop = await _lopHocPhanService.GetGiaoVienTrongLopAsync(lopHocPhan.Id),
            SinhVienTrongLop = await _lopHocPhanService.GetSinhVienTrongLopAsync(lopHocPhan.Id),
            Tab = tab == "sinhvien" ? "sinhvien" : "giaovien",
            // Cảnh báo của lần phân công vừa thực hiện (đặt trong POST, hiện sau redirect)
            CanhBao = TempData["phanCongCanhBao"] is string json && !string.IsNullOrEmpty(json)
                ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>()
                : new List<string>()
        };
        return View(vm);
    }

    // POST: /Admin/LopHocPhan/PhanCongGiaoVien – phân công giảng viên vào lớp học phần
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PhanCongGiaoVien(int lopHocPhanId, int[]? giaoVienIds, int vaiTro)
    {
        if (lopHocPhanId <= 0)
        {
            return NotFound();
        }

        GhiKetQua(await _lopHocPhanService.PhanCongGiaoVienAsync(
            lopHocPhanId, giaoVienIds ?? Array.Empty<int>(), vaiTro));
        return RedirectToAction(nameof(PhanCong), new { id = lopHocPhanId, tab = "giaovien" });
    }

    // POST: /Admin/LopHocPhan/DoiVaiTroGiaoVien – đổi vai trò phụ trách / trợ giảng
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DoiVaiTroGiaoVien(int lopHocPhanId, int giaoVienId, int vaiTro)
    {
        if (lopHocPhanId <= 0)
        {
            return NotFound();
        }

        GhiKetQua(await _lopHocPhanService.DoiVaiTroGiaoVienAsync(lopHocPhanId, giaoVienId, vaiTro));
        return RedirectToAction(nameof(PhanCong), new { id = lopHocPhanId, tab = "giaovien" });
    }

    // POST: /Admin/LopHocPhan/BoGiaoVien – bỏ phân công giảng viên
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BoGiaoVien(int lopHocPhanId, int[]? giaoVienIds)
    {
        if (lopHocPhanId <= 0)
        {
            return NotFound();
        }

        GhiKetQua(await _lopHocPhanService.BoGiaoVienAsync(lopHocPhanId, giaoVienIds ?? Array.Empty<int>()));
        return RedirectToAction(nameof(PhanCong), new { id = lopHocPhanId, tab = "giaovien" });
    }

    // POST: /Admin/LopHocPhan/ThemSinhVien – phân sinh viên vào lớp học phần
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ThemSinhVien(int lopHocPhanId, int[]? sinhVienIds)
    {
        if (lopHocPhanId <= 0)
        {
            return NotFound();
        }

        GhiKetQua(await _lopHocPhanService.ThemSinhVienAsync(lopHocPhanId, sinhVienIds ?? Array.Empty<int>()));
        return RedirectToAction(nameof(PhanCong), new { id = lopHocPhanId, tab = "sinhvien" });
    }

    // POST: /Admin/LopHocPhan/BoSinhVien – bỏ sinh viên khỏi lớp học phần
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BoSinhVien(int lopHocPhanId, int[]? sinhVienIds)
    {
        if (lopHocPhanId <= 0)
        {
            return NotFound();
        }

        GhiKetQua(await _lopHocPhanService.BoSinhVienAsync(lopHocPhanId, sinhVienIds ?? Array.Empty<int>()));
        return RedirectToAction(nameof(PhanCong), new { id = lopHocPhanId, tab = "sinhvien" });
    }

    // Kết quả phân công -> toast (thành công/lỗi) + danh sách cảnh báo hiện trên trang sau redirect
    private void GhiKetQua(PhanCongResult kq)
    {
        if (kq.Success)
        {
            TempData["success"] = kq.Message;
        }
        else
        {
            TempData["error"] = kq.Message;
        }

        if (kq.CanhBao.Count > 0)
        {
            TempData["phanCongCanhBao"] = System.Text.Json.JsonSerializer.Serialize(kq.CanhBao);
        }
    }

    // Kiểm tra nghiệp vụ dùng chung cho Thêm / Sửa
    private async Task KiemTraAsync(LopHocPhan lopHocPhan, int? excludeId = null)
    {
        if (!string.IsNullOrWhiteSpace(lopHocPhan.MaLopHocPhan)
            && await _lopHocPhanService.MaLopHocPhanExistsAsync(lopHocPhan.MaLopHocPhan, excludeId))
        {
            ModelState.AddModelError(nameof(LopHocPhan.MaLopHocPhan),
                $"Mã lớp học phần \"{lopHocPhan.MaLopHocPhan.Trim()}\" đã tồn tại");
        }

        if (lopHocPhan.HocPhanId > 0 && !await _hocPhanService.ExistsAsync(lopHocPhan.HocPhanId))
        {
            ModelState.AddModelError(nameof(LopHocPhan.HocPhanId), "Vui lòng chọn học phần hợp lệ");
        }

        if (lopHocPhan.NamHocId > 0 && await _namHocService.GetNamHocByIdAsync(lopHocPhan.NamHocId) == null)
        {
            ModelState.AddModelError(nameof(LopHocPhan.NamHocId), "Vui lòng chọn năm học hợp lệ");
        }
    }

    // Dropdown học phần / năm học / học kỳ cho form Thêm, Sửa
    private async Task LoadDanhMuc(LopHocPhan lopHocPhan)
    {
        var hocPhans = await _hocPhanService.GetAllHocPhanAsync();
        ViewData["HocPhanList"] = new SelectList(
            hocPhans.Select(h => new
            {
                h.Id,
                Ten = $"{h.MaHocPhan} – {h.TenHocPhan} ({h.SoTinChi} TC, {h.Khoa?.TenKhoa})"
            }),
            "Id", "Ten", lopHocPhan.HocPhanId);

        var namHocs = await _namHocService.GetAllNamHocAsync();
        ViewData["NamHocList"] = new SelectList(
            namHocs.Select(n => new
            {
                n.Id,
                Ten = n.LaNamHienTai ? $"{n.TenNamHoc} (hiện tại)" : n.TenNamHoc
            }),
            "Id", "Ten", lopHocPhan.NamHocId);

        ViewData["HocKyList"] = new SelectList(
            LopHocPhanLabel.TatCaHocKy.Select(k => new { Id = k, Ten = LopHocPhanLabel.TenHocKy(k) }),
            "Id", "Ten", lopHocPhan.HocKy);
    }

    #region CALL API

    public async Task<IActionResult> GetAllLopHocPhan()
    {
        var data = (await _lopHocPhanService.GetAllLopHocPhanAsync())
            .Select(l => new
            {
                l.Id,
                l.MaLopHocPhan,
                l.TenLopHocPhan,
                l.MaHocPhan,
                l.TenHocPhan,
                l.TenMonHoc,
                l.TenKhoa,
                l.SoTinChi,
                l.NamHocId,
                l.MaNamHoc,
                l.NamHienTai,
                l.HocKy,
                TenHocKy = LopHocPhanLabel.TenHocKy(l.HocKy),
                l.SiSoToiDa,
                l.SoSinhVien,
                l.SoGiaoVien,
                l.GiaoVienPhuTrach,
                l.GhiChu
            });
        return Json(new { data });
    }

    // Ứng viên để phân công: giảng viên chưa có trong lớp học phần
    public async Task<IActionResult> GetGiaoVienNgoaiLop(int? lopHocPhanId)
    {
        if (lopHocPhanId == null || lopHocPhanId <= 0)
        {
            return Json(new { data = Array.Empty<object>() });
        }

        var data = (await _lopHocPhanService.GetGiaoVienNgoaiLopAsync(lopHocPhanId.Value))
            .Select(g => new
            {
                g.Id,
                g.MaGiaoVien,
                g.HoTen,
                g.Email,
                g.BoMonId,
                g.TenBoMon,
                g.KhoaId,
                g.TenKhoa,
                g.SoLopHocPhan
            });
        return Json(new { data });
    }

    // Ứng viên để phân vào lớp học phần: sinh viên chưa có trong lớp
    public async Task<IActionResult> GetSinhVienNgoaiLop(int? lopHocPhanId)
    {
        if (lopHocPhanId == null || lopHocPhanId <= 0)
        {
            return Json(new { data = Array.Empty<object>() });
        }

        var data = (await _lopHocPhanService.GetSinhVienNgoaiLopAsync(lopHocPhanId.Value))
            .Select(s => new
            {
                s.Id,
                s.MaSinhVien,
                s.HoTen,
                NgaySinh = s.NgaySinh.ToString("dd/MM/yyyy"),
                GioiTinh = s.GioiTinh ? "Nam" : "Nữ",
                s.Email,
                s.LopId,
                s.MaLop,
                s.TenLop,
                s.KhoaId,
                s.TenKhoa
            });
        return Json(new { data });
    }

    [HttpDelete]
    [ValidateAntiForgeryToken] // token gửi qua header "RequestVerificationToken" từ lophocphan.js
    public async Task<IActionResult> DeleteLopHocPhan(int? id)
    {
        if (id == null || id == 0)
        {
            return Json(new { success = false, message = "id không hợp lệ" });
        }

        var (success, message) = await _lopHocPhanService.DeleteLopHocPhanAsync(id.Value);
        return Json(new { success, message });
    }

    #endregion
}
