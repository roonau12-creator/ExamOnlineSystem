using ExamOnlineSystem.Areas.Admin.Models;
using ExamOnlineSystem.Business.Service.IService;
using ExamOnlineSystem.Models;
using ExamOnlineSystem.Models.ViewModels;
using ExamOnlineSystem.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ExamOnlineSystem.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Teacher)]
public class LichThiController : Controller
{
    // Cho phép bắt đầu lệch về quá khứ tối đa 5 phút (thời gian điền form)
    private static readonly TimeSpan DungSaiQuaKhu = TimeSpan.FromMinutes(5);

    private readonly ILichThiService _lichThiService;
    private readonly IDeThiService _deThiService;
    private readonly ICauHoiService _cauHoiService;
    private readonly ILopService _lopService;
    private readonly UserManager<ApplicationUser> _userManager;

    public LichThiController(ILichThiService lichThiService, IDeThiService deThiService,
        ICauHoiService cauHoiService, ILopService lopService, UserManager<ApplicationUser> userManager)
    {
        _lichThiService = lichThiService;
        _deThiService = deThiService;
        _cauHoiService = cauHoiService;
        _lopService = lopService;
        _userManager = userManager;
    }

    // GET: /Admin/LichThi
    public IActionResult Index()
    {
        return View();
    }

    // GET: /Admin/LichThi/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }
        var lt = await _lichThiService.GetLichThiByIdAsync(id.Value);
        if (lt == null)
        {
            return NotFound();
        }

        var bayGio = DateTime.UtcNow;
        var trangThai = TrangThaiLichThi.Tinh(lt.ThoiGianBatDau, lt.ThoiGianKetThuc, bayGio);
        // Bản ghi danh sách đã có sẵn tên môn, tên giảng viên (không phụ thuộc Include của GetLichThiByIdAsync)
        var item = (await _lichThiService.GetAllLichThiAsync()).FirstOrDefault(x => x.Id == lt.Id);

        // Quyền thao tác: Admin hoặc giảng viên phụ trách
        var laNguoiPhuTrach = User.IsInRole(SD.Role_Admin);
        if (!laNguoiPhuTrach)
        {
            var gv = await GiaoVienHienTaiAsync();
            laNguoiPhuTrach = gv != null && gv.Id == lt.GiaoVienId;
        }

        // Lớp tham gia (kèm khoa, sĩ số) và danh sách sinh viên dự thi
        var lopIds = lt.LichThiLops.Select(x => x.LopId).ToHashSet();
        var cacLop = (await _lichThiService.GetLopChoLichThiAsync())
            .Where(l => lopIds.Contains(l.Id))
            .OrderBy(l => l.MaLop)
            .Select(l => new LopDuThiVM
            {
                Id = l.Id,
                MaLop = l.MaLop,
                TenLop = l.TenLop,
                TenKhoa = l.TenKhoa,
                SoSinhVien = l.SoSinhVien
            })
            .ToList();

        var dsDuThi = await _lichThiService.GetSinhVienDuThiByLichThiAsync(lt.Id);
        var sinhViens = dsDuThi
            .Where(x => x.SinhVien != null)
            .Select(x =>
            {
                var sv = x.SinhVien!;
                return new SinhVienDuThiVM
                {
                    MaSinhVien = sv.MaSinhVien,
                    HoTen = sv.HoTen,
                    NgaySinh = sv.NgaySinh,
                    GioiTinh = sv.GioiTinh,
                    MaLop = sv.Lop?.MaLop ?? string.Empty,
                    TenLop = sv.Lop?.TenLop ?? string.Empty,
                    Email = sv.Email,
                    SoDienThoai = sv.SoDienThoai,
                    TrangThai = x.TrangThai,
                    ThoiGianBatDau = x.ThoiGianBatDau,
                    ThoiGianNopBai = x.ThoiGianNopBai,
                    Diem = x.Diem
                };
            })
            .ToList();

