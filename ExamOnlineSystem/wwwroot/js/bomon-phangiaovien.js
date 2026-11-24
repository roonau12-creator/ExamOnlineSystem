// Trang Gán giảng viên vào bộ môn (/Admin/BoMon/PhanGiaoVien/{id})
// Hai cột (form .frm-chon) dùng chung cách lọc / chọn; danh sách do Razor render sẵn.
$(function () {
    var $forms = $(".frm-chon");
    if ($forms.length === 0) {
        return;
    }

    // Lọc theo từ khóa (+ khoa nếu cột có ô lọc khoa), cập nhật số đã chọn và nút lưu
    function capNhat($form) {
        var tu = ($form.find(".o-tim").val() || "").trim().toLowerCase();
        var khoaId = $form.find(".o-loc-khoa").val() || "";
        var soHien = 0;

        $form.find(".dong-gv").each(function () {
            var khop = (tu === "" || this.dataset.tim.indexOf(tu) >= 0) && (!khoaId || this.dataset.khoa === khoaId);
            $(this).toggleClass("d-none", !khop);
            if (khop) {
                soHien++;
            } else {
                // Dòng bị ẩn thì bỏ chọn để không gửi lên server ngoài ý muốn
                $(this).find("input[name='giaoVienIds']").prop("checked", false);
            }
        });

        var soChon = $form.find("input[name='giaoVienIds']:checked").length;
        $form.find(".so-chon").text(soChon);
        $form.find(".btn-luu").prop("disabled", soChon === 0);
        $form.find(".o-dem").text("Đang hiện " + soHien + " giảng viên · đã chọn " + soChon);
    }

    $forms.each(function () {
        var $form = $(this);
        $form.on("input", ".o-tim", function () { capNhat($form); });
        $form.on("change", ".o-loc-khoa, input[name='giaoVienIds']", function () { capNhat($form); });
        $form.on("click", ".btn-chon-tat-ca", function () {
            $form.find(".dong-gv:not(.d-none) input[name='giaoVienIds']").prop("checked", true);
            capNhat($form);
        });
        $form.on("click", ".btn-bo-chon", function () {
            $form.find("input[name='giaoVienIds']").prop("checked", false);
            capNhat($form);
        });
        capNhat($form);
    });

    // Xác nhận trước khi gửi form
    function xacNhan(form, options) {
        if (form.dataset.daXacNhan === "1") {
            return true; // đã xác nhận -> cho submit
        }
        Swal.fire($.extend({
            icon: "question",
            showCancelButton: true,
            cancelButtonText: "Hủy",
            cancelButtonColor: "#6c757d",
            reverseButtons: true
        }, options)).then(function (result) {
            if (result.isConfirmed) {
                form.dataset.daXacNhan = "1";
                form.submit();
            }
        });
        return false;
    }

    $("#frmGan").on("submit", function (e) {
        var $chon = $(this).find("input[name='giaoVienIds']:checked");
        if ($chon.length === 0) {
            e.preventDefault();
            return;
        }
        var khoaCuaBoMon = this.dataset.khoaId;
        var khacKhoa = $chon.closest(".dong-gv").filter(function () { return this.dataset.khoa !== khoaCuaBoMon; }).length;
        if (!xacNhan(this, {
            title: "Gán " + $chon.length + " giảng viên vào " + this.dataset.tenBoMon + "?",
            text: khacKhoa > 0
                ? khacKhoa + " giảng viên đang thuộc khoa khác và sẽ được đổi khoa theo bộ môn này."
                : "Giảng viên sẽ rời bộ môn hiện tại của họ.",
            confirmButtonText: "Gán vào bộ môn"
        })) {
            e.preventDefault();
        }
    });

    $("#frmChuyen").on("submit", function (e) {
        var soChon = $(this).find("input[name='giaoVienIds']:checked").length;
        if (soChon === 0) {
            e.preventDefault();
            return;
        }
        var $dich = $("#boMonDichId");
        if (!$dich.val()) {
            e.preventDefault();
            toastr.warning("Vui lòng chọn bộ môn cần chuyển giảng viên sang");
            $dich.trigger("focus");
            return;
        }
        if (!xacNhan(this, {
            title: "Chuyển " + soChon + " giảng viên?",
            text: "Giảng viên sẽ chuyển sang " + $dich.find("option:selected").text() + " và đổi khoa theo bộ môn mới.",
            confirmButtonText: "Chuyển"
        })) {
            e.preventDefault();
        }
    });
});
