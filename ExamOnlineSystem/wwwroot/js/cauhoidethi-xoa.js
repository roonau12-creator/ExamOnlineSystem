// Bỏ câu hỏi khỏi đề thi (dùng chung cho trang Danh sách câu hỏi đề thi và Chi tiết đề thi).
// Nút cần: class "btn-bo-khoi-de", data-url, data-name; trang cần form #deleteForm chứa anti-forgery token.
// Xong: nếu trang có DataTable (window.cauHoiDeThiTable) thì tải lại bảng, ngược lại tải lại trang.
$(function () {
    $(document).on("click", ".btn-bo-khoi-de", function () {
        var btn = this;
        Swal.fire({
            title: "Bỏ câu hỏi khỏi đề?",
            text: btn.dataset.name + " sẽ bị bỏ khỏi đề, các câu phía sau được đánh lại số thứ tự. " +
                  "Câu hỏi trong ngân hàng vẫn được giữ nguyên.",
            icon: "warning",
            showCancelButton: true,
            confirmButtonColor: "#d33",
            cancelButtonColor: "#6c757d",
            confirmButtonText: "Bỏ khỏi đề",
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
                if (window.cauHoiDeThiTable) {
                    window.cauHoiDeThiTable.ajax.reload(null, false); // giữ nguyên trang hiện tại
                } else {
                    setTimeout(function () { location.reload(); }, 900);
                }
            }).fail(function () {
                toastr.error("Có lỗi xảy ra khi bỏ câu hỏi khỏi đề");
            });
        });
    });
});
