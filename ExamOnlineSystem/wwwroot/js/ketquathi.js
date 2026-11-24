// Theo dõi kết quả thi - danh sách: DataTable tải qua Ajax + thống kê chung + lọc trạng thái / môn / lớp
$(function () {
    var $table = $("#tblKetQuaThi");
    if ($table.length === 0) {
        return;
    }

    function escapeHtml(value) {
        return $("<div>").text(value == null ? "" : value).html();
    }

    function text(d, type) {
        return type === "display" ? escapeHtml(d) : d;
    }

    // Điểm thang 10 -> màu theo xếp loại
    function mauDiem(d) {
        if (d == null) return "secondary";
        if (d >= 9) return "success";
        if (d >= 8) return "primary";
        if (d >= 6.5) return "info";
        if (d >= 5) return "warning";
        return "danger";
    }

    function soDiem(d) {
        return d == null ? '<span class="text-muted">—</span>'
            : '<span class="badge text-bg-' + mauDiem(d) + '">' + d.toFixed(2) + '</span>';
    }

    var mauTrangThai = { "Sắp diễn ra": "info", "Đang diễn ra": "success", "Đã kết thúc": "secondary" };

    function locCot(cot, giaTri) {
        var regex = giaTri ? "^" + $.fn.dataTable.util.escapeRegex(giaTri) + "$" : "";
        dataTable.column(cot).search(regex, true, false).draw();
    }

    var dataTable = $table.DataTable({
        ajax: { url: "/Admin/KetQuaThi/GetAllKetQua", dataSrc: "data" },
        columns: [
            {
                data: "tenLichThi",
                render: function (d, type, row) {
                    if (type !== "display") {
                        return d;
                    }
                    return '<a href="/Admin/KetQuaThi/Details/' + row.id + '" class="fw-semibold text-decoration-none"' +
                           ' title="Theo dõi kết quả lịch thi này">' + escapeHtml(d) + '</a>' +
                           '<div class="small text-muted">Đề ' + escapeHtml(row.maDeThi) +
                           ' · thang ' + row.tongDiem + ' điểm</div>';
                }
            },
            {
                data: "tenMonHoc",
                render: function (d, type, row) {
                    return type === "display"
                        ? escapeHtml(d) + '<div class="small text-muted">' + escapeHtml(row.lops) + '</div>'
                        : d;
                }
            },
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
                    var den = row.cungNgay ? row.ketThuc.substring(11) : row.ketThuc;
                    return escapeHtml(d) + ' – ' + escapeHtml(den);
                }
            },
            {
                data: "trangThai",
                className: "text-center text-nowrap",
                render: function (d, type) {
                    return type === "display"
                        ? '<span class="badge text-bg-' + (mauTrangThai[d] || "secondary") + '">' + escapeHtml(d) + '</span>'
                        : d;
                }
            },
            {
                data: "tiLeNop",
                className: "text-nowrap",
                render: function (d, type, row) {
                    if (type !== "display") {
                        return d;
                    }
                    var chuaCham = row.soChuaCham > 0
                        ? ' · <span class="text-danger">' + row.soChuaCham + ' chưa chấm</span>' : '';
                    return '<div class="progress" role="progressbar" aria-valuenow="' + d +
                           '" aria-valuemin="0" aria-valuemax="100" style="height:1rem;min-width:110px">' +
                           '<div class="progress-bar bg-success" style="width:' + d + '%">' + d + '%</div></div>' +
                           '<div class="small text-muted">' + row.soDaNop + '/' + row.soSinhVien + ' đã nộp · ' +
                           row.soDangThi + ' đang làm' + chuaCham + '</div>';
                }
            },
            { data: "diemTrungBinh", className: "text-center", render: function (d, type) { return type === "display" ? soDiem(d) : (d == null ? -1 : d); } },
            { data: "diemCaoNhat", className: "text-center", render: function (d, type) { return type === "display" ? soDiem(d) : (d == null ? -1 : d); } },
            { data: "diemThapNhat", className: "text-center", render: function (d, type) { return type === "display" ? soDiem(d) : (d == null ? -1 : d); } },
            {
                data: "tiLeDat",
                className: "text-center text-nowrap",
                render: function (d, type, row) {
                    if (type !== "display") {
                        return row.soCoDiem === 0 ? -1 : d;
                    }
                    if (row.soCoDiem === 0) {
                        return '<span class="text-muted">—</span>';
                    }
                    return '<span class="fw-semibold text-' + (d >= 50 ? "success" : "danger") + '">' + d + '%</span>' +
                           '<div class="small text-muted">' + row.soDat + '/' + row.soCoDiem + ' đạt</div>';
                }
            },
            {
                data: "id",
                className: "text-center text-nowrap",
                orderable: false,
                searchable: false,
                render: function (id, type, row) {
                    return '<div class="d-flex flex-column gap-1">' +
                           '<a href="/Admin/KetQuaThi/Details/' + id + '" class="btn btn-sm btn-outline-primary"' +
                           ' title="Theo dõi kết quả thi"><i class="bi bi-clipboard-data"></i> Theo dõi</a>' +
                           (row.soCoDiem > 0
                               ? '<a href="/Admin/KetQuaThi/XuatExcel/' + id + '" class="btn btn-sm btn-outline-success"' +
                                 ' title="Tải bảng điểm Excel"><i class="bi bi-file-earmark-excel"></i> Bảng điểm</a>'
                               : '') +
                           '</div>';
                }
            }
        ],
        order: [[2, "desc"]],
        pageLength: 10,
        lengthMenu: [5, 10, 25, 50],
        initComplete: function () {
            var api = this.api();
            var rows = api.rows().data().toArray();

            var soDangDienRa = 0, tongSv = 0, tongNop = 0, tongChuaCham = 0, tongDat = 0, tongCoDiem = 0, tongDiem = 0;
            rows.forEach(function (r) {
                if (r.trangThai === "Đang diễn ra") soDangDienRa++;
                tongSv += r.soSinhVien;
                tongNop += r.soDaNop;
                tongChuaCham += r.soChuaCham;
                tongDat += r.soDat;
                tongCoDiem += r.soCoDiem;
                if (r.soCoDiem > 0 && r.diemTrungBinh != null) tongDiem += r.diemTrungBinh * r.soCoDiem;
            });
            $('[data-tk="soLichThi"]').text(rows.length);
            $('[data-tk="soDangDienRa"]').text(soDangDienRa);
            $('[data-tk="soDaNop"]').text(tongNop);
            $('[data-tk="soSinhVien"]').text(tongSv);
            $('[data-tk="soChuaCham"]').text(tongChuaCham);
            $('[data-tk="diemTrungBinh"]').text(tongCoDiem > 0 ? (tongDiem / tongCoDiem).toFixed(2) : "—");
            $('[data-tk="tiLeDat"]').text(tongCoDiem > 0 ? Math.round(tongDat * 100 / tongCoDiem) + "%" : "—");

            // Bộ lọc môn và lớp
            api.column(1).data().unique().sort().each(function (ten) { $("#locMonHoc").append($("<option>").text(ten)); });
            var lop = {};
            rows.forEach(function (r) { (r.lops || "").split(", ").forEach(function (x) { if (x) lop[x] = true; }); });
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
            paginate: { first: "Đầu", last: "Cuối", next: "Sau", previous: "Trước" }
        }
    });

    // Lọc theo lớp (một lịch thi có nhiều lớp) và "chỉ lịch thi đã có bài nộp"
    $.fn.dataTable.ext.search.push(function (settings, searchData, index, rowData) {
        if (settings.nTable !== $table[0]) {
            return true;
        }
        var lop = $("#locLop").val();
        if (lop && (rowData.lops || "").split(", ").indexOf(lop) < 0) {
            return false;
        }
        return !$("#chiCoKetQua").prop("checked") || rowData.soDaNop > 0;
    });

    $("#locTrangThai").on("change", function () { locCot(3, this.value); });
    $("#locMonHoc").on("change", function () { locCot(1, this.value); });
    $("#locLop, #chiCoKetQua").on("change", function () { dataTable.draw(); });
});
