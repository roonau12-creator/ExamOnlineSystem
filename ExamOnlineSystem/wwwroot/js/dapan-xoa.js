// Xóa đáp án (dùng chung cho trang Danh sách đáp án và Chi tiết câu hỏi).
// Nút cần: class "btn-xoa-dap-an-ajax", data-url, data-name; trang cần form #deleteForm chứa anti-forgery token.
// Xóa xong: nếu trang có DataTable đáp án (window.dapAnTable) thì tải lại bảng, ngược lại tải lại trang.
$(function () {
    $(document).on("click", ".btn-xoa-dap-an-ajax", function () {
        var btn = this;
        Swal.fire({
            title: "Bạn có chắc chắn?",
            text: "Đáp án \"" + btn.dataset.name + "\" sẽ bị xóa và không thể hoàn tác! " +
                  "Các đáp án còn lại sẽ được đánh lại thứ tự A, B, C...",
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
                if (!res.success) {
                    toastr.error(res.message);
                    return;
                }
                toastr.success(res.message);
                if (window.dapAnTable) {
                    window.dapAnTable.ajax.reload(null, false); // giữ nguyên trang hiện tại
                } else {
                    setTimeout(function () { location.reload(); }, 600);
                }
            }).fail(function () {
                toastr.error("Có lỗi xảy ra khi xóa đáp án");
            });
        });
    });
});
