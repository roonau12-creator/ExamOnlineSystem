// Form Thêm / Sửa câu hỏi:
//  - Dropdown danh mục chỉ hiện các danh mục thuộc môn học đang chọn
//  - Ẩn/hiện phần đáp án theo loại câu hỏi (chỉ trắc nghiệm mới có đáp án)
//  - Thêm/xóa dòng đáp án, đánh lại chỉ số name="DapAns[i]..." để model binding đúng
$(function () {
    var $form = $("#frmCauHoi");
    if ($form.length === 0) {
        return;
    }

    var MIN_DAP_AN = 2;
    var MAX_DAP_AN = 10;

    // ===== Lọc danh mục theo môn học =====
    var $monHoc = $("#MonHocId");
    var $danhMuc = $("#DanhMucCauHoiId");

    function locDanhMuc() {
        var monHocId = $monHoc.val();
        $danhMuc.find("option[data-mon-hoc-id]").each(function () {
            var hop = monHocId === "" || this.dataset.monHocId === monHocId;
            $(this).prop("hidden", !hop).prop("disabled", !hop);
        });
        if ($danhMuc.find("option:selected").prop("disabled")) {
            $danhMuc.val("");
        }
    }

    // Chọn danh mục trước khi chọn môn học => tự chọn môn học tương ứng
    $danhMuc.on("change", function () {
        var monHocId = $(this).find("option:selected").data("mon-hoc-id");
        if (monHocId && $monHoc.val() === "") {
            $monHoc.val(String(monHocId));
            locDanhMuc();
        }
    });
    $monHoc.on("change", locDanhMuc);
    locDanhMuc();

    // ===== Đáp án =====
    var $khung = $("#khungDapAn");
    var $ds = $("#dsDapAn");
    var mau = document.getElementById("mauDapAn");

    function danhLaiChiSo() {
        $ds.children(".dap-an").each(function (i) {
            $(this).find(".dap-an-nhan").text(String.fromCharCode(65 + i)); // A, B, C...
            $(this).find(".dap-an-id").attr("name", "DapAns[" + i + "].Id");
            $(this).find(".dap-an-dung").attr("name", "DapAns[" + i + "].DapAnDung");
            $(this).find(".dap-an-noi-dung").attr("name", "DapAns[" + i + "].NoiDung");
        });
        var soDong = $ds.children(".dap-an").length;
        $ds.find(".btn-xoa-dap-an").prop("disabled", soDong <= MIN_DAP_AN);
        $("#btnThemDapAn").prop("disabled", soDong >= MAX_DAP_AN);
    }

    $("#btnThemDapAn").on("click", function () {
        $ds.append(mau.content.cloneNode(true));
        danhLaiChiSo();
        $ds.children(".dap-an").last().find(".dap-an-noi-dung").trigger("focus");
    });

    $ds.on("click", ".btn-xoa-dap-an", function () {
        $(this).closest(".dap-an").remove();
        danhLaiChiSo();
    });

    // Câu tự luận: ẩn và không gửi đáp án
    var $loai = $("#LoaiCauHoi");
    function capNhatTheoLoai() {
        var tracNghiem = $loai.val() === "1";
        $khung.toggle(tracNghiem);
        $khung.find("input, button").prop("disabled", !tracNghiem);
        if (tracNghiem) {
            danhLaiChiSo();
        }
    }
    $loai.on("change", capNhatTheoLoai);

    // Kiểm tra nhanh phía trình duyệt (server vẫn kiểm tra lại)
    $form.on("submit", function (e) {
        if ($loai.val() !== "1") {
            return;
        }
        var coNoiDung = 0, coDung = false;
        $ds.children(".dap-an").each(function () {
            var noiDung = $.trim($(this).find(".dap-an-noi-dung").val());
            if (noiDung !== "") {
                coNoiDung++;
                if ($(this).find(".dap-an-dung").is(":checked")) {
                    coDung = true;
                }
            }
        });
        var loi = coNoiDung < MIN_DAP_AN ? "Câu trắc nghiệm phải có ít nhất 2 đáp án"
                : !coDung ? "Vui lòng đánh dấu ít nhất 1 đáp án đúng"
                : null;
        if (loi) {
            e.preventDefault();
            toastr.error(loi);
        }
    });

    capNhatTheoLoai();

    // ===== Ảnh minh họa: xem trước ảnh vừa chọn, tick "Xóa ảnh" thì ẩn ảnh =====
    var $fileAnh = $("#HinhAnhFile");
    var $khungAnh = $("#khungXemTruocAnh");
    var $anh = $("#xemTruocAnh");
    var $xoaAnh = $("#XoaHinhAnh");
    var urlTam = null;

    function hienAnh() {
        if (urlTam) {
            URL.revokeObjectURL(urlTam);
            urlTam = null;
        }
        var file = $fileAnh[0] && $fileAnh[0].files[0];
        if (file) {
            if (file.size > 5 * 1024 * 1024) {
                toastr.error("Ảnh không được lớn hơn 5 MB");
                $fileAnh.val("");
                hienAnh();
                return;
            }
            urlTam = URL.createObjectURL(file);
            $anh.attr("src", urlTam);
            $khungAnh.removeClass("d-none");
            $xoaAnh.prop("checked", false).prop("disabled", true); // chọn ảnh mới = thay ảnh cũ
            return;
        }
        $xoaAnh.prop("disabled", false);
        var goc = $anh.data("anh-goc");
        if (goc && !$xoaAnh.is(":checked")) {
            $anh.attr("src", goc);
            $khungAnh.removeClass("d-none");
        } else {
            $khungAnh.addClass("d-none");
        }
    }

    $fileAnh.on("change", hienAnh);
    $xoaAnh.on("change", hienAnh);
});
