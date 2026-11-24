// Trang Phân sinh viên vào lớp (/Admin/Lop/PhanSinhVien/{id})
// - Cột trái: ứng viên (sinh viên lớp khác) tải qua /Admin/Lop/GetSinhVienNgoaiLop, lọc tại trình duyệt
// - Cột phải: sinh viên của lớp (do Razor render sẵn), lọc theo từ khóa
$(function () {
    var $frmThem = $("#frmThem");
    if ($frmThem.length === 0) {
        return;
    }

    var lopId = Number($frmThem.find("input[name='lopId']").val());
    var khoaIdCuaLop = Number($frmThem.attr("data-khoa-id")); // khoa của lớp -> đếm SV sẽ bị đổi khoa

    function escapeHtml(value) {
        return $("<div>").text(value == null ? "" : value).html();
    }

    // Bật/tắt nút lưu + cập nhật số đã chọn của một form.
    // soChon: truyền vào khi lựa chọn được giữ ngoài DOM (cột ứng viên có bộ lọc),
    // bỏ trống thì đếm trực tiếp các ô đang tích.
    function capNhatChon($form, $dem, tongHien, soChon) {
        if (soChon === undefined) {
            soChon = $form.find("input[name='sinhVienIds']:checked").length;
        }
        $form.find(".so-chon").text(soChon);
        $form.find(".btn-luu").prop("disabled", soChon === 0);
        if ($dem) {
            $dem.text("Đang hiện " + tongHien() + " sinh viên · đã chọn " + soChon);
        }
    }

    // ---------- Cột phải: sinh viên trong lớp ----------
    var $frmChuyen = $("#frmChuyen");
    var $dsTrongLop = $("#dsTrongLop");
    var $demTrongLop = $("#demTrongLop");

    function demHienTrongLop() {
        return $dsTrongLop.find(".dong-sv:visible").length;
    }

    function locTrongLop() {
        var tu = $("#timTrongLop").val().trim().toLowerCase();
        $dsTrongLop.find(".dong-sv").each(function () {
            var khop = tu === "" || this.dataset.tim.indexOf(tu) >= 0;
            $(this).toggle(khop);
            if (!khop) {
                // Dòng bị ẩn thì bỏ chọn để không gửi lên server ngoài ý muốn
                $(this).find("input[name='sinhVienIds']").prop("checked", false);
            }
        });
        capNhatChon($frmChuyen, $demTrongLop, demHienTrongLop);
    }

    $("#timTrongLop").on("input", locTrongLop);
    $dsTrongLop.on("change", "input[name='sinhVienIds']", function () {
        capNhatChon($frmChuyen, $demTrongLop, demHienTrongLop);
    });
    $("#chonTatCaTrongLop").on("click", function () {
        $dsTrongLop.find(".dong-sv:visible input[name='sinhVienIds']").prop("checked", true);
        capNhatChon($frmChuyen, $demTrongLop, demHienTrongLop);
    });
    $("#boChonTrongLop").on("click", function () {
        $dsTrongLop.find("input[name='sinhVienIds']").prop("checked", false);
        capNhatChon($frmChuyen, $demTrongLop, demHienTrongLop);
    });

    // Chuyển sinh viên đi: bắt buộc chọn lớp đích và xác nhận
    $frmChuyen.on("submit", function (e) {
        var soChon = $frmChuyen.find("input[name='sinhVienIds']:checked").length;
        if (soChon === 0) {
            e.preventDefault();
            return;
        }

        var $lopDich = $("#lopDichId");
        if (!$lopDich.val()) {
            e.preventDefault();
            toastr.warning("Vui lòng chọn lớp cần chuyển sinh viên sang");
            $lopDich.trigger("focus");
            return;
        }

        if (this.dataset.daXacNhan === "1") {
            return; // đã xác nhận -> cho submit
        }

        e.preventDefault();
        var form = this;
        var tenLopDich = $lopDich.find("option:selected").text();
        Swal.fire({
            title: "Chuyển " + soChon + " sinh viên?",
            text: "Sinh viên sẽ chuyển sang lớp " + tenLopDich + " và đổi khoa theo lớp mới.",
            icon: "question",
            showCancelButton: true,
            confirmButtonText: "Chuyển",
            cancelButtonText: "Hủy",
            cancelButtonColor: "#6c757d",
            reverseButtons: true
        }).then(function (result) {
            if (result.isConfirmed) {
                form.dataset.daXacNhan = "1";
                form.submit();
            }
        });
    });

    // ---------- Cột trái: ứng viên từ lớp khác ----------
    var $dsUngVien = $("#dsUngVien");
    var $demUngVien = $("#demUngVien");
    var ungVien = [];
    var daChon = {}; // id -> true, giữ lựa chọn khi đổi bộ lọc

    function demHienUngVien() {
        return $dsUngVien.find(".dong-sv").length;
    }

    // Lựa chọn của cột ứng viên nằm trong daChon, không chỉ trong DOM
    function capNhatChonUngVien() {
        capNhatChon($frmThem, $demUngVien, demHienUngVien, Object.keys(daChon).length);
    }

    function veUngVien() {
        var tu = $("#timUngVien").val().trim().toLowerCase();
        var khoaId = $("#locKhoa").val();
        var locLopId = $("#locLop").val();

        var html = "";
        var soHien = 0;
        ungVien.forEach(function (sv) {
            if (khoaId && String(sv.khoaId) !== khoaId) { return; }
            if (locLopId && String(sv.lopId) !== locLopId) { return; }
            if (tu && (sv.maSinhVien + " " + sv.hoTen + " " + sv.email).toLowerCase().indexOf(tu) < 0) { return; }

            soHien++;
            html += '<label class="list-group-item d-flex gap-2 align-items-start dong-sv">' +
                        '<input type="checkbox" name="sinhVienIds" value="' + sv.id + '"' +
                            ' class="form-check-input mt-1 flex-shrink-0"' + (daChon[sv.id] ? " checked" : "") + ' />' +
                        '<span class="flex-grow-1">' +
                            '<span class="fw-semibold">' + escapeHtml(sv.maSinhVien) + '</span> – ' + escapeHtml(sv.hoTen) +
                            ' <span class="badge bg-light text-dark border ms-1">' + escapeHtml(sv.gioiTinh) + '</span>' +
                            '<br /><small class="text-muted">' +
                                '<i class="bi bi-people"></i> ' + escapeHtml(sv.maLop) +
                                ' · ' + escapeHtml(sv.tenKhoa) +
                                ' · ' + escapeHtml(sv.email) +
                            '</small>' +
                        '</span>' +
                    '</label>';
        });

        if (soHien === 0) {
            html = '<div class="list-group-item text-muted"><i class="bi bi-info-circle"></i> ' +
                   (ungVien.length === 0
                        ? "Không có sinh viên nào ở lớp khác."
                        : "Không tìm thấy sinh viên phù hợp bộ lọc.") +
                   "</div>";
        }
        $dsUngVien.html(html);
        capNhatChonUngVien();
    }

    // Ô lọc Khoa / Lớp dựng từ chính dữ liệu ứng viên
    function dungBoLoc() {
        var khoas = {};
        var lops = {};
        ungVien.forEach(function (sv) {
            khoas[sv.khoaId] = sv.tenKhoa;
            lops[sv.lopId] = sv.maLop + " – " + sv.tenLop;
        });

        function nap($select, map, nhan) {
            var options = '<option value="">' + nhan + "</option>";
            Object.keys(map)
                .sort(function (a, b) { return map[a].localeCompare(map[b], "vi"); })
                .forEach(function (id) {
                    options += '<option value="' + id + '">' + escapeHtml(map[id]) + "</option>";
                });
            $select.html(options);
        }

        nap($("#locKhoa"), khoas, "Tất cả khoa");
        nap($("#locLop"), lops, "Tất cả lớp");
    }

    // Chỉ còn các lớp của khoa đang lọc trong ô "Lớp"
    $("#locKhoa").on("change", function () {
        var khoaId = $(this).val();
        var $locLop = $("#locLop");
        var dangChon = $locLop.val();
        var lops = {};
        ungVien.forEach(function (sv) {
            if (!khoaId || String(sv.khoaId) === khoaId) {
                lops[sv.lopId] = sv.maLop + " – " + sv.tenLop;
            }
        });

        var options = '<option value="">Tất cả lớp</option>';
        Object.keys(lops)
            .sort(function (a, b) { return lops[a].localeCompare(lops[b], "vi"); })
            .forEach(function (id) {
                options += '<option value="' + id + '">' + escapeHtml(lops[id]) + "</option>";
            });
        $locLop.html(options);
        if (dangChon && lops[dangChon]) {
            $locLop.val(dangChon);
        }
        veUngVien();
    });

    $("#timUngVien").on("input", veUngVien);
    $("#locLop").on("change", veUngVien);

    // Lựa chọn phải sống sót khi danh sách được vẽ lại
    $dsUngVien.on("change", "input[name='sinhVienIds']", function () {
        if (this.checked) {
            daChon[this.value] = true;
        } else {
            delete daChon[this.value];
        }
        capNhatChonUngVien();
    });

    $("#chonTatCaUngVien").on("click", function () {
        $dsUngVien.find("input[name='sinhVienIds']").each(function () {
            this.checked = true;
            daChon[this.value] = true;
        });
        capNhatChonUngVien();
    });

    $("#boChonUngVien").on("click", function () {
        daChon = {};
        $dsUngVien.find("input[name='sinhVienIds']").prop("checked", false);
        capNhatChonUngVien();
    });

    // Gửi tất cả id đã chọn (kể cả những dòng đang bị bộ lọc ẩn)
    $frmThem.on("submit", function (e) {
        var ids = Object.keys(daChon);
        if (ids.length === 0) {
            e.preventDefault();
            return;
        }

        if (this.dataset.daXacNhan === "1") {
            return;
        }

        e.preventDefault();
        var form = this;
        var khacKhoa = ungVien.filter(function (sv) {
            return daChon[sv.id] && sv.khoaId !== khoaIdCuaLop;
        }).length;

        Swal.fire({
            title: "Thêm " + ids.length + " sinh viên vào lớp?",
            text: khacKhoa > 0
                ? khacKhoa + " sinh viên đang thuộc khoa khác và sẽ được đổi khoa theo lớp này."
                : "Sinh viên sẽ được chuyển khỏi lớp hiện tại của họ.",
            icon: "question",
            showCancelButton: true,
            confirmButtonText: "Thêm vào lớp",
            cancelButtonText: "Hủy",
            cancelButtonColor: "#6c757d",
            reverseButtons: true
        }).then(function (result) {
            if (!result.isConfirmed) {
                return;
            }

            // Dòng bị bộ lọc ẩn không có ô checkbox trong DOM -> gửi kèm hidden input
            $("#hiddenUngVien").remove();
            var $hidden = $('<div id="hiddenUngVien" class="d-none"></div>');
            ids.forEach(function (id) {
                if (form.querySelector("input[name='sinhVienIds'][value='" + id + "']:checked") === null) {
                    $hidden.append($('<input type="hidden" name="sinhVienIds" />').val(id));
                }
            });
            $(form).append($hidden);

            form.dataset.daXacNhan = "1";
            form.submit();
        });
    });

    // Tải ứng viên
    $.getJSON("/Admin/Lop/GetSinhVienNgoaiLop", { lopId: lopId })
        .done(function (res) {
            ungVien = (res && res.data) || [];
            dungBoLoc();
            veUngVien();
        })
        .fail(function () {
            $dsUngVien.html('<div class="list-group-item text-danger">' +
                '<i class="bi bi-exclamation-triangle"></i> Không tải được danh sách sinh viên. Vui lòng tải lại trang.</div>');
            toastr.error("Không tải được danh sách sinh viên ở lớp khác");
        });

    // Hiện số đếm ban đầu của cột "sinh viên trong lớp"
    locTrongLop();
});
