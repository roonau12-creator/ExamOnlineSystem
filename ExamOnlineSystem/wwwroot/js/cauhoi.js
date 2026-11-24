// Danh sách câu hỏi: DataTable tải dữ liệu qua Ajax + bộ lọc theo môn học / loại / mức độ
$(function () {
    var $table = $("#tblCauHoi");
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

    var mauMucDo = { 1: "success", 2: "info", 3: "warning", 4: "danger" };

    // Lọc đúng nguyên giá trị của cột (không lọc theo chuỗi con)
    function locCot(cot, giaTri) {
        var regex = giaTri ? "^" + $.fn.dataTable.util.escapeRegex(giaTri) + "$" : "";
        dataTable.column(cot).search(regex, true, false).draw();
    }

    var dataTable = $table.DataTable({
        ajax: { url: "/Admin/CauHoi/GetAllCauHoi", dataSrc: "data" },
        columns: [
            {
                data: "noiDung",
                // Bấm vào nội dung để mở trang chi tiết
                render: function (d, type, row) {
                    if (type !== "display") {
                        return d;
                    }
                    var iconAnh = row.coHinhAnh
                        ? ' <i class="bi bi-image text-primary" title="Có ảnh minh họa"></i>' : '';
                    return '<a href="/Admin/CauHoi/Details/' + row.id + '" class="text-reset text-decoration-none">' +
                           rutGon(d, 120) + '</a>' + iconAnh;
                }
            },
            {
                data: "danhMuc",
                render: function (d, type, row) {
                    if (type !== "display") {
                        return d;
                    }
                    return '<a href="/Admin/DanhMucCauHoi/Details/' + row.danhMucCauHoiId + '">' + escapeHtml(d) + '</a>';
                }
            },
            { data: "tenMonHoc", render: function (d, type) { return type === "display" ? escapeHtml(d) : d; } },
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
            { data: "diemMacDinh", className: "text-center" },
            {
                data: "soDapAn",
                className: "text-center",
                render: function (d, type, row) {
                    // Câu tự luận không cần đáp án
                    return row.loaiCauHoi === 2 && type === "display" ? '<span class="text-muted">—</span>' : d;
                }
            },
            { data: "tenGiaoVien", render: function (d, type) { return type === "display" ? escapeHtml(d) : d; } },
            {
                data: "id",
                className: "text-center text-nowrap",
                orderable: false,
                searchable: false,
                render: function (id, type, row) {
                    var ten = row.noiDung && row.noiDung.length > 60 ? row.noiDung.substring(0, 60) + "…" : row.noiDung;
                    return '<a href="/Admin/CauHoi/Details/' + id + '" class="btn btn-sm btn-outline-info me-1">' +
                               '<i class="bi bi-eye"></i> Chi tiết</a>' +
                           '<a href="/Admin/CauHoi/Edit/' + id + '" class="btn btn-sm btn-outline-primary me-1">' +
                               '<i class="bi bi-pencil-square"></i> Sửa</a>' +
                           '<button type="button" class="btn btn-sm btn-outline-danger btn-delete"' +
                               ' data-url="/Admin/CauHoi/DeleteCauHoi/' + id + '"' +
                               ' data-name="' + escapeHtml(ten) + '">' +
                               '<i class="bi bi-trash3"></i> Xóa</button>';
                }
            }
        ],
        order: [[2, "asc"]],
        pageLength: 10,
        lengthMenu: [5, 10, 25, 50],
        initComplete: function () {
            // Đổ danh sách môn học có câu hỏi vào bộ lọc
            var $loc = $("#locMonHoc");
            this.api().column(2).data().unique().sort().each(function (ten) {
                $loc.append($("<option>").text(ten));
            });
        },
        language: {
            search: "Tìm kiếm:",
            lengthMenu: "Hiển thị _MENU_ dòng",
            info: "Hiển thị _START_ - _END_ trong tổng số _TOTAL_ câu hỏi",
            infoEmpty: "Không có dữ liệu",
            infoFiltered: "(lọc từ _MAX_ câu hỏi)",
            zeroRecords: "Không tìm thấy câu hỏi phù hợp",
            emptyTable: "Chưa có câu hỏi nào",
            paginate: {
                first: "Đầu",
                last: "Cuối",
                next: "Sau",
                previous: "Trước"
            }
        }
    });

    // Lọc danh mục: câu thuộc danh mục đã chọn hoặc danh mục con/cháu (khóa lọc "|gốc|...|chính nó|")
    var $locDanhMuc = $("#locDanhMuc");
    $.fn.dataTable.ext.search.push(function (settings, searchData, index, rowData) {
        if (settings.nTable !== $table[0]) {
            return true;
        }
        var dm = $locDanhMuc.val();
        return !dm || (rowData.danhMucLoc || "").indexOf("|" + dm + "|") >= 0;
    });
    $locDanhMuc.on("change", function () { dataTable.draw(); });

    // Đổi môn học: chỉ hiện danh mục của môn đó
    function locDanhMucTheoMon() {
        var mon = $("#locMonHoc").val();
        $locDanhMuc.find("option[data-ten-mon]").each(function () {
            var hop = !mon || this.dataset.tenMon === mon;
            $(this).prop("hidden", !hop).prop("disabled", !hop);
        });
        if ($locDanhMuc.find("option:selected").prop("disabled")) {
            $locDanhMuc.val("");
        }
    }

    $("#xoaLoc").on("click", function () {
        $("#locMonHoc, #locLoai, #locMucDo, #locDanhMuc").val("");
        locDanhMucTheoMon();
        dataTable.search("").columns().search("").draw();
    });

    $("#locMonHoc").on("change", function () { locDanhMucTheoMon(); locCot(2, this.value); });
    $("#locLoai").on("change", function () { locCot(3, this.value); });
    $("#locMucDo").on("change", function () { locCot(4, this.value); });

    // Event delegation để nút Xóa hoạt động ở mọi trang của DataTable
    $table.on("click", ".btn-delete", function () {
        var btn = this;
        Swal.fire({
            title: "Bạn có chắc chắn?",
            text: "Câu hỏi \"" + btn.dataset.name + "\" và các đáp án của nó sẽ bị xóa và không thể hoàn tác!",
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
                toastr.error("Có lỗi xảy ra khi xóa câu hỏi");
            });
        });
    });
});
