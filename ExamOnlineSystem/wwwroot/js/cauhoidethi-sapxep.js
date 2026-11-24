// Sắp xếp câu hỏi trong đề: kéo thả (HTML5 drag & drop), nút ↑↓, sắp nhanh.
// Thứ tự gửi lên = thứ tự các <input name="thuTu"> (Id dòng câu-đề) trong danh sách.
$(function () {
    var ds = document.getElementById("dsSapXep");
    if (!ds) {
        return;
    }
    var $ds = $(ds);
    var thuTuBanDau = $ds.children(".cau-sap").toArray();
    var dangKeo = null;

    // Đánh lại "Câu n", bật/tắt nút ↑↓ đầu/cuối, đánh dấu câu đổi vị trí
    function capNhat() {
        var soDoi = 0;
        $ds.children(".cau-sap").each(function (i) {
            var doi = String(i + 1) !== String(this.dataset.goc);
            if (doi) soDoi++;
            $(this).find(".so-cau").text("Câu " + (i + 1));
            $(this).find(".doi-cho").toggleClass("d-none", !doi);
            $(this).toggleClass("list-group-item-warning", doi);
            $(this).find(".btn-len").prop("disabled", i === 0);
            $(this).find(".btn-xuong").prop("disabled", i === $ds.children(".cau-sap").length - 1);
        });
        $("#soCauDoi").html(soDoi === 0 ? "Chưa thay đổi" : '<strong class="text-warning">' + soDoi + " câu đổi vị trí</strong> (chưa lưu)");
    }

    function sapTheo(mang) {
        mang.forEach(function (el) { ds.appendChild(el); });
        capNhat();
    }

    // ===== Kéo thả =====
    $ds.on("dragstart", ".cau-sap", function (e) {
        dangKeo = this;
        e.originalEvent.dataTransfer.effectAllowed = "move";
        e.originalEvent.dataTransfer.setData("text/plain", this.dataset.id); // Firefox cần setData
        $(this).addClass("opacity-50");
    });
    $ds.on("dragend", ".cau-sap", function () {
        $(this).removeClass("opacity-50");
        $ds.children().removeClass("border-primary border-2");
        dangKeo = null;
        capNhat();
    });
    $ds.on("dragover", ".cau-sap", function (e) {
        if (!dangKeo || dangKeo === this) {
            return;
        }
        e.preventDefault();
        // Thả vào nửa trên => đặt trước, nửa dưới => đặt sau
        var r = this.getBoundingClientRect();
        var truoc = e.originalEvent.clientY < r.top + r.height / 2;
        ds.insertBefore(dangKeo, truoc ? this : this.nextSibling);
    });
    $ds.on("drop", function (e) {
        e.preventDefault();
    });

    // ===== Nút ↑↓ =====
    $ds.on("click", ".btn-len", function () {
        var li = $(this).closest(".cau-sap")[0];
        if (li.previousElementSibling) {
            ds.insertBefore(li, li.previousElementSibling);
            capNhat();
        }
    });
    $ds.on("click", ".btn-xuong", function () {
        var li = $(this).closest(".cau-sap")[0];
        if (li.nextElementSibling) {
            ds.insertBefore(li.nextElementSibling, li);
            capNhat();
        }
    });

    // ===== Sắp nhanh (sắp ổn định: cùng khóa thì giữ thứ tự hiện tại) =====
    $("[data-sap]").on("click", function () {
        var hienTai = $ds.children(".cau-sap").toArray();
        var viTri = new Map(hienTai.map(function (el, i) { return [el, i]; }));
        var theoKhoa = function (khoa) {
            return hienTai.slice().sort(function (a, b) { return khoa(a) - khoa(b) || viTri.get(a) - viTri.get(b); });
        };
        switch (this.dataset.sap) {
            case "de-kho": sapTheo(theoKhoa(function (el) { return +el.dataset.mucDo; })); break;
            case "kho-de": sapTheo(theoKhoa(function (el) { return -el.dataset.mucDo; })); break;
            case "tn-truoc": sapTheo(theoKhoa(function (el) { return +el.dataset.loai; })); break;
            case "ban-dau": sapTheo(thuTuBanDau); break;
            case "ngau-nhien":
                for (var k = hienTai.length - 1; k > 0; k--) {
                    var j = Math.floor(Math.random() * (k + 1));
                    var tam = hienTai[k]; hienTai[k] = hienTai[j]; hienTai[j] = tam;
                }
                sapTheo(hienTai);
                break;
        }
    });

    // Rời trang khi chưa lưu => trình duyệt hỏi lại
    var daLuu = false;
    $("#frmSapXep").on("submit", function () { daLuu = true; });
    $(window).on("beforeunload", function () {
        var coDoi = $ds.children(".cau-sap").toArray().some(function (el, i) { return String(i + 1) !== String(el.dataset.goc); });
        if (coDoi && !daLuu) {
            return "Thứ tự mới chưa được lưu";
        }
    });

    capNhat();
});
