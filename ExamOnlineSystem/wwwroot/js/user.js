// Quản lý tài khoản: DataTable tải dữ liệu qua Ajax từ /Admin/User/GetAllUser
// + lọc theo vai trò / khoa / trạng thái + khóa, mở khóa tài khoản
$(function () {
    var $table = $("#tblUser");
    if ($table.length === 0) {
        return;
    }

    function escapeHtml(value) {
        return $("<div>").text(value == null ? "" : value).html();
    }

    function text(d, type) {
        return type === "display" ? escapeHtml(d) : d;
    }

    // Lọc đúng nguyên giá trị của cột (không lọc theo chuỗi con)
    function locCot(cot, giaTri) {
        var regex = giaTri ? "^" + $.fn.dataTable.util.escapeRegex(giaTri) + "$" : "";
        dataTable.column(cot).search(regex, true, false).draw();
    }

    var COT_VAITRO = 4, COT_KHOA = 5, COT_TRANGTHAI = 7;

    var dataTable = $table.DataTable({
        ajax: { url: "/Admin/User/GetAllUser", dataSrc: "data" },
        columns: [
            {
                data: null,
                orderable: false,
                searchable: false,
                className: "text-center",
                // STT theo thứ tự đang hiển thị (sau khi sắp xếp/lọc/phân trang)
                render: function (d, type, row, meta) { return meta.row + meta.settings._iDisplayStart + 1; }
            },
            {
                data: "hoTen",
                render: function (d, type, row) {
                    if (type !== "display") {
                        return d;
                    }
                    var html = '<span class="fw-semibold">' + escapeHtml(d) + '</span>';
                    if (row.laChinhMinh) {
                        html += ' <span class="badge text-bg-info">Bạn</span>';
                    }
                    if (row.chucDanh) {
                        html += '<div class="small text-muted">' + escapeHtml(row.chucDanh) + '</div>';
                    }
                    return html;
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
                data: "tenVaiTro",
                render: function (d, type, row) {
                    if (type !== "display") {
                        return d;
                    }
                    var mau = row.vaiTro === "Admin" ? "text-bg-danger"
                            : row.vaiTro === "Teacher" ? "text-bg-primary"
                            : row.vaiTro === "Student" ? "text-bg-success" : "text-bg-light";
                    return '<span class="badge ' + mau + '">' + escapeHtml(d) + '</span>';
                }
            },
            {
                data: "tenKhoa",
                render: function (d, type) {
                    return type === "display" && !d ? '<span class="text-muted">—</span>' : text(d, type);
                }
            },
            {
                data: "hoSo",
                render: function (d, type) {
                    return type === "display" && !d ? '<span class="text-muted">—</span>' : text(d, type);
                }
            },
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
                    var html = '<a href="/Admin/User/Edit/' + encodeURIComponent(id) + '" class="btn btn-sm btn-outline-primary me-1">' +
                        '<i class="bi bi-pencil-square"></i> Sửa</a>';

                    // Không cho tự khóa tài khoản đang đăng nhập
                    if (row.laChinhMinh) {
                        return html + '<button type="button" class="btn btn-sm btn-outline-secondary" disabled' +
                            ' title="Không thể khóa tài khoản đang đăng nhập">' +
                            '<i class="bi bi-lock"></i> Khóa</button>';
                    }

                    if (row.dangHoatDong) {
                        html += '<button type="button" class="btn btn-sm btn-outline-danger btn-trangthai"' +
                            ' data-id="' + escapeHtml(id) + '" data-khoa="true"' +
                            ' data-name="' + escapeHtml(row.hoTen + " (" + row.email + ")") + '">' +
                            '<i class="bi bi-lock"></i> Khóa</button>';
                    } else {
                        html += '<button type="button" class="btn btn-sm btn-outline-success btn-trangthai"' +
                            ' data-id="' + escapeHtml(id) + '" data-khoa="false"' +
                            ' data-name="' + escapeHtml(row.hoTen + " (" + row.email + ")") + '">' +
                            '<i class="bi bi-unlock"></i> Mở khóa</button>';
                    }
                    return html;
                }
            }
        ],
        order: [[1, "asc"]],
        pageLength: 10,
        lengthMenu: [5, 10, 25, 50, 100],
        language: {
            search: "Tìm kiếm:",
            lengthMenu: "Hiển thị _MENU_ dòng",
            info: "Hiển thị _START_ - _END_ trong tổng số _TOTAL_ tài khoản",
            infoEmpty: "Không có dữ liệu",
            infoFiltered: "(lọc từ _MAX_ tài khoản)",
            zeroRecords: "Không tìm thấy tài khoản phù hợp",
            emptyTable: "Chưa có tài khoản nào",
            paginate: {
                first: "Đầu",
                last: "Cuối",
                next: "Sau",
                previous: "Trước"
            }
        }
    });

    $("#locVaiTro").on("change", function () { locCot(COT_VAITRO, this.value); });
    $("#locKhoa").on("change", function () { locCot(COT_KHOA, this.value); });
    $("#locTrangThai").on("change", function () { locCot(COT_TRANGTHAI, this.value); });

    // Event delegation để nút Khóa / Mở khóa hoạt động ở mọi trang của DataTable
    $table.on("click", ".btn-trangthai", function () {
        var btn = this;
        var khoa = btn.dataset.khoa === "true";

        Swal.fire({
            title: khoa ? "Khóa tài khoản?" : "Mở khóa tài khoản?",
            text: khoa
                ? "Tài khoản " + btn.dataset.name + " sẽ không đăng nhập được nữa và bị đăng xuất khỏi các phiên đang mở. Dữ liệu đã tạo vẫn được giữ nguyên."
                : "Tài khoản " + btn.dataset.name + " sẽ đăng nhập lại được bình thường.",
            icon: khoa ? "warning" : "question",
            showCancelButton: true,
            confirmButtonColor: khoa ? "#d33" : "#198754",
            cancelButtonColor: "#6c757d",
            confirmButtonText: khoa ? "Khóa" : "Mở khóa",
            cancelButtonText: "Hủy",
            reverseButtons: true
        }).then(function (result) {
            if (!result.isConfirmed) {
                return;
            }
            var token = $("#deleteForm input[name='__RequestVerificationToken']").val();
            $.ajax({
                url: "/Admin/User/DoiTrangThai/" + encodeURIComponent(btn.dataset.id),
                type: "POST",
                data: { khoa: khoa },
                headers: { "RequestVerificationToken": token }
            }).done(function (res) {
                if (res.success) {
                    toastr.success(res.message);
                } else {
                    toastr.error(res.message);
                }
                dataTable.ajax.reload(null, false); // tải lại, giữ nguyên trang hiện tại
            }).fail(function () {
                toastr.error("Có lỗi xảy ra khi đổi trạng thái tài khoản");
            });
        });
    });
});
