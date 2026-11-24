// Quản lý danh sách học phần: DataTable, xác nhận xóa bằng SweetAlert2,
// và dropdown môn học phụ thuộc khoa trên form Thêm / Sửa.
$(function () {
    // ===== Trang danh sách =====
    var $table = $("#tblHocPhan");
    if ($table.length > 0) {
        $table.DataTable({
            order: [[2, "asc"], [3, "asc"], [0, "asc"]],
            pageLength: 10,
            lengthMenu: [5, 10, 25, 50],
            columnDefs: [
                { targets: 4, className: "dt-left" },
                { targets: 5, orderable: false, searchable: false }
            ],
            language: {
                search: "Tìm kiếm:",
                lengthMenu: "Hiển thị _MENU_ dòng",
                info: "Hiển thị _START_ - _END_ trong tổng số _TOTAL_ học phần",
                infoEmpty: "Không có dữ liệu",
                infoFiltered: "(lọc từ _MAX_ học phần)",
                zeroRecords: "Không tìm thấy học phần phù hợp",
                emptyTable: "Chưa có học phần nào",
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
                text: "Học phần \"" + btn.dataset.name + "\" sẽ bị xóa và không thể hoàn tác!",
                icon: "warning",
                showCancelButton: true,
                confirmButtonColor: "#d33",
                cancelButtonColor: "#6c757d",
                confirmButtonText: "Xóa",
                cancelButtonText: "Hủy",
                reverseButtons: true
            }).then(function (result) {
                if (result.isConfirmed) {
                    var form = document.getElementById("deleteForm");
                    form.action = btn.dataset.url;
                    form.submit();
                }
            });
        });
    }

    // ===== Form Thêm / Sửa: chọn khoa -> nạp lại danh sách môn học =====
    var $khoa = $("#KhoaId");
    var $monHoc = $("#MonHocId");
    if ($khoa.length > 0 && $monHoc.length > 0) {
        $khoa.on("change", function () {
            $monHoc.find("option:not(:first)").remove();
            $monHoc.val("");

            var khoaId = $khoa.val();
            if (!khoaId) {
                return;
            }

            $.getJSON($khoa.data("monhoc-url"), { khoaId: khoaId })
                .done(function (items) {
                    items.forEach(function (m) {
                        $monHoc.append($("<option>").val(m.id).text(m.ten));
                    });
                })
                .fail(function () {
                    toastr.error("Không tải được danh sách môn học");
                });
        });
    }
});
