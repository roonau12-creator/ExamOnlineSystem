// Danh sách lịch thi: DataTable tải dữ liệu qua Ajax + thống kê theo trạng thái + lọc trạng thái / môn / lớp
$(function () {
    var $table = $("#tblLichThi");
    if ($table.length === 0) {
        return;
    }

    function escapeHtml(value) {
        return $("<div>").text(value == null ? "" : value).html();
    }

    function text(d, type) {
        return type === "display" ? escapeHtml(d) : d;
    }

    // "còn 1 giờ 20 phút", "còn 2 ngày"
    function conLai(phut) {
        if (phut >= 1440) return Math.floor(phut / 1440) + " ngày";
        if (phut >= 60) return Math.floor(phut / 60) + " giờ" + (phut % 60 ? " " + (phut % 60) + " phút" : "");
        return Math.max(phut, 0) + " phút";
    }

    var mauTrangThai = { "Sắp diễn ra": "info", "Đang diễn ra": "success", "Đã kết thúc": "secondary" };

    // Lọc đúng nguyên giá trị của cột (không lọc theo chuỗi con)
    function locCot(cot, giaTri) {
        var regex = giaTri ? "^" + $.fn.dataTable.util.escapeRegex(giaTri) + "$" : "";
        dataTable.column(cot).search(regex, true, false).draw();
    }

    var dataTable = $table.DataTable({
        ajax: { url: "/Admin/LichThi/GetAllLichThi", dataSrc: "data" },
        columns: [
            {
                data: "tenLichThi",
                render: function (d, type, row) {
                    if (type !== "display") {
                        return d;
                    }
                    return '<a href="/Admin/LichThi/Details/' + row.id + '" class="fw-semibold text-decoration-none"' +
                           ' title="Xem chi tiết lịch thi">' + escapeHtml(d) + '</a>' +
                           (row.ghiChu ? '<div class="small text-muted">' + escapeHtml(row.ghiChu) + '</div>' : '');
                }
            },
            {
                data: "maDeThi",
                render: function (d, type, row) {
                    return type === "display"
                        ? '<a href="/Admin/DeThi/Details/' + row.deThiId + '" title="' + escapeHtml(row.tenDeThi) + '">' + escapeHtml(d) + '</a>'
                        : d;
                }
            },
            { data: "tenMonHoc", render: text },
            {
                data: "batDau",
                className: "text-nowrap",
                render: function (d, type, row) {
                    if (type === "sort") {
                        return row.batDauSort;
                    }
                    if (type !== "display") {
                        return d + " " + row.ketThuc;
                    }
                    // Cùng ngày: "08/12/2026 07:30 – 09:00"
                    var den = row.cungNgay ? row.ketThuc.substring(11) : row.ketThuc;
                    return escapeHtml(d) + ' – ' + escapeHtml(den);
                }
            },
            { data: "thoiLuong", className: "text-center", render: function (d, type) { return type === "display" ? d + " phút" : d; } },
            { data: "lops", render: text },
            { data: "soSinhVien", className: "text-center" },
            {
                data: "trangThai",
                className: "text-center text-nowrap",
                render: function (d, type, row) {
                    if (type !== "display") {
                        return d;
                    }
                    var ghiChu = d === "Sắp diễn ra" ? "bắt đầu sau " + conLai(row.conLaiPhut)
                               : d === "Đang diễn ra" ? "còn " + conLai(row.conLaiPhut)
                               : "";
                    return '<span class="badge text-bg-' + (mauTrangThai[d] || "secondary") + '">' + escapeHtml(d) + '</span>' +
                           (ghiChu ? '<div class="small text-muted">' + ghiChu + '</div>' : '');
                }
            },
            { data: "tenGiaoVien", render: text },
            {
                data: "id",
                className: "text-center text-nowrap",
                orderable: false,
                searchable: false,
                render: function (id, type, row) {
                    var chiTiet = '<a href="/Admin/LichThi/Details/' + id + '" class="btn btn-sm btn-outline-secondary"' +
                                  ' title="Xem chi tiết lịch thi"><i class="bi bi-eye"></i> Chi tiết</a>';
                    if (row.trangThai === "Đã kết thúc") {
                        return '<div class="d-flex flex-column gap-1">' + chiTiet +
                               '<span class="text-muted" title="Lịch thi đã kết thúc"><i class="bi bi-lock-fill"></i></span></div>';
                    }
                    if (row.trangThai === "Đang diễn ra") {
                        return '<div class="d-flex flex-column gap-1">' + chiTiet +
                               '<a href="/Admin/LichThi/Edit/' + id + '" class="btn btn-sm btn-outline-primary"' +
                               ' title="Đang diễn ra: chỉ sửa tên, ghi chú, kéo dài giờ kết thúc">' +
                               '<i class="bi bi-pencil-square"></i> Sửa</a></div>';
                    }
                    // Sắp diễn ra: hiển thị nút Chi tiết, Sửa và Xóa
                    return '<div class="d-flex flex-column gap-1">' + chiTiet +
                           '<a href="/Admin/LichThi/Edit/' + id + '" class="btn btn-sm btn-outline-primary"' +
                           ' title="Chỉnh sửa lịch thi"><i class="bi bi-pencil-square"></i> Sửa</a>' +
                           '<button type="button" class="btn btn-sm btn-outline-danger btn-delete"' +
                           ' data-id="' + id + '" title="Xóa lịch thi"><i class="bi bi-trash3"></i> Xóa</button>' +
                           '</div>';
                }
            }
        ],
        order: [[3, "asc"]],
        pageLength: 10,
        lengthMenu: [5, 10, 25, 50],
        initComplete: function () {
            var api = this.api();
            // Thống kê theo trạng thái
            var dem = {};
            api.rows().data().each(function (r) { dem[r.trangThai] = (dem[r.trangThai] || 0) + 1; });
            $("[data-tk]").each(function () { $(this).text(dem[this.dataset.tk] || 0); });
            // Bộ lọc môn và lớp
            api.column(2).data().unique().sort().each(function (ten) { $("#locMonHoc").append($("<option>").text(ten)); });
            var lop = {};
            api.rows().data().each(function (r) { (r.lops || "").split(", ").forEach(function (x) { if (x) lop[x] = true; }); });
            Object.keys(lop).sort().forEach(function (x) { $("#locLop").append($("<option>").text(x)); });
        },
        language: {
            search: "Tìm kiếm:",
            lengthMenu: "Hiển thị _MENU_ dòng",
            info: "Hiển thị _START_ - _END_ trong tổng số _TOTAL_ lịch thi",
            infoEmpty: "Không có dữ liệu",
            infoFiltered: "(lọc từ _MAX_ lịch thi)",
            zeroRecords: "Không tìm thấy lịch thi phù hợp",
            emptyTable: "Chưa có lịch thi nào",
            paginate: {
                first: "Đầu",
                last: "Cuối",
                next: "Sau",
                previous: "Trước"
            }
        }
    });

    // Lọc lớp: một lịch thi có thể gồm nhiều lớp => so khớp trong danh sách lớp của dòng
    $.fn.dataTable.ext.search.push(function (settings, searchData, index, rowData) {
        if (settings.nTable !== $table[0]) {
            return true;
        }
        var lop = $("#locLop").val();
        return !lop || (rowData.lops || "").split(", ").indexOf(lop) >= 0;
    });

    $("#locTrangThai").on("change", function () { locCot(7, this.value); });
    $("#locMonHoc").on("change", function () { locCot(2, this.value); });
    $("#locLop").on("change", function () { dataTable.draw(); });

    // Xử lý sự kiện nút Xóa
    $table.on("click", ".btn-delete", function () {
        var id = $(this).data("id");
        var row = dataTable.row($(this).closest("tr"));
        var rowData = row.data();

        Swal.fire({
            title: "Xác nhận xóa",
            text: 'Bạn có chắc chắn muốn xóa lịch thi "' + rowData.tenLichThi + '" không? Thao tác này không thể hoàn tác.',
            icon: "warning",
            showCancelButton: true,
            confirmButtonColor: "#d33",
            cancelButtonColor: "#6c757d",
            confirmButtonText: "Có, xóa ngay!",
            cancelButtonText: "Hủy"
        }).then(function (result) {
            if (result.isConfirmed) {
                var token = $('input[name="__RequestVerificationToken"]').val();
                var headers = {};
                headers["RequestVerificationToken"] = token;

                $.ajax({
                    url: "/Admin/LichThi/Delete/" + id,
                    type: "DELETE",
                    headers: headers,
                    success: function (res) {
                        if (res.success) {
                            Swal.fire("Thành công!", res.message, "success").then(function () {
                                dataTable.row(row).remove().draw(false);
                            });
                        } else {
                            Swal.fire("Lỗi!", res.message || "Không thể xóa lịch thi", "error");
                        }
                    },
                    error: function () {
                        Swal.fire("Lỗi!", "Có lỗi xảy ra khi xóa lịch thi", "error");
                    }
                });
            }
        });
    });
});
