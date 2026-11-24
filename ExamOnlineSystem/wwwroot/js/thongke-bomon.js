// Thống kê bộ môn: DataTable trên bảng dựng sẵn + lọc theo khoa / ngân hàng đề / giảng viên
$(function () {
    var $table = $("#tblThongKeBoMon");
    if ($table.length === 0) {
        return;
    }

    var dataTable = $table.DataTable({
        order: [[6, "desc"]],
        pageLength: 25,
        lengthMenu: [10, 25, 50, 100],
        columnDefs: [{ targets: 12, orderable: false, searchable: false }],
        language: {
            search: "Tìm kiếm:",
            lengthMenu: "Hiển thị _MENU_ dòng",
            info: "Hiển thị _START_ - _END_ trong tổng số _TOTAL_ bộ môn",
            infoEmpty: "Không có dữ liệu",
            infoFiltered: "(lọc từ _MAX_ bộ môn)",
            zeroRecords: "Không tìm thấy bộ môn phù hợp",
            emptyTable: "Chưa có bộ môn nào",
            paginate: { first: "Đầu", last: "Cuối", next: "Sau", previous: "Trước" }
        }
    });

    // Lọc theo thuộc tính data-* của dòng
    $.fn.dataTable.ext.search.push(function (settings, searchData, dataIndex) {
        if (settings.nTable !== $table[0]) {
            return true;
        }
        var tr = settings.aoData[dataIndex].nTr;
        if (!tr) {
            return true;
        }
        var khoa = $("#locKhoa").val();
        var nganHangDe = $("#locNganHangDe").val();
        var giaoVien = $("#locGiaoVien").val();
        return (!khoa || tr.getAttribute("data-khoa") === khoa) &&
               (!nganHangDe || tr.getAttribute("data-ngan-hang-de") === nganHangDe) &&
               (!giaoVien || tr.getAttribute("data-giao-vien") === giaoVien);
    });

    $("#locKhoa, #locNganHangDe, #locGiaoVien").on("change", function () { dataTable.draw(); });
});