        var vm = new LichThiChiTietVM
        {
            LichThi = lt,
            TenMonHoc = item?.TenMonHoc ?? lt.DeThi?.MonHoc?.TenMonHoc ?? string.Empty,
            TenGiaoVien = item?.TenGiaoVien ?? lt.GiaoVien?.HoTen ?? string.Empty,
            TrangThai = trangThai,
            ConLaiPhut = (int)Math.Ceiling(((bayGio < lt.ThoiGianBatDau ? lt.ThoiGianBatDau : lt.ThoiGianKetThuc) - bayGio).TotalMinutes),
            SuaDuoc = laNguoiPhuTrach && trangThai != TrangThaiLichThi.DaKetThuc,
            XoaDuoc = laNguoiPhuTrach && trangThai == TrangThaiLichThi.SapDienRa,
            Lops = cacLop,
            SinhViens = sinhViens.OrderBy(s => s.MaLop).ThenBy(s => s.MaSinhVien).ToList()
        };
        return View(vm);
    }

    // GET: /Admin/LichThi/Create?deThiId=5 (deThiId không bắt buộc: chọn sẵn đề)
    public async Task<IActionResult> Create(int? deThiId)
    {
        var vm = new LichThiFormVM();
        var cacDe = await DeDuocChonAsync();
        var de = cacDe.FirstOrDefault(d => d.Id == deThiId);
        if (de != null)
        {
            vm.DeThiId = de.Id;
            vm.TenLichThi = de.TenDeThi;
        }
        await NapDuLieuForm(cacDe);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LichThiFormVM vm)
    {
        var cacDe = await DeDuocChonAsync();
        var cacLop = await _lichThiService.GetLopChoLichThiAsync();
        var (de, batDau, ketThuc) = await ValidateAsync(vm, cacDe, cacLop, boQuaId: null);

        if (!ModelState.IsValid)
        {
            await NapDuLieuForm(cacDe, cacLop);
            return View(vm);
        }

        // Giảng viên phụ trách: tài khoản giảng viên => chính họ; Admin => giảng viên tạo đề
        var gv = await GiaoVienHienTaiAsync();
        var lichThi = await _lichThiService.CreateLichThiAsync(new LichThi
        {
            TenLichThi = vm.TenLichThi,
            DeThiId = de!.Id,
            ThoiGianBatDau = batDau!.Value,
            ThoiGianKetThuc = ketThuc!.Value,
            GiaoVienId = gv?.Id ?? de.GiaoVienId,
            GhiChu = vm.GhiChu?.Trim() ?? string.Empty,
            NgayTao = DateTime.UtcNow
        }, vm.LopIds);

        var soSv = cacLop.Where(l => vm.LopIds.Contains(l.Id)).Sum(l => l.SoSinhVien);
        TempData["success"] = $"Đã tạo lịch thi \"{lichThi.TenLichThi}\" ({batDau.Value.ToLocalTime():dd/MM/yyyy HH:mm}) " +
                              $"cho {vm.LopIds.Count} lớp, {soSv} sinh viên";
        return RedirectToAction("Index");
    }

    // GET: /Admin/LichThi/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }
        var lt = await _lichThiService.GetLichThiByIdAsync(id.Value);
        if (lt == null)
        {
            return NotFound();
        }
        if (await LoiKhongSuaDuocAsync(lt) is { } loi)
        {
            TempData["error"] = loi;
            return RedirectToAction("Index");
        }

        var vm = new LichThiFormVM
        {
            Id = lt.Id,
            TenLichThi = lt.TenLichThi,
            DeThiId = lt.DeThiId,
            ThoiGianBatDau = lt.ThoiGianBatDau.ToLocalTime(),
            ThoiGianKetThuc = lt.ThoiGianKetThuc.ToLocalTime(),
            LopIds = lt.LichThiLops.Select(x => x.LopId).ToList(),
            GhiChu = lt.GhiChu
        };
        await NapDuLieuForm(await DeDuocChonAsync(lt.DeThiId));
        ViewData["TrangThai"] = TrangThaiCua(lt);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LichThiFormVM vm)
    {
        if (id != vm.Id)
        {
            return NotFound();
        }
        var lt = await _lichThiService.GetLichThiByIdAsync(id);
        if (lt == null)
        {
            return NotFound();
        }
        if (await LoiKhongSuaDuocAsync(lt) is { } loi)
        {
            TempData["error"] = loi;
            return RedirectToAction("Index");
        }

        var trangThai = TrangThaiCua(lt);
        var cacDe = await DeDuocChonAsync(lt.DeThiId);
        var cacLop = await _lichThiService.GetLopChoLichThiAsync();
        DeThiListItemVM? de;
        DateTime? batDau, ketThuc;

        if (trangThai == TrangThaiLichThi.DangDienRa)
        {
            // Đang diễn ra: đề, giờ bắt đầu, lớp giữ nguyên theo DB (ô trên form bị khóa nên không gửi lên)
            vm.DeThiId = lt.DeThiId;
            vm.ThoiGianBatDau = lt.ThoiGianBatDau.ToLocalTime();
            vm.LopIds = lt.LichThiLops.Select(x => x.LopId).ToList();
            ModelState.Remove(nameof(vm.DeThiId));
            ModelState.Remove(nameof(vm.ThoiGianBatDau));
            ModelState.Remove(nameof(vm.LopIds));
            (de, batDau, ketThuc) = await ValidateDangDienRaAsync(vm, lt, cacDe);
        }
        else
        {
            (de, batDau, ketThuc) = await ValidateAsync(vm, cacDe, cacLop, boQuaId: id);
        }

        if (!ModelState.IsValid)
        {
            await NapDuLieuForm(cacDe, cacLop);
            ViewData["TrangThai"] = trangThai;
            return View(vm);
        }

        await _lichThiService.UpdateLichThiAsync(new LichThi
        {
            Id = id,
            TenLichThi = vm.TenLichThi,
            DeThiId = de!.Id,
            ThoiGianBatDau = batDau!.Value,
            ThoiGianKetThuc = ketThuc!.Value,
            GhiChu = vm.GhiChu?.Trim() ?? string.Empty
        }, vm.LopIds);

        var thayDoi = new List<string>();
        if (lt.DeThiId != de.Id) thayDoi.Add($"đề {lt.DeThi?.MaDeThi} → {de.MaDeThi}");
        if (lt.ThoiGianBatDau != batDau) thayDoi.Add($"bắt đầu {lt.ThoiGianBatDau.ToLocalTime():dd/MM HH:mm} → {batDau.Value.ToLocalTime():dd/MM HH:mm}");
        if (lt.ThoiGianKetThuc != ketThuc) thayDoi.Add($"kết thúc {lt.ThoiGianKetThuc.ToLocalTime():dd/MM HH:mm} → {ketThuc.Value.ToLocalTime():dd/MM HH:mm}");
        var lopCu = lt.LichThiLops.Select(x => x.LopId).ToHashSet();
        if (!lopCu.SetEquals(vm.LopIds)) thayDoi.Add($"lớp: {lopCu.Count} → {vm.LopIds.Count} lớp");
        if (lt.TenLichThi != vm.TenLichThi) thayDoi.Add("tên");
        if (lt.GhiChu != (vm.GhiChu?.Trim() ?? "")) thayDoi.Add("ghi chú");
        TempData["success"] = $"Đã cập nhật lịch thi \"{vm.TenLichThi}\"" +
                              (thayDoi.Count > 0 ? $": {string.Join(", ", thayDoi)}" : " (không có thay đổi)");
        return RedirectToAction("Index");
    }

    // ===== Kiểm tra dữ liệu =====

    // POST: /Admin/LichThi/ThietLapThoiGian - đặt ngày giờ bắt đầu / kết thúc từ trang Chi tiết (AJAX).
    // batDau, ketThuc dạng "dd/MM/yyyy HH:mm" (giờ địa phương). Lịch đang diễn ra: chỉ đổi giờ kết thúc.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ThietLapThoiGian(int id, string? batDau, string? ketThuc)
    {
        var lt = await _lichThiService.GetLichThiByIdAsync(id);
        if (lt == null)
        {
            return Json(new { success = false, errors = new[] { "Không tìm thấy lịch thi" } });
        }
        if (await LoiKhongSuaDuocAsync(lt) is { } loi)
        {
            return Json(new { success = false, errors = new[] { loi } });
        }

        var trangThai = TrangThaiCua(lt);
        var dangDienRa = trangThai == TrangThaiLichThi.DangDienRa;
        var loiNhap = new List<string>();
        var bd = DocNgayGio(dangDienRa ? lt.ThoiGianBatDau.ToLocalTime().ToString(DinhDangNgayGio) : batDau,
            "Ngày giờ bắt đầu", loiNhap);
        var kt = DocNgayGio(ketThuc, "Ngày giờ kết thúc", loiNhap);
        if (loiNhap.Count > 0)
        {
            return Json(new { success = false, errors = loiNhap });
        }

        // Giữ nguyên đề, lớp, tên, ghi chú; chỉ thay thời gian rồi chạy lại toàn bộ kiểm tra như trang Sửa
        var vm = new LichThiFormVM
        {
            Id = lt.Id,
            TenLichThi = lt.TenLichThi,
            DeThiId = lt.DeThiId,
            ThoiGianBatDau = bd,
            ThoiGianKetThuc = kt,
            LopIds = lt.LichThiLops.Select(x => x.LopId).ToList(),
            GhiChu = lt.GhiChu
        };
        ModelState.Clear();
        var cacDe = await DeDuocChonAsync(lt.DeThiId);
        var (de, batDauUtc, ketThucUtc) = dangDienRa
            ? await ValidateDangDienRaAsync(vm, lt, cacDe)
            : await ValidateAsync(vm, cacDe, await _lichThiService.GetLopChoLichThiAsync(), boQuaId: id);
        if (de == null)
        {
            ModelState.AddModelError(string.Empty, "Đề thi của lịch này không còn hợp lệ (chưa phát hành hoặc không thuộc quyền của bạn)");
        }
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).Distinct().ToList();
            return Json(new { success = false, errors });
        }

        if (lt.ThoiGianBatDau == batDauUtc && lt.ThoiGianKetThuc == ketThucUtc)
        {
            return Json(new { success = true, message = "Thời gian không thay đổi" });
        }

        await _lichThiService.UpdateLichThiAsync(new LichThi
        {
            Id = id,
            TenLichThi = lt.TenLichThi,
            DeThiId = lt.DeThiId,
            ThoiGianBatDau = batDauUtc!.Value,
            ThoiGianKetThuc = ketThucUtc!.Value,
            GhiChu = lt.GhiChu ?? string.Empty
        }, vm.LopIds);

        var moi = batDauUtc.Value.ToLocalTime();
        var moiKt = ketThucUtc.Value.ToLocalTime();
        var message = $"Đã đặt thời gian lịch thi \"{lt.TenLichThi}\": {moi:dd/MM/yyyy HH:mm} – " +
                      (moi.Date == moiKt.Date ? moiKt.ToString("HH:mm") : moiKt.ToString("dd/MM/yyyy HH:mm"));
        TempData["success"] = message;
        return Json(new { success = true, message });
    }

    private const string DinhDangNgayGio = "dd/MM/yyyy HH:mm";

    // "dd/MM/yyyy HH:mm" (24 giờ) -> DateTime giờ địa phương
    private static DateTime? DocNgayGio(string? s, string ten, List<string> loi)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            loi.Add($"Vui lòng nhập {ten.ToLower()}");
            return null;
        }
        if (DateTime.TryParseExact(s.Trim(), new[] { DinhDangNgayGio, "d/M/yyyy H:mm" },
                System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d))
        {
            return d;
        }
        loi.Add($"{ten} \"{s}\" không đúng định dạng dd/MM/yyyy HH:mm");
        return null;
    }

    // Tạo mới / sửa lịch "Sắp diễn ra": kiểm tra đầy đủ. Trả về đề + thời gian (UTC) đã chuẩn hóa.
    private async Task<(DeThiListItemVM? De, DateTime? BatDau, DateTime? KetThuc)> ValidateAsync(
        LichThiFormVM vm, List<DeThiListItemVM> cacDe, List<LopChonVM> cacLop, int? boQuaId)
    {
        vm.TenLichThi = vm.TenLichThi?.Trim() ?? string.Empty;
        vm.LopIds = vm.LopIds.Where(id => id > 0).Distinct().ToList();

        // Đề thi: đã phát hành + người dùng được chọn
        var de = cacDe.FirstOrDefault(d => d.Id == vm.DeThiId);
        if (vm.DeThiId > 0 && de == null)
        {
            ModelState.AddModelError(nameof(vm.DeThiId), "Đề thi không tồn tại, chưa phát hành hoặc không phải đề của bạn");
        }

        var (batDau, ketThuc) = DoiSangUtc(vm);
        if (batDau != null && ketThuc != null)
        {
            if (batDau < DateTime.UtcNow - DungSaiQuaKhu)
                ModelState.AddModelError(nameof(vm.ThoiGianBatDau), "Thời gian bắt đầu đã qua");
            KiemTraKhungGio(vm, de, batDau.Value, ketThuc.Value);
        }

        // Lớp: có ít nhất 1 lớp, lớp tồn tại, không trùng giờ với lịch thi khác của lớp đó
        const string keyLop = nameof(LichThiFormVM.LopIds);
        if (vm.LopIds.Count == 0)
        {
            ModelState.AddModelError(keyLop, "Vui lòng chọn ít nhất 1 lớp");
        }
        else if (vm.LopIds.Any(id => cacLop.All(l => l.Id != id)))
        {
            ModelState.AddModelError(keyLop, "Có lớp không tồn tại");
        }
        else if (batDau != null && ketThuc != null && ketThuc > batDau)
        {
            await KiemTraTrungGioAsync(vm.LopIds, batDau.Value, ketThuc.Value, boQuaId);
        }
        return (de, batDau, ketThuc);
    }

    // Sửa lịch "Đang diễn ra": chỉ đổi tên, ghi chú, giờ kết thúc (không sớm hơn hiện tại)
    private async Task<(DeThiListItemVM? De, DateTime? BatDau, DateTime? KetThuc)> ValidateDangDienRaAsync(
        LichThiFormVM vm, LichThi lt, List<DeThiListItemVM> cacDe)
    {
        vm.TenLichThi = vm.TenLichThi?.Trim() ?? string.Empty;
        var de = cacDe.FirstOrDefault(d => d.Id == lt.DeThiId);
        var batDau = lt.ThoiGianBatDau;
        var (_, ketThuc) = DoiSangUtc(vm);
        if (ketThuc != null)
        {
            if (ketThuc <= DateTime.UtcNow)
                ModelState.AddModelError(nameof(vm.ThoiGianKetThuc), "Lịch thi đang diễn ra: giờ kết thúc mới phải sau thời điểm hiện tại");
            KiemTraKhungGio(vm, de, batDau, ketThuc.Value);
            if (ketThuc > lt.ThoiGianKetThuc)
                await KiemTraTrungGioAsync(vm.LopIds, batDau, ketThuc.Value, lt.Id); // kéo dài có thể đè lịch sau
        }
        return (de, batDau, ketThuc);
    }

    // Giờ nhập là giờ địa phương (máy chủ) -> UTC
    private static (DateTime? BatDau, DateTime? KetThuc) DoiSangUtc(LichThiFormVM vm) => (
        vm.ThoiGianBatDau.HasValue ? DateTime.SpecifyKind(vm.ThoiGianBatDau.Value, DateTimeKind.Local).ToUniversalTime() : null,
        vm.ThoiGianKetThuc.HasValue ? DateTime.SpecifyKind(vm.ThoiGianKetThuc.Value, DateTimeKind.Local).ToUniversalTime() : null);

    private void KiemTraKhungGio(LichThiFormVM vm, DeThiListItemVM? de, DateTime batDau, DateTime ketThuc)
    {
        if (ketThuc <= batDau)
            ModelState.AddModelError(nameof(vm.ThoiGianKetThuc), "Thời gian kết thúc phải sau thời gian bắt đầu");
        else if (de != null && (ketThuc - batDau).TotalMinutes < de.ThoiLuong)
            ModelState.AddModelError(nameof(vm.ThoiGianKetThuc),
                $"Khung giờ thi ({(ketThuc - batDau).TotalMinutes:0} phút) ngắn hơn thời gian làm bài của đề ({de.ThoiLuong} phút)");
    }

    private async Task KiemTraTrungGioAsync(IEnumerable<int> lopIds, DateTime batDau, DateTime ketThuc, int? boQuaId)
    {
        foreach (var t in await _lichThiService.GetLichTrungGioAsync(lopIds, batDau, ketThuc, boQuaId))
        {
            ModelState.AddModelError(nameof(LichThiFormVM.LopIds),
                $"Lớp {t.TenLop} bị trùng giờ với lịch thi \"{t.TenLichThi}\" " +
                $"({t.ThoiGianBatDau.ToLocalTime():dd/MM/yyyy HH:mm} – {t.ThoiGianKetThuc.ToLocalTime():HH:mm})");
        }
    }

    // ===== Quyền / dữ liệu dùng chung =====

    private static string TrangThaiCua(LichThi lt) =>
        TrangThaiLichThi.Tinh(lt.ThoiGianBatDau, lt.ThoiGianKetThuc, DateTime.UtcNow);

    // null = sửa được; ngược lại là lý do không sửa được
    private async Task<string?> LoiKhongSuaDuocAsync(LichThi lt)
    {
        if (!User.IsInRole(SD.Role_Admin))
        {
            var gv = await GiaoVienHienTaiAsync();
            if (gv == null || gv.Id != lt.GiaoVienId)
                return "Bạn chỉ được sửa lịch thi do mình phụ trách";
        }
        if (TrangThaiCua(lt) == TrangThaiLichThi.DaKetThuc)
            return $"Lịch thi \"{lt.TenLichThi}\" đã kết thúc nên không sửa được";
        return null;
    }

    // Đề được chọn: đã phát hành; Admin = mọi đề, giảng viên = đề của mình.
    // giuDeId: khi sửa, luôn giữ đề hiện tại của lịch thi trong danh sách.
    private async Task<List<DeThiListItemVM>> DeDuocChonAsync(int? giuDeId = null)
    {
        var tatCa = await _deThiService.GetAllDeThiAsync();
        var ds = tatCa.Where(d => d.DaPhatHanh);
        if (!User.IsInRole(SD.Role_Admin))
        {
            var gv = await GiaoVienHienTaiAsync();
            ds = ds.Where(d => (gv != null && d.GiaoVienId == gv.Id) || d.Id == giuDeId);
        }
        return ds.OrderBy(d => d.TenMonHoc).ThenBy(d => d.MaDeThi).ToList();
    }

    private async Task NapDuLieuForm(List<DeThiListItemVM> cacDe, List<LopChonVM>? cacLop = null)
    {
        ViewData["DeThiList"] = cacDe;
        ViewData["LopList"] = cacLop ?? await _lichThiService.GetLopChoLichThiAsync();
    }

    private async Task<GiaoVien?> GiaoVienHienTaiAsync()
    {
        var userId = _userManager.GetUserId(User);
        return userId == null ? null : await _cauHoiService.GetGiaoVienByUserIdAsync(userId);
    }

    #region  API CALLS

    public async Task<IActionResult> GetAllLichThi()
    {
        var bayGio = DateTime.UtcNow;
        var ds = await _lichThiService.GetAllLichThiAsync();
        var data = ds.Select(l => new
        {
            l.Id,
            l.TenLichThi,
            l.DeThiId,
            l.MaDeThi,
            l.TenDeThi,
            l.TenMonHoc,
            l.ThoiLuong,
            // Giờ hiển thị theo múi giờ máy chủ; *Sort để DataTables sắp đúng thứ tự thời gian
            BatDau = l.ThoiGianBatDau.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
            KetThuc = l.ThoiGianKetThuc.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
            BatDauSort = l.ThoiGianBatDau.ToString("yyyyMMddHHmm"),
            CungNgay = l.ThoiGianBatDau.ToLocalTime().Date == l.ThoiGianKetThuc.ToLocalTime().Date,
            l.TenGiaoVien,
            l.GhiChu,
            Lops = string.Join(", ", l.Lops),
            l.SoSinhVien,
            TrangThai = TrangThaiLichThi.Tinh(l.ThoiGianBatDau, l.ThoiGianKetThuc, bayGio),
            // Đang diễn ra: còn bao nhiêu phút; sắp diễn ra: còn bao lâu nữa bắt đầu
            ConLaiPhut = (int)Math.Ceiling(((bayGio < l.ThoiGianBatDau ? l.ThoiGianBatDau : l.ThoiGianKetThuc) - bayGio).TotalMinutes)
        });
        return Json(new { data });
    }

    // DELETE: /Admin/LichThi/Delete/5 - Cho AJAX xóa trực tiếp
    [HttpDelete]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var lt = await _lichThiService.GetLichThiByIdAsync(id);
        if (lt == null)
        {
            return Json(new { success = false, message = "Không tìm thấy lịch thi" });
        }
        // Kiểm tra quyền: Admin hoặc giảng viên phụ trách
        if (!User.IsInRole(SD.Role_Admin))
        {
            var gv = await GiaoVienHienTaiAsync();
            if (gv == null || gv.Id != lt.GiaoVienId)
            {
                return Json(new { success = false, message = "Bạn chỉ được xóa lịch thi do mình phụ trách" });
            }
        }
        // Không cho xóa nếu đang diễn ra hoặc đã kết thúc
        var trangThai = TrangThaiCua(lt);
        if (trangThai != TrangThaiLichThi.SapDienRa)
        {
            return Json(new { success = false, message = $"Lịch thi \"{lt.TenLichThi}\" {trangThai} nên không xóa được" });
        }
        await _lichThiService.DeleteLichThiAsync(id);
        return Json(new { success = true, message = "Đã xóa lịch thi thành công" });
    }

    // GET: /Admin/LichThi/Delete/5 - Trang xác nhận xóa (dành cho user không JS)
    public async Task<IActionResult> DeleteConfirm(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }
        var lt = await _lichThiService.GetLichThiByIdAsync(id.Value);
        if (lt == null)
        {
            return NotFound();
        }
        // Kiểm tra quyền: Admin hoặc giảng viên phụ trách
        if (!User.IsInRole(SD.Role_Admin))
        {
            var gv = await GiaoVienHienTaiAsync();
            if (gv == null || gv.Id != lt.GiaoVienId)
            {
                TempData["error"] = "Bạn chỉ được xóa lịch thi do mình phụ trách";
                return RedirectToAction("Index");
            }
        }
        // Không cho xóa nếu đang diễn ra hoặc đã kết thúc
        var trangThai = TrangThaiCua(lt);
        if (trangThai != TrangThaiLichThi.SapDienRa)
        {
            TempData["error"] = $"Lịch thi \"{lt.TenLichThi}\" {trangThai} nên không xóa được";
            return RedirectToAction("Index");
        }
        return View(lt);
    }

    // POST: /Admin/LichThi/DeleteConfirm/5 - Trang xác nhận xóa (dành cho user không JS)
    [HttpPost, ActionName("DeleteConfirm")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmPost(int id)
    {
        var lt = await _lichThiService.GetLichThiByIdAsync(id);
        if (lt == null)
        {
            return NotFound();
        }
        // Kiểm tra lại quyền trước khi xóa
        if (!User.IsInRole(SD.Role_Admin))
        {
            var gv = await GiaoVienHienTaiAsync();
            if (gv == null || gv.Id != lt.GiaoVienId)
            {
                TempData["error"] = "Bạn chỉ được xóa lịch thi do mình phụ trách";
                return RedirectToAction("Index");
            }
        }
        await _lichThiService.DeleteLichThiAsync(id);
        TempData["success"] = $"Đã xóa lịch thi \"{lt.TenLichThi}\"";
        return RedirectToAction("Index");
    }

    #endregion
}
