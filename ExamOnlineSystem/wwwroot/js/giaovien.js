// Danh sách giảng viên: DataTable tải dữ liệu qua Ajax + lọc theo khoa / bộ môn (theo id, ghi lên URL) / trạng thái
$(function () {
    var $table = $("#tblGiaoVien");
    if ($table.length === 0) {
        return;
    }

    function escapeHtml(value) {
        return $("<div>").text(value == null ? "" : value).html();
    }

    var laAdmin = $table.data("admin") === true;

    function text(d, type) {
        return type === "display" ? escapeHtml(d) : d;
    }

    // Lọc theo khoa / bộ môn: so theo id; chỉ áp dụng cho bảng giảng viên
    var $locKhoa = $("#locKhoa"), $locBoMon = $("#locBoMon");
    $.fn.dataTable.ext.search.push(function (settings, data, dataIndex, row) {
        if (settings.nTable !== $table[0]) {
            return true;
        }
        var khoaId = $locKhoa.val(), boMonId = $locBoMon.val();
        return (!khoaId || String(row.khoaId) === khoaId) && (!boMonId || String(row.boMonId) === boMonId);
    });

    // Lọc đúng nguyên giá trị của cột (không lọc theo chuỗi con)
    function locCot(cot, giaTri) {
        var regex = giaTri ? "^" + $.fn.dataTable.util.escapeRegex(giaTri) + "$" : "";
        dataTable.column(cot).search(regex, true, false).draw();
    }

    var dataTable = $table.DataTable({
        ajax: { url: "/Admin/GiaoVien/GetAllGiaoVien", dataSrc: "data" },
        columns: [
            { data: "maGiaoVien", render: function (d, type) { return type === "display" ? '<span class="fw-semibold">' + escapeHtml(d) + '</span>' : d; } },
            {
                data: "hoTen",
                render: function (d, type, row) {
                    if (type !== "display") {
                        return d;
                    }
                    return escapeHtml(d) + (row.chucDanh ? '<div class="small text-muted">' + escapeHtml(row.chucDanh) + '</div>' : '');
                }
            },
            {
                data: "email",
                render: function (d, type) {
                    return type === "display" && d ? '<a href="mailto:' + escapeHtml(d) + '">' + escapeHtml(d) + '</a>' : d;
                }
            },
            { data: "soDienThoai", render: text },
            {
                data: "tenBoMon",
                render: function (d, type, row) {
                    if (type !== "display" || !laAdmin) {
                        return text(d, type);
                    }
                    return '<a href="/Admin/BoMon/Details/' + row.boMonId + '" title="Xem chi tiết bộ môn">' + escapeHtml(d) + '</a>';
                }
            },
            { data: "tenKhoa", render: text },
            { data: "soCauHoi", className: "text-center" },
            { data: "soDeThi", className: "text-center" },
            {
                data: "trangThai",
                className: "text-center text-nowrap",
                render: function (d, type, row) {
                    if (type !== "display") {
                        return d;
                    }
                    return row.dangHoatDong
                        ? '<span class="badge text-bg-success">Đang hoạt động</span>'
                        : '<span class="badge text-bg-secondary"><i class="bi bi-lock-fill"></i> Đã khóa</span>';
                }
            },
            {
                data: "lanDangNhapCuoi",
                className: "text-nowrap",
                render: function (d, type, row) {
                    if (type === "sort") {
                        return row.lanDangNhapCuoiSort;
                    }
                    return type === "display" ? (d ? escapeHtml(d) : '<span class="text-muted">Chưa đăng nhập</span>') : d;
                }
            },
            {
                data: "id",
                orderable: false,
                searchable: false,
                className: "text-center text-nowrap",
                render: function (id, type, row) {
                    var html = '<a href="/Admin/GiaoVien/Details/' + id + '" class="btn btn-sm btn-outline-info">' +
                               '<i class="bi bi-eye"></i> Xem</a>';
                    if (laAdmin) {
                        html += ' <a href="/Admin/GiaoVien/Edit/' + id + '" class="btn btn-sm btn-outline-primary">' +
                                    '<i class="bi bi-pencil-square"></i> Sửa</a>' +
                                ' <button type="button" class="btn btn-sm btn-outline-danger btn-delete"' +
                                    ' data-url="/Admin/GiaoVien/DeleteGiaoVien/' + id + '"' +
                                    ' data-name="' + escapeHtml(row.maGiaoVien + " – " + row.hoTen) + '">' +
                                    '<i class="bi bi-trash3"></i> Xóa</button>';
                    }
                    return html;
                }
            }
        ],
        order: [[5, "asc"], [4, "asc"], [0, "asc"]],
        pageLength: 10,
        lengthMenu: [5, 10, 25, 50],
        initComplete: function () {
            // Ghi số giảng viên vào từng lựa chọn khoa / bộ môn, vd "Khoa Công nghệ thông tin (5)"
            var demKhoa = {}, demBoMon = {};
            this.api().rows().data().each(function (r) {
                demKhoa[r.khoaId] = (demKhoa[r.khoaId] || 0) + 1;
                demBoMon[r.boMonId] = (demBoMon[r.boMonId] || 0) + 1;
            });
            $locKhoa.find("option[value!='']").each(function () {
                $(this).text($(this).text() + " (" + (demKhoa[this.value] || 0) + ")");
            });
            $locBoMon.find("option[value!='']").each(function () {
                $(this).text($(this).text() + " (" + (demBoMon[this.value] || 0) + ")");
            });
        },
        language: {
            search: "Tìm kiếm:",
            lengthMenu: "Hiển thị _MENU_ dòng",
            info: "Hiển thị _START_ - _END_ trong tổng số _TOTAL_ giảng viên",
            infoEmpty: "Không có dữ liệu",
            infoFiltered: "(lọc từ _MAX_ giảng viên)",
            zeroRecords: "Không tìm thấy giảng viên phù hợp",
            emptyTable: "Chưa có giảng viên nào",
            paginate: {
                first: "Đầu",
                last: "Cuối",
                next: "Sau",
                previous: "Trước"
            }
        }
    });

    // Chỉ hiện bộ môn thuộc khoa đang chọn; bộ môn đang chọn không thuộc khoa thì bỏ chọn
    function capNhatBoMonTheoKhoa() {
        var khoaId = $locKhoa.val();
        $locBoMon.find("option[data-khoa]").each(function () {
            var hop = !khoaId || this.dataset.khoa === khoaId;
            $(this).prop("hidden", !hop).prop("disabled", !hop);
        });
        if ($locBoMon.find("option:selected").prop("disabled")) {
            $locBoMon.val("");
        }
    }

    // Ghi bộ lọc lên URL (tải lại trang / quay lại / chia sẻ link vẫn giữ bộ lọc)
    function ghiUrl() {
        var url = new URL(window.location.href);
        [["khoaId", $locKhoa.val()], ["boMonId", $locBoMon.val()]].forEach(function (p) {
            if (p[1]) {
                url.searchParams.set(p[0], p[1]);
            } else {
                url.searchParams.delete(p[0]);
            }
        });
        history.replaceState(null, "", url);
    }

    capNhatBoMonTheoKhoa(); // áp dụng cho khoa chọn sẵn từ URL
    $locKhoa.on("change", function () {
        capNhatBoMonTheoKhoa();
        ghiUrl();
        dataTable.draw();
    });
    $locBoMon.on("change", function () {
        ghiUrl();
        dataTable.draw();
    });
    $("#locTrangThai").on("change", function () { locCot(8, this.value); });

    // Xóa giảng viên (chỉ Admin thấy nút); event delegation để chạy ở mọi trang của DataTable
    $table.on("click", ".btn-delete", function () {
        var btn = this;
        Swal.fire({
            title: "Bạn có chắc chắn?",
            text: "Giảng viên \"" + btn.dataset.name + "\" và tài khoản đăng nhập sẽ bị xóa, không thể hoàn tác!",
            icon: "warning",
            showCancelButton: true,
            confirmButtonColor: "#d33",
            cancelButtonColor: "#6c757d",
            confirmButtonText: "Xóa",
            cancelButtonText: "Hủy",
            reverseButtons: true
        }).then(function (result) {
            if (!result.isConfirmed) {
                return;
            }
            $.ajax({
                url: btn.dataset.url,
                type: "DELETE",
                headers: { "RequestVerificationToken": $("#deleteForm input[name='__RequestVerificationToken']").val() }
            }).done(function (res) {
                if (res.success) {
                    toastr.success(res.message);
                    dataTable.ajax.reload(null, false); // tải lại, giữ nguyên trang hiện tại
                } else {
                    Swal.fire({ icon: "error", title: "Không xóa được", text: res.message });
                }
            }).fail(function () {
                toastr.error("Có lỗi xảy ra khi xóa giảng viên");
            });
        });
    });
});
