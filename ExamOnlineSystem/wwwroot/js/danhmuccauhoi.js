// Danh mục câu hỏi:
//  - Trang danh sách: DataTable tải dữ liệu qua Ajax
//  - Form Thêm/Sửa: dropdown "Danh mục cha" chỉ hiện các danh mục cùng môn học đang chọn

// Escape dữ liệu trước khi chèn vào HTML
function escapeHtml(value) {
    return $("<div>").text(value == null ? "" : value).html();
}

// ===== Trang danh sách =====
$(function () {
    var $table = $("#tblDanhMucCauHoi");
    if ($table.length === 0) {
        return;
    }

    var dataTable = $table.DataTable({
        ajax: { url: "/Admin/DanhMucCauHoi/GetAllDanhMucCauHoi", dataSrc: "data" },
        columns: [
            {
                // Thụt lề theo cấp để thấy rõ cây danh mục; khi sắp xếp dùng thuTu để giữ thứ tự cây
                data: "tenDanhMuc",
                render: function (d, type, row) {
                    // Chỉ dùng số cho sắp xếp (đệm 0 để sắp đúng); không trả số cho "type" vì DataTables sẽ coi cột là số và lọc chữ không khớp
                    if (type === "sort") {
                        return String(row.thuTu).padStart(6, "0");
                    }
                    if (type !== "display") {
                        return d;
                    }
                    if (row.cap <= 1) {
                        return '<span class="fw-semibold">' + escapeHtml(d) + '</span>';
                    }
                    return '<span style="padding-left:' + ((row.cap - 1) * 20) + 'px">' +
                               '<span class="text-muted">└ </span>' + escapeHtml(d) + '</span>';
                }
            },
            {
                // Đường dẫn đầy đủ từ danh mục gốc tới cha trực tiếp
                data: "duongDanCha",
                render: function (d, type) {
                    if (type !== "display") {
                        return d;
                    }
                    return d ? escapeHtml(d) : '<span class="text-muted fst-italic">(Danh mục gốc)</span>';
                }
            },
            { data: "cap", className: "text-center" },
            { data: "tenMonHoc", render: function (d) { return escapeHtml(d); } },
            { data: "moTa", render: function (d) { return escapeHtml(d); } },
            {
                data: "id",
                className: "text-center text-nowrap",
                orderable: false,
                searchable: false,
                render: function (id, type, row) {
                    return '<a href="/Admin/DanhMucCauHoi/Details/' + id + '" class="btn btn-sm btn-outline-info me-1">' +
                               '<i class="bi bi-eye"></i> Chi tiết</a>' +
                           '<a href="/Admin/DanhMucCauHoi/Edit/' + id + '" class="btn btn-sm btn-outline-primary me-1">' +
                               '<i class="bi bi-pencil-square"></i> Sửa</a>' +
                           '<button type="button" class="btn btn-sm btn-outline-danger btn-delete"' +
                               ' data-url="/Admin/DanhMucCauHoi/DeleteDanhMucCauHoi/' + id + '"' +
                               ' data-name="' + escapeHtml(row.tenDanhMuc) + '">' +
                               '<i class="bi bi-trash3"></i> Xóa</button>';
                }
            }
        ],
        order: [[0, "asc"]], // thứ tự cây do server trả về
        pageLength: 10,
        lengthMenu: [5, 10, 25, 50],
        language: {
            search: "Tìm kiếm:",
            lengthMenu: "Hiển thị _MENU_ dòng",
            info: "Hiển thị _START_ - _END_ trong tổng số _TOTAL_ danh mục",
            infoEmpty: "Không có dữ liệu",
            infoFiltered: "(lọc từ _MAX_ danh mục)",
            zeroRecords: "Không tìm thấy danh mục phù hợp",
            emptyTable: "Chưa có danh mục câu hỏi nào",
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
            text: "Danh mục \"" + btn.dataset.name + "\" sẽ bị xóa và không thể hoàn tác!",
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
                toastr.error("Có lỗi xảy ra khi xóa danh mục");
            });
        });
    });
});

// ===== Form Thêm / Sửa =====
$(function () {
    var $monHoc = $("#MonHocId");
    var $cha = $("#DanhMucChaId");
    if ($monHoc.length === 0 || $cha.length === 0) {
        return;
    }

    function locDanhMucCha() {
        var monHocId = $monHoc.val();
        $cha.find("option[data-mon-hoc-id]").each(function () {
            var hop = monHocId !== "" && this.dataset.monHocId === monHocId;
            $(this).prop("hidden", !hop).prop("disabled", !hop);
        });
        if ($cha.find("option:selected").prop("disabled")) {
            $cha.val("");
        }
    }

    $monHoc.on("change", locDanhMucCha);
    locDanhMucCha();
});
