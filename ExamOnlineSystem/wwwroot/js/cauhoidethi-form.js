// Form Thêm câu hỏi vào đề — chọn nhiều câu, thêm cùng lúc:
//  - Chọn đề => tải câu của môn (kèm lý do nếu không thêm được) + câu hiện có (để chọn vị trí, tính tổng điểm)
//  - Lọc theo từ khóa / danh mục (gồm danh mục con) / mức độ; "Chọn tất cả câu đang hiện"
//  - Câu được thêm theo THỨ TỰ TICK; mỗi câu có ô điểm riêng (mặc định = điểm mặc định của câu)
//  - Gửi lên: Cau[i].CauHoiId, Cau[i].Diem theo thứ tự đã chọn
$(function () {
    var $form = $("#frmThemNhieu");
    if ($form.length === 0) {
        return;
    }

    var $de = $("#DeThiId");
    var $viTri = $("#ViTri");
    var $khung = $("#khungChon");
    var $ds = $("#dsCau");
    var duLieu = null;                 // dữ liệu đề đang chọn
    var theoId = {};                   // id -> câu
    var daChon = [];                   // [{ cauHoiId, diem }] theo thứ tự tick
    var chonBanDau = ($khung.data("chon") || []).map(function (c) { return { cauHoiId: c.cauHoiId, diem: c.diem }; });
    var lanDau = true;
    var mauMucDo = { 1: "success", 2: "info", 3: "warning", 4: "danger" };

    function escapeHtml(value) {
        return $("<div>").text(value == null ? "" : value).html();
    }

    function rutGon(text, max) {
        text = text == null ? "" : String(text);
        return text.length <= max ? text : text.substring(0, max) + "…";
    }

    function lamTron(x) {
        return Math.round(x * 100) / 100;
    }

    function viTriChon(id) {
        for (var i = 0; i < daChon.length; i++) {
            if (daChon[i].cauHoiId === id) return i;
        }
        return -1;
    }

    // ===== Tải dữ liệu đề =====
    function taiDe() {
        var id = $de.val();
        duLieu = null;
        theoId = {};
        daChon = [];
        $viTri.find("option:not(:first)").remove();
        $khung.addClass("d-none");
        $("#tongDiem").addClass("d-none");
        capNhat();
        if (!id) {
            return;
        }
        $ds.html('<div class="list-group-item text-muted">Đang tải...</div>');
        $.getJSON("/Admin/CauHoiDeThi/GetCauHoiChoDe", { deThiId: id }).done(function (res) {
            if (!res.success) {
                toastr.error(res.message);
                return;
            }
            duLieu = res;
            res.cauCoTheThem.forEach(function (c) { theoId[c.id] = c; });

            res.cauHienCo.forEach(function (c) {
                $viTri.append($("<option>").val(c.thuTu).text("Bắt đầu từ câu " + c.thuTu + " (trước câu #" + c.cauHoiId + " hiện tại)"));
            });

            var $dm = $("#locDanhMuc");
            $dm.val("").find("option:not(:first)").remove();
            (res.danhMucs || []).forEach(function (d) {
                $dm.append($("<option>").val(d.id).text((d.cap > 1 ? " ".repeat((d.cap - 1) * 4) + "└ " : "") + d.ten));
            });

            // Form trả về do lỗi: khôi phục câu đã chọn (chỉ câu còn thêm được) + vị trí
            if (lanDau) {
                chonBanDau.forEach(function (c) {
                    if (theoId[c.cauHoiId] && !theoId[c.cauHoiId].lyDo && viTriChon(c.cauHoiId) < 0) {
                        daChon.push({ cauHoiId: c.cauHoiId, diem: c.diem });
                    }
                });
                var vt = String($viTri.data("chon") || "");
                if (vt) $viTri.val(vt);
                lanDau = false;
            }

            $khung.removeClass("d-none");
            $("#tongDiem").removeClass("d-none");
            veDanhSach();
        }).fail(function () {
            $ds.html('<div class="list-group-item text-danger">Không tải được câu hỏi</div>');
        });
    }

    // ===== Danh sách câu theo bộ lọc =====
    function cauDangHien() {
        if (!duLieu) return [];
        var tim = $.trim($("#timCau").val()).toLowerCase();
        var dm = $("#locDanhMuc").val();
        var md = $("#locMucDo").val();
        var an = $("#anKhongThem").is(":checked");
        return duLieu.cauCoTheThem.filter(function (c) {
            return (!an || !c.lyDo) &&
                   (!dm || (c.danhMucLoc || "").indexOf("|" + dm + "|") >= 0) &&
                   (!md || String(c.mucDo) === md) &&
                   (!tim || ("#" + c.id + " " + c.noiDung + " " + c.danhMuc).toLowerCase().indexOf(tim) >= 0);
        });
    }

    function veDanhSach() {
        var ds = cauDangHien();
        var tong = duLieu ? duLieu.cauCoTheThem.length : 0;
        var themDuoc = duLieu ? duLieu.cauCoTheThem.filter(function (c) { return !c.lyDo; }).length : 0;
        $("#demHien").text("Đang hiện " + ds.length + "/" + tong + " câu · " + themDuoc + " câu thêm được");

        if (tong === 0) {
            $ds.html('<div class="list-group-item text-muted">Môn học của đề chưa có câu hỏi nào trong ngân hàng.</div>');
        } else if (ds.length === 0) {
            $ds.html('<div class="list-group-item text-muted">Không có câu nào phù hợp bộ lọc.</div>');
        } else {
            $ds.html(ds.map(function (c) {
                var i = viTriChon(c.id);
                var chon = i >= 0;
                return '<label class="list-group-item d-flex gap-2 align-items-start ' +
                       (c.lyDo ? "bg-light text-muted" : chon ? "list-group-item-primary" : "") + '">' +
                       '<input type="checkbox" class="form-check-input mt-1 o-chon" data-id="' + c.id + '"' +
                           (chon ? " checked" : "") + (c.lyDo ? " disabled" : "") + ' />' +
                       '<span class="badge text-bg-primary mt-1 ' + (chon ? "" : "invisible") + '" style="min-width: 28px">' + (i + 1) + '</span>' +
                       '<div class="me-auto">' +
                           '<div><span class="text-muted">#' + c.id + '</span> ' + escapeHtml(rutGon(c.noiDung, 120)) + '</div>' +
                           '<div class="small mt-1">' +
                               '<span class="badge text-bg-light border">' + escapeHtml(c.tenLoaiCauHoi) + '</span> ' +
                               '<span class="badge text-bg-' + (mauMucDo[c.mucDo] || "secondary") + '">' + escapeHtml(c.tenMucDo) + '</span> ' +
                               '<span class="text-muted">' + escapeHtml(c.danhMuc) + ' · mặc định ' + c.diemMacDinh + 'đ</span>' +
                               (c.lyDo ? ' <span class="text-danger">(' + escapeHtml(c.lyDo) + ')</span>' : '') +
                           '</div>' +
                       '</div>' +
                       (chon
                           ? '<div class="input-group input-group-sm" style="width: 110px">' +
                             '<input type="number" step="0.25" min="0.25" class="form-control text-end o-diem" data-id="' + c.id + '" value="' + daChon[i].diem + '" />' +
                             '<span class="input-group-text">đ</span></div>'
                           : '') +
                       '</label>';
            }).join(""));
        }
        capNhat();
    }

    // ===== Tổng hợp + hidden inputs =====
    function capNhat() {
        var n = daChon.length;
        $(".so-chon").text(n);
        $(".btn-luu").prop("disabled", n === 0 || !duLieu);

        $("#hiddenCau").html(daChon.map(function (c, i) {
            return '<input type="hidden" name="Cau[' + i + '].CauHoiId" value="' + c.cauHoiId + '" />' +
                   '<input type="hidden" name="Cau[' + i + '].Diem" value="' + (parseFloat(c.diem) || 0) + '" />';
        }).join(""));

        if (!duLieu) return;
        var them = lamTron(daChon.reduce(function (s, c) { return s + (parseFloat(c.diem) || 0); }, 0));
        var sau = lamTron(duLieu.tongDiemCau + them);
        var khop = Math.abs(sau - duLieu.tongDiem) < 0.001;
        $("#tongDiem")
            .toggleClass("alert-success", khop)
            .toggleClass("alert-warning", !khop)
            .html("Đã chọn <strong>" + n + "</strong> câu (+" + them + " điểm) · Đề hiện có " + duLieu.cauHienCo.length +
                  " câu, " + duLieu.tongDiemCau + " điểm → sau khi thêm <strong>" + (duLieu.cauHienCo.length + n) + " câu, " + sau +
                  "</strong> / tổng điểm đề <strong>" + duLieu.tongDiem + "</strong>" +
                  (khop ? ' <i class="bi bi-check-circle-fill"></i>'
                        : " — " + (sau < duLieu.tongDiem ? "còn thiếu " + lamTron(duLieu.tongDiem - sau) : "thừa " + lamTron(sau - duLieu.tongDiem)) + " điểm"));
    }

    // ===== Sự kiện =====
    $de.on("change", function () {
        if (daChon.length === 0) {
            taiDe();
            return;
        }
        Swal.fire({
            title: "Đổi đề thi?", text: "Các câu đã chọn (" + daChon.length + ") sẽ bị bỏ chọn.", icon: "warning",
            showCancelButton: true, confirmButtonText: "Đổi đề", cancelButtonText: "Giữ đề cũ", reverseButtons: true
        }).then(function (r) {
            if (r.isConfirmed) taiDe();
            else $de.val($de.data("cu") || "");
        });
    });
    $de.on("focus", function () { $de.data("cu", $de.val()); });

    $ds.on("change", ".o-chon", function () {
        var id = $(this).data("id");
        var i = viTriChon(id);
        if (this.checked && i < 0) {
            daChon.push({ cauHoiId: id, diem: theoId[id].diemMacDinh });
        } else if (!this.checked && i >= 0) {
            daChon.splice(i, 1);
        }
        veDanhSach();
    });

    $ds.on("input change", ".o-diem", function () {
        var i = viTriChon($(this).data("id"));
        if (i >= 0) {
            daChon[i].diem = this.value;
            capNhat();
        }
    });
    // Bấm vào chữ "đ" (nằm trong <label>) không được làm đổi trạng thái tick của dòng.
    // Bấm vào ô điểm thì trình duyệt tự không đổi tick (ô nhập là phần tử tương tác riêng).
    $ds.on("click", ".input-group-text", function (e) { e.preventDefault(); $(this).siblings(".o-diem").trigger("focus"); });

    $("#chonTatCa").on("click", function () {
        cauDangHien().forEach(function (c) {
            if (!c.lyDo && viTriChon(c.id) < 0) {
                daChon.push({ cauHoiId: c.id, diem: c.diemMacDinh });
            }
        });
        veDanhSach();
    });
    $("#boChon").on("click", function () {
        daChon = [];
        veDanhSach();
    });

    $("#timCau").on("input", veDanhSach);
    $("#locDanhMuc, #locMucDo, #anKhongThem").on("change", veDanhSach);

    $form.on("submit", function (e) {
        if (daChon.length === 0) {
            e.preventDefault();
            toastr.error("Vui lòng chọn ít nhất 1 câu hỏi");
            return;
        }
        if (daChon.some(function (c) { return !(parseFloat(c.diem) > 0); })) {
            e.preventDefault();
            toastr.error("Điểm mỗi câu phải lớn hơn 0");
        }
    });

    taiDe();
});
