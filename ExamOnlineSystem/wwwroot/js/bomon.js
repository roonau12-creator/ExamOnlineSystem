// Quản lý danh sách bộ môn: DataTable (tải dữ liệu qua Ajax) + lọc theo khoa + nút Sửa/Xóa + xác nhận xóa bằng SweetAlert2
$(function () {
    var $table = $("#tblBoMon");
    if ($table.length === 0) {
        return;
    }

    // Escape dữ liệu trước khi chèn vào HTML
    function escapeHtml(value) {
        return $("<div>").text(value == null ? "" : value).html();
    }

    // Lọc theo khoa (so theo khoaId); chỉ áp dụng cho bảng bộ môn
    var $locKhoa = $("#locKhoa");
    $.fn.dataTable.ext.search.push(function (settings, data, dataIndex, row) {
        if (settings.nTable !== $table[0]) {
            return true;
        }
        var khoaId = $locKhoa.val();
        return !khoaId || String(row.khoaId) === khoaId;
    });

    var dataTable = $table.DataTable({
        ajax: { url: "/Admin/BoMon/GetAllBoMon", dataSrc: "data" },
        columns: [
            { data: "maBoMon", render: function (d) { return escapeHtml(d); } },
            { data: "tenBoMon", render: function (d) { return escapeHtml(d); } },
            { data: "khoa", render: function (k) { return escapeHtml(k ? k.tenKhoa : ""); } },
            { data: "soThuTu" },
            {
                data: "id",
                className: "text-center text-nowrap",
                render: function (id, type, row) {
                    return '<a href="/Admin/BoMon/Details/' + id + '" class="btn btn-sm btn-outline-info me-1">' +
                               '<i class="bi bi-eye"></i> Xem</a>' +
                           '<a href="/Admin/BoMon/PhanGiaoVien/' + id + '" class="btn btn-sm btn-outline-success me-1">' +
                               '<i class="bi bi-people"></i> Gán GV</a>' +
                           '<a href="/Admin/BoMon/Edit/' + id + '" class="btn btn-sm btn-outline-primary me-1">' +
                               '<i class="bi bi-pencil-square"></i> Sửa</a>' +
                           '<button type="button" class="btn btn-sm btn-outline-danger btn-delete"' +
                               ' data-url="/Admin/BoMon/DeleteBoMon/' + id + '"' +
                               ' data-name="' + escapeHtml(row.tenBoMon) + '">' +
                               '<i class="bi bi-trash3"></i> Xóa</button>';
                }
            }
        ],
        order: [[2, "asc"], [3, "asc"]],
        pageLength: 10,
        lengthMenu: [5, 10, 25, 50],
        columnDefs: [
            { targets: 3, className: "dt-left" },
            { targets: 4, orderable: false, searchable: false }
        ],
        language: {
            search: "Tìm kiếm:",
            lengthMenu: "Hiển thị _MENU_ dòng",
            info: "Hiển thị _START_ - _END_ trong tổng số _TOTAL_ bộ môn",
            infoEmpty: "Không có dữ liệu",
            infoFiltered: "(lọc từ _MAX_ bộ môn)",
            zeroRecords: "Không tìm thấy bộ môn phù hợp",
            emptyTable: "Chưa có bộ môn nào",
            paginate: {
                first: "Đầu",
                last: "Cuối",
                next: "Sau",
                previous: "Trước"
            }
        }
    });

    // Đổi khoa: lọc lại bảng và ghi khoaId lên URL (tải lại trang / quay lại vẫn giữ bộ lọc)
    $locKhoa.on("change", function () {
        dataTable.draw();
        var url = new URL(window.location.href);
        if (this.value) {
            url.searchParams.set("khoaId", this.value);
        } else {
            url.searchParams.delete("khoaId");
        }
        history.replaceState(null, "", url);
    });

    // Event delegation để nút Xóa hoạt động ở mọi trang của DataTable
    $table.on("click", ".btn-delete", function () {
        var btn = this;
        Swal.fire({
            title: "Bạn có chắc chắn?",
            text: "Bộ môn \"" + btn.dataset.name + "\" sẽ bị xóa và không thể hoàn tác!",
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
                    dataTable.ajax.reload(null, false); // tải lại, giữ nguyên trang hiện tại
                } else {
                    toastr.error(res.message);
                }
            }).fail(function () {
                toastr.error("Có lỗi xảy ra khi xóa bộ môn");
            });
        });
    });
});
