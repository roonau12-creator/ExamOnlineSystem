// Form Thêm đáp án:
//  - Dropdown câu hỏi chỉ hiện các câu thuộc môn học đang chọn
//  - Chọn câu hỏi => hiện các đáp án đang có (Ajax) và nhãn của đáp án mới
$(function () {
    var $form = $("#frmDapAn");
    if ($form.length === 0) {
        return;
    }

    var $monHoc = $("#MonHocId");
    var $cauHoi = $("#CauHoiId");
    var $khung = $("#khungDapAnHienCo");
    var $ds = $("#dsDapAnHienCo");

    function escapeHtml(value) {
        return $("<div>").text(value == null ? "" : value).html();
    }

    function locCauHoi() {
        var monHocId = $monHoc.val();
        $cauHoi.find("option[data-mon-hoc-id]").each(function () {
            var hop = monHocId === "" || this.dataset.monHocId === monHocId;
            $(this).prop("hidden", !hop).prop("disabled", !hop);
        });
        if ($cauHoi.find("option:selected").prop("disabled")) {
            $cauHoi.val("");
            hienDapAnHienCo();
        }
    }

    function hienDapAnHienCo() {
        var id = $cauHoi.val();
        if (!id) {
            $khung.addClass("d-none");
            return;
        }
        // Chọn câu hỏi trước => tự chọn môn học tương ứng
        var monHocId = $cauHoi.find("option:selected").data("mon-hoc-id");
        if (monHocId && $monHoc.val() === "") {
            $monHoc.val(String(monHocId));
        }

        $.getJSON("/Admin/DapAn/GetDapAnByCauHoi", { cauHoiId: id }).done(function (res) {
            $ds.empty();
            if (res.data.length === 0) {
                $ds.append('<li class="list-group-item text-muted">Chưa có đáp án nào</li>');
            }
            res.data.forEach(function (d) {
                $ds.append(
                    '<li class="list-group-item d-flex gap-2 ' + (d.dapAnDung ? "list-group-item-success" : "") + '">' +
                    '<span class="badge rounded-pill ' + (d.dapAnDung ? "text-bg-success" : "text-bg-secondary") + '">' +
                    escapeHtml(d.nhan) + '</span>' +
                    '<span class="me-auto">' + escapeHtml(d.noiDung) + '</span>' +
                    (d.dapAnDung ? '<i class="bi bi-check-circle-fill text-success"></i>' : '') +
                    '</li>');
            });
            var n = res.data.length;
            $("#nhanMoi").text(n < 26 ? String.fromCharCode(65 + n) : String(n + 1));
            $khung.removeClass("d-none");
        }).fail(function () {
            $khung.addClass("d-none");
        });
    }

    $monHoc.on("change", locCauHoi);
    $cauHoi.on("change", hienDapAnHienCo);
    locCauHoi();
    hienDapAnHienCo();
});
