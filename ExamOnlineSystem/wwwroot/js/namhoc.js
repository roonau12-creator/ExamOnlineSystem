// Danh sách năm học: DataTable tải dữ liệu qua Ajax từ /Admin/NamHoc/GetAllNamHoc
// + nút Sửa / Đặt năm hiện tại / Xóa (xác nhận bằng SweetAlert2)
$(function () {
    var $table = $("#tblNamHoc");
    if ($table.length === 0) {
        return;
    }

    // Escape dữ liệu trước khi chèn vào HTML
    function escapeHtml(value) {
        return $("<div>").text(value == null ? "" : value).html();
    }

    function token() {
        return $("#deleteForm input[name='__RequestVerificationToken']").val();
    }

    var dataTable = $table.DataTable({
        ajax: { url: "/Admin/NamHoc/GetAllNamHoc", dataSrc: "data" },
        columns: [
            {
                data: null,
                orderable: false,
                searchable: false,
                className: "text-center",
                // STT theo thứ tự đang hiển thị (sau khi sắp xếp/lọc/phân trang)
                render: function (d, type, row, meta) { return meta.row + meta.settings._iDisplayStart + 1; }
            },
            { data: "maNamHoc", render: function (d) { return escapeHtml(d); } },
            {
                data: "tenNamHoc",
                render: function (d, type, row) {
                    var ten = escapeHtml(d);
                    if (type !== "display") {
                        return d;
                    }
                    if (row.ghiChu) {
                        ten += '<div class="small text-muted">' + escapeHtml(row.ghiChu) + '</div>';
                    }
                    return ten;
                }
            },
            // Hiện dd/MM/yyyy nhưng sắp xếp theo yyyy-MM-dd
            { data: { _: "ngayBatDau", sort: "ngayBatDauSort", filter: "ngayBatDau" }, className: "text-center text-nowrap" },
            { data: { _: "ngayKetThuc", sort: "ngayKetThucSort", filter: "ngayKetThuc" }, className: "text-center text-nowrap" },
            {
                data: "laNamHienTai",
                className: "text-center",
                render: function (d, type) {
                    if (type !== "display") {
                        return d ? 1 : 0;
                    }
                    return d
                        ? '<span class="badge bg-success"><i class="bi bi-check-circle"></i> Hiện tại</span>'
                        : '<span class="badge bg-light text-muted">—</span>';
                }
            },
            {
                data: "id",
                orderable: false,
                searchable: false,
                className: "text-center text-nowrap",
                render: function (id, type, row) {
                    var html = "";
                    if (!row.laNamHienTai) {
                        html += '<button type="button" class="btn btn-sm btn-outline-success btn-hientai"' +
                                    ' data-url="/Admin/NamHoc/DatNamHienTai/' + id + '"' +
                                    ' data-name="' + escapeHtml(row.tenNamHoc) + '">' +
                                    '<i class="bi bi-calendar-check"></i> Đặt hiện tại</button> ';
                    }
                    html += '<a href="/Admin/NamHoc/Edit/' + id + '" class="btn btn-sm btn-outline-primary">' +
                                '<i class="bi bi-pencil-square"></i> Sửa</a> ' +
                            '<button type="button" class="btn btn-sm btn-outline-danger btn-delete"' +
                                ' data-url="/Admin/NamHoc/DeleteNamHoc/' + id + '"' +
                                ' data-name="' + escapeHtml(row.tenNamHoc) + '"' +
                                ' data-hientai="' + (row.laNamHienTai ? "1" : "0") + '">' +
                                '<i class="bi bi-trash3"></i> Xóa</button>';
                    return html;
                }
            }
        ],
        order: [[3, "desc"]],
        pageLength: 10,
        lengthMenu: [5, 10, 25, 50],
        language: {
            search: "Tìm kiếm:",
            lengthMenu: "Hiển thị _MENU_ dòng",
            info: "Hiển thị _START_ - _END_ trong tổng số _TOTAL_ năm học",
            infoEmpty: "Không có dữ liệu",
            infoFiltered: "(lọc từ _MAX_ năm học)",
            zeroRecords: "Không tìm thấy năm học phù hợp",
            emptyTable: "Chưa có năm học nào",
            paginate: {
                first: "Đầu",
                last: "Cuối",
                next: "Sau",
                previous: "Trước"
            }
        }
    });

    // Event delegation để các nút hoạt động ở mọi trang của DataTable
    $table.on("click", ".btn-hientai", function () {
        var btn = this;
        Swal.fire({
            title: "Đặt năm học hiện tại?",
            text: "\"" + btn.dataset.name + "\" sẽ thành năm học hiện tại, năm học đang được đánh dấu sẽ bị bỏ dấu.",
            icon: "question",
            showCancelButton: true,
            confirmButtonColor: "#198754",
            cancelButtonColor: "#6c757d",
            confirmButtonText: "Đồng ý",
            cancelButtonText: "Hủy",
            reverseButtons: true
        }).then(function (result) {
            if (!result.isConfirmed) {
                return;
            }
            $.ajax({
                url: btn.dataset.url,
                type: "POST",
                headers: { "RequestVerificationToken": token() }
            }).done(function (res) {
                if (res.success) {
                    toastr.success(res.message);
                } else {
                    toastr.error(res.message);
                }
                dataTable.ajax.reload(null, false); // tải lại, giữ nguyên trang hiện tại
            }).fail(function () {
                toastr.error("Có lỗi xảy ra khi đặt năm học hiện tại");
            });
        });
    });

    $table.on("click", ".btn-delete", function () {
        var btn = this;
        var canhBao = btn.dataset.hientai === "1"
            ? " Đây đang là năm học hiện tại!"
            : "";

        Swal.fire({
            title: "Bạn có chắc chắn?",
            text: "Năm học \"" + btn.dataset.name + "\" sẽ bị xóa và không thể hoàn tác!" + canhBao,
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
                headers: { "RequestVerificationToken": token() }
            }).done(function (res) {
                if (res.success) {
                    toastr.success(res.message);
                } else {
                    toastr.error(res.message);
                }
                dataTable.ajax.reload(null, false); // tải lại, giữ nguyên trang hiện tại
            }).fail(function () {
                toastr.error("Có lỗi xảy ra khi xóa năm học");
            });
        });
    });
});
