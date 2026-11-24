using ClosedXML.Excel;
using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Models;
using ExamOnlineSystem.Models.ViewModels;
using ExamOnlineSystem.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ExamOnlineSystem.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Teacher)]
public class DeThiController : Controller
{
    private readonly IDeThiService _deThiService;
    private readonly ICauHoiService _cauHoiService;
    private readonly IDanhMucCauHoiService _danhMucCauHoiService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILichThiService _lichThiService;

    public DeThiController(IDeThiService deThiService, ICauHoiService cauHoiService,
        IDanhMucCauHoiService danhMucCauHoiService, UserManager<ApplicationUser> userManager,
        ILichThiService lichThiService)
    {
        _lichThiService = lichThiService;
        _deThiService = deThiService;
        _cauHoiService = cauHoiService;
        _danhMucCauHoiService = danhMucCauHoiService;
        _userManager = userManager;
    }

    // GET: /Admin/DeThi
    public IActionResult Index()
    {
        return View();
    }

    // GET: /Admin/DeThi/Create
    public async Task<IActionResult> Create()
    {
        var vm = new DeThiFormVM();
        await LoadDropdowns(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DeThiFormVM vm)
    {
        vm.MaDeThi = vm.MaDeThi?.Trim() ?? string.Empty;
        vm.TenDeThi = vm.TenDeThi?.Trim() ?? string.Empty;

        if (vm.MaDeThi != "" && await _deThiService.MaDeThiExistsAsync(vm.MaDeThi))
        {
            ModelState.AddModelError(nameof(vm.MaDeThi), $"Mã đề thi \"{vm.MaDeThi}\" đã tồn tại");
        }
        var monHocs = await _danhMucCauHoiService.GetAllMonHocAsync();
        if (vm.MonHocId > 0 && monHocs.All(m => m.Id != vm.MonHocId))
        {
            ModelState.AddModelError(nameof(vm.MonHocId), "Môn học không tồn tại");
        }
        var giaoVienId = await XacDinhGiaoVienAsync(vm.GiaoVienId);
        await ValidateCauHoisAsync(vm);

        if (!ModelState.IsValid || giaoVienId == null)
        {
            await LoadDropdowns(vm);
            return View(vm);
        }

        var deThi = new DeThi
        {
            MaDeThi = vm.MaDeThi,
            TenDeThi = vm.TenDeThi,
            MonHocId = vm.MonHocId,
            GiaoVienId = giaoVienId.Value,
            ThoiLuong = vm.ThoiLuong,
            TongDiem = vm.TongDiem,
            LoaiDeThi = vm.LoaiDeThi
        };
        var cauHois = vm.CauHois
            .Select(c => new CauHoiDeThi { CauHoiId = c.CauHoiId, Diem = Math.Round(c.Diem, 2) })
            .ToList();

        try
        {
            await _deThiService.CreateDeThiAsync(deThi, cauHois);
        }
        catch (DbUpdateException)
        {
            // Hai người cùng lưu một mã đề cùng lúc: unique index trong DB chặn lại
            if (!await _deThiService.MaDeThiExistsAsync(vm.MaDeThi))
            {
                throw;
            }
            ModelState.AddModelError(nameof(vm.MaDeThi), $"Mã đề thi \"{vm.MaDeThi}\" đã tồn tại");
            await LoadDropdowns(vm);
            return View(vm);
        }

        var tongDiemCau = cauHois.Sum(c => c.Diem);
        TempData["success"] = Math.Abs(tongDiemCau - vm.TongDiem) < 0.001
            ? $"Thêm đề thi {vm.MaDeThi} thành công ({cauHois.Count} câu, {tongDiemCau} điểm)"
            : $"Đã lưu đề thi {vm.MaDeThi} ({cauHois.Count} câu). Lưu ý: tổng điểm các câu là {tongDiemCau}/{vm.TongDiem}";
        return RedirectToAction("Index");
    }

    // GET: /Admin/DeThi/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }
        var deThi = await _deThiService.GetDeThiByIdAsync(id.Value);
        if (deThi == null)
        {
            return NotFound();
        }
        if (!await CoQuyenSuaAsync(deThi))
        {
            TempData["error"] = "Bạn chỉ được sửa đề thi do mình tạo";
            return RedirectToAction("Index");
        }
        if (deThi.DaPhatHanh)
        {
            TempData["error"] = $"Đề {deThi.MaDeThi} đã phát hành nên không sửa được. Hãy hủy phát hành trước.";
            return RedirectToAction("Details", new { id = deThi.Id });
        }

        var vm = new DeThiFormVM
        {
            Id = deThi.Id,
            MaDeThi = deThi.MaDeThi,
            TenDeThi = deThi.TenDeThi,
            MonHocId = deThi.MonHocId,
            GiaoVienId = deThi.GiaoVienId,
            TenGiaoVien = TenGiaoVien(deThi),
            LoaiDeThi = deThi.LoaiDeThi,
            ThoiLuong = deThi.ThoiLuong,
            TongDiem = deThi.TongDiem,
            CauHois = (await _deThiService.GetCauHoiDeThisAsync(deThi.Id))
                .Select(c => new CauHoiDeThiInputVM { CauHoiId = c.CauHoiId, Diem = c.Diem })
                .ToList()
        };
        await LoadDropdowns(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DeThiFormVM vm)
    {
        if (id != vm.Id)
        {
            return NotFound();
        }
        var cu = await _deThiService.GetDeThiByIdAsync(id);
        if (cu == null)
        {
            return NotFound();
        }
        if (!await CoQuyenSuaAsync(cu))
        {
            TempData["error"] = "Bạn chỉ được sửa đề thi do mình tạo";
            return RedirectToAction("Index");
        }
        if (cu.DaPhatHanh)
        {
            TempData["error"] = $"Đề {cu.MaDeThi} đã phát hành nên không sửa được. Hãy hủy phát hành trước.";
            return RedirectToAction("Details", new { id = cu.Id });
        }

        vm.MaDeThi = vm.MaDeThi?.Trim() ?? string.Empty;
        vm.TenDeThi = vm.TenDeThi?.Trim() ?? string.Empty;
        vm.TenGiaoVien = TenGiaoVien(cu);

        if (vm.MaDeThi != "" && await _deThiService.MaDeThiExistsAsync(vm.MaDeThi, boQuaId: id))
        {
            ModelState.AddModelError(nameof(vm.MaDeThi), $"Mã đề thi \"{vm.MaDeThi}\" đã tồn tại");
        }
        var monHocs = await _danhMucCauHoiService.GetAllMonHocAsync();
        if (vm.MonHocId > 0 && monHocs.All(m => m.Id != vm.MonHocId))
        {
            ModelState.AddModelError(nameof(vm.MonHocId), "Môn học không tồn tại");
        }
        await ValidateCauHoisAsync(vm);

        if (!ModelState.IsValid)
        {
            await LoadDropdowns(vm);
            return View(vm);
        }

        var deThi = new DeThi
        {
            Id = id,
            MaDeThi = vm.MaDeThi,
            TenDeThi = vm.TenDeThi,
            MonHocId = vm.MonHocId,
            ThoiLuong = vm.ThoiLuong,
            TongDiem = vm.TongDiem,
            LoaiDeThi = vm.LoaiDeThi
        };
        var cauHois = vm.CauHois
            .Select(c => new CauHoiDeThi { CauHoiId = c.CauHoiId, Diem = Math.Round(c.Diem, 2) })
            .ToList();

        try
        {
            await _deThiService.UpdateDeThiAsync(deThi, cauHois);
        }
        catch (DbUpdateException)
        {
            if (!await _deThiService.MaDeThiExistsAsync(vm.MaDeThi, boQuaId: id))
            {
                throw;
            }
            ModelState.AddModelError(nameof(vm.MaDeThi), $"Mã đề thi \"{vm.MaDeThi}\" đã tồn tại");
            await LoadDropdowns(vm);
            return View(vm);
        }

        var tongDiemCau = cauHois.Sum(c => c.Diem);
        TempData["success"] = Math.Abs(tongDiemCau - vm.TongDiem) < 0.001
            ? $"Cập nhật đề thi {vm.MaDeThi} thành công ({cauHois.Count} câu, {tongDiemCau} điểm)"
            : $"Đã cập nhật đề thi {vm.MaDeThi} ({cauHois.Count} câu). Lưu ý: tổng điểm các câu là {tongDiemCau}/{vm.TongDiem}";
        return RedirectToAction("Index");
    }

    private static string? TenGiaoVien(DeThi deThi) =>
        deThi.GiaoVien == null ? null : $"{deThi.GiaoVien.HoTen} ({deThi.GiaoVien.MaGiaoVien})";

    // Admin sửa được mọi đề; giảng viên chỉ sửa được đề do mình tạo
    private async Task<bool> CoQuyenSuaAsync(DeThi deThi)
    {
        if (User.IsInRole(SD.Role_Admin))
        {
            return true;
        }
        var giaoVien = await GiaoVienHienTaiAsync();
        return giaoVien != null && giaoVien.Id == deThi.GiaoVienId;
    }

    // Câu hỏi trong đề: tồn tại, đúng môn, không trùng, điểm > 0, loại câu phù hợp loại đề
    private async Task ValidateCauHoisAsync(DeThiFormVM vm)
    {
        const string key = nameof(DeThiFormVM.CauHois);
        if (vm.CauHois.Count == 0 || vm.MonHocId <= 0)
        {
            return;
        }

        if (vm.CauHois.Select(c => c.CauHoiId).Distinct().Count() != vm.CauHois.Count)
        {
            ModelState.AddModelError(key, "Một câu hỏi chỉ được thêm vào đề một lần");
        }
        if (vm.CauHois.Any(c => c.Diem <= 0 || c.Diem > 1000))
        {
            ModelState.AddModelError(key, "Điểm của mỗi câu phải lớn hơn 0");
        }

        var nganHang = (await _deThiService.GetCauHoiTheoMonHocAsync(vm.MonHocId)).ToDictionary(c => c.Id);
        var khongThuocMon = vm.CauHois.Where(c => !nganHang.ContainsKey(c.CauHoiId)).Select(c => "#" + c.CauHoiId).ToList();
        if (khongThuocMon.Count > 0)
        {
            ModelState.AddModelError(key, $"Câu hỏi {string.Join(", ", khongThuocMon)} không tồn tại hoặc không thuộc môn học của đề");
        }

        var saiLoai = vm.CauHois
            .Where(c => nganHang.TryGetValue(c.CauHoiId, out var ch) && !LoaiCauPhuHop(vm.LoaiDeThi, ch.LoaiCauHoi))
            .Select(c => "#" + c.CauHoiId).ToList();
        if (saiLoai.Count > 0)
        {
            ModelState.AddModelError(key,
                $"Đề {DeThiLabel.TenLoaiDeThi(vm.LoaiDeThi).ToLower()} không được chứa câu {string.Join(", ", saiLoai)} " +
                "(khác loại). Hãy bỏ các câu này hoặc chọn loại đề Kết hợp.");
        }

        // Trắc nghiệm có đáp án hợp lệ mới thi được
        var thieuDapAn = vm.CauHois
            .Where(c => nganHang.TryGetValue(c.CauHoiId, out var ch) && ch.LoaiCauHoi == 1 && ch.SoDapAn < 2)
            .Select(c => "#" + c.CauHoiId).ToList();
        if (thieuDapAn.Count > 0)
        {
            ModelState.AddModelError(key, $"Câu trắc nghiệm {string.Join(", ", thieuDapAn)} chưa đủ 2 đáp án");
        }
    }

    // LoaiDeThi 1 = chỉ câu trắc nghiệm, 2 = chỉ câu tự luận, 3 = cả hai
    private static bool LoaiCauPhuHop(int loaiDe, int loaiCau) =>
        loaiDe == 3 || loaiDe == loaiCau;

    // Trả về Id giảng viên của đề; null (kèm lỗi trong ModelState) nếu không xác định được
    private async Task<int?> XacDinhGiaoVienAsync(int? giaoVienIdChon)
    {
        var giaoVien = await GiaoVienHienTaiAsync();
        if (giaoVien != null)
        {
            return giaoVien.Id;
        }
        if (giaoVienIdChon == null || giaoVienIdChon <= 0)
        {
            ModelState.AddModelError(nameof(DeThiFormVM.GiaoVienId), "Vui lòng chọn giảng viên");
            return null;
        }
        if (!await _cauHoiService.GiaoVienExistsAsync(giaoVienIdChon.Value))
        {
            ModelState.AddModelError(nameof(DeThiFormVM.GiaoVienId), "Giảng viên không tồn tại");
            return null;
        }
        return giaoVienIdChon.Value;
    }

    private async Task<GiaoVien?> GiaoVienHienTaiAsync()
    {
        var userId = _userManager.GetUserId(User);
        return userId == null ? null : await _cauHoiService.GetGiaoVienByUserIdAsync(userId);
    }

    private async Task LoadDropdowns(DeThiFormVM vm)
    {
        var monHocs = await _danhMucCauHoiService.GetAllMonHocAsync();
        ViewData["MonHocList"] = new SelectList(monHocs, nameof(MonHoc.Id), nameof(MonHoc.TenMonHoc),
            vm.MonHocId > 0 ? vm.MonHocId : null);

        var giaoVien = await GiaoVienHienTaiAsync();
        ViewData["GiaoVienHienTai"] = giaoVien;
        if (giaoVien == null)
        {
            var giaoViens = await _cauHoiService.GetAllGiaoVienAsync();
            ViewData["GiaoVienList"] = new SelectList(
                giaoViens.Select(g => new { g.Id, Ten = $"{g.HoTen} ({g.MaGiaoVien})" }), "Id", "Ten", vm.GiaoVienId);
        }
    }

    // GET: /Admin/DeThi/Details/5 — đề theo đúng thứ tự câu và đáp án của đề + bảng đáp án
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }
        var deThi = await _deThiService.GetDeThiByIdAsync(id.Value);
        if (deThi == null)
        {
            return NotFound();
        }

