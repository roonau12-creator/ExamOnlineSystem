// Các trang không phải trang làm bài: báo cho tab đang thi (nếu có) biết vừa có tab mới được mở,
// và hiện lỗi trên tab mới này. Trang làm bài tự xử lý trong lambai.js.
(function () {
    if (!window.BroadcastChannel || document.getElementById("trangLamBai")) {
        return;
    }

    var kenhTab = new BroadcastChannel("lb-tab-thi");
    var maTab = Date.now() + "-" + Math.random();

    kenhTab.onmessage = function (e) {
        if (e.data.loai !== "dang-thi" || e.data.den !== maTab) {
            return;
        }
        Swal.fire({
            title: "Không được mở thêm tab khi đang thi",
            text: "Bạn đang làm bài thi ở một tab khác. Việc mở thêm tab là vi phạm quy chế, bài thi đã bị khóa.",
            icon: "error",
            allowOutsideClick: false,
            allowEscapeKey: false,
            showConfirmButton: false
        });
    };
    kenhTab.postMessage({ loai: "mo-tab", tu: maTab });
})();
