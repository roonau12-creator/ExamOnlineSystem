// Thống kê giảng viên: DataTable trên bảng dựng sẵn + lọc theo khoa / bộ môn / mức độ hoạt động
$(function () {
    var $table = $("#tblThongKeGiaoVien");
    if ($table.length === 0) {
        return;
    }

    var dataTable = $table.DataTable({
        order: [[4, "desc"]],
        pageLength: 25,
        lengthMenu: [10, 25, 50, 100],
        columnDefs: [{ targets: 12, orderable: false, searchable: false }],
        language: {
            search: "Tìm kiếm:",
            lengthMenu: "Hiển thị _MENU_ dòng",
            info: "Hiển thị _START_ - _END_ trong tổng số _TOTAL_ giảng viên",
            infoEmpty: "Không có dữ liệu",
            infoFiltered: "(lọc từ _MAX_ giảng viên)",
            zeroRecords: "Không tìm thấy giảng viên phù hợp",
            emptyTable: "Chưa có giảng viên nào",
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
        var boMon = $("#locBoMon").val();
        var mucDo = $("#locMucDo").val();
        return (!khoa || tr.getAttribute("data-khoa") === khoa) &&
               (!boMon || tr.getAttribute("data-bo-mon") === boMon) &&
               (!mucDo || tr.getAttribute("data-muc-do") === mucDo);
    });

    $("#locKhoa, #locBoMon, #locMucDo").on("change", function () { dataTable.draw(); });
});
