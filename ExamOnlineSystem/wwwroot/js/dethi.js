// Danh sách đề thi: DataTable tải dữ liệu qua Ajax + bộ lọc theo môn học / loại đề
$(function () {
    var $table = $("#tblDeThi");
    if ($table.length === 0) {
        return;
    }

    // Escape dữ liệu trước khi chèn vào HTML
    function escapeHtml(value) {
        return $("<div>").text(value == null ? "" : value).html();
    }

    function text(d, type) {
        return type === "display" ? escapeHtml(d) : d;
    }

    var mauLoai = { 1: "info", 2: "warning", 3: "primary" };

    // Lọc đúng nguyên giá trị của cột (không lọc theo chuỗi con)
    function locCot(cot, giaTri) {
        var regex = giaTri ? "^" + $.fn.dataTable.util.escapeRegex(giaTri) + "$" : "";
        dataTable.column(cot).search(regex, true, false).draw();
    }

    var dataTable = $table.DataTable({
        ajax: { url: "/Admin/DeThi/GetAllDeThi", dataSrc: "data" },
        columns: [
            {
                data: "maDeThi",
                render: function (d, type, row) {
                    return type === "display"
                        ? '<a href="/Admin/DeThi/Details/' + row.id + '" class="fw-semibold text-decoration-none">' + escapeHtml(d) + '</a>'
                        : d;
                }
            },
            { data: "tenDeThi", render: text },
            { data: "tenMonHoc", render: text },
            {
                data: "tenLoaiDeThi",
                render: function (d, type, row) {
                    if (type !== "display") {
                        return d;
                    }
                    return '<span class="badge text-bg-' + (mauLoai[row.loaiDeThi] || "secondary") + '">' + escapeHtml(d) + '</span>';
                }
            },
            {
                data: "thoiLuong",
                className: "text-center",
                render: function (d, type) { return type === "display" ? d + " phút" : d; }
            },
            { data: "soCauHoi", className: "text-center" },
            {
                // Tổng điểm các câu đã thêm / tổng điểm khai báo của đề; lệch nhau thì tô cảnh báo
                data: "tongDiem",
                className: "text-center text-nowrap",
                render: function (d, type, row) {
                    if (type !== "display") {
                        return d;
                    }
                    var khop = Math.abs(row.tongDiemCauHoi - d) < 0.001;
                    return '<span class="' + (khop ? "text-success" : "text-warning fw-semibold") + '"' +
                           ' title="' + (khop ? "Tổng điểm các câu khớp với tổng điểm đề"
                                             : "Tổng điểm các câu chưa khớp với tổng điểm đề") + '">' +
                           row.tongDiemCauHoi + ' / ' + d + '</span>';
                }
            },
            {
                data: "trangThai",
                className: "text-center text-nowrap",
                render: function (d, type, row) {
                    if (type !== "display") {
                        return d;
                    }
                    return row.daPhatHanh
                        ? '<span class="badge text-bg-success" title="Phát hành lúc ' + escapeHtml(row.ngayPhatHanh) + '">' +
                          '<i class="bi bi-check2-circle"></i> Đã phát hành</span>'
                        : '<span class="badge text-bg-light text-secondary border">Nháp</span>';
                }
            },
            { data: "tenGiaoVien", render: text },
            {
                data: "id",
                className: "text-center text-nowrap",
                orderable: false,
                searchable: false,
                render: function (id, type, row) {
                    var chiTiet = '<a href="/Admin/DeThi/Details/' + id + '" class="btn btn-sm btn-outline-info me-1">' +
                               '<i class="bi bi-eye"></i> Chi tiết</a>';
                    var tron = '<a href="/Admin/DeThi/TronDe/' + id + '" class="btn btn-sm btn-outline-secondary me-1"' +
                               (row.soCauHoi < 2 ? ' title="Cần ít nhất 2 câu để trộn"' : ' title="Tạo nhiều mã đề với thứ tự câu xáo trộn"') + '>' +
                               '<i class="bi bi-shuffle"></i> Trộn</a>';
                    if (row.daPhatHanh) {
                        // Đề đã phát hành bị khóa: muốn sửa/xóa phải hủy phát hành ở trang Chi tiết
                        return chiTiet + tron +
                               '<span class="text-muted" title="Đề đã phát hành: hủy phát hành ở trang Chi tiết để sửa/xóa">' +
                               '<i class="bi bi-lock-fill"></i></span>';
                    }
                    return chiTiet +
                           '<a href="/Admin/DeThi/Edit/' + id + '" class="btn btn-sm btn-outline-primary me-1">' +
                               '<i class="bi bi-pencil-square"></i> Sửa</a>' + tron +
                           '<button type="button" class="btn btn-sm btn-outline-danger btn-delete"' +
                               ' data-url="/Admin/DeThi/DeleteDeThi/' + id + '"' +
                               ' data-name="' + escapeHtml(row.maDeThi + " - " + row.tenDeThi) + '"' +
                               ' data-so-cau="' + row.soCauHoi + '">' +
                               '<i class="bi bi-trash3"></i> Xóa</button>';
                }
            }
        ],
        order: [[2, "asc"], [0, "asc"]],
        pageLength: 10,
        lengthMenu: [5, 10, 25, 50],
        initComplete: function () {
            // Cảnh báo số đề lệch tổng điểm
            var lech = 0, chua = 0;
            this.api().rows().data().each(function (r) {
                if (r.tinhTrangDiem === "Lệch") lech++;
                if (r.tinhTrangDiem === "Chưa có câu") chua++;
            });
            if (lech + chua > 0) {
                $("#canhBaoDiem span").text(
                    (lech > 0 ? "Có " + lech + " đề chưa khớp tổng điểm" : "") +
                    (lech > 0 && chua > 0 ? ", " : "") +
                    (chua > 0 ? (lech > 0 ? "" : "Có ") + chua + " đề chưa có câu hỏi" : "") + ".");
                $("#canhBaoDiem").removeClass("d-none");
            }
            // Đổ danh sách môn học có đề thi vào bộ lọc
            var $loc = $("#locMonHoc");
            this.api().column(2).data().unique().sort().each(function (ten) {
                $loc.append($("<option>").text(ten));
            });
        },
        language: {
            search: "Tìm kiếm:",
            lengthMenu: "Hiển thị _MENU_ dòng",
            info: "Hiển thị _START_ - _END_ trong tổng số _TOTAL_ đề thi",
            infoEmpty: "Không có dữ liệu",
            infoFiltered: "(lọc từ _MAX_ đề thi)",
            zeroRecords: "Không tìm thấy đề thi phù hợp",
            emptyTable: "Chưa có đề thi nào",
            paginate: {
                first: "Đầu",
                last: "Cuối",
                next: "Sau",
                previous: "Trước"
            }
        }
    });

    $("#locMonHoc").on("change", function () { locCot(2, this.value); });
    $("#locLoai").on("change", function () { locCot(3, this.value); });
    $("#locTrangThai").on("change", function () { locCot(7, this.value); });

    $.fn.dataTable.ext.search.push(function (settings, searchData, index, rowData) {
        if (settings.nTable !== $table[0]) {
            return true;
        }
        var v = $("#locDiem").val();
        return !v || rowData.tinhTrangDiem === v;
    });
    $("#locDiem").on("change", function () { dataTable.draw(); });

    // Event delegation để nút Xóa hoạt động ở mọi trang của DataTable
    $table.on("click", ".btn-delete", function () {
        var btn = this;
        var soCau = parseInt(btn.dataset.soCau, 10) || 0;
        Swal.fire({
            title: "Bạn có chắc chắn?",
            text: "Đề thi \"" + btn.dataset.name + "\"" +
                  (soCau > 0 ? " (" + soCau + " câu)" : "") +
                  " sẽ bị xóa và không thể hoàn tác! Các câu hỏi trong ngân hàng vẫn được giữ nguyên.",
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
                    dataTable.ajax.reload(null, false); // tải lại, giữ nguyên trang hiện tại
                } else {
                    toastr.error(res.message);
                }
            }).fail(function () {
                toastr.error("Có lỗi xảy ra khi xóa đề thi");
            });
        });
    });
});
