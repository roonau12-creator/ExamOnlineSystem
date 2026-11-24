// Trang Phân công giảng viên, sinh viên (/Admin/LopHocPhan/PhanCong/{id})
// - Tab Giảng viên: cột trái là ứng viên tải qua GetGiaoVienNgoaiLop, cột phải là người đã phân công (Razor render)
// - Tab Sinh viên: cột trái là ứng viên tải qua GetSinhVienNgoaiLop, cột phải là sinh viên trong lớp (Razor render)
$(function () {
    var $frmThemGV = $("#frmThemGV");
    if ($frmThemGV.length === 0) {
        return;
    }

    var lopHocPhanId = Number($frmThemGV.find("input[name='lopHocPhanId']").val());

    function escapeHtml(value) {
        return $("<div>").text(value == null ? "" : value).html();
    }

    // Nút submit có thể nằm ngoài form (dùng thuộc tính form="..."), nên phải tìm cả 2 nơi
    function nutLuu($form) {
        var id = $form.attr("id");
        return $form.find(".btn-luu").add($(".btn-luu[form='" + id + "']"));
    }

    // Bật/tắt nút lưu + cập nhật số đã chọn của một form
    // $dem / moTa / tongHien chỉ cần cho các cột có bộ lọc; soChon truyền vào khi
    // lựa chọn được giữ ngoài DOM (cột ứng viên), bỏ trống thì đếm các ô đang tích.
    function capNhatChon($form, tenInput, $dem, moTa, tongHien, soChon) {
        if (soChon === undefined) {
            soChon = $form.find("input[name='" + tenInput + "']:checked").length;
        }
        var $nut = nutLuu($form);
        $nut.find(".so-chon").text(soChon);
        $nut.prop("disabled", soChon === 0);
        if ($dem && $dem.length && tongHien) {
            $dem.text("Đang hiện " + tongHien() + " " + moTa + " · đã chọn " + soChon);
        }
    }

    // ---------- Cột "đã có trong lớp": lọc theo từ khóa trên dữ liệu Razor đã render ----------
    // Dùng cho danh sách sinh viên trong lớp học phần (mỗi dòng có class .dong-sv và data-tim)
    function ganLocDanhSachTinh($form, $ds, $tim, $dem, moTa, tenInput, idChonTatCa, idBoChon) {
        function demHien() {
            return $ds.find(".dong-sv:visible").length;
        }

        function loc() {
            var tu = ($tim.val() || "").trim().toLowerCase();
            $ds.find(".dong-sv").each(function () {
                var khop = tu === "" || this.dataset.tim.indexOf(tu) >= 0;
                $(this).toggle(khop);
                if (!khop) {
                    // Dòng bị ẩn thì bỏ chọn để không gửi lên server ngoài ý muốn
                    $(this).find("input[name='" + tenInput + "']").prop("checked", false);
                }
            });
            capNhatChon($form, tenInput, $dem, moTa, demHien);
        }

        $tim.on("input", loc);
        $ds.on("change", "input[name='" + tenInput + "']", function () {
            capNhatChon($form, tenInput, $dem, moTa, demHien);
        });
        $(idChonTatCa).on("click", function () {
            $ds.find(".dong-sv:visible input[name='" + tenInput + "']").prop("checked", true);
            capNhatChon($form, tenInput, $dem, moTa, demHien);
        });
        $(idBoChon).on("click", function () {
            $ds.find("input[name='" + tenInput + "']").prop("checked", false);
            capNhatChon($form, tenInput, $dem, moTa, demHien);
        });

        loc(); // hiện số đếm ban đầu
    }

    // ---------- Cột ứng viên tải qua Ajax: giữ lựa chọn ngoài DOM để sống sót khi đổi bộ lọc ----------
    // cauHinh: { form, ds, dem, moTa, tenInput, url, veDong, locDong, dungBoLoc, xacNhan }
    function taoCotUngVien(cauHinh) {
        var $form = cauHinh.form;
        var $ds = cauHinh.ds;
        var $dem = cauHinh.dem;
        var danhSach = [];
        var daChon = {}; // id -> true

        function demHien() {
            return $ds.find(".dong-chon").length;
        }

        function capNhat() {
            capNhatChon($form, cauHinh.tenInput, $dem, cauHinh.moTa, demHien, Object.keys(daChon).length);
        }

        function ve() {
            var html = "";
            var soHien = 0;
            danhSach.forEach(function (item) {
                if (!cauHinh.locDong(item)) {
                    return;
                }
                soHien++;
                html += '<label class="list-group-item d-flex gap-2 align-items-start dong-chon">' +
                            '<input type="checkbox" name="' + cauHinh.tenInput + '" value="' + item.id + '"' +
                                ' class="form-check-input mt-1 flex-shrink-0"' + (daChon[item.id] ? " checked" : "") + " />" +
                            '<span class="flex-grow-1">' + cauHinh.veDong(item) + "</span>" +
                        "</label>";
            });

            if (soHien === 0) {
                html = '<div class="list-group-item text-muted"><i class="bi bi-info-circle"></i> ' +
                       (danhSach.length === 0 ? cauHinh.trongRong : "Không tìm thấy " + cauHinh.moTa + " phù hợp bộ lọc.") +
                       "</div>";
            }
            $ds.html(html);
            capNhat();
        }

        $ds.on("change", "input[name='" + cauHinh.tenInput + "']", function () {
            if (this.checked) {
                daChon[this.value] = true;
            } else {
                delete daChon[this.value];
            }
            capNhat();
        });

        $(cauHinh.idChonTatCa).on("click", function () {
            $ds.find("input[name='" + cauHinh.tenInput + "']").each(function () {
                this.checked = true;
                daChon[this.value] = true;
            });
            capNhat();
        });

        $(cauHinh.idBoChon).on("click", function () {
            daChon = {};
            $ds.find("input[name='" + cauHinh.tenInput + "']").prop("checked", false);
            capNhat();
        });

        // Gửi tất cả id đã chọn, kể cả dòng đang bị bộ lọc ẩn
        $form.on("submit", function (e) {
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
            var hopLe = cauHinh.xacNhan(ids, danhSach, daChon);
            if (hopLe === false) {
                return;
            }

            Swal.fire(hopLe).then(function (result) {
                if (!result.isConfirmed) {
                    return;
                }

                // Dòng bị bộ lọc ẩn không có checkbox trong DOM -> gửi kèm hidden input
                $form.find(".hidden-chon").remove();
                var $hidden = $('<div class="hidden-chon d-none"></div>');
                ids.forEach(function (id) {
                    if (form.querySelector("input[name='" + cauHinh.tenInput + "'][value='" + id + "']:checked") === null) {
                        $hidden.append($('<input type="hidden" name="' + cauHinh.tenInput + '" />').val(id));
                    }
                });
                $form.append($hidden);

                form.dataset.daXacNhan = "1";
                form.submit();
            });
        });

        $.getJSON(cauHinh.url, { lopHocPhanId: lopHocPhanId })
            .done(function (res) {
                danhSach = (res && res.data) || [];
                cauHinh.dungBoLoc(danhSach, ve);
                ve();
            })
            .fail(function () {
                $ds.html('<div class="list-group-item text-danger">' +
                    '<i class="bi bi-exclamation-triangle"></i> Không tải được danh sách. Vui lòng tải lại trang.</div>');
                toastr.error("Không tải được danh sách " + cauHinh.moTa);
            });

        return { ve: ve };
    }

    // Nạp option cho một ô lọc từ map { value: text }
    function napOption($select, map, nhan) {
        var options = '<option value="">' + nhan + "</option>";
        Object.keys(map)
            .sort(function (a, b) { return map[a].localeCompare(map[b], "vi"); })
            .forEach(function (key) {
                options += '<option value="' + key + '">' + escapeHtml(map[key]) + "</option>";
            });
        $select.html(options);
    }

    // ===================== TAB GIẢNG VIÊN =====================

    taoCotUngVien({
        form: $frmThemGV,
        ds: $("#dsGV"),
        dem: $("#demGV"),
        moTa: "giảng viên",
        trongRong: "Mọi giảng viên đều đã được phân công vào lớp học phần này.",
        tenInput: "giaoVienIds",
        url: "/Admin/LopHocPhan/GetGiaoVienNgoaiLop",
        idChonTatCa: "#chonTatCaGV",
        idBoChon: "#boChonGV",
        locDong: function (gv) {
            var tu = ($("#timGV").val() || "").trim().toLowerCase();
            var khoaId = $("#locKhoaGV").val();
            var boMonId = $("#locBoMonGV").val();
            if (khoaId && String(gv.khoaId) !== khoaId) { return false; }
            if (boMonId && String(gv.boMonId) !== boMonId) { return false; }
            if (tu && (gv.maGiaoVien + " " + gv.hoTen + " " + gv.email).toLowerCase().indexOf(tu) < 0) { return false; }
            return true;
        },
        veDong: function (gv) {
            return '<span class="fw-semibold">' + escapeHtml(gv.maGiaoVien) + "</span> – " + escapeHtml(gv.hoTen) +
                   '<br /><small class="text-muted">' +
                       '<i class="bi bi-diagram-3"></i> ' + escapeHtml(gv.tenBoMon) +
                       " · " + escapeHtml(gv.tenKhoa) +
                       " · đang dạy " + gv.soLopHocPhan + " lớp HP" +
                   "</small>";
        },
        dungBoLoc: function (danhSach, ve) {
            var khoas = {};
            var boMons = {};
            danhSach.forEach(function (gv) {
                khoas[gv.khoaId] = gv.tenKhoa;
                boMons[gv.boMonId] = gv.tenBoMon;
            });
            napOption($("#locKhoaGV"), khoas, "Tất cả khoa");
            napOption($("#locBoMonGV"), boMons, "Tất cả bộ môn");

            // Đổi khoa thì ô bộ môn chỉ còn bộ môn của khoa đó
            $("#locKhoaGV").on("change", function () {
                var khoaId = $(this).val();
                var dangChon = $("#locBoMonGV").val();
                var loc = {};
                danhSach.forEach(function (gv) {
                    if (!khoaId || String(gv.khoaId) === khoaId) {
                        loc[gv.boMonId] = gv.tenBoMon;
                    }
                });
                napOption($("#locBoMonGV"), loc, "Tất cả bộ môn");
                if (dangChon && loc[dangChon]) {
                    $("#locBoMonGV").val(dangChon);
                }
                ve();
            });
            $("#timGV").on("input", ve);
            $("#locBoMonGV").on("change", ve);
        },
        xacNhan: function (ids) {
            var laPhuTrach = $("#vaiTroGV").val() === "1";
            if (laPhuTrach && ids.length > 1) {
                toastr.warning("Mỗi lớp học phần chỉ có 1 giảng viên phụ trách. Hãy chọn 1 người hoặc đổi vai trò sang Trợ giảng.");
                return false;
            }
            return {
                title: "Phân công " + ids.length + " giảng viên?",
                text: laPhuTrach
                    ? "Giảng viên sẽ được phân công làm người phụ trách lớp học phần."
                    : "Giảng viên sẽ được phân công làm trợ giảng của lớp học phần.",
                icon: "question",
                showCancelButton: true,
                confirmButtonText: "Phân công",
                cancelButtonText: "Hủy",
                cancelButtonColor: "#6c757d",
                reverseButtons: true
            };
        }
    });

    // Bỏ phân công giảng viên: xác nhận trước khi gửi
    $("#frmBoGV").on("submit", function (e) {
        var soChon = $(this).find("input[name='giaoVienIds']:checked").length;
        if (soChon === 0) {
            e.preventDefault();
            return;
        }
        if (this.dataset.daXacNhan === "1") {
            return;
        }
        e.preventDefault();
        var form = this;
        Swal.fire({
            title: "Bỏ phân công " + soChon + " giảng viên?",
            text: "Giảng viên sẽ không còn phụ trách lớp học phần này nữa.",
            icon: "warning",
            showCancelButton: true,
            confirmButtonColor: "#d33",
            confirmButtonText: "Bỏ phân công",
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

    // Cột giảng viên đã phân công không có bộ lọc, chỉ cần đếm số đang chọn
    function demChonGV() {
        capNhatChon($("#frmBoGV"), "giaoVienIds");
    }
    $("#dsGVTrongLop").on("change", "input[name='giaoVienIds']", demChonGV);
    demChonGV();

    // ===================== TAB SINH VIÊN =====================

    var $frmThemSV = $("#frmThemSV");
    var conLai = Number($frmThemSV.attr("data-con-lai"));

    taoCotUngVien({
        form: $frmThemSV,
        ds: $("#dsSV"),
        dem: $("#demSV"),
        moTa: "sinh viên",
        trongRong: "Mọi sinh viên đều đã có trong lớp học phần này.",
        tenInput: "sinhVienIds",
        url: "/Admin/LopHocPhan/GetSinhVienNgoaiLop",
        idChonTatCa: "#chonTatCaSV",
        idBoChon: "#boChonSV",
        locDong: function (sv) {
            var tu = ($("#timSV").val() || "").trim().toLowerCase();
            var khoaId = $("#locKhoaSV").val();
            var lopId = $("#locLopSV").val();
            if (khoaId && String(sv.khoaId) !== khoaId) { return false; }
            if (lopId && String(sv.lopId) !== lopId) { return false; }
            if (tu && (sv.maSinhVien + " " + sv.hoTen + " " + sv.email).toLowerCase().indexOf(tu) < 0) { return false; }
            return true;
        },
        veDong: function (sv) {
            return '<span class="fw-semibold">' + escapeHtml(sv.maSinhVien) + "</span> – " + escapeHtml(sv.hoTen) +
                   ' <span class="badge bg-light text-dark border ms-1">' + escapeHtml(sv.gioiTinh) + "</span>" +
                   '<br /><small class="text-muted">' +
                       '<i class="bi bi-people"></i> ' + escapeHtml(sv.maLop) +
                       " · " + escapeHtml(sv.tenKhoa) +
                       " · " + escapeHtml(sv.email) +
                   "</small>";
        },
        dungBoLoc: function (danhSach, ve) {
            var khoas = {};
            var lops = {};
            danhSach.forEach(function (sv) {
                khoas[sv.khoaId] = sv.tenKhoa;
                lops[sv.lopId] = sv.maLop + " – " + sv.tenLop;
            });
            napOption($("#locKhoaSV"), khoas, "Tất cả khoa");
            napOption($("#locLopSV"), lops, "Tất cả lớp");

            // Đổi khoa thì ô lớp chỉ còn lớp của khoa đó
            $("#locKhoaSV").on("change", function () {
                var khoaId = $(this).val();
                var dangChon = $("#locLopSV").val();
                var loc = {};
                danhSach.forEach(function (sv) {
                    if (!khoaId || String(sv.khoaId) === khoaId) {
                        loc[sv.lopId] = sv.maLop + " – " + sv.tenLop;
                    }
                });
                napOption($("#locLopSV"), loc, "Tất cả lớp");
                if (dangChon && loc[dangChon]) {
                    $("#locLopSV").val(dangChon);
                }
                ve();
            });
            $("#timSV").on("input", ve);
            $("#locLopSV").on("change", ve);
        },
        xacNhan: function (ids) {
            // Chặn sớm khi vượt sĩ số (server vẫn kiểm tra lại)
            if (ids.length > conLai) {
                toastr.warning("Lớp học phần chỉ còn " + Math.max(conLai, 0) + " suất nhưng đang chọn " +
                               ids.length + " sinh viên.");
                return false;
            }
            return {
                title: "Thêm " + ids.length + " sinh viên vào lớp học phần?",
                text: "Sinh viên vẫn giữ nguyên lớp sinh hoạt và khoa hiện tại.",
                icon: "question",
                showCancelButton: true,
                confirmButtonText: "Thêm vào lớp",
                cancelButtonText: "Hủy",
                cancelButtonColor: "#6c757d",
                reverseButtons: true
            };
        }
    });

    // Cột phải: sinh viên trong lớp học phần (Razor render sẵn) + nút Bỏ
    ganLocDanhSachTinh($("#frmBoSV"), $("#dsSVTrongLop"), $("#timSVTrongLop"), $("#demSVTrongLop"),
        "sinh viên", "sinhVienIds", "#chonTatCaSVTrongLop", "#boChonSVTrongLop");

    $("#frmBoSV").on("submit", function (e) {
        var soChon = $(this).find("input[name='sinhVienIds']:checked").length;
        if (soChon === 0) {
            e.preventDefault();
            return;
        }
        if (this.dataset.daXacNhan === "1") {
            return;
        }
        e.preventDefault();
        var form = this;
        Swal.fire({
            title: "Bỏ " + soChon + " sinh viên khỏi lớp học phần?",
            text: "Sinh viên chỉ bị bỏ khỏi lớp học phần này, không bị xóa khỏi hệ thống.",
            icon: "warning",
            showCancelButton: true,
            confirmButtonColor: "#d33",
            confirmButtonText: "Bỏ khỏi lớp",
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
});
