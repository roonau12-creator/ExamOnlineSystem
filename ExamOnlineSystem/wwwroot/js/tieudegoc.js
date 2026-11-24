// Quản lý tiêu đề gốc: DataTable và xác nhận xóa bằng SweetAlert2.
$(function () {
    var $table = $("#tblTieuDeGoc");
    if ($table.length === 0) {
        return;
    }

    $table.DataTable({
        order: [[3, "desc"], [5, "desc"]],
        pageLength: 10,
        lengthMenu: [5, 10, 25, 50],
        columnDefs: [
            { targets: 6, orderable: false, searchable: false }
        ],
        language: {
            search: "Tìm kiếm:",
            lengthMenu: "Hiển thị _MENU_ dòng",
            info: "Hiển thị _START_ - _END_ trong tổng số _TOTAL_ tiêu đề gốc",
            infoEmpty: "Không có dữ liệu",
            infoFiltered: "(lọc từ _MAX_ tiêu đề gốc)",
            zeroRecords: "Không tìm thấy tiêu đề gốc phù hợp",
            emptyTable: "Chưa có tiêu đề gốc nào",
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
            text: "Tiêu đề gốc \"" + btn.dataset.name + "\" và tệp đính kèm sẽ bị xóa, không thể hoàn tác!",
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
});
