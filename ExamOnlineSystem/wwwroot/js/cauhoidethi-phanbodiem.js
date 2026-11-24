// Phân bổ điểm cho các câu trong đề.
// Phân bổ theo trọng số + làm tròn theo bước bằng phương pháp "phần dư lớn nhất" (largest remainder):
// tổng các câu luôn đúng bằng tổng điểm đề (khi tổng điểm chia hết cho bước), không lệch do làm tròn.
$(function () {
    var $form = $("#frmPhanBo");
    var $dong = $(".dong-cau");
    if ($form.length === 0 || $dong.length === 0) {
        return;
    }
    var $tong = $("#TongDiem");

    function lamTron(x) {
        return Math.round(x * 100) / 100;
    }

    // trongSo: mảng số >= 0; trả về mảng điểm (bội số của bước), tổng = tongDiem
    function phanBo(trongSo, tongDiem, buoc) {
        var n = trongSo.length;
        var tongTS = trongSo.reduce(function (s, x) { return s + x; }, 0);
        if (tongTS <= 0) {
            trongSo = trongSo.map(function () { return 1; });
            tongTS = n;
        }
        var soDonVi = Math.round(tongDiem / buoc);          // tổng số "bước"
        var lech = lamTron(tongDiem - soDonVi * buoc);       // phần lẻ khi tổng không chia hết cho bước
        var tho = trongSo.map(function (w) { return w / tongTS * soDonVi; });
        var nguyen = tho.map(Math.floor);
        var conLai = soDonVi - nguyen.reduce(function (s, x) { return s + x; }, 0);
        // Chia các đơn vị còn lại cho câu có phần dư lớn nhất (cùng phần dư thì câu trước được trước)
        tho.map(function (x, i) { return { i: i, du: x - nguyen[i] }; })
           .sort(function (a, b) { return b.du - a.du || a.i - b.i; })
           .slice(0, conLai)
           .forEach(function (x) { nguyen[x.i]++; });
        var diem = nguyen.map(function (u) { return lamTron(u * buoc); });
        if (lech !== 0) {
            diem[n - 1] = lamTron(diem[n - 1] + lech);       // dồn phần lẻ vào câu cuối
        }
        return diem;
    }

    function capNhat() {
        var tongDe = parseFloat($tong.val()) || 0;
        var diem = $dong.map(function () { return parseFloat($(this).find(".o-diem").val()) || 0; }).get();
        var tong = lamTron(diem.reduce(function (s, x) { return s + x; }, 0));
        var khop = Math.abs(tong - tongDe) < 0.001;
        var coCauKhong = diem.some(function (x) { return x <= 0; });
        $("#thanhTong")
            .toggleClass("alert-success", khop && !coCauKhong)
            .toggleClass("alert-warning", !khop || coCauKhong)
            .html("Tổng điểm các câu: <strong>" + tong + "</strong> / tổng điểm đề <strong>" + tongDe + "</strong>" +
                  (khop ? ' <i class="bi bi-check-circle-fill"></i>'
                        : " — " + (tong < tongDe ? "còn thiếu " + lamTron(tongDe - tong) : "thừa " + lamTron(tong - tongDe)) + " điểm") +
                  (coCauKhong ? ' · <span class="text-danger">có câu 0 điểm (điểm mỗi câu phải lớn hơn 0)</span>' : ""));
        $dong.each(function (i) {
            $(this).find(".ty-trong").text(tong > 0 ? Math.round(diem[i] / tong * 1000) / 10 + "%" : "");
            $(this).find(".o-diem").toggleClass("is-invalid", diem[i] <= 0);
        });
    }

    function ganDiem(diem) {
        $dong.each(function (i) { $(this).find(".o-diem").val(diem[i]); });
        capNhat();
    }

    $("[data-phan-bo]").on("click", function () {
        var kieu = this.dataset.phanBo;
        if (kieu === "ban-dau") {
            ganDiem($dong.map(function () { return this.dataset.banDau; }).get());
            return;
        }
        var tongDe = parseFloat($tong.val()) || 0;
        var buoc = parseFloat($("#buoc").val()) || 0.25;
        if (tongDe <= 0) {
            toastr.error("Hãy nhập tổng điểm đề trước");
            return;
        }
        if ($dong.length * buoc > tongDe + 1e-9) {
            toastr.error("Bước làm tròn " + buoc + " quá lớn: " + $dong.length + " câu cần ít nhất " +
                         lamTron($dong.length * buoc) + " điểm. Hãy chọn bước nhỏ hơn.");
            return;
        }
        var heSo = {};
        $(".he-so").each(function () { heSo[this.dataset.mucDo] = Math.max(0, parseFloat(this.value) || 0); });
        var trongSo = $dong.map(function () {
            if (kieu === "muc-do") return heSo[this.dataset.mucDo] || 0;
            if (kieu === "mac-dinh") return parseFloat(this.dataset.macDinh) || 0;
            return 1; // chia đều
        }).get();
        var diem = phanBo(trongSo, tongDe, buoc);
        if (diem.some(function (x) { return x <= 0; })) {
            toastr.warning("Có câu nhận 0 điểm (hệ số 0 hoặc bước làm tròn quá lớn so với tỷ trọng). Hãy chỉnh lại trước khi lưu.");
        }
        ganDiem(diem);
    });

    $form.on("input change", ".o-diem", capNhat);
    $tong.on("input change", capNhat);

    // Trước khi lưu: chặn câu 0 điểm, hỏi lại nếu tổng chưa khớp
    var daXacNhan = false;
    $form.on("submit", function (e) {
        if (daXacNhan) {
            return;
        }
        var tongDe = parseFloat($tong.val()) || 0;
        var diem = $dong.map(function () { return parseFloat($(this).find(".o-diem").val()) || 0; }).get();
        if (diem.some(function (x) { return x <= 0; })) {
            e.preventDefault();
            toastr.error("Điểm mỗi câu phải lớn hơn 0");
            return;
        }
        var tong = lamTron(diem.reduce(function (s, x) { return s + x; }, 0));
        if (Math.abs(tong - tongDe) < 0.001) {
            return;
        }
        e.preventDefault();
        Swal.fire({
            title: "Tổng điểm chưa khớp",
            text: "Tổng điểm các câu là " + tong + " nhưng tổng điểm đề là " + tongDe +
                  ". Đề sẽ chưa phát hành được cho tới khi khớp. Vẫn lưu?",
            icon: "question",
            showCancelButton: true,
            confirmButtonText: "Vẫn lưu",
            cancelButtonText: "Quay lại sửa",
            reverseButtons: true
        }).then(function (r) {
            if (r.isConfirmed) {
                daXacNhan = true;
                $form.trigger("submit");
            }
        });
    });

    capNhat();
});
