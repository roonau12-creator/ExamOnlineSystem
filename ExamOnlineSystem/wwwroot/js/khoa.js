// Quản lý danh sách khoa: DataTable (tải dữ liệu qua Ajax) + nút Sửa/Xóa + xác nhận xóa bằng SweetAlert2
$(function () {
    var $table = $("#tblKhoa");
    if ($table.length === 0) {
        return;
    }

    // Escape dữ liệu trước khi chèn vào HTML
    function escapeHtml(value) {
        return $("<div>").text(value == null ? "" : value).html();
    }

    $table.DataTable({
        ajax: { url: "/Admin/Khoa/GetAllKhoa", dataSrc: "data" },
        columns: [
            { data: "maKhoa", render: function (d) { return escapeHtml(d); } },
            { data: "tenKhoa", render: function (d) { return escapeHtml(d); } },
            { data: "soThuTu" },
            {
                data: "id",
                className: "text-center text-nowrap",
                render: function (id, type, row) {
                    return '<a href="/Admin/Khoa/Details/' + id + '" class="btn btn-sm btn-outline-info me-1">' +
                               '<i class="bi bi-eye"></i> Xem</a>' +
                           '<a href="/Admin/Khoa/Edit/' + id + '" class="btn btn-sm btn-outline-primary me-1">' +
                               '<i class="bi bi-pencil-square"></i> Sửa</a>' +
                           '<button type="button" class="btn btn-sm btn-outline-danger btn-delete"' +
                               ' data-url="/Admin/Khoa/DeleteKhoa/' + id + '"' +
                               ' data-name="' + escapeHtml(row.tenKhoa) + '">' +
                               '<i class="bi bi-trash3"></i> Xóa</button>';
                }
            }
        ],
        order: [[2, "asc"]],
        pageLength: 10,
        lengthMenu: [5, 10, 25, 50],
        columnDefs: [
            { targets: 2, className: "dt-left" },
            { targets: 3, orderable: false, searchable: false }
        ],
        language: {
            search: "Tìm kiếm:",
            lengthMenu: "Hiển thị _MENU_ dòng",
            info: "Hiển thị _START_ - _END_ trong tổng số _TOTAL_ khoa",
            infoEmpty: "Không có dữ liệu",
            infoFiltered: "(lọc từ _MAX_ khoa)",
            zeroRecords: "Không tìm thấy khoa phù hợp",
            emptyTable: "Chưa có khoa nào",
            paginate: {
                first: "Đầu",
                last: "Cuối",
                next: "Sau",
                previous: "Trước"
            }
        }
    });

    // Event delegation để nút Xóa hoạt động ở mọi trang của DataTable
    $table.on("click", ".btn-delete", function () {
        var btn = this;
        Swal.fire({
            title: "Bạn có chắc chắn?",
            text: "Khoa \"" + btn.dataset.name + "\" sẽ bị xóa và không thể hoàn tác!",
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
            var token = $("#deleteForm input[name='__RequestVerificationToken']").val();
            $.ajax({
                url: btn.dataset.url,
                type: "DELETE",
                headers: { "RequestVerificationToken": token }
            }).done(function (res) {
                if (res.success) {
                    toastr.success(res.message);
                    $table.DataTable().ajax.reload(null, false); // tải lại, giữ nguyên trang hiện tại
                } else {
                    toastr.error(res.message);
                }
            }).fail(function () {
                toastr.error("Có lỗi xảy ra khi xóa khoa");
            });
        });
    });
});
