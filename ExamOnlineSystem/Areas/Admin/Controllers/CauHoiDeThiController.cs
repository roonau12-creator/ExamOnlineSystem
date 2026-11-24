using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Models;
using ExamOnlineSystem.Models.ViewModels;
using ExamOnlineSystem.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ExamOnlineSystem.Areas.Admin.Controllers;

// Câu hỏi đề thi: danh sách (xem) + thêm 1 câu vào đề nháp.
// Sắp xếp lại/bỏ nhiều câu cùng lúc vẫn dùng form Sửa đề thi.
[Area("Admin")]
[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Teacher)]
public class CauHoiDeThiController : Controller
{
    private readonly IDeThiService _deThiService;
    private readonly ICauHoiService _cauHoiService;
    private readonly IDanhMucCauHoiService _danhMucCauHoiService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CauHoiDeThiController(IDeThiService deThiService, ICauHoiService cauHoiService,
        IDanhMucCauHoiService danhMucCauHoiService, UserManager<ApplicationUser> userManager)
    {
        _deThiService = deThiService;
        _cauHoiService = cauHoiService;
        _danhMucCauHoiService = danhMucCauHoiService;
        _userManager = userManager;
    }

    // GET: /Admin/CauHoiDeThi?deThiId=5 (deThiId không bắt buộc: lọc sẵn theo đề)
    public IActionResult Index(int? deThiId)
    {
        ViewData["DeThiId"] = deThiId;
        return View();
    }

    // GET: /Admin/CauHoiDeThi/Create?deThiId=5 (deThiId không bắt buộc: chọn sẵn đề) — chọn nhiều câu thêm cùng lúc
    public async Task<IActionResult> Create(int? deThiId)
    {
        var vm = new ThemNhieuCauVM();
        var cacDe = await DeDuocSuaAsync();
        if (deThiId != null && cacDe.Any(d => d.Id == deThiId))
        {
            vm.DeThiId = deThiId.Value;
        }
        ViewData["DeThiList"] = cacDe;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ThemNhieuCauVM vm)
    {
        const string keyCau = nameof(ThemNhieuCauVM.Cau);
        var de = vm.DeThiId > 0 ? await _deThiService.GetDeThiByIdAsync(vm.DeThiId) : null;
        if (vm.DeThiId > 0)
        {
            if (de == null)
                ModelState.AddModelError(nameof(vm.DeThiId), "Đề thi không tồn tại");
            else if (!await CoQuyenSuaAsync(de))
                ModelState.AddModelError(nameof(vm.DeThiId), "Bạn chỉ được thêm câu vào đề thi do mình tạo");
            else if (de.DaPhatHanh)
                ModelState.AddModelError(nameof(vm.DeThiId), $"Đề {de.MaDeThi} đã phát hành nên không thêm câu được. Hãy hủy phát hành trước.");
        }

        vm.Cau = vm.Cau.Where(c => c.CauHoiId > 0).ToList();
        if (vm.Cau.Count == 0)
        {
            ModelState.AddModelError(keyCau, "Vui lòng chọn ít nhất 1 câu hỏi");
        }
        else if (de != null && ModelState.IsValid)
        {
            // Mỗi câu: cùng môn, đúng loại đề, chưa có trong đề, đủ đáp án; không trùng trong lần thêm; điểm > 0
            var ungVien = (await DanhSachCauChoDeAsync(de)).ToDictionary(x => x.Id, x => x.LyDo);
            foreach (var g in vm.Cau.GroupBy(c => c.CauHoiId).Where(g => g.Count() > 1))
                ModelState.AddModelError(keyCau, $"Câu hỏi #{g.Key} được chọn nhiều lần");
            foreach (var c in vm.Cau.DistinctBy(c => c.CauHoiId))
            {
                if (!ungVien.TryGetValue(c.CauHoiId, out var lyDo))
                    ModelState.AddModelError(keyCau, $"Câu hỏi #{c.CauHoiId} không tồn tại hoặc không thuộc môn học của đề");
                else if (lyDo != null)
                    ModelState.AddModelError(keyCau, $"Câu hỏi #{c.CauHoiId}: {lyDo}");
                if (c.Diem <= 0 || c.Diem > 1000)
                    ModelState.AddModelError(keyCau, $"Câu hỏi #{c.CauHoiId}: điểm phải lớn hơn 0 và không quá 1000");
            }
        }

        if (!ModelState.IsValid)
        {
            ViewData["DeThiList"] = await DeDuocSuaAsync();
            return View(vm);
        }

        var batDau = await _deThiService.AddNhieuCauHoiVaoDeAsync(
            vm.DeThiId, vm.Cau.Select(c => (c.CauHoiId, Math.Round(c.Diem, 2))).ToList(), vm.ViTri);
        var tong = Math.Round((await _deThiService.GetCauHoiDeThisAsync(vm.DeThiId)).Sum(c => c.Diem), 2);
        var viTri = vm.Cau.Count == 1 ? $"câu {batDau}" : $"câu {batDau}–{batDau + vm.Cau.Count - 1}";
        TempData["success"] = $"Đã thêm {vm.Cau.Count} câu hỏi vào đề {de!.MaDeThi} ({viTri}). " +
                              $"Tổng điểm các câu: {tong}/{de.TongDiem}" + (Math.Abs(tong - de.TongDiem) < 0.001 ? " ✓" : "");

        return vm.ThemTiep
            ? RedirectToAction("Create", new { deThiId = vm.DeThiId })
            : RedirectToAction("Index", new { deThiId = vm.DeThiId });
    }

