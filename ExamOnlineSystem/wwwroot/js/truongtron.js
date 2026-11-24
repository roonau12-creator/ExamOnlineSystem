// Quản lý trường trộn: DataTable, xác nhận xóa bằng SweetAlert2, ẩn/hiện cột nguồn trên form.
$(function () {
    // ===== Form Thêm / Sửa: chỉ hiện "Cột nguồn" khi nguồn khác Nhập tay =====
    var $nguon = $("#Nguon");
    if ($nguon.length) {
        var toggleCotNguon = function () {
            var nhapTay = $nguon.val() === "NHAP_TAY";
            $("#cotNguonGroup").toggle(!nhapTay);
            if (nhapTay) {
                $("#CotNguon").val("");
            }
        };
        $nguon.on("change", toggleCotNguon);
        toggleCotNguon();

        // Mã luôn IN HOA, khoảng trắng đổi thành gạch dưới
        $("#Ma").on("input", function () {
            this.value = this.value.toUpperCase().replace(/\s+/g, "_");
        });
    }

    // ===== Danh sách =====
    var $table = $("#tblTruongTron");
    if ($table.length === 0) {
        return;
    }

    $table.DataTable({
        order: [[0, "asc"]],
        pageLength: 10,
        lengthMenu: [5, 10, 25, 50],
        columnDefs: [
            { targets: 7, orderable: false, searchable: false }
        ],
        language: {
            search: "Tìm kiếm:",
            lengthMenu: "Hiển thị _MENU_ dòng",
            info: "Hiển thị _START_ - _END_ trong tổng số _TOTAL_ trường trộn",
            infoEmpty: "Không có dữ liệu",
            infoFiltered: "(lọc từ _MAX_ trường trộn)",
            zeroRecords: "Không tìm thấy trường trộn phù hợp",
            emptyTable: "Chưa có trường trộn nào",
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
            text: "Trường trộn \"" + btn.dataset.name + "\" sẽ bị xóa, không thể hoàn tác!",
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
