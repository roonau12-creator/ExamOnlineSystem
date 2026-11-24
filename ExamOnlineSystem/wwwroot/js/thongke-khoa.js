// Thống kê khoa: DataTable trên bảng dựng sẵn + lọc theo hoạt động chuyên môn / có sinh viên
$(function () {
    var $table = $("#tblThongKeKhoa");
    if ($table.length === 0) {
        return;
    }

    var dataTable = $table.DataTable({
        order: [[5, "desc"]],
        pageLength: 25,
        lengthMenu: [10, 25, 50, 100],
        columnDefs: [{ targets: 13, orderable: false, searchable: false }],
        language: {
            search: "Tìm kiếm:",
            lengthMenu: "Hiển thị _MENU_ dòng",
            info: "Hiển thị _START_ - _END_ trong tổng số _TOTAL_ khoa",
            infoEmpty: "Không có dữ liệu",
            infoFiltered: "(lọc từ _MAX_ khoa)",
            zeroRecords: "Không tìm thấy khoa phù hợp",
            emptyTable: "Chưa có khoa nào",
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
        var hoatDong = $("#locHoatDong").val();
        var sinhVien = $("#locSinhVien").val();
        return (!hoatDong || tr.getAttribute("data-hoat-dong") === hoatDong) &&
               (!sinhVien || tr.getAttribute("data-sinh-vien") === sinhVien);
    });

    $("#locHoatDong, #locSinhVien").on("change", function () { dataTable.draw(); });
});
