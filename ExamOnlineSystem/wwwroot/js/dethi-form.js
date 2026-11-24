// Form Thêm đề thi: chọn câu hỏi từ ngân hàng của môn học, đặt điểm, sắp thứ tự.
//  - nganHang: các câu của môn học đang chọn (tải qua Ajax)
//  - daChon:   [{ cauHoiId, diem }] theo thứ tự câu trong đề; gửi lên dưới dạng CauHois[i].CauHoiId / CauHois[i].Diem
$(function () {
    var $form = $("#frmDeThi");
    if ($form.length === 0) {
        return;
    }

    var $monHoc = $("#MonHocId");
    var $loaiDe = $("#LoaiDeThi");
    var $tongDiem = $("#TongDiem");
    var $dsNganHang = $("#dsNganHang");
    var $dsDaChon = $("#dsDaChon");

    var nganHang = [];            // câu hỏi của môn học
    var theoId = {};              // id -> câu hỏi
    var daChon = ($dsDaChon.data("chon") || []).map(function (c) {
        return { cauHoiId: c.cauHoiId, diem: c.diem };
    });
    var monHocHienTai = $monHoc.val();

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

    // Loại đề 1 = chỉ trắc nghiệm, 2 = chỉ tự luận, 3 = cả hai
    function phuHop(cau) {
        var loai = $loaiDe.val();
        return loai === "3" || String(cau.loaiCauHoi) === loai;
    }

    function laDaChon(id) {
        return daChon.some(function (c) { return c.cauHoiId === id; });
    }

    function nhanCau(cau) {
        return '<span class="badge text-bg-light border">' + escapeHtml(cau.tenLoaiCauHoi) + '</span> ' +
               '<span class="badge text-bg-' + (mauMucDo[cau.mucDo] || "secondary") + '">' + escapeHtml(cau.tenMucDo) + '</span>' +
               (cau.coHinhAnh ? ' <i class="bi bi-image text-primary" title="Có ảnh"></i>' : '');
    }

    // ===== Ngân hàng câu hỏi =====
    function taiNganHang() {
        var id = $monHoc.val();
        nganHang = [];
        theoId = {};
        if (!id) {
            $("#locDanhMucCau").val("").find("option:not(:first)").remove();
            $dsNganHang.html('<div class="list-group-item text-muted">Chọn môn học để hiện câu hỏi.</div>');
            veDaChon();
            return;
        }
        $dsNganHang.html('<div class="list-group-item text-muted">Đang tải...</div>');
        $.getJSON("/Admin/DeThi/GetCauHoiByMonHoc", { monHocId: id }).done(function (res) {
            nganHang = res.data;
            // Bộ lọc danh mục: cây danh mục của môn, thụt lề theo cấp
            var $dm = $("#locDanhMucCau");
            var dmCu = $dm.val();
            $dm.find("option:not(:first)").remove();
            (res.danhMucs || []).forEach(function (d) {
                $dm.append($("<option>").val(d.id).text((d.cap > 1 ? "\u00A0".repeat((d.cap - 1) * 4) + "└ " : "") + d.ten));
            });
            $dm.val($dm.find('option[value="' + dmCu + '"]').length ? dmCu : "");
            nganHang.forEach(function (c) { theoId[c.id] = c; });
            veNganHang();
            veDaChon();
        }).fail(function () {
            $dsNganHang.html('<div class="list-group-item text-danger">Không tải được câu hỏi</div>');
        });
    }

    function veNganHang() {
        var tim = $.trim($("#timCauHoi").val()).toLowerCase();
        var loai = $("#locLoaiCau").val();
        var mucDo = $("#locMucDoCau").val();
        var danhMuc = $("#locDanhMucCau").val();
        var ds = nganHang.filter(function (c) {
            return (!loai || String(c.loaiCauHoi) === loai) &&
                   (!mucDo || String(c.mucDo) === mucDo) &&
                   (!danhMuc || (c.danhMucLoc || "").indexOf("|" + danhMuc + "|") >= 0) &&
                   (!tim || (c.noiDung + " " + c.danhMuc + " #" + c.id).toLowerCase().indexOf(tim) >= 0);
        });
        $("#soCauNganHang").text("(" + ds.length + "/" + nganHang.length + " câu)");

        if (nganHang.length === 0) {
            $dsNganHang.html('<div class="list-group-item text-muted">Môn học này chưa có câu hỏi nào.</div>');
            return;
        }
        if (ds.length === 0) {
            $dsNganHang.html('<div class="list-group-item text-muted">Không có câu hỏi phù hợp bộ lọc.</div>');
            return;
        }

        $dsNganHang.html(ds.map(function (c) {
            var chon = laDaChon(c.id);
            var hop = phuHop(c);
            var thieuDapAn = c.loaiCauHoi === 1 && c.soDapAn < 2;
            var nut;
            if (chon) {
                nut = '<span class="badge text-bg-success"><i class="bi bi-check-lg"></i> Đã chọn</span>';
            } else if (!hop) {
                nut = '<span class="badge text-bg-light border text-muted" title="Loại câu không phù hợp loại đề">Khác loại đề</span>';
            } else if (thieuDapAn) {
                nut = '<span class="badge text-bg-light border text-danger" title="Câu trắc nghiệm chưa đủ 2 đáp án">Thiếu đáp án</span>';
            } else {
                nut = '<button type="button" class="btn btn-sm btn-outline-primary btn-them-cau" data-id="' + c.id + '">' +
                      '<i class="bi bi-plus-lg"></i> Thêm</button>';
            }
            return '<div class="list-group-item d-flex gap-2 align-items-start ' + (chon || !hop ? "bg-light" : "") + '">' +
                   '<div class="me-auto">' +
                       '<div><span class="text-muted">#' + c.id + '</span> ' +
                           '<a href="/Admin/CauHoi/Details/' + c.id + '" target="_blank" class="text-reset" title="' + escapeHtml(c.noiDung) + '">' +
                           escapeHtml(rutGon(c.noiDung, 110)) + '</a></div>' +
                       '<div class="mt-1">' + nhanCau(c) + ' <span class="text-muted">' + escapeHtml(c.danhMuc) + ' · ' + c.diemMacDinh + 'đ</span></div>' +
                   '</div>' +
                   '<div class="text-nowrap">' + nut + '</div>' +
                   '</div>';
        }).join(""));
    }

    // ===== Câu hỏi trong đề =====
    function veDaChon() {
        if (daChon.length === 0) {
            $dsDaChon.html('<div class="list-group-item text-muted">Chưa có câu hỏi nào. Bấm <strong>Thêm</strong> ở ngân hàng câu hỏi.</div>');
        } else {
            $dsDaChon.html(daChon.map(function (c, i) {
                var cau = theoId[c.cauHoiId];
                var hop = !cau || phuHop(cau);
                var noiDung = cau ? escapeHtml(rutGon(cau.noiDung, 90)) : '<span class="text-danger">Câu hỏi #' + c.cauHoiId + ' không thuộc môn học này</span>';
                return '<div class="list-group-item d-flex gap-2 align-items-center ' + (hop && cau ? "" : "list-group-item-danger") + '">' +
                       '<span class="fw-semibold text-nowrap">Câu ' + (i + 1) + '</span>' +
                       '<div class="me-auto">' +
                           '<div><span class="text-muted">#' + c.cauHoiId + '</span> ' + noiDung + '</div>' +
                           (cau ? '<div class="mt-1">' + nhanCau(cau) + (hop ? '' : ' <span class="text-danger">Khác loại đề</span>') + '</div>' : '') +
                       '</div>' +
                       '<div class="input-group input-group-sm" style="width: 105px">' +
                           '<input type="number" step="0.25" min="0.25" class="form-control o-diem" data-i="' + i + '" value="' + c.diem + '" />' +
                           '<span class="input-group-text">đ</span>' +
                       '</div>' +
                       '<div class="btn-group btn-group-sm">' +
                           '<button type="button" class="btn btn-outline-secondary btn-len" data-i="' + i + '" title="Lên"' + (i === 0 ? " disabled" : "") + '><i class="bi bi-arrow-up"></i></button>' +
                           '<button type="button" class="btn btn-outline-secondary btn-xuong" data-i="' + i + '" title="Xuống"' + (i === daChon.length - 1 ? " disabled" : "") + '><i class="bi bi-arrow-down"></i></button>' +
                           '<button type="button" class="btn btn-outline-danger btn-bo" data-i="' + i + '" title="Bỏ khỏi đề"><i class="bi bi-x-lg"></i></button>' +
                       '</div>' +
                       '</div>';
            }).join(""));
        }
        $("#soCauDaChon").text("(" + daChon.length + " câu)");
        capNhatTongDiem();
        veHidden();
    }

    function tongDiemCau() {
        return lamTron(daChon.reduce(function (s, c) { return s + (parseFloat(c.diem) || 0); }, 0));
    }

    function capNhatTongDiem() {
        var tong = tongDiemCau();
        var de = parseFloat($tongDiem.val()) || 0;
        var khop = Math.abs(tong - de) < 0.001;
        $("#tongDiemDe")
            .toggleClass("alert-success", khop)
            .toggleClass("alert-warning", !khop)
            .html('Tổng điểm các câu: <strong>' + tong + '</strong> / tổng điểm đề <strong>' + de + '</strong>' +
                  (khop ? ' <i class="bi bi-check-circle-fill"></i>'
                        : ' — ' + (tong < de ? "còn thiếu " + lamTron(de - tong) : "thừa " + lamTron(tong - de)) + ' điểm'));
    }

    // Hidden inputs để model binding: CauHois[i].CauHoiId, CauHois[i].Diem
    function veHidden() {
        $("#hiddenCauHois").html(daChon.map(function (c, i) {
            return '<input type="hidden" name="CauHois[' + i + '].CauHoiId" value="' + c.cauHoiId + '" />' +
                   '<input type="hidden" name="CauHois[' + i + '].Diem" value="' + (parseFloat(c.diem) || 0) + '" />';
        }).join(""));
    }

    // ===== Sự kiện =====
    $dsNganHang.on("click", ".btn-them-cau", function () {
        var cau = theoId[$(this).data("id")];
        if (!cau || laDaChon(cau.id)) {
            return;
        }
        daChon.push({ cauHoiId: cau.id, diem: cau.diemMacDinh });
        veNganHang();
        veDaChon();
    });

    $dsDaChon.on("input change", ".o-diem", function () {
        daChon[$(this).data("i")].diem = this.value;
        capNhatTongDiem();
        veHidden();
    });

    $dsDaChon.on("click", ".btn-len, .btn-xuong", function () {
        var i = $(this).data("i");
        var j = $(this).hasClass("btn-len") ? i - 1 : i + 1;
        var tam = daChon[i];
        daChon[i] = daChon[j];
        daChon[j] = tam;
        veDaChon();
    });

    $dsDaChon.on("click", ".btn-bo", function () {
        daChon.splice($(this).data("i"), 1);
        veNganHang();
        veDaChon();
    });

    // Chia đều tổng điểm đề cho các câu (làm tròn 2 chữ số, câu cuối nhận phần dư)
    $("#btnChiaDeu").on("click", function () {
        var de = parseFloat($tongDiem.val()) || 0;
        var n = daChon.length;
        if (n === 0 || de <= 0) {
            return;
        }
        var moiCau = Math.floor(de / n * 100) / 100;
        daChon.forEach(function (c, i) {
            c.diem = i === n - 1 ? lamTron(de - moiCau * (n - 1)) : moiCau;
        });
        veDaChon();
    });

    $("#timCauHoi").on("input", veNganHang);
    $("#locLoaiCau, #locMucDoCau, #locDanhMucCau").on("change", veNganHang);
    $tongDiem.on("input change", capNhatTongDiem);
    $loaiDe.on("change", function () {
        veNganHang();
        veDaChon();
    });

    // Đổi môn học: các câu đã chọn thuộc môn cũ phải bỏ đi
    $monHoc.on("change", function () {
        var moi = $monHoc.val();
        if (daChon.length === 0) {
            monHocHienTai = moi;
            taiNganHang();
            return;
        }
        Swal.fire({
            title: "Đổi môn học?",
            text: "Đề đang có " + daChon.length + " câu hỏi của môn cũ. Đổi môn học sẽ bỏ các câu này khỏi đề.",
            icon: "warning",
            showCancelButton: true,
            confirmButtonText: "Đổi môn",
            cancelButtonText: "Giữ môn cũ",
            reverseButtons: true
        }).then(function (r) {
            if (r.isConfirmed) {
                daChon = [];
                monHocHienTai = moi;
                taiNganHang();
            } else {
                $monHoc.val(monHocHienTai);
            }
        });
    });

    // Trước khi lưu: chặn câu khác loại, hỏi lại nếu tổng điểm chưa khớp
    var daXacNhan = false;
    $form.on("submit", function (e) {
        if (daXacNhan || !$form.valid()) {
            return;
        }
        var khacLoai = daChon.filter(function (c) { return theoId[c.cauHoiId] && !phuHop(theoId[c.cauHoiId]); });
        if (khacLoai.length > 0) {
            e.preventDefault();
            toastr.error("Đề có " + khacLoai.length + " câu khác loại đề. Hãy bỏ các câu này hoặc chọn loại đề Kết hợp.");
            return;
        }
        var tong = tongDiemCau();
        var de = parseFloat($tongDiem.val()) || 0;
        if (daChon.length > 0 && Math.abs(tong - de) < 0.001) {
            return;
        }
        e.preventDefault();
        Swal.fire({
            title: daChon.length === 0 ? "Đề chưa có câu hỏi" : "Tổng điểm chưa khớp",
            text: daChon.length === 0
                ? "Đề thi chưa có câu hỏi nào. Vẫn lưu đề (nháp) để thêm câu hỏi sau?"
                : "Tổng điểm các câu là " + tong + " nhưng tổng điểm đề là " + de + ". Vẫn lưu đề?",
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

    taiNganHang();
});
