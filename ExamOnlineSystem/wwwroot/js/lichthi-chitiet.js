// Chi tiết lịch thi: bảng sinh viên dự thi (DataTable + lọc theo lớp) và nút Xóa lịch thi
$(function () {
    var $table = $("#tblSinhVienDuThi");
    if ($table.length) {
        var dataTable = $table.DataTable({
            order: [[5, "asc"], [1, "asc"]],
            columnDefs: [{ targets: 0, orderable: false, searchable: false }],
            pageLength: 25,
            lengthMenu: [10, 25, 50, 100],
            language: {
                search: "Tìm kiếm:",
                lengthMenu: "Hiển thị _MENU_ dòng",
                info: "Hiển thị _START_ - _END_ trong tổng số _TOTAL_ sinh viên",
                infoEmpty: "Không có dữ liệu",
                infoFiltered: "(lọc từ _MAX_ sinh viên)",
                zeroRecords: "Không tìm thấy sinh viên phù hợp",
                emptyTable: "Chưa có sinh viên nào",
                paginate: { first: "Đầu", last: "Cuối", next: "Sau", previous: "Trước" }
            }
        });

        // STT luôn đánh lại theo thứ tự đang hiển thị (sau sắp xếp / lọc)
        dataTable.on("draw", function () {
            var start = dataTable.page.info().start;
            dataTable.column(0, { search: "applied", order: "applied", page: "current" }).nodes().each(function (cell, i) {
                cell.textContent = start + i + 1;
            });
        }).draw();

        // Lọc đúng nguyên mã lớp (cột 5) và trạng thái (cột 8)
        $("#locLopSv").on("change", function () {
            var regex = this.value ? "^" + $.fn.dataTable.util.escapeRegex(this.value) + "$" : "";
            dataTable.column(5).search(regex, true, false).draw();
        });
        $("#locTrangThaiSv").on("change", function () {
            var regex = this.value ? "^" + $.fn.dataTable.util.escapeRegex(this.value) + "$" : "";
            dataTable.column(8).search(regex, true, false).draw();
        });
    }

    // ===== Thiết lập thời gian (modal) =====
    var $modal = $("#modalThoiGian");
    if ($modal.length) {
        var thoiLuong = parseInt($modal.data("thoi-luong"), 10) || 0;
        var dangDienRa = String($modal.data("dang-dien-ra")) === "1";
        var p2 = function (n) { return String(n).padStart(2, "0"); };

        // "dd/MM/yyyy" -> Date (null nếu sai, kể cả 31/02)
        var docNgay = function (s) {
            var m = /^(\d{1,2})\/(\d{1,2})\/(\d{4})$/.exec($.trim(s || ""));
            if (!m) return null;
            var d = new Date(+m[3], +m[2] - 1, +m[1]);
            return d.getDate() === +m[1] && d.getMonth() === +m[2] - 1 ? d : null;
        };
        var vietNgay = function (d) { return p2(d.getDate()) + "/" + p2(d.getMonth() + 1) + "/" + d.getFullYear(); };

        // Đọc / ghi bộ ô (ngày + giờ + phút) của "bd" hoặc "kt"
        var doc = function (ma) {
            var d = docNgay($("#" + ma + "Ngay").val());
            if (!d) return null;
            d.setHours(+$("#" + ma + "Gio").val(), +$("#" + ma + "Phut").val(), 0, 0);
            return d;
        };
        var chuoi = function (ma) {
            return $.trim($("#" + ma + "Ngay").val()) + " " + $("#" + ma + "Gio").val() + ":" + $("#" + ma + "Phut").val();
        };
        var ghi = function (ma, d) {
            $("#" + ma + "Ngay").val(vietNgay(d));
            $("#" + ma + "Gio").val(p2(d.getHours()));
            var $phut = $("#" + ma + "Phut"), phut = p2(d.getMinutes());
            if ($phut.find('option[value="' + phut + '"]').length === 0) {
                // Phút lẻ (không chia hết cho 5): thêm vào danh sách để không bị làm tròn ngầm
                $phut.append($("<option>").val(phut).text(phut));
                $phut.find("option").sort(function (a, b) { return a.value - b.value; }).appendTo($phut);
            }
            $phut.val(phut);
        };
        var hienThi = function (d) {
            return vietNgay(d) + " " + p2(d.getHours()) + ":" + p2(d.getMinutes());
        };

        var tomTat = function () {
            var $tt = $("#tgTomTat").removeClass("text-danger text-success text-muted");
            $(".tg-ngay").each(function () {
                $(this).toggleClass("is-invalid", !!$.trim(this.value) && !docNgay(this.value));
            });
            var bd = doc("bd"), kt = doc("kt");
            if (!bd || !kt) {
                $tt.addClass("text-danger").text("Ngày phải đúng định dạng dd/MM/yyyy.");
                return false;
            }
            var khung = Math.round((kt - bd) / 60000);
            var bayGio = new Date();
            var loi = null;
            if (khung <= 0) loi = "Giờ kết thúc phải sau giờ bắt đầu.";
            else if (thoiLuong && khung < thoiLuong) loi = "Khung giờ " + khung + " phút ngắn hơn thời gian làm bài " + thoiLuong + " phút.";
            else if (dangDienRa && kt <= bayGio) loi = "Giờ kết thúc mới phải sau thời điểm hiện tại.";
            else if (!dangDienRa && bd < new Date(bayGio.getTime() - 5 * 60000)) loi = "Thời gian bắt đầu đã qua.";
            if (loi) {
                $tt.addClass("text-danger").html('<i class="bi bi-exclamation-circle"></i> ' + loi);
                return false;
            }
            var cungNgay = bd.toDateString() === kt.toDateString();
            $tt.addClass("text-success").html('<i class="bi bi-check-circle"></i> ' +
                "Thi từ <strong>" + hienThi(bd) + "</strong> đến <strong>" + (cungNgay ? p2(kt.getHours()) + ":" + p2(kt.getMinutes()) : hienThi(kt)) +
                "</strong> · khung giờ " + khung + " phút" +
                (thoiLuong && khung > thoiLuong ? " (vào muộn tối đa " + (khung - thoiLuong) + " phút)" : "") + ".");
            return true;
        };

        // Gõ ngày: tự chèn dấu "/" (ddMMyyyy -> dd/MM/yyyy)
        $modal.on("input", ".tg-ngay", function () {
            var so = this.value.replace(/\D/g, "").substring(0, 8);
            if (/^\d{1,2}\/\d{1,2}\/\d{0,4}$/.test(this.value)) { tomTat(); return; }
            this.value = so.length > 4 ? so.substring(0, 2) + "/" + so.substring(2, 4) + "/" + so.substring(4)
                       : so.length > 2 ? so.substring(0, 2) + "/" + so.substring(2) : so;
            tomTat();
        });
        $modal.on("change", ".tg-gio, .tg-phut", tomTat);

        // Nút lịch: mở bộ chọn ngày của trình duyệt, kết quả ghi về ô dd/MM/yyyy
        $modal.on("click", ".tg-lich", function () {
            var ma = $(this).data("for");
            var $goc = $modal.find('.tg-lich-goc[data-for="' + ma + '"]');
            var d = docNgay($("#" + ma).val());
            $goc.val(d ? d.getFullYear() + "-" + p2(d.getMonth() + 1) + "-" + p2(d.getDate()) : "");
            try { $goc[0].showPicker(); } catch (e) { $goc[0].focus(); $goc[0].click(); }
        });
        $modal.on("change", ".tg-lich-goc", function () {
            if (!this.value) return;
            var t = this.value.split("-");
            $("#" + $(this).data("for")).val(t[2] + "/" + t[1] + "/" + t[0]);
            tomTat();
        });

        // Giữ nguyên độ dài khung giờ khi đổi giờ bắt đầu bằng nút chọn nhanh
        var doiBatDau = function (moi) {
            var bd = doc("bd"), kt = doc("kt");
            var khung = bd && kt && kt > bd ? kt - bd : thoiLuong * 60000;
            ghi("bd", moi);
            if (khung > 0) ghi("kt", new Date(moi.getTime() + khung));
            tomTat();
        };
        $modal.on("click", ".tg-ngay-nhanh", function () {
            var d = new Date(), bd = doc("bd");
            d.setDate(d.getDate() + (+$(this).data("cong")));
            d.setHours(bd ? bd.getHours() : 7, bd ? bd.getMinutes() : 30, 0, 0);
            doiBatDau(d);
        });
        $modal.on("click", ".tg-ca", function () {
            var bd = doc("bd") || new Date(), t = String($(this).data("gio")).split(":");
            var d = new Date(bd);
            d.setHours(+t[0], +t[1], 0, 0);
            doiBatDau(d);
        });
        $modal.on("click", ".tg-kt-cong", function () {
            var bd = doc("bd");
            if (!bd) { tomTat(); return; }
            ghi("kt", new Date(bd.getTime() + (thoiLuong + (+$(this).data("phut"))) * 60000));
            tomTat();
        });

        $modal.on("shown.bs.modal", function () { $("#tgLoi").addClass("d-none").empty(); tomTat(); });

        $("#btnLuuThoiGian").on("click", function () {
            if (!tomTat()) return;
            var $btn = $(this).prop("disabled", true);
            $.ajax({
                url: "/Admin/LichThi/ThietLapThoiGian",
                type: "POST",
                data: {
                    id: $modal.data("id"),
                    batDau: chuoi("bd"),
                    ketThuc: chuoi("kt"),
                    __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
                },
                success: function (res) {
                    if (res.success) {
                        window.location.reload(); // thông báo hiển thị qua TempData
                        return;
                    }
                    var $ul = $("<ul class='mb-0 ps-3'>");
                    (res.errors || ["Không thể lưu thời gian"]).forEach(function (e) { $ul.append($("<li>").text(e)); });
                    $("#tgLoi").removeClass("d-none").empty().append($ul);
                    $btn.prop("disabled", false);
                },
                error: function () {
                    $("#tgLoi").removeClass("d-none").text("Có lỗi xảy ra khi lưu thời gian.");
                    $btn.prop("disabled", false);
                }
            });
        });
    }

    // Xóa lịch thi (chỉ hiện khi lịch "Sắp diễn ra" và người dùng có quyền)
    $("#btnXoaLichThi").on("click", function () {
        var id = $(this).data("id");
        var ten = $(this).data("ten");

        Swal.fire({
            title: "Xác nhận xóa",
            text: 'Bạn có chắc chắn muốn xóa lịch thi "' + ten + '" không? Thao tác này không thể hoàn tác.',
            icon: "warning",
            showCancelButton: true,
            confirmButtonColor: "#d33",
            cancelButtonColor: "#6c757d",
            confirmButtonText: "Có, xóa ngay!",
            cancelButtonText: "Hủy"
        }).then(function (result) {
            if (!result.isConfirmed) {
                return;
            }
            $.ajax({
                url: "/Admin/LichThi/Delete/" + id,
                type: "DELETE",
                headers: { "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val() },
                success: function (res) {
                    if (res.success) {
                        Swal.fire("Thành công!", res.message, "success").then(function () {
                            window.location.href = "/Admin/LichThi";
                        });
                    } else {
                        Swal.fire("Lỗi!", res.message || "Không thể xóa lịch thi", "error");
                    }
                },
                error: function () {
                    Swal.fire("Lỗi!", "Có lỗi xảy ra khi xóa lịch thi", "error");
                }
            });
        });
    });
});
