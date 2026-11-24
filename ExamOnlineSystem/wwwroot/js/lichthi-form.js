// Form Tạo lịch thi:
//  - Chọn đề => gợi ý tên lịch thi; chọn giờ bắt đầu => tự điền giờ kết thúc = bắt đầu + thời gian làm bài
//  - Hiện "khung giờ X phút / làm bài Y phút", cảnh báo khi khung giờ ngắn hơn thời gian làm bài
//  - Lớp: tìm kiếm, chọn cả khoa, đếm số lớp và tổng sinh viên đã chọn
$(function () {
    var $form = $("#frmLichThi");
    if ($form.length === 0) {
        return;
    }
    var $de = $("#DeThiId"), $ten = $("#TenLichThi"), $bd = $("#ThoiGianBatDau"), $kt = $("#ThoiGianKetThuc");
    var tenTuDong = !$.trim($ten.val()) || $ten.val() === ($de.find("option:selected").data("ten") || "");
    var ktTuDong = !$kt.val();

    function thoiLuong() {
        return parseInt($de.find("option:selected").data("thoi-luong"), 10) || 0;
    }

    // "yyyy-MM-ddTHH:mm" (giờ địa phương) <-> Date
    function docGio(s) {
        return s ? new Date(s) : null;
    }
    function vietGio(d) {
        var p = function (n) { return String(n).padStart(2, "0"); };
        return d.getFullYear() + "-" + p(d.getMonth() + 1) + "-" + p(d.getDate()) + "T" + p(d.getHours()) + ":" + p(d.getMinutes());
    }

    function capNhatGio() {
        var bd = docGio($bd.val()), kt = docGio($kt.val()), tl = thoiLuong();
        var $tt = $("#thongTinGio").removeClass("text-danger text-success");
        if (!bd || !kt) {
            $tt.text(tl ? "Thời gian làm bài: " + tl + " phút. Chọn giờ bắt đầu: giờ kết thúc tự điền." :
                          "Chọn đề và giờ bắt đầu: giờ kết thúc tự điền = bắt đầu + thời gian làm bài.");
            return;
        }
        var khung = Math.round((kt - bd) / 60000);
        if (khung <= 0) {
            $tt.addClass("text-danger").text("Giờ kết thúc phải sau giờ bắt đầu.");
        } else if (tl && khung < tl) {
            $tt.addClass("text-danger").text("Khung giờ " + khung + " phút ngắn hơn thời gian làm bài " + tl + " phút.");
        } else {
            $tt.addClass("text-success").text("Khung giờ thi " + khung + " phút" + (tl ? " · làm bài " + tl + " phút" +
                (khung > tl ? " (sinh viên có thể vào muộn tối đa " + (khung - tl) + " phút)" : "") : "") + ".");
        }
        // Lịch đang diễn ra (ô bắt đầu bị khóa): giờ bắt đầu đã qua là đương nhiên; chỉ kiểm tra giờ kết thúc
        if ($bd.prop("disabled")) {
            if (kt <= new Date()) {
                $tt.removeClass("text-success").addClass("text-danger").text("Giờ kết thúc mới phải sau thời điểm hiện tại.");
            }
        } else if (bd < new Date(Date.now() - 5 * 60000)) {
            $tt.removeClass("text-success").addClass("text-danger").text("Thời gian bắt đầu đã qua.");
        }
    }

    $de.on("change", function () {
        if (tenTuDong) {
            $ten.val($de.find("option:selected").data("ten") || "");
        }
        if (ktTuDong && $bd.val() && thoiLuong()) {
            $kt.val(vietGio(new Date(docGio($bd.val()).getTime() + thoiLuong() * 60000)));
        }
        capNhatGio();
    });
    $ten.on("input", function () { tenTuDong = !$.trim(this.value); });
    $bd.on("change input", function () {
        var bd = docGio($bd.val());
        if (bd && thoiLuong() && (ktTuDong || !$kt.val())) {
            $kt.val(vietGio(new Date(bd.getTime() + thoiLuong() * 60000)));
            ktTuDong = true;
        }
        capNhatGio();
    });
    $kt.on("change input", function () { ktTuDong = false; capNhatGio(); });

    // Không cho chọn giờ trong quá khứ trên bộ chọn ngày giờ
    $bd.attr("min", vietGio(new Date()));
    if ($bd.prop("disabled")) {
        $kt.attr("min", vietGio(new Date()));
    }

    // ===== Lớp =====
    function capNhatLop() {
        var $chon = $(".o-lop:checked");
        var soSv = 0;
        $chon.each(function () { soSv += parseInt(this.dataset.siSo, 10) || 0; });
        $("#tongChon").html($chon.length === 0 ? '<span class="text-muted">Chưa chọn lớp nào</span>'
            : 'Đã chọn <strong>' + $chon.length + '</strong> lớp · <strong>' + soSv + '</strong> sinh viên');
        $(".nhom-khoa").each(function () {
            var $o = $(this).find(".lop:not(.d-none) .o-lop");
            $(this).find(".chon-ca-khoa").text($o.length && $o.filter(":checked").length === $o.length ? "Bỏ chọn cả khoa" : "Chọn cả khoa");
        });
    }

    $("#timLop").on("input", function () {
        var tim = $.trim(this.value).toLowerCase();
        $(".lop").each(function () { $(this).toggleClass("d-none", !!tim && this.dataset.tim.indexOf(tim) < 0); });
        $(".nhom-khoa").each(function () { $(this).toggleClass("d-none", $(this).find(".lop:not(.d-none)").length === 0); });
        capNhatLop();
    });
    $(".chon-ca-khoa").on("click", function () {
        var $o = $(this).closest(".nhom-khoa").find(".lop:not(.d-none) .o-lop");
        var chonHet = $o.filter(":checked").length === $o.length;
        $o.prop("checked", !chonHet);
        capNhatLop();
    });
    $(document).on("change", ".o-lop", capNhatLop);

    $form.on("submit", function (e) {
        if ($(".o-lop:checked").length === 0) {
            e.preventDefault();
            toastr.error("Vui lòng chọn ít nhất 1 lớp");
        }
    });

    capNhatGio();
    capNhatLop();
});