    // GET: /Admin/CauHoiDeThi/Edit/5?returnUrl=... — đổi câu hỏi / điểm / vị trí của 1 câu trong đề
    public async Task<IActionResult> Edit(int? id, string? returnUrl)
    {
        if (id == null)
        {
            return NotFound();
        }
        var dong = await _deThiService.GetCauHoiDeThiByIdAsync(id.Value);
        if (dong == null)
        {
            return NotFound();
        }
        if (await LoiKhongSuaDuocAsync(dong.DeThi) is { } loi)
        {
            TempData["error"] = loi;
            return RedirectToAction("Details", "DeThi", new { id = dong.DeThiId });
        }

        var vm = new CauHoiDeThiFormVM
        {
            Id = dong.Id,
            DeThiId = dong.DeThiId,
            CauHoiId = dong.CauHoiId,
            Diem = dong.Diem,
            ViTri = dong.ThuTu,
            ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null
        };
        await LoadEditInfo(dong);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CauHoiDeThiFormVM vm)
    {
        if (id != vm.Id)
        {
            return NotFound();
        }
        // Đề lấy theo dòng trong DB, không tin DeThiId gửi lên (không cho chuyển câu sang đề khác)
        var dong = await _deThiService.GetCauHoiDeThiByIdAsync(id);
        if (dong == null)
        {
            return NotFound();
        }
        if (await LoiKhongSuaDuocAsync(dong.DeThi) is { } loi)
        {
            TempData["error"] = loi;
            return RedirectToAction("Details", "DeThi", new { id = dong.DeThiId });
        }
        vm.DeThiId = dong.DeThiId;

        // Đổi sang câu khác: phải cùng môn, đúng loại đề, chưa có trong đề, đủ đáp án
        if (vm.CauHoiId > 0 && vm.CauHoiId != dong.CauHoiId)
        {
            var ungVien = (await DanhSachCauChoDeAsync(dong.DeThi)).FirstOrDefault(c => c.Id == vm.CauHoiId);
            if (ungVien.Id == 0)
                ModelState.AddModelError(nameof(vm.CauHoiId), $"Câu hỏi #{vm.CauHoiId} không tồn tại hoặc không thuộc môn học của đề");
            else if (ungVien.LyDo != null)
                ModelState.AddModelError(nameof(vm.CauHoiId), $"Câu hỏi #{vm.CauHoiId}: {ungVien.LyDo}");
        }
        var soCau = (await _deThiService.GetCauHoiDeThisAsync(dong.DeThiId)).Count;
        if (vm.ViTri is not > 0 || vm.ViTri > soCau)
        {
            ModelState.AddModelError(nameof(vm.ViTri), $"Vị trí phải từ 1 đến {soCau}");
        }

        if (!ModelState.IsValid)
        {
            await LoadEditInfo(dong);
            return View(vm);
        }

        await _deThiService.UpdateCauHoiDeThiAsync(id, vm.CauHoiId, Math.Round(vm.Diem, 2), vm.ViTri!.Value);
        var tong = Math.Round((await _deThiService.GetCauHoiDeThisAsync(dong.DeThiId)).Sum(c => c.Diem), 2);
        var thayDoi = new List<string>();
        if (vm.CauHoiId != dong.CauHoiId) thayDoi.Add($"đổi câu #{dong.CauHoiId} → #{vm.CauHoiId}");
        if (Math.Abs(vm.Diem - dong.Diem) >= 0.001) thayDoi.Add($"điểm {dong.Diem} → {Math.Round(vm.Diem, 2)}");
        if (vm.ViTri != dong.ThuTu) thayDoi.Add($"vị trí câu {dong.ThuTu} → câu {vm.ViTri}");
        TempData["success"] = $"Đã cập nhật câu trong đề {dong.DeThi.MaDeThi}" +
                              (thayDoi.Count > 0 ? $": {string.Join(", ", thayDoi)}" : " (không có thay đổi)") +
                              $". Tổng điểm các câu: {tong}/{dong.DeThi.TongDiem}";

        return Url.IsLocalUrl(vm.ReturnUrl)
            ? LocalRedirect(vm.ReturnUrl!)
            : RedirectToAction("Details", "DeThi", new { id = dong.DeThiId });
    }

