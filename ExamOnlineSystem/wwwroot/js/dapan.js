// Danh sách đáp án (chỉ xem): DataTable tải dữ liệu qua Ajax + lọc theo môn học / đúng-sai
$(function () {
    var $table = $("#tblDapAn");
    if ($table.length === 0) {
        return;
    }

    // Escape dữ liệu trước khi chèn vào HTML
    function escapeHtml(value) {
        return $("<div>").text(value == null ? "" : value).html();
    }

    // Nội dung dài thì rút gọn, rê chuột để xem đầy đủ
    function rutGon(text, max) {
        text = text == null ? "" : String(text);
        if (text.length <= max) {
            return escapeHtml(text);
        }
        return '<span title="' + escapeHtml(text) + '">' + escapeHtml(text.substring(0, max)) + '…</span>';
    }

    function rutGonText(text, max) {
        text = text == null ? "" : String(text);
        return text.length <= max ? text : text.substring(0, max) + "…";
    }

    // Lọc đúng nguyên giá trị của cột (không lọc theo chuỗi con)
    function locCot(cot, giaTri) {
        var regex = giaTri ? "^" + $.fn.dataTable.util.escapeRegex(giaTri) + "$" : "";
        dataTable.column(cot).search(regex, true, false).draw();
    }

    var dataTable = $table.DataTable({
        ajax: { url: "/Admin/DapAn/GetAllDapAn", dataSrc: "data" },
        columns: [
            {
                // Sắp theo câu hỏi rồi thứ tự đáp án để các đáp án của cùng câu nằm liền nhau
                data: "noiDungCauHoi",
                render: function (d, type, row) {
                    // Chỉ dùng số cho sắp xếp (đệm 0 để sắp đúng); không trả số cho "type" vì DataTables sẽ coi cột là số và lọc chữ không khớp
                    if (type === "sort") {
                        return String(row.cauHoiId).padStart(8, "0") + "." + String(row.thuTu).padStart(3, "0");
                    }
                    if (type !== "display") {
                        return d;
                    }
                    return '<a href="/Admin/CauHoi/Details/' + row.cauHoiId + '" class="text-reset text-decoration-none">' +
                           '<span class="text-muted">#' + row.cauHoiId + '</span> ' + rutGon(d, 80) + '</a>';
                }
            },
            { data: "danhMuc", render: function (d, type) { return type === "display" ? escapeHtml(d) : d; } },
            { data: "tenMonHoc", render: function (d, type) { return type === "display" ? escapeHtml(d) : d; } },
            {
                data: "nhan",
                className: "text-center",
                render: function (d, type, row) {
                    if (type !== "display") {
                        return d;
                    }
                    return '<span class="badge rounded-pill ' + (row.dapAnDung ? "text-bg-success" : "text-bg-secondary") + '">' +
                           escapeHtml(d) + '</span>';
                }
            },
            { data: "noiDung", render: function (d, type) { return type === "display" ? rutGon(d, 120) : d; } },
            {
                data: "dapAnDung",
                className: "text-center",
                render: function (d, type) {
                    var chu = d ? "Đúng" : "Sai";
                    if (type !== "display") {
                        return chu;
                    }
                    return d
                        ? '<span class="text-success fw-semibold"><i class="bi bi-check-circle-fill"></i> Đúng</span>'
                        : '<span class="text-muted"><i class="bi bi-x-circle"></i> Sai</span>';
                }
            },
            { data: "tenGiaoVien", render: function (d, type) { return type === "display" ? escapeHtml(d) : d; } },
            {
                data: "id",
                className: "text-center text-nowrap",
                orderable: false,
                searchable: false,
                render: function (id, type, row) {
                    return '<a href="/Admin/DapAn/Edit/' + id + '?returnUrl=' + encodeURIComponent("/Admin/DapAn") + '"' +
                               ' class="btn btn-sm btn-outline-primary me-1">' +
                               '<i class="bi bi-pencil-square"></i> Sửa</a>' +
                           '<button type="button" class="btn btn-sm btn-outline-danger me-1 btn-xoa-dap-an-ajax"' +
                               ' data-url="/Admin/DapAn/DeleteDapAn/' + id + '"' +
                               ' data-name="' + escapeHtml(row.nhan + ". " + rutGonText(row.noiDung, 60)) + '">' +
                               '<i class="bi bi-trash3"></i> Xóa</button>' +
                           '<a href="/Admin/CauHoi/Details/' + row.cauHoiId + '" class="btn btn-sm btn-outline-info">' +
                               '<i class="bi bi-eye"></i> Câu hỏi</a>';
                }
            }
        ],
        order: [[2, "asc"], [0, "asc"]],
        pageLength: 25,
        lengthMenu: [10, 25, 50, 100],
        initComplete: function () {
            // Đổ danh sách môn học có đáp án vào bộ lọc
            var $loc = $("#locMonHoc");
            this.api().column(2).data().unique().sort().each(function (ten) {
                $loc.append($("<option>").text(ten));
            });
        },
        language: {
            search: "Tìm kiếm:",
            lengthMenu: "Hiển thị _MENU_ dòng",
            info: "Hiển thị _START_ - _END_ trong tổng số _TOTAL_ đáp án",
            infoEmpty: "Không có dữ liệu",
            infoFiltered: "(lọc từ _MAX_ đáp án)",
            zeroRecords: "Không tìm thấy đáp án phù hợp",
            emptyTable: "Chưa có đáp án nào",
            paginate: {
                first: "Đầu",
                last: "Cuối",
                next: "Sau",
                previous: "Trước"
            }
        }
    });

    window.dapAnTable = dataTable; // dapan-xoa.js dùng để tải lại bảng sau khi xóa

    $("#locMonHoc").on("change", function () { locCot(2, this.value); });
    $("#locDung").on("change", function () { locCot(5, this.value); });
});
