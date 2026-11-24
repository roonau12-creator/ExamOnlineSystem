// Danh sách lớp: DataTable tải dữ liệu qua Ajax từ /Admin/Lop/GetAllLop + lọc theo khoa (ghi lên URL)
$(function () {
    var $table = $("#tblLop");
    if ($table.length === 0) {
        return;
    }

    // Escape dữ liệu trước khi chèn vào HTML
    function escapeHtml(value) {
        return $("<div>").text(value == null ? "" : value).html();
    }

    // Lọc theo khoa (so theo khoaId); chỉ áp dụng cho bảng lớp
    var $locKhoa = $("#locKhoa");
    $.fn.dataTable.ext.search.push(function (settings, data, dataIndex, row) {
        if (settings.nTable !== $table[0]) {
            return true;
        }
        var khoaId = $locKhoa.val();
        return !khoaId || String(row.khoaId) === khoaId;
    });

    var dataTable = $table.DataTable({
        ajax: { url: "/Admin/Lop/GetAllLop", dataSrc: "data" },
        columns: [
            {
                data: null,
                orderable: false,
                searchable: false,
                className: "text-center",
                // STT theo thứ tự đang hiển thị (sau khi sắp xếp/lọc/phân trang)
                render: function (d, type, row, meta) { return meta.row + meta.settings._iDisplayStart + 1; }
            },
            { data: "maLop", render: function (d) { return escapeHtml(d); } },
            { data: "tenLop", render: function (d) { return escapeHtml(d); } },
            { data: "tenKhoa", render: function (d) { return escapeHtml(d); } },
            { data: "soSinhVien", className: "text-center" },
            {
                data: "id",
                orderable: false,
                searchable: false,
                className: "text-center text-nowrap",
                render: function (id, type, row) {
                    return '<a href="/Admin/Lop/Details/' + id + '" class="btn btn-sm btn-outline-info">' +
                               '<i class="bi bi-eye"></i> Xem</a> ' +
                           '<a href="/Admin/Lop/PhanSinhVien/' + id + '" class="btn btn-sm btn-outline-success">' +
                               '<i class="bi bi-person-plus"></i> Phân SV</a> ' +
                           '<a href="/Admin/Lop/Edit/' + id + '" class="btn btn-sm btn-outline-primary">' +
                               '<i class="bi bi-pencil-square"></i> Sửa</a> ' +
                           '<button type="button" class="btn btn-sm btn-outline-danger btn-delete"' +
                               ' data-url="/Admin/Lop/DeleteLop/' + id + '"' +
                               ' data-name="' + escapeHtml(row.maLop) + '"' +
                               ' data-count="' + row.soSinhVien + '">' +
                               '<i class="bi bi-trash3"></i> Xóa</button>';
                }
            }
        ],
        order: [[3, "asc"], [1, "asc"]],
        initComplete: function () {
            // Ghi số lớp vào từng lựa chọn khoa, vd "Khoa Công nghệ thông tin (5)"
            var dem = {};
            this.api().rows().data().each(function (r) {
                dem[r.khoaId] = (dem[r.khoaId] || 0) + 1;
            });
            $locKhoa.find("option[value!='']").each(function () {
                $(this).text($(this).text() + " (" + (dem[this.value] || 0) + ")");
            });
        },
        pageLength: 10,
        lengthMenu: [5, 10, 25, 50],
        language: {
            search: "Tìm kiếm:",
            lengthMenu: "Hiển thị _MENU_ dòng",
            info: "Hiển thị _START_ - _END_ trong tổng số _TOTAL_ lớp",
            infoEmpty: "Không có dữ liệu",
            infoFiltered: "(lọc từ _MAX_ lớp)",
            zeroRecords: "Không tìm thấy lớp phù hợp",
            emptyTable: "Chưa có lớp nào",
            paginate: {
                first: "Đầu",
                last: "Cuối",
                next: "Sau",
                previous: "Trước"
            }
        }
    });

    // Đổi khoa: lọc lại bảng và ghi khoaId lên URL (tải lại trang / quay lại vẫn giữ bộ lọc)
    $locKhoa.on("change", function () {
        dataTable.draw();
        var url = new URL(window.location.href);
        if (this.value) {
            url.searchParams.set("khoaId", this.value);
        } else {
            url.searchParams.delete("khoaId");
        }
        history.replaceState(null, "", url);
    });

    // Event delegation để nút Xóa hoạt động ở mọi trang của DataTable
    $table.on("click", ".btn-delete", function () {
        var btn = this;

        // Lớp còn sinh viên: báo luôn, không gọi server (server vẫn kiểm tra lại)
        if (Number(btn.dataset.count) > 0) {
            Swal.fire({
                title: "Không thể xóa",
                text: "Lớp \"" + btn.dataset.name + "\" còn " + btn.dataset.count +
                      " sinh viên. Hãy chuyển hoặc xóa sinh viên trước.",
                icon: "info",
                confirmButtonText: "Đã hiểu"
            });
            return;
        }

        Swal.fire({
            title: "Bạn có chắc chắn?",
            text: "Lớp \"" + btn.dataset.name + "\" sẽ bị xóa và không thể hoàn tác!",
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
                if (res.success) {
                    toastr.success(res.message);
                } else {
                    toastr.error(res.message);
                }
                dataTable.ajax.reload(null, false); // tải lại, giữ nguyên trang hiện tại
            }).fail(function () {
                toastr.error("Có lỗi xảy ra khi xóa lớp");
            });
        });
    });
});
