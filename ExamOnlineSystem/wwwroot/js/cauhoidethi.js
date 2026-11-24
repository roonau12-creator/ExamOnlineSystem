// Danh sách câu hỏi đề thi (chỉ xem): DataTable tải dữ liệu qua Ajax + lọc theo đề / môn / trạng thái / loại câu
$(function () {
    var $table = $("#tblCauHoiDeThi");
    if ($table.length === 0) {
        return;
    }

    function escapeHtml(value) {
        return $("<div>").text(value == null ? "" : value).html();
    }

    function rutGon(text, max) {
        text = text == null ? "" : String(text);
        if (text.length <= max) {
            return escapeHtml(text);
        }
        return '<span title="' + escapeHtml(text) + '">' + escapeHtml(text.substring(0, max)) + '…</span>';
    }

    function text(d, type) {
        return type === "display" ? escapeHtml(d) : d;
    }

    var mauMucDo = { 1: "success", 2: "info", 3: "warning", 4: "danger" };

    // Lọc đúng nguyên giá trị của cột (không lọc theo chuỗi con)
    function locCot(cot, giaTri) {
        var regex = giaTri ? "^" + $.fn.dataTable.util.escapeRegex(giaTri) + "$" : "";
        dataTable.column(cot).search(regex, true, false).draw();
    }

    var dataTable = $table.DataTable({
        ajax: { url: "/Admin/CauHoiDeThi/GetAllCauHoiDeThi", dataSrc: "data" },
        columns: [
            {
                // Lọc/tìm theo mã đề; sắp theo mã đề rồi câu số
                data: "maDeThi",
                render: function (d, type, row) {
                    if (type === "display") {
                        return '<a href="/Admin/DeThi/Details/' + row.deThiId + '" class="fw-semibold text-decoration-none"' +
                               ' title="' + escapeHtml(row.tenDeThi) + '">' + escapeHtml(d) + '</a>';
                    }
                    if (type === "sort") {
                        return d + "|" + String(row.thuTu).padStart(4, "0");
                    }
                    return d;
                }
            },
            { data: "thuTu", className: "text-center", render: function (d, type) { return type === "display" ? "Câu " + d : d; } },
            {
                data: "noiDung",
                render: function (d, type, row) {
                    if (type !== "display") {
                        return d;
                    }
                    return '<a href="/Admin/CauHoi/Details/' + row.cauHoiId + '" class="text-reset text-decoration-none">' +
                           '<span class="text-muted">#' + row.cauHoiId + '</span> ' + rutGon(d, 90) + '</a>';
                }
            },
            { data: "tenMonHoc", render: text },
            { data: "tenLoaiCauHoi" },
            {
                data: "tenMucDo",
                render: function (d, type, row) {
                    // Chỉ dùng số cho sắp xếp (đệm 0 để sắp đúng); không trả số cho "type" vì DataTables sẽ coi cột là số và lọc chữ không khớp
                    if (type === "sort") {
                        return String(row.mucDo).padStart(2, "0");
                    }
                    if (type !== "display") {
                        return d;
                    }
                    return '<span class="badge text-bg-' + (mauMucDo[row.mucDo] || "secondary") + '">' + escapeHtml(d) + '</span>';
                }
            },
            { data: "diem", className: "text-center" },
            {
                data: "dapAnDung",
                className: "text-center text-nowrap",
                render: function (d, type, row) {
                    if (type !== "display") {
                        return d;
                    }
                    if (row.loaiCauHoi !== 1) {
                        return '<span class="text-muted">' + escapeHtml(d) + '</span>';
                    }
                    return '<span class="fw-bold text-success">' + escapeHtml(d) + '</span>' +
                           (row.daTronDapAn ? ' <i class="bi bi-shuffle text-secondary" title="Đáp án đã được trộn trong đề này"></i>' : '');
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
                        ? '<span class="badge text-bg-success">Đã phát hành</span>'
                        : '<span class="badge text-bg-light text-secondary border">Nháp</span>';
                }
            },
            {
                data: "deThiId",
                className: "text-center text-nowrap",
                orderable: false,
                searchable: false,
                render: function (id, type, row) {
                    var nut = '<a href="/Admin/DeThi/Details/' + id + '" class="btn btn-sm btn-outline-info me-1">' +
                              '<i class="bi bi-eye"></i> Đề</a>';
                    if (!row.daPhatHanh) {
                        nut += '<a href="/Admin/CauHoiDeThi/SapXep?deThiId=' + id + '" class="btn btn-sm btn-outline-secondary me-1"' +
                               ' title="Sắp xếp lại thứ tự các câu trong đề"><i class="bi bi-sort-numeric-down"></i></a>' +
                               '<a href="/Admin/CauHoiDeThi/PhanBoDiem?deThiId=' + id + '" class="btn btn-sm btn-outline-secondary me-1"' +
                               ' title="Phân bổ điểm các câu trong đề"><i class="bi bi-calculator"></i></a>';
                    }
                    if (row.daPhatHanh) {
                        return nut + '<span class="text-muted" title="Đề đã phát hành: hủy phát hành để sửa"><i class="bi bi-lock-fill"></i></span>';
                    }
                    var quayVe = encodeURIComponent("/Admin/CauHoiDeThi?deThiId=" + id);
                    return nut + '<a href="/Admin/CauHoiDeThi/Edit/' + row.id + '?returnUrl=' + quayVe + '"' +
                                 ' class="btn btn-sm btn-outline-primary me-1" title="Đổi câu hỏi, điểm hoặc vị trí của câu này">' +
                                 '<i class="bi bi-pencil-square"></i> Sửa</a>' +
                           '<button type="button" class="btn btn-sm btn-outline-danger btn-bo-khoi-de"' +
                               ' data-url="/Admin/CauHoiDeThi/DeleteCauHoiDeThi/' + row.id + '"' +
                               ' data-name="' + escapeHtml("Câu " + row.thuTu + " (#" + row.cauHoiId + ") của đề " + row.maDeThi) + '">' +
                               '<i class="bi bi-x-lg"></i> Bỏ khỏi đề</button>';
                }
            }
        ],
        order: [[0, "asc"]],
        pageLength: 25,
        lengthMenu: [10, 25, 50, 100],
        initComplete: function () {
            var api = this.api();
            // Đổ danh sách đề và môn học vào bộ lọc
            var $de = $("#locDeThi");
            var daCo = {};
            api.rows().data().each(function (r) {
                if (!daCo[r.maDeThi]) {
                    daCo[r.maDeThi] = true;
                    $de.append($("<option>").val(r.maDeThi).attr("data-id", r.deThiId).text(r.maDeThi + " – " + r.tenDeThi));
                }
            });
            var $mon = $("#locMonHoc");
            api.column(3).data().unique().sort().each(function (ten) {
                $mon.append($("<option>").text(ten));
            });
            // Mở từ trang đề thi (?deThiId=...) => lọc sẵn theo đề đó
            var chon = String($de.data("chon") || "");
            if (chon) {
                var $opt = $de.find('option[data-id="' + chon + '"]');
                if ($opt.length) {
                    $de.val($opt.val());
                    locCot(0, $opt.val());
                }
            }
        },
        language: {
            search: "Tìm kiếm:",
            lengthMenu: "Hiển thị _MENU_ dòng",
            info: "Hiển thị _START_ - _END_ trong tổng số _TOTAL_ câu",
            infoEmpty: "Không có dữ liệu",
            infoFiltered: "(lọc từ _MAX_ câu)",
            zeroRecords: "Không tìm thấy câu hỏi phù hợp",
            emptyTable: "Chưa có câu hỏi nào trong đề thi",
            paginate: {
                first: "Đầu",
                last: "Cuối",
                next: "Sau",
                previous: "Trước"
            }
        }
    });

    window.cauHoiDeThiTable = dataTable; // cauhoidethi-xoa.js dùng để tải lại bảng

    $("#locDeThi").on("change", function () { locCot(0, this.value); });
    $("#locMonHoc").on("change", function () { locCot(3, this.value); });
    $("#locLoai").on("change", function () { locCot(4, this.value); });
    $("#locTrangThai").on("change", function () { locCot(8, this.value); });
});
