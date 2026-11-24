// Thống kê sinh viên: DataTable trên bảng dựng sẵn + lọc theo khoa / lớp / xếp loại / đã dự thi
$(function () {
    var $table = $("#tblThongKeSinhVien");
    if ($table.length === 0) {
        return;
    }

    var dataTable = $table.DataTable({
        order: [[7, "desc"]],
        pageLength: 25,
        lengthMenu: [10, 25, 50, 100],
        columnDefs: [{ targets: 12, orderable: false, searchable: false }],
        language: {
            search: "Tìm kiếm:",
            lengthMenu: "Hiển thị _MENU_ dòng",
            info: "Hiển thị _START_ - _END_ trong tổng số _TOTAL_ sinh viên",
            infoEmpty: "Không có dữ liệu",
            infoFiltered: "(lọc từ _MAX_ sinh viên)",
            zeroRecords: "Không tìm thấy sinh viên phù hợp",
            emptyTable: "Chưa có sinh viên nào",
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
        var lop = $("#locLop").val();
        var xepLoai = $("#locXepLoai").val();
        var duThi = $("#locDuThi").val();
        return (!khoa || tr.getAttribute("data-khoa") === khoa) &&
               (!lop || tr.getAttribute("data-lop") === lop) &&
               (!xepLoai || tr.getAttribute("data-xep-loai") === xepLoai) &&
               (!duThi || tr.getAttribute("data-du-thi") === duThi);
    });

    $("#locKhoa, #locLop, #locXepLoai, #locDuThi").on("change", function () { dataTable.draw(); });
});