        return View(await TaoChiTietVMAsync(deThi));
    }

    // GET: /Admin/DeThi/KiemTraTongDiem?tatCa=true — đối chiếu tổng điểm các câu với tổng điểm đề của mọi đề
    public async Task<IActionResult> KiemTraTongDiem(bool tatCa = false)
    {
        var deThis = await _deThiService.GetAllDeThiAsync();
        var gv = User.IsInRole(SD.Role_Admin) ? null : await GiaoVienHienTaiAsync();
        var ds = deThis.Select(d => new KiemTraTongDiemVM
        {
            DeThi = d,
            TinhTrang = d.SoCauHoi == 0 ? TinhTrangDiem.ChuaCoCau
                      : Math.Abs(d.TongDiemCauHoi - d.TongDiem) < 0.001 ? TinhTrangDiem.Khop
                      : TinhTrangDiem.Lech,
            DuocSua = !d.DaPhatHanh && (User.IsInRole(SD.Role_Admin) || (gv != null && gv.Id == d.GiaoVienId))
        }).ToList();

        ViewData["TatCa"] = tatCa;
        ViewData["SoKhop"] = ds.Count(x => x.TinhTrang == TinhTrangDiem.Khop);
        ViewData["SoLech"] = ds.Count(x => x.TinhTrang == TinhTrangDiem.Lech);
        ViewData["SoChuaCoCau"] = ds.Count(x => x.TinhTrang == TinhTrangDiem.ChuaCoCau);
        return View(ds
            .Where(x => tatCa || x.TinhTrang != TinhTrangDiem.Khop)
            .OrderBy(x => x.TinhTrang == TinhTrangDiem.Khop).ThenBy(x => x.DeThi.MaDeThi)
            .ToList());
    }

    // POST: /Admin/DeThi/DatTongDiemTheoCau/5 — tổng điểm đề := tổng điểm các câu
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DatTongDiemTheoCau(int id, string? returnUrl)
    {
        var deThi = await _deThiService.GetDeThiByIdAsync(id);
        if (deThi == null)
        {
            return NotFound();
        }
        if (await LoiSuaDiemAsync(deThi) is { } loi)
        {
            TempData["error"] = loi;
        }
        else
        {
            var cu = deThi.TongDiem;
            var moi = await _deThiService.DatTongDiemTheoCauAsync(id);
            if (moi == null)
                TempData["error"] = $"Đề {deThi.MaDeThi} chưa có câu hỏi nào";
            else
                TempData["success"] = $"Đề {deThi.MaDeThi}: tổng điểm đề {cu} → {moi} (bằng tổng điểm các câu) ✓";
        }
        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction("KiemTraTongDiem");
    }

    // POST: /Admin/DeThi/CoGianDiemCau/5 — co giãn điểm các câu theo tỷ lệ để tổng = tổng điểm đề
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CoGianDiemCau(int id, string? returnUrl)
    {
        var deThi = await _deThiService.GetDeThiByIdAsync(id);
        if (deThi == null)
        {
            return NotFound();
        }
        if (await LoiSuaDiemAsync(deThi) is { } loi)
        {
            TempData["error"] = loi;
        }
        else if (!await _deThiService.CoGianDiemCauAsync(id))
        {
            TempData["error"] = $"Đề {deThi.MaDeThi} chưa có câu hỏi nào";
        }
        else
        {
            var diem = (await _deThiService.GetCauHoiDeThisAsync(id)).Select(c => c.Diem).ToList();
            TempData["success"] = $"Đề {deThi.MaDeThi}: đã co giãn điểm {diem.Count} câu thành {string.Join(" / ", diem)} " +
                                  $"(tổng {Math.Round(diem.Sum(), 2)}/{deThi.TongDiem}) ✓";
        }
        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl!) : RedirectToAction("KiemTraTongDiem");
    }

    private async Task<string?> LoiSuaDiemAsync(DeThi deThi)
    {
        if (!await CoQuyenSuaAsync(deThi))
            return "Bạn chỉ được sửa điểm đề thi do mình tạo";
        if (deThi.DaPhatHanh)
            return $"Đề {deThi.MaDeThi} đã phát hành nên không sửa điểm được. Hãy hủy phát hành trước.";
        return null;
    }

    // POST: /Admin/DeThi/PhatHanh/5 — cho phép dùng đề để tổ chức thi (đề bị khóa chỉnh sửa)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PhatHanh(int id)
    {
        var deThi = await _deThiService.GetDeThiByIdAsync(id);
        if (deThi == null)
        {
            return NotFound();
        }
        if (!await CoQuyenSuaAsync(deThi))
        {
            TempData["error"] = "Bạn chỉ được phát hành đề thi do mình tạo";
            return RedirectToAction("Details", new { id });
        }
        if (deThi.DaPhatHanh)
        {
            TempData["error"] = $"Đề {deThi.MaDeThi} đã được phát hành trước đó";
            return RedirectToAction("Details", new { id });
        }

        var vanDe = await _deThiService.KiemTraPhatHanhAsync(id);
        if (vanDe.Count > 0)
        {
            TempData["error"] = $"Chưa phát hành được đề {deThi.MaDeThi}: {vanDe.Count} vấn đề cần sửa (xem danh sách trên trang)";
            return RedirectToAction("Details", new { id });
        }

        await _deThiService.DatPhatHanhAsync(id, true);
        TempData["success"] = $"Đã phát hành đề {deThi.MaDeThi}. Đề có thể dùng để tổ chức thi và đã bị khóa chỉnh sửa.";
        return RedirectToAction("Details", new { id });
    }

    // POST: /Admin/DeThi/HuyPhatHanh/5 — đưa đề về nháp để chỉnh sửa
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> HuyPhatHanh(int id)
    {
        var deThi = await _deThiService.GetDeThiByIdAsync(id);
        if (deThi == null)
        {
            return NotFound();
        }
        if (!await CoQuyenSuaAsync(deThi))
        {
            TempData["error"] = "Bạn chỉ được hủy phát hành đề thi do mình tạo";
            return RedirectToAction("Details", new { id });
        }
        if (!deThi.DaPhatHanh)
        {
            TempData["error"] = $"Đề {deThi.MaDeThi} chưa được phát hành";
            return RedirectToAction("Details", new { id });
        }
        // Đề đang được dùng trong lịch thi thì không hủy phát hành được
        var soLichThi = await _lichThiService.CountLichThiTheoDeAsync(id);
        if (soLichThi > 0)
        {
            TempData["error"] = $"Đề {deThi.MaDeThi} đang được dùng trong {soLichThi} lịch thi nên không hủy phát hành được.";
            return RedirectToAction("Details", new { id });
        }

        await _deThiService.DatPhatHanhAsync(id, false);
        TempData["success"] = $"Đã hủy phát hành đề {deThi.MaDeThi}. Đề trở về nháp và có thể chỉnh sửa.";
        return RedirectToAction("Details", new { id });
    }

    // GET: /Admin/DeThi/XemTruoc/5 — xem đề như sinh viên sẽ thấy, làm thử, kiểm tra trước khi phát hành
    public async Task<IActionResult> XemTruoc(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }
        var deThi = await _deThiService.GetDeThiByIdAsync(id.Value);
        if (deThi == null)
        {
            return NotFound();
        }

        var vm = await TaoChiTietVMAsync(deThi);
        if (deThi.DaPhatHanh)
        {
            // Đề đã phát hành vẫn chạy lại kiểm tra để phát hiện thay đổi bất thường
            vm.VanDePhatHanh = await _deThiService.KiemTraPhatHanhAsync(deThi.Id);
        }
        vm.LuuY = TimLuuY(vm);
        return View(vm);
    }

    // Những điểm nên xem lại trước khi phát hành (không chặn phát hành)
    private static List<string> TimLuuY(DeThiChiTietVM vm)
    {
        var luuY = new List<string>();
        static string Ds(IEnumerable<CauTrongDeVM> cs) => string.Join(", ", cs.Select(c => $"câu {c.ThuTu}"));
        static string ChuanHoa(string s) =>
            string.Join(' ', s.Normalize().ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        var nhieuDung = vm.CauHois.Where(c => c.LoaiCauHoi == 1 && c.DapAns.Count(d => d.DapAnDung) > 1).ToList();
        if (nhieuDung.Count > 0)
            luuY.Add($"{Ds(nhieuDung)} có nhiều đáp án đúng — sinh viên phải chọn đủ mới được điểm (đề hiển thị \"chọn nhiều đáp án\")");

        var thieuGiaiThich = vm.CauHois.Where(c => string.IsNullOrWhiteSpace(c.GiaiThich)).ToList();
        if (thieuGiaiThich.Count > 0)
            luuY.Add($"{Ds(thieuGiaiThich)} chưa có giải thích / gợi ý chấm");

        var trungNoiDung = vm.CauHois.GroupBy(c => ChuanHoa(c.NoiDung)).Where(g => g.Count() > 1).ToList();
        foreach (var g in trungNoiDung)
            luuY.Add($"{Ds(g)} có nội dung giống nhau");

        var trungDapAn = vm.CauHois.Where(c => c.DapAns.GroupBy(d => ChuanHoa(d.NoiDung)).Any(g => g.Count() > 1)).ToList();
        if (trungDapAn.Count > 0)
            luuY.Add($"{Ds(trungDapAn)} có đáp án trùng nội dung");

        if (vm.DeThi.LoaiDeThi == 3 && vm.CauHois.Count > 0 && vm.CauHois.Select(c => c.LoaiCauHoi).Distinct().Count() == 1)
            luuY.Add($"Đề loại Kết hợp nhưng chỉ có câu {CauHoiLabel.TenLoaiCauHoi(vm.CauHois[0].LoaiCauHoi).ToLower()} — có thể đổi loại đề cho đúng");

        if (vm.CauHois.Count > 0)
        {
            var phutMoiCau = (double)vm.DeThi.ThoiLuong / vm.CauHois.Count;
            if (phutMoiCau < 0.5)
                luuY.Add($"Thời lượng {vm.DeThi.ThoiLuong} phút cho {vm.CauHois.Count} câu (≈ {Math.Round(phutMoiCau * 60)} giây/câu) có thể quá ngắn");
            else if (vm.CauHois.All(c => c.LoaiCauHoi == 1) && phutMoiCau > 10)
                luuY.Add($"Thời lượng {vm.DeThi.ThoiLuong} phút cho {vm.CauHois.Count} câu trắc nghiệm (≈ {Math.Round(phutMoiCau, 1)} phút/câu) có thể quá dài");
        }
        return luuY;
    }

    // Dữ liệu đề theo đúng thứ tự câu và thứ tự đáp án của đề (dùng cho Chi tiết và Xem trước)
    private async Task<DeThiChiTietVM> TaoChiTietVMAsync(DeThi deThi)
    {
        var cauTrongDe = await _deThiService.GetCauHoiDeThisAsync(deThi.Id);
        var thongTin = (await _deThiService.GetCauHoiTheoIdsAsync(cauTrongDe.Select(c => c.CauHoiId))).ToDictionary(c => c.Id);
        var dapAnTheoCau = await _cauHoiService.GetDapAnsTheoCauHoiAsync(cauTrongDe.Select(c => c.CauHoiId));
        var monHoc = (await _danhMucCauHoiService.GetAllMonHocAsync()).FirstOrDefault(m => m.Id == deThi.MonHocId);

        var vm = new DeThiChiTietVM
        {
            DeThi = deThi,
            TenMonHoc = monHoc?.TenMonHoc ?? "",
            TenGiaoVien = TenGiaoVien(deThi) ?? "",
            CoQuyenSua = await CoQuyenSuaAsync(deThi),
            VanDePhatHanh = deThi.DaPhatHanh ? new() : await _deThiService.KiemTraPhatHanhAsync(deThi.Id),
            CauHois = cauTrongDe.Select(c =>
            {
                thongTin.TryGetValue(c.CauHoiId, out var ch);
                var goc = dapAnTheoCau[c.CauHoiId]; // đã sắp theo ThuTu gốc
                var theoDe = ThuTuDapAnHelper.SapXep(goc, c.ThuTuDapAn);
                return new CauTrongDeVM
                {
                    Id = c.Id,
                    ThuTu = c.ThuTu,
                    CauHoiId = c.CauHoiId,
                    NoiDung = ch?.NoiDung ?? "",
                    LoaiCauHoi = ch?.LoaiCauHoi ?? 0,
                    MucDo = ch?.MucDo ?? 0,
                    Diem = c.Diem,
                    CoHinhAnh = ch?.CoHinhAnh ?? false,
                    GiaiThich = ch?.GiaiThich ?? "",
                    DapAns = theoDe,
                    NhanGoc = goc.Select((d, i) => (d, i)).ToDictionary(x => x.d.Id, x => ThuTuDapAnHelper.Nhan(x.i)),
                    DaTronDapAn = !theoDe.Select(d => d.Id).SequenceEqual(goc.Select(d => d.Id))
                };
            }).ToList()
        };
        return vm;
    }

    // POST: /Admin/DeThi/TronDapAn/5 — trộn lại thứ tự đáp án các câu trắc nghiệm của chính đề này
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TronDapAn(int id)
    {
        var deThi = await _deThiService.GetDeThiByIdAsync(id);
        if (deThi == null)
        {
            return NotFound();
        }
        if (!await CoQuyenSuaAsync(deThi))
        {
            TempData["error"] = "Bạn chỉ được trộn đáp án của đề thi do mình tạo";
            return RedirectToAction("Details", new { id });
        }
        if (deThi.DaPhatHanh)
        {
            TempData["error"] = $"Đề {deThi.MaDeThi} đã phát hành nên không trộn lại đáp án được. Hãy hủy phát hành trước.";
            return RedirectToAction("Details", new { id });
        }

        var soCau = await _deThiService.TronDapAnAsync(id);
        if (soCau == 0)
        {
            TempData["error"] = "Đề không có câu trắc nghiệm nào có từ 2 đáp án trở lên để trộn";
        }
        else
        {
            TempData["success"] = $"Đã trộn thứ tự đáp án của {soCau} câu trắc nghiệm trong đề {deThi.MaDeThi}";
        }
        return RedirectToAction("Details", new { id });
    }

    #region  TRỘN ĐỀ

    // GET: /Admin/DeThi/TronDe/5
    public async Task<IActionResult> TronDe(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }
        var goc = await _deThiService.GetDeThiByIdAsync(id.Value);
        if (goc == null)
        {
            return NotFound();
        }
        if (!await CoQuyenSuaAsync(goc))
        {
            TempData["error"] = "Bạn chỉ được trộn đề thi do mình tạo";
            return RedirectToAction("Index");
        }

        var vm = new TronDeVM { DeThiGocId = goc.Id, TienToMa = TienToMacDinh(goc.MaDeThi) };
        await LoadTronDeInfo(goc);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TronDe(TronDeVM vm)
    {
        var goc = await _deThiService.GetDeThiByIdAsync(vm.DeThiGocId);
        if (goc == null)
        {
            return NotFound();
        }
        if (!await CoQuyenSuaAsync(goc))
        {
            TempData["error"] = "Bạn chỉ được trộn đề thi do mình tạo";
            return RedirectToAction("Index");
        }

        vm.TienToMa = vm.TienToMa?.Trim() ?? string.Empty;
        var cauGoc = await _deThiService.GetCauHoiDeThisAsync(goc.Id);

        // Số cách xáo khác thứ tự gốc = n! - 1
        if (cauGoc.Count < 2)
        {
            ModelState.AddModelError(string.Empty, "Đề cần ít nhất 2 câu hỏi mới trộn được thứ tự");
        }
        else
        {
            var soCachTron = GiaiThua(cauGoc.Count) - 1;
            if (vm.SoDe > soCachTron)
            {
                ModelState.AddModelError(nameof(vm.SoDe),
                    $"Đề có {cauGoc.Count} câu chỉ có {soCachTron} cách xáo khác thứ tự gốc, không tạo được {vm.SoDe} đề khác nhau");
            }
        }
        if (!ModelState.IsValid)
        {
            await LoadTronDeInfo(goc);
            return View(vm);
        }

        // Mã đề mới: tiền tố + số, bỏ qua các mã đã có
        var maDaCo = await _deThiService.GetAllMaDeThiAsync();
        var maMoi = new List<string>();
        for (var so = 1; maMoi.Count < vm.SoDe; so++)
        {
            var ma = vm.TienToMa + so;
            if (!maDaCo.Contains(ma.ToLowerInvariant()))
            {
                maMoi.Add(ma);
            }
        }
        if (maMoi.Any(m => m.Length > 50))
        {
            ModelState.AddModelError(nameof(vm.TienToMa), "Mã đề sinh ra dài quá 50 ký tự, hãy rút ngắn tiền tố");
            await LoadTronDeInfo(goc);
            return View(vm);
        }

        // Đáp án của các câu (để trộn thứ tự đáp án nếu được chọn); chỉ câu trắc nghiệm có đáp án
        var dapAnTheoCau = await _cauHoiService.GetDapAnsTheoCauHoiAsync(cauGoc.Select(c => c.CauHoiId));

        // Xáo thứ tự câu (Fisher–Yates): khác thứ tự gốc và khác các đề đã tạo
        var daDung = new HashSet<string> { string.Join(",", cauGoc.Select(c => c.CauHoiId)) };
        var deMoi = new List<(DeThi DeThi, List<CauHoiDeThi> CauHois)>();
        for (var i = 0; i < vm.SoDe; i++)
        {
            List<CauHoiDeThi> tron;
            do
            {
                tron = cauGoc.ToList();
                for (var k = tron.Count - 1; k > 0; k--)
                {
                    var j = Random.Shared.Next(k + 1);
                    (tron[k], tron[j]) = (tron[j], tron[k]);
                }
            } while (!daDung.Add(string.Join(",", tron.Select(c => c.CauHoiId))));

            deMoi.Add((new DeThi
            {
                MaDeThi = maMoi[i],
                TenDeThi = $"{goc.TenDeThi} (mã đề {maMoi[i]})",
                MonHocId = goc.MonHocId,
                GiaoVienId = goc.GiaoVienId,
                ThoiLuong = goc.ThoiLuong,
                TongDiem = goc.TongDiem,
                LoaiDeThi = goc.LoaiDeThi
            }, tron.Select(c => new CauHoiDeThi
            {
                CauHoiId = c.CauHoiId,
                Diem = c.Diem,
                // Trộn đáp án: thứ tự mới khác thứ tự của đề gốc; không trộn: giữ thứ tự của đề gốc
                ThuTuDapAn = vm.TronDapAn
                    ? ThuTuDapAnHelper.Tron(ThuTuDapAnHelper.SapXep(dapAnTheoCau[c.CauHoiId], c.ThuTuDapAn)) ?? c.ThuTuDapAn
                    : c.ThuTuDapAn
            }).ToList()));
        }

        try
        {
            await _deThiService.ImportDeThiAsync(deMoi);
        }
        catch (DbUpdateException)
        {
            // Có người vừa tạo đề trùng mã cùng lúc (unique index chặn lại)
            ModelState.AddModelError(string.Empty, "Mã đề vừa bị trùng do có người khác tạo cùng lúc, vui lòng thử lại");
            await LoadTronDeInfo(goc);
            return View(vm);
        }

        TempData["success"] = $"Đã trộn đề {goc.MaDeThi} thành {vm.SoDe} đề" +
                              (vm.TronDapAn ? " (trộn cả câu hỏi và đáp án)" : " (trộn thứ tự câu hỏi)") +
                              $": {string.Join(", ", maMoi)}";
        return RedirectToAction("Index");
    }

    // Mã gốc "CSDL-GK-01" -> tiền tố "CSDL-GK-01-T" (cắt bớt để mã mới không quá 50 ký tự)
    private static string TienToMacDinh(string maGoc)
    {
        var tienTo = maGoc + "-T";
        return tienTo.Length <= 45 ? tienTo : tienTo[^45..];
    }

    private static long GiaiThua(int n)
    {
        long kq = 1;
        for (var i = 2; i <= n && kq < 1_000_000; i++) // chỉ cần so với số đề tối đa
        {
            kq *= i;
        }
        return kq;
    }

    // Thông tin đề gốc + câu hỏi theo thứ tự để hiển thị trên trang Trộn đề
    private async Task LoadTronDeInfo(DeThi goc)
    {
        var cauGoc = await _deThiService.GetCauHoiDeThisAsync(goc.Id);
        var thongTin = (await _deThiService.GetCauHoiTheoIdsAsync(cauGoc.Select(c => c.CauHoiId))).ToDictionary(c => c.Id);
        ViewData["DeGoc"] = goc;
        ViewData["CauGoc"] = cauGoc.Select(c => (
            c.ThuTu, c.CauHoiId, c.Diem,
            NoiDung: thongTin.TryGetValue(c.CauHoiId, out var ch) ? ch.NoiDung : "",
            Loai: thongTin.TryGetValue(c.CauHoiId, out var ch2) ? ch2.LoaiCauHoi : 0
        )).ToList();
    }

    #endregion

    #region  NHẬP EXCEL

    public async Task<IActionResult> Import()
    {
        await LoadGiaoVienChoImport(null);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Import(IFormFile? file, int? giaoVienId)
    {
        var gvId = await XacDinhGiaoVienAsync(giaoVienId);
        if (file == null || file.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Vui lòng chọn file Excel");
        }
        else if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, "Chỉ hỗ trợ file .xlsx");
        }
        if (!ModelState.IsValid || gvId == null)
        {
            await LoadGiaoVienChoImport(giaoVienId);
            return View();
        }

        var errors = new List<string>();
        // Đề đọc từ sheet DeThi (theo mã đề chuẩn hóa) và câu của từng đề từ sheet CauHoiDeThi
        var deDoc = new Dictionary<string, (int Dong, DeThi DeThi)>();
        var cauDoc = new List<(int Dong, string MaDe, string MaGoc, int CauHoiId, double? Diem)>();
        var monHocs = (await _danhMucCauHoiService.GetAllMonHocAsync()).ToList();

        try
        {
            using var stream = file!.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var sheetDe = workbook.Worksheets.FirstOrDefault(w => ChuanHoa(w.Name) == "dethi") ?? workbook.Worksheet(1);
            var sheetCau = workbook.Worksheets.FirstOrDefault(w => ChuanHoa(w.Name) == "cauhoidethi")
                           ?? (workbook.Worksheets.Count >= 2 ? workbook.Worksheet(2) : null);

            // ----- Sheet DeThi -----
            int cMa = 1, cTen = 2, cMon = 3, cLoai = 4, cThoiGian = 5, cTong = 6;
            for (var c = 1; c <= (sheetDe.Row(1).LastCellUsed()?.Address.ColumnNumber ?? 0); c++)
            {
                var h = ChuanHoa(sheetDe.Cell(1, c).GetFormattedString());
                if (h.Contains("mã đề")) cMa = c;
                else if (h.Contains("tên")) cTen = c;
                else if (h.Contains("môn")) cMon = c;
                else if (h.Contains("loại")) cLoai = c;
                else if (h.Contains("thời")) cThoiGian = c;
                else if (h.Contains("điểm")) cTong = c;
            }
            for (var r = 2; r <= (sheetDe.LastRowUsed()?.RowNumber() ?? 0); r++)
            {
                string O(int c) => sheetDe.Cell(r, c).GetFormattedString().Trim();
                var ma = O(cMa); var ten = O(cTen); var maMon = O(cMon);
                if (ma == "" && ten == "" && maMon == "")
                {
                    continue;
                }
                var loi = new List<string>();
                if (ma == "") loi.Add("thiếu mã đề");
                else if (ma.Length > 50) loi.Add("mã đề tối đa 50 ký tự");
                if (ten == "") loi.Add("thiếu tên đề");
                var mon = monHocs.FirstOrDefault(m => m.MaMonHoc.Equals(maMon, StringComparison.OrdinalIgnoreCase));
                if (maMon == "") loi.Add("thiếu mã môn học");
                else if (mon == null) loi.Add($"không tìm thấy môn học có mã \"{maMon}\"");
                var loai = DocLoaiDe(O(cLoai));
                if (loai == null) loi.Add($"loại đề \"{O(cLoai)}\" không hợp lệ (Trắc nghiệm / Tự luận / Kết hợp)");
                var tgText = O(cThoiGian);
                var thoiGian = 45; // để trống -> 45 phút
                if (tgText != "" && (!int.TryParse(tgText, out thoiGian) || thoiGian < 1 || thoiGian > 600))
                    loi.Add($"thời lượng \"{tgText}\" không hợp lệ (1–600 phút)");
                var tong = DocSo(O(cTong)) ?? 10; // để trống -> 10 điểm
                if (O(cTong) != "" && (DocSo(O(cTong)) == null || tong <= 0))
                    loi.Add($"tổng điểm \"{O(cTong)}\" không hợp lệ (số > 0)");

                var khoa = ma.ToLowerInvariant();
                if (ma != "" && deDoc.TryGetValue(khoa, out var truoc))
                    loi.Add($"trùng mã đề với dòng {truoc.Dong}");

                if (loi.Count > 0)
                {
                    errors.AddRange(loi.Select(l => $"Sheet DeThi, dòng {r}: {l}"));
                    continue;
                }
                deDoc[khoa] = (r, new DeThi
                {
                    MaDeThi = ma, TenDeThi = ten, MonHocId = mon!.Id, GiaoVienId = gvId.Value,
                    LoaiDeThi = loai!.Value, ThoiLuong = thoiGian, TongDiem = tong
                });
            }

            // ----- Sheet CauHoiDeThi (không bắt buộc: không có thì các đề là đề nháp) -----
            if (sheetCau != null)
            {
                int qMa = 1, qCau = 2, qDiem = 3;
                for (var c = 1; c <= (sheetCau.Row(1).LastCellUsed()?.Address.ColumnNumber ?? 0); c++)
                {
                    var h = ChuanHoa(sheetCau.Cell(1, c).GetFormattedString());
                    if (h.Contains("mã đề")) qMa = c;
                    else if (h.Contains("câu hỏi")) qCau = c;
                    else if (h.Contains("điểm")) qDiem = c;
                }
                for (var r = 2; r <= (sheetCau.LastRowUsed()?.RowNumber() ?? 0); r++)
                {
                    string O(int c) => sheetCau.Cell(r, c).GetFormattedString().Trim();
                    var ma = O(qMa); var cauText = O(qCau).TrimStart('#'); var diemText = O(qDiem);
                    if (ma == "" && cauText == "" && diemText == "")
                    {
                        continue;
                    }
                    var ok = true;
                    if (ma == "") { errors.Add($"Sheet CauHoiDeThi, dòng {r}: thiếu mã đề"); ok = false; }
                    if (!int.TryParse(cauText, out var cauId) || cauId <= 0)
                    {
                        errors.Add($"Sheet CauHoiDeThi, dòng {r}: mã câu hỏi \"{O(qCau)}\" không hợp lệ (vd: 9 hoặc #9)"); ok = false;
                    }
                    var diem = DocSo(diemText);
                    if (diemText != "" && (diem == null || diem <= 0))
                    {
                        errors.Add($"Sheet CauHoiDeThi, dòng {r}: điểm \"{diemText}\" không hợp lệ (số > 0, để trống = điểm mặc định)"); ok = false;
                    }
                    if (ok)
                    {
                        cauDoc.Add((r, ma.ToLowerInvariant(), ma, cauId, diem));
                    }
                }
            }
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, "Không đọc được file Excel. Vui lòng kiểm tra lại định dạng file.");
            await LoadGiaoVienChoImport(giaoVienId);
            return View();
        }

        // Câu hỏi phải gắn với một đề có trong sheet DeThi
        foreach (var g in cauDoc.Where(c => !deDoc.ContainsKey(c.MaDe)).GroupBy(c => c.MaDe))
        {
            errors.Add($"Sheet CauHoiDeThi, dòng {string.Join(", ", g.Select(x => x.Dong))}: " +
                       $"mã đề \"{g.First().MaGoc}\" không có trong sheet DeThi");
        }

        // Kiểm tra câu của từng đề (giống form Thêm đề thi)
        var thongTinCau = (await _deThiService.GetCauHoiTheoIdsAsync(cauDoc.Select(c => c.CauHoiId)))
            .ToDictionary(c => c.Id);
        var maDaCo = await _deThiService.GetAllMaDeThiAsync();
        var themMoi = new List<(DeThi DeThi, List<CauHoiDeThi> CauHois)>();
        var boQua = new List<string>();
        var lechDiem = new List<string>();

        foreach (var (khoa, (dong, deThi)) in deDoc)
        {
            var cacCau = cauDoc.Where(c => c.MaDe == khoa).ToList();
            var dsCau = new List<CauHoiDeThi>();
            var loiDe = new List<string>();
            var daGap = new Dictionary<int, int>();

            foreach (var c in cacCau)
            {
                if (daGap.TryGetValue(c.CauHoiId, out var dongTruoc))
                {
                    loiDe.Add($"dòng {c.Dong}: câu #{c.CauHoiId} trùng với dòng {dongTruoc}");
                    continue;
                }
                daGap[c.CauHoiId] = c.Dong;
                if (!thongTinCau.TryGetValue(c.CauHoiId, out var ch))
                {
                    loiDe.Add($"dòng {c.Dong}: câu #{c.CauHoiId} không tồn tại");
                    continue;
                }
                if (ch.MonHocId != deThi.MonHocId)
                    loiDe.Add($"dòng {c.Dong}: câu #{c.CauHoiId} không thuộc môn học của đề");
                else if (!LoaiCauPhuHop(deThi.LoaiDeThi, ch.LoaiCauHoi))
                    loiDe.Add($"dòng {c.Dong}: câu #{c.CauHoiId} là câu {CauHoiLabel.TenLoaiCauHoi(ch.LoaiCauHoi).ToLower()}, " +
                              $"không phù hợp đề {DeThiLabel.TenLoaiDeThi(deThi.LoaiDeThi).ToLower()}");
                else if (ch.LoaiCauHoi == 1 && ch.SoDapAn < 2)
                    loiDe.Add($"dòng {c.Dong}: câu trắc nghiệm #{c.CauHoiId} chưa đủ 2 đáp án");
                else
                    dsCau.Add(new CauHoiDeThi { CauHoiId = c.CauHoiId, Diem = Math.Round(c.Diem ?? ch.DiemMacDinh, 2) });
            }

            if (loiDe.Count > 0)
            {
                errors.AddRange(loiDe.Select(l => $"Đề {deThi.MaDeThi} (sheet CauHoiDeThi, {l})"));
                continue;
            }
            if (maDaCo.Contains(khoa))
            {
                boQua.Add(deThi.MaDeThi); // mã đề đã có trong hệ thống -> bỏ qua
                continue;
            }
            var tongCau = Math.Round(dsCau.Sum(c => c.Diem), 2);
            if (Math.Abs(tongCau - deThi.TongDiem) >= 0.001)
            {
                lechDiem.Add($"{deThi.MaDeThi} ({tongCau}/{deThi.TongDiem})");
            }
            themMoi.Add((deThi, dsCau));
        }

        if (errors.Count > 0)
        {
            ViewBag.Errors = errors;
            await LoadGiaoVienChoImport(giaoVienId);
            return View();
        }
        if (themMoi.Count == 0)
        {
            ModelState.AddModelError(string.Empty, boQua.Count > 0
                ? $"Tất cả {boQua.Count} đề trong file đã có trong hệ thống (trùng mã đề), không có đề nào mới"
                : "File không có đề thi nào");
            await LoadGiaoVienChoImport(giaoVienId);
            return View();
        }

        var added = await _deThiService.ImportDeThiAsync(themMoi);
        var tb = $"Nhập Excel thành công: thêm {added} đề thi ({themMoi.Sum(d => d.CauHois.Count)} câu)";
        if (boQua.Count > 0) tb += $", bỏ qua {boQua.Count} đề đã có ({string.Join(", ", boQua)})";
        if (lechDiem.Count > 0) tb += $". Lưu ý: tổng điểm các câu chưa khớp tổng điểm đề ở: {string.Join(", ", lechDiem)}";
        TempData["success"] = tb;
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> DownloadTemplate()
    {
        var monHocs = (await _danhMucCauHoiService.GetAllMonHocAsync()).ToList();
        var tenMon = monHocs.ToDictionary(m => m.Id, m => m.MaMonHoc);
        var cauHois = await _deThiService.GetCauHoiTheoIdsAsync(
            (await _cauHoiService.GetAllCauHoiAsync()).Select(c => c.Id));

        using var workbook = new XLWorkbook();

        // Sheet 1: thông tin đề
        var de = workbook.Worksheets.Add("DeThi");
        string[] hDe = { "Mã đề thi", "Tên đề thi", "Mã môn học", "Loại đề", "Thời lượng (phút)", "Tổng điểm" };
        for (var i = 0; i < hDe.Length; i++) de.Cell(1, i + 1).Value = hDe[i];
        var cauMau = cauHois.Where(c => c.LoaiCauHoi == 1 && c.SoDapAn >= 2).Take(2).ToList();
        var monMau = cauMau.Count > 0 ? tenMon.GetValueOrDefault(cauMau[0].MonHocId, "") : monHocs.FirstOrDefault()?.MaMonHoc ?? "";
        object[] dongDe = { "MAU-01", "Đề thi mẫu (xóa dòng này trước khi nhập)", monMau, "Trắc nghiệm", 30, 10 };
        for (var i = 0; i < dongDe.Length; i++) de.Cell(2, i + 1).Value = XLCellValue.FromObject(dongDe[i]);
        de.Range("D2:D1000").CreateDataValidation().List("\"Trắc nghiệm,Tự luận,Kết hợp\"");
        de.Column(1).Style.NumberFormat.Format = "@";
        de.Column(3).Style.NumberFormat.Format = "@";
        StyleHeader(de.Range(1, 1, 1, hDe.Length));
        de.Columns().AdjustToContents();

        // Sheet 2: câu hỏi của từng đề (thứ tự dòng = thứ tự câu trong đề)
        var cau = workbook.Worksheets.Add("CauHoiDeThi");
        string[] hCau = { "Mã đề thi", "Mã câu hỏi", "Điểm" };
        for (var i = 0; i < hCau.Length; i++) cau.Cell(1, i + 1).Value = hCau[i];
        for (var i = 0; i < cauMau.Count; i++)
        {
            cau.Cell(i + 2, 1).Value = "MAU-01";
            cau.Cell(i + 2, 2).Value = cauMau[i].Id;
            cau.Cell(i + 2, 3).Value = 5;
        }
        cau.Column(1).Style.NumberFormat.Format = "@";
        StyleHeader(cau.Range(1, 1, 1, hCau.Length));
        cau.Columns().AdjustToContents();

        // Sheet 3: tham khảo câu hỏi trong ngân hàng
        var refCau = workbook.Worksheets.Add("DanhSachCauHoi");
        string[] hRef = { "Mã câu hỏi", "Mã môn học", "Loại câu", "Mức độ", "Điểm mặc định", "Số đáp án", "Nội dung" };
        for (var i = 0; i < hRef.Length; i++) refCau.Cell(1, i + 1).Value = hRef[i];
        var r = 2;
        foreach (var c in cauHois.OrderBy(c => tenMon.GetValueOrDefault(c.MonHocId)).ThenBy(c => c.Id))
        {
            refCau.Cell(r, 1).Value = c.Id;
            refCau.Cell(r, 2).Value = tenMon.GetValueOrDefault(c.MonHocId, "");
            refCau.Cell(r, 3).Value = CauHoiLabel.TenLoaiCauHoi(c.LoaiCauHoi);
            refCau.Cell(r, 4).Value = CauHoiLabel.TenMucDo(c.MucDo);
            refCau.Cell(r, 5).Value = c.DiemMacDinh;
            refCau.Cell(r, 6).Value = c.SoDapAn;
            refCau.Cell(r, 7).Value = c.NoiDung;
            r++;
        }
        StyleHeader(refCau.Range(1, 1, 1, hRef.Length));
        refCau.Columns().AdjustToContents();
        refCau.Column(7).Width = 80;

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "MauNhapDeThi.xlsx");
    }

    private static void StyleHeader(IXLRange header)
    {
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.LightBlue;
    }

    // Chuẩn hóa chuỗi để so sánh: Unicode NFC, chữ thường, gộp khoảng trắng
    private static string ChuanHoa(string s) =>
        string.Join(' ', s.Normalize().ToLowerInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static int? DocLoaiDe(string s) => ChuanHoa(s) switch
    {
        "1" or "trắc nghiệm" or "tn" => 1,
        "2" or "tự luận" or "tl" => 2,
        "3" or "kết hợp" or "kh" => 3,
        _ => null
    };

    // Đọc số chấp nhận cả dấu phẩy và dấu chấm thập phân; chuỗi rỗng/sai -> null
    private static double? DocSo(string s) =>
        double.TryParse(s.Replace(',', '.'), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var x) ? x : null;

    private async Task LoadGiaoVienChoImport(int? giaoVienId)
    {
        var giaoVien = await GiaoVienHienTaiAsync();
        ViewData["GiaoVienHienTai"] = giaoVien;
        if (giaoVien == null)
        {
            var giaoViens = await _cauHoiService.GetAllGiaoVienAsync();
            ViewData["GiaoVienList"] = new SelectList(
                giaoViens.Select(g => new { g.Id, Ten = $"{g.HoTen} ({g.MaGiaoVien})" }), "Id", "Ten", giaoVienId);
        }
    }

    #endregion

    #region  CALL API

    public async Task<IActionResult> GetAllDeThi()
    {
        var deThis = await _deThiService.GetAllDeThiAsync();
        var data = deThis.Select(d => new
        {
            d.Id,
            d.MaDeThi,
            d.TenDeThi,
            d.TenMonHoc,
            d.LoaiDeThi,
            TenLoaiDeThi = DeThiLabel.TenLoaiDeThi(d.LoaiDeThi),
            d.ThoiLuong,
            d.TongDiem,
            d.SoCauHoi,
            TongDiemCauHoi = Math.Round(d.TongDiemCauHoi, 2),
            d.TenGiaoVien,
            d.DaPhatHanh,
            TrangThai = d.DaPhatHanh ? "Đã phát hành" : "Nháp",
            TinhTrangDiem = d.SoCauHoi == 0 ? "Chưa có câu"
                          : Math.Abs(d.TongDiemCauHoi - d.TongDiem) < 0.001 ? "Khớp" : "Lệch",
            NgayPhatHanh = d.NgayPhatHanh?.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
        });
        return Json(new { data });
    }

    [HttpDelete]
    [ValidateAntiForgeryToken] // token gửi qua header "RequestVerificationToken" từ dethi.js
    public async Task<IActionResult> DeleteDeThi(int? id)
    {
        if (id == null || id == 0)
        {
            return Json(new { success = false, message = "id không hợp lệ" });
        }

        var deThi = await _deThiService.GetDeThiByIdAsync(id.Value);
        if (deThi == null)
        {
            return Json(new { success = false, message = "Không tìm thấy đề thi cần xóa" });
        }
        if (!await CoQuyenSuaAsync(deThi))
        {
            return Json(new { success = false, message = "Bạn chỉ được xóa đề thi do mình tạo" });
        }
        if (deThi.DaPhatHanh)
        {
            return Json(new { success = false, message = $"Đề {deThi.MaDeThi} đã phát hành nên không xóa được. Hãy hủy phát hành trước." });
        }
        if (await _lichThiService.CountLichThiTheoDeAsync(deThi.Id) is var soLich and > 0)
        {
            return Json(new { success = false, message = $"Đề {deThi.MaDeThi} đang được dùng trong {soLich} lịch thi nên không xóa được." });
        }

        await _deThiService.DeleteDeThiAsync(deThi.Id);
        return Json(new { success = true, message = $"Đã xóa đề thi {deThi.MaDeThi}" });
    }

    // Câu hỏi của một môn học để chọn vào đề (form Thêm đề thi)
    public async Task<IActionResult> GetCauHoiByMonHoc(int monHocId)
    {
        var cauHois = await _deThiService.GetCauHoiTheoMonHocAsync(monHocId);
        var nodes = await _danhMucCauHoiService.GetDanhMucTreeAsync();
        var cay = nodes.ToDictionary(n => n.DanhMuc.Id);
        var data = cauHois.Select(c => new
        {
            c.Id,
            c.NoiDung,
            DanhMuc = cay.TryGetValue(c.DanhMucCauHoiId, out var n) ? n.DuongDanDayDu : "",
            DanhMucLoc = cay.TryGetValue(c.DanhMucCauHoiId, out var n2) ? n2.KhoaLoc : "",
            c.LoaiCauHoi,
            TenLoaiCauHoi = CauHoiLabel.TenLoaiCauHoi(c.LoaiCauHoi),
            c.MucDo,
            TenMucDo = CauHoiLabel.TenMucDo(c.MucDo),
            c.DiemMacDinh,
            c.CoHinhAnh,
            c.SoDapAn
        });
        // Danh mục của môn (dạng cây) cho bộ lọc
        var danhMucs = nodes.Where(x => x.DanhMuc.MonHocId == monHocId)
            .Select(x => new { x.DanhMuc.Id, Ten = x.DanhMuc.TenDanhMuc, x.Cap });
        return Json(new { data, danhMucs });
    }

    #endregion
}