    // GET: /Admin/CauHoiDeThi/SapXep?deThiId=5 — kéo thả / sắp nhanh thứ tự các câu trong đề
    public async Task<IActionResult> SapXep(int? deThiId)
    {
        if (deThiId == null)
        {
            return NotFound();
        }
        var de = await _deThiService.GetDeThiByIdAsync(deThiId.Value);
        if (de == null)
        {
            return NotFound();
        }
        if (await LoiKhongSuaDuocAsync(de) is { } loi)
        {
            TempData["error"] = loi;
            return RedirectToAction("Details", "DeThi", new { id = de.Id });
        }

        var cauTrongDe = await _deThiService.GetCauHoiDeThisAsync(de.Id);
        var thongTin = (await _deThiService.GetCauHoiTheoIdsAsync(cauTrongDe.Select(c => c.CauHoiId))).ToDictionary(c => c.Id);
        ViewData["DeThi"] = de;
        ViewData["CauHois"] = cauTrongDe.Select(c =>
        {
            thongTin.TryGetValue(c.CauHoiId, out var ch);
            return (DongId: c.Id, c.ThuTu, c.CauHoiId, c.Diem,
                    NoiDung: ch?.NoiDung ?? "", Loai: ch?.LoaiCauHoi ?? 0, MucDo: ch?.MucDo ?? 0, CoHinhAnh: ch?.CoHinhAnh ?? false);
        }).ToList();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SapXep(int deThiId, List<int> thuTu)
    {
        var de = await _deThiService.GetDeThiByIdAsync(deThiId);
        if (de == null)
        {
            return NotFound();
        }
        if (await LoiKhongSuaDuocAsync(de) is { } loi)
        {
            TempData["error"] = loi;
            return RedirectToAction("Details", "DeThi", new { id = de.Id });
        }

        var cu = (await _deThiService.GetCauHoiDeThisAsync(deThiId)).Select(c => c.Id).ToList();
        if (!await _deThiService.SapXepCauHoiAsync(deThiId, thuTu))
        {
            TempData["error"] = "Danh sách câu đã thay đổi (có thể vừa được thêm/bỏ câu ở nơi khác). Vui lòng sắp xếp lại.";
            return RedirectToAction("SapXep", new { deThiId });
        }

        var soCauDoi = thuTu.Where((dongId, i) => cu[i] != dongId).Count();
        TempData["success"] = soCauDoi == 0
            ? $"Thứ tự câu của đề {de.MaDeThi} không thay đổi"
            : $"Đã lưu thứ tự mới cho đề {de.MaDeThi} ({soCauDoi}/{cu.Count} câu đổi vị trí)";
        return RedirectToAction("Details", "DeThi", new { id = deThiId });
    }

    // GET: /Admin/CauHoiDeThi/PhanBoDiem?deThiId=5 — thiết lập điểm từng câu (nhập tay hoặc phân bổ nhanh)
    public async Task<IActionResult> PhanBoDiem(int? deThiId)
    {
        if (deThiId == null)
        {
            return NotFound();
        }
        var de = await _deThiService.GetDeThiByIdAsync(deThiId.Value);
        if (de == null)
        {
            return NotFound();
        }
        if (await LoiKhongSuaDuocAsync(de) is { } loi)
        {
            TempData["error"] = loi;
            return RedirectToAction("Details", "DeThi", new { id = de.Id });
        }

        var vm = new PhanBoDiemVM { DeThiId = de.Id, TongDiem = de.TongDiem };
        await NapThongTinCau(vm, null);
        ViewData["DeThi"] = de;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PhanBoDiem(PhanBoDiemVM vm)
    {
        var de = await _deThiService.GetDeThiByIdAsync(vm.DeThiId);
        if (de == null)
        {
            return NotFound();
        }
        if (await LoiKhongSuaDuocAsync(de) is { } loi)
        {
            TempData["error"] = loi;
            return RedirectToAction("Details", "DeThi", new { id = de.Id });
        }

        if (vm.Cau.Any(c => c.Diem <= 0 || c.Diem > 1000))
        {
            ModelState.AddModelError(string.Empty, "Điểm mỗi câu phải lớn hơn 0 và không quá 1000");
        }
        if (vm.Cau.Select(c => c.Id).Distinct().Count() != vm.Cau.Count)
        {
            ModelState.AddModelError(string.Empty, "Dữ liệu câu bị trùng");
        }
        if (!ModelState.IsValid)
        {
            await NapThongTinCau(vm, vm.Cau.ToDictionary(c => c.Id, c => c.Diem, EqualityComparer<int>.Default));
            ViewData["DeThi"] = de;
            return View(vm);
        }

        var diem = vm.Cau.ToDictionary(c => c.Id, c => Math.Round(c.Diem, 2));
        if (!await _deThiService.PhanBoDiemAsync(de.Id, Math.Round(vm.TongDiem, 2), diem))
        {
            TempData["error"] = "Danh sách câu đã thay đổi (có thể vừa được thêm/bỏ câu ở nơi khác). Vui lòng phân bổ lại.";
            return RedirectToAction("PhanBoDiem", new { deThiId = de.Id });
        }

        var tong = Math.Round(diem.Values.Sum(), 2);
        var tongDe = Math.Round(vm.TongDiem, 2);
        TempData["success"] = Math.Abs(tong - tongDe) < 0.001
            ? $"Đã lưu điểm {diem.Count} câu của đề {de.MaDeThi}: tổng {tong}/{tongDe} ✓"
            : $"Đã lưu điểm {diem.Count} câu của đề {de.MaDeThi}. Lưu ý: tổng điểm các câu là {tong}/{tongDe}";
        return RedirectToAction("Details", "DeThi", new { id = de.Id });
    }

    // Nạp thông tin hiển thị của các câu theo thứ tự trong đề; diemDaNhap != null => giữ điểm người dùng vừa nhập
    private async Task NapThongTinCau(PhanBoDiemVM vm, Dictionary<int, double>? diemDaNhap)
    {
        var cauTrongDe = await _deThiService.GetCauHoiDeThisAsync(vm.DeThiId);
        var thongTin = (await _deThiService.GetCauHoiTheoIdsAsync(cauTrongDe.Select(c => c.CauHoiId))).ToDictionary(c => c.Id);
        vm.Cau = cauTrongDe.Select(c =>
        {
            thongTin.TryGetValue(c.CauHoiId, out var ch);
            return new DiemCauVM
            {
                Id = c.Id,
                Diem = diemDaNhap != null && diemDaNhap.TryGetValue(c.Id, out var d) ? d : c.Diem,
                ThuTu = c.ThuTu,
                CauHoiId = c.CauHoiId,
                NoiDung = ch?.NoiDung ?? "",
                LoaiCauHoi = ch?.LoaiCauHoi ?? 0,
                MucDo = ch?.MucDo ?? 0,
                DiemMacDinh = ch?.DiemMacDinh ?? 0
            };
        }).ToList();
    }

    // null = sửa được; ngược lại là lý do không sửa được câu trong đề
    private async Task<string?> LoiKhongSuaDuocAsync(DeThi de)
    {
        if (!await CoQuyenSuaAsync(de))
            return "Bạn chỉ được sửa câu trong đề thi do mình tạo";
        if (de.DaPhatHanh)
            return $"Đề {de.MaDeThi} đã phát hành nên không sửa câu được. Hãy hủy phát hành trước.";
        return null;
    }

    // Dữ liệu cho form Sửa: đề, các câu trong đề (để chọn vị trí, tính tổng điểm), câu có thể đổi sang
    private async Task LoadEditInfo(CauHoiDeThi dong)
    {
        var cauTrongDe = await _deThiService.GetCauHoiDeThisAsync(dong.DeThiId);
        var thongTin = (await _deThiService.GetCauHoiTheoIdsAsync(cauTrongDe.Select(c => c.CauHoiId))).ToDictionary(c => c.Id);
        ViewData["DeThi"] = dong.DeThi;
        ViewData["DongGoc"] = dong;
        ViewData["CauTrongDe"] = cauTrongDe
            .Select(c => (c.ThuTu, c.CauHoiId, c.Diem, NoiDung: thongTin.TryGetValue(c.CauHoiId, out var ch) ? ch.NoiDung : ""))
            .ToList();
        // Câu hiện tại luôn chọn được; câu khác theo điều kiện như khi thêm
        ViewData["UngVien"] = (await DanhSachCauChoDeAsync(dong.DeThi))
            .Select(x => (x.Cau, LyDo: x.Id == dong.CauHoiId ? null : x.LyDo))
            .ToList();
    }

    // Đề nháp mà người dùng được sửa: Admin = mọi đề nháp, giảng viên = đề nháp của mình
    private async Task<List<DeThiListItemVM>> DeDuocSuaAsync()
    {
        var tatCa = (await _deThiService.GetAllDeThiAsync()).Where(d => !d.DaPhatHanh);
        if (!User.IsInRole(SD.Role_Admin))
        {
            var gv = await GiaoVienHienTaiAsync();
            tatCa = tatCa.Where(d => gv != null && d.GiaoVienId == gv.Id);
        }
        return tatCa.OrderBy(d => d.MaDeThi).ToList();
    }

    // Câu hỏi cùng môn với đề, kèm lý do không thêm được (null = thêm được)
    private async Task<List<(int Id, CauHoiChonVaoDeVM Cau, string? LyDo)>> DanhSachCauChoDeAsync(DeThi de)
    {
        var daCo = (await _deThiService.GetCauHoiDeThisAsync(de.Id)).Select(c => c.CauHoiId).ToHashSet();
        return (await _deThiService.GetCauHoiTheoMonHocAsync(de.MonHocId)).Select(c =>
        {
            string? lyDo = daCo.Contains(c.Id) ? "đã có trong đề"
                : de.LoaiDeThi != 3 && de.LoaiDeThi != c.LoaiCauHoi
                    ? $"câu {CauHoiLabel.TenLoaiCauHoi(c.LoaiCauHoi).ToLower()} không phù hợp đề {DeThiLabel.TenLoaiDeThi(de.LoaiDeThi).ToLower()}"
                : c.LoaiCauHoi == 1 && c.SoDapAn < 2 ? "câu trắc nghiệm chưa đủ 2 đáp án"
                : null;
            return (c.Id, c, lyDo);
        }).ToList();
    }

    private async Task<bool> CoQuyenSuaAsync(DeThi deThi)
    {
        if (User.IsInRole(SD.Role_Admin))
        {
            return true;
        }
        var gv = await GiaoVienHienTaiAsync();
        return gv != null && gv.Id == deThi.GiaoVienId;
    }

    private async Task<GiaoVien?> GiaoVienHienTaiAsync()
    {
        var userId = _userManager.GetUserId(User);
        return userId == null ? null : await _cauHoiService.GetGiaoVienByUserIdAsync(userId);
    }

    #region  CALL API

    public async Task<IActionResult> GetAllCauHoiDeThi()
    {
        var ds = await _deThiService.GetAllCauHoiDeThiAsync();

        // Đáp án đúng theo thứ tự đáp án của từng đề (đã áp dụng trộn đáp án)
        var dapAnTheoCau = await _cauHoiService.GetDapAnsTheoCauHoiAsync(ds.Select(c => c.CauHoiId));

        var data = ds.Select(c =>
        {
            var goc = dapAnTheoCau.GetValueOrDefault(c.CauHoiId) ?? new List<DapAn>();
            var theoDe = ThuTuDapAnHelper.SapXep(goc, c.ThuTuDapAn);
            var dapAnDung = c.LoaiCauHoi != 1
                ? "Tự luận"
                : string.Join(", ", theoDe.Select((d, i) => (d, i)).Where(x => x.d.DapAnDung).Select(x => ThuTuDapAnHelper.Nhan(x.i)));
            return new
            {
                c.Id,
                c.DeThiId,
                c.MaDeThi,
                c.TenDeThi,
                c.DaPhatHanh,
                TrangThai = c.DaPhatHanh ? "Đã phát hành" : "Nháp",
                c.TenMonHoc,
                c.ThuTu,
                c.CauHoiId,
                c.NoiDung,
                c.LoaiCauHoi,
                TenLoaiCauHoi = CauHoiLabel.TenLoaiCauHoi(c.LoaiCauHoi),
                c.MucDo,
                TenMucDo = CauHoiLabel.TenMucDo(c.MucDo),
                c.Diem,
                DapAnDung = dapAnDung == "" ? "—" : dapAnDung,
                DaTronDapAn = c.LoaiCauHoi == 1 && !theoDe.Select(d => d.Id).SequenceEqual(goc.Select(d => d.Id))
            };
        });
        return Json(new { data });
    }

    [HttpDelete]
    [ValidateAntiForgeryToken] // token gửi qua header "RequestVerificationToken"
    public async Task<IActionResult> DeleteCauHoiDeThi(int? id)
    {
        if (id == null || id == 0)
        {
            return Json(new { success = false, message = "id không hợp lệ" });
        }
        var dong = await _deThiService.GetCauHoiDeThiByIdAsync(id.Value);
        if (dong == null)
        {
            return Json(new { success = false, message = "Không tìm thấy câu hỏi trong đề" });
        }
        if (!await CoQuyenSuaAsync(dong.DeThi))
        {
            return Json(new { success = false, message = "Bạn chỉ được bỏ câu khỏi đề thi do mình tạo" });
        }
        if (dong.DeThi.DaPhatHanh)
        {
            return Json(new
            {
                success = false,
                message = $"Đề {dong.DeThi.MaDeThi} đã phát hành nên không bỏ câu được. Hãy hủy phát hành trước."
            });
        }

        await _deThiService.RemoveCauHoiKhoiDeAsync(dong.Id);
        var conLai = await _deThiService.GetCauHoiDeThisAsync(dong.DeThiId);
        var tong = Math.Round(conLai.Sum(c => c.Diem), 2);
        return Json(new
        {
            success = true,
            message = $"Đã bỏ câu hỏi #{dong.CauHoiId} khỏi đề {dong.DeThi.MaDeThi}. " +
                      $"Đề còn {conLai.Count} câu, tổng điểm {tong}/{dong.DeThi.TongDiem}"
        });
    }

    // Thông tin đề + câu hiện có + câu có thể thêm (form Thêm câu hỏi vào đề)
    public async Task<IActionResult> GetCauHoiChoDe(int deThiId)
    {
        var de = await _deThiService.GetDeThiByIdAsync(deThiId);
        if (de == null || de.DaPhatHanh || !await CoQuyenSuaAsync(de))
        {
            return Json(new { success = false, message = "Không thêm được câu vào đề này" });
        }

        var cauTrongDe = await _deThiService.GetCauHoiDeThisAsync(deThiId);
        var thongTin = (await _deThiService.GetCauHoiTheoIdsAsync(cauTrongDe.Select(c => c.CauHoiId))).ToDictionary(c => c.Id);
        var nodes = await _danhMucCauHoiService.GetDanhMucTreeAsync();
        var cay = nodes.ToDictionary(n => n.DanhMuc.Id);

        return Json(new
        {
            success = true,
            // Danh mục của môn (dạng cây) cho bộ lọc
            DanhMucs = nodes.Where(x => x.DanhMuc.MonHocId == de.MonHocId)
                .Select(x => new { x.DanhMuc.Id, Ten = x.DanhMuc.TenDanhMuc, x.Cap }),
            de.TongDiem,
            TenLoaiDeThi = DeThiLabel.TenLoaiDeThi(de.LoaiDeThi),
            TongDiemCau = Math.Round(cauTrongDe.Sum(c => c.Diem), 2),
            CauHienCo = cauTrongDe.Select(c => new
            {
                c.ThuTu,
                c.CauHoiId,
                c.Diem,
                NoiDung = thongTin.TryGetValue(c.CauHoiId, out var ch) ? ch.NoiDung : ""
            }),
            CauCoTheThem = (await DanhSachCauChoDeAsync(de)).Select(x => new
            {
                x.Cau.Id,
                x.Cau.NoiDung,
                DanhMuc = cay.TryGetValue(x.Cau.DanhMucCauHoiId, out var n) ? n.DuongDanDayDu : "",
                DanhMucLoc = cay.TryGetValue(x.Cau.DanhMucCauHoiId, out var n2) ? n2.KhoaLoc : "",
                TenLoaiCauHoi = CauHoiLabel.TenLoaiCauHoi(x.Cau.LoaiCauHoi),
                x.Cau.MucDo,
                TenMucDo = CauHoiLabel.TenMucDo(x.Cau.MucDo),
                x.Cau.DiemMacDinh,
                x.LyDo
            })
        });
    }

    #endregion
}
