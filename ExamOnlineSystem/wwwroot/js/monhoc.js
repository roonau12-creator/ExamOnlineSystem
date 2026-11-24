// Quản lý danh sách môn học: DataTable, xác nhận xóa bằng SweetAlert2,
// và dropdown bộ môn phụ thuộc khoa trên form Thêm / Sửa.
$(function () {
    // ===== Trang danh sách =====
    var $table = $("#tblMonHoc");
    if ($table.length > 0) {
        $table.DataTable({
            order: [[3, "asc"], [4, "asc"], [7, "asc"]],
            pageLength: 10,
            lengthMenu: [5, 10, 25, 50],
            columnDefs: [
                { targets: [6, 7], className: "dt-left" },
                { targets: 9, orderable: false, searchable: false }
            ],
            language: {
                search: "Tìm kiếm:",
                lengthMenu: "Hiển thị _MENU_ dòng",
                info: "Hiển thị _START_ - _END_ trong tổng số _TOTAL_ môn học",
                infoEmpty: "Không có dữ liệu",
                infoFiltered: "(lọc từ _MAX_ môn học)",
                zeroRecords: "Không tìm thấy môn học phù hợp",
                emptyTable: "Chưa có môn học nào",
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
                text: "Môn học \"" + btn.dataset.name + "\" sẽ bị xóa và không thể hoàn tác!",
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

    // ===== Form Thêm / Sửa: chọn khoa -> nạp lại danh sách bộ môn =====
    var $khoa = $("#KhoaId");
    var $boMon = $("#BoMonId");
    if ($khoa.length > 0 && $boMon.length > 0) {
        $khoa.on("change", function () {
            $boMon.find("option:not(:first)").remove();
            $boMon.val("");

            var khoaId = $khoa.val();
            if (!khoaId) {
                return;
            }

            $.getJSON($khoa.data("bomon-url"), { khoaId: khoaId })
                .done(function (items) {
                    items.forEach(function (b) {
                        $boMon.append($("<option>").val(b.id).text(b.ten));
                    });
                })
                .fail(function () {
                    toastr.error("Không tải được danh sách bộ môn");
                });
        });
    }
});
