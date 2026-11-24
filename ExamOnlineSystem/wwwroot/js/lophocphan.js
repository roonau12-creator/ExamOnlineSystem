// Danh sách lớp học phần: DataTable tải dữ liệu qua Ajax từ /Admin/LopHocPhan/GetAllLopHocPhan + lọc theo năm học / học kỳ (ghi lên URL)
$(function () {
    var $table = $("#tblLopHocPhan");
    if ($table.length === 0) {
        return;
    }

    // Escape dữ liệu trước khi chèn vào HTML
    function escapeHtml(value) {
        return $("<div>").text(value == null ? "" : value).html();
    }

    // Lọc theo năm học (0 = tất cả) và học kỳ; chỉ áp dụng cho bảng lớp học phần
    var $locNamHoc = $("#locNamHoc"), $locHocKy = $("#locHocKy");
    $.fn.dataTable.ext.search.push(function (settings, data, dataIndex, row) {
        if (settings.nTable !== $table[0]) {
            return true;
        }
        var namHocId = $locNamHoc.val(), hocKy = $locHocKy.val();
        return (!namHocId || namHocId === "0" || String(row.namHocId) === namHocId) &&
               (!hocKy || String(row.hocKy) === hocKy);
    });

    var dataTable = $table.DataTable({
        ajax: { url: "/Admin/LopHocPhan/GetAllLopHocPhan", dataSrc: "data" },
        columns: [
            {
                data: null,
                orderable: false,
                searchable: false,
                className: "text-center",
                // STT theo thứ tự đang hiển thị (sau khi sắp xếp/lọc/phân trang)
                render: function (d, type, row, meta) { return meta.row + meta.settings._iDisplayStart + 1; }
            },
            { data: "maLopHocPhan", render: function (d) { return escapeHtml(d); } },
            {
                data: "tenLopHocPhan",
                render: function (d, type, row) {
                    if (type !== "display") {
                        return d;
                    }
                    var html = escapeHtml(d);
                    if (row.ghiChu) {
                        html += '<div class="small text-muted">' + escapeHtml(row.ghiChu) + "</div>";
                    }
                    return html;
                }
            },
            {
                data: "maHocPhan",
                render: function (d, type, row) {
                    if (type !== "display") {
                        return d + " " + row.tenHocPhan;
                    }
                    return '<span class="fw-semibold">' + escapeHtml(d) + "</span>" +
                           '<div class="small text-muted">' + escapeHtml(row.tenHocPhan) +
                           " · " + row.soTinChi + " TC · " + escapeHtml(row.tenKhoa) + "</div>";
                }
            },
            {
                data: "maNamHoc",
                className: "text-nowrap",
                render: function (d, type, row) {
                    if (type !== "display") {
                        return d;
                    }
                    return escapeHtml(d) +
                           (row.namHienTai ? ' <span class="badge bg-success">hiện tại</span>' : "");
                }
            },
            { data: "tenHocKy", className: "text-nowrap" },
            {
                data: "giaoVienPhuTrach",
                render: function (d, type, row) {
                    if (type !== "display") {
                        return d;
                    }
                    var html = d
                        ? escapeHtml(d)
                        : '<span class="text-danger"><i class="bi bi-exclamation-triangle"></i> Chưa phân công</span>';
                    if (row.soGiaoVien > 1) {
                        html += '<div class="small text-muted">+' + (row.soGiaoVien - 1) + " trợ giảng</div>";
                    }
                    return html;
                }
            },
            {
                data: "soSinhVien",
                className: "text-center text-nowrap",
                render: function (d, type, row) {
                    if (type !== "display") {
                        return d;
                    }
                    var mau = d >= row.siSoToiDa ? "bg-danger" : (d === 0 ? "bg-secondary" : "bg-primary");
                    return '<span class="badge ' + mau + '">' + d + " / " + row.siSoToiDa + "</span>";
                }
            },
            {
                data: "id",
                orderable: false,
                searchable: false,
                className: "text-center text-nowrap",
                render: function (id, type, row) {
                    return '<a href="/Admin/LopHocPhan/PhanCong/' + id + '" class="btn btn-sm btn-outline-success">' +
                               '<i class="bi bi-person-check"></i> Phân công</a> ' +
                           '<a href="/Admin/LopHocPhan/Details/' + id + '" class="btn btn-sm btn-outline-info">' +
                               '<i class="bi bi-eye"></i> Xem</a> ' +
                           '<a href="/Admin/LopHocPhan/Edit/' + id + '" class="btn btn-sm btn-outline-primary">' +
                               '<i class="bi bi-pencil-square"></i> Sửa</a> ' +
                           '<button type="button" class="btn btn-sm btn-outline-danger btn-delete"' +
                               ' data-url="/Admin/LopHocPhan/DeleteLopHocPhan/' + id + '"' +
                               ' data-name="' + escapeHtml(row.maLopHocPhan) + '"' +
                               ' data-count="' + row.soSinhVien + '">' +
                               '<i class="bi bi-trash3"></i> Xóa</button>';
                }
            }
        ],
        order: [],
        initComplete: function () {
            // Ghi số lớp học phần vào từng lựa chọn năm học, vd "2026-2027 (hiện tại) (12)"
            var dem = {};
            this.api().rows().data().each(function (r) {
                dem[r.namHocId] = (dem[r.namHocId] || 0) + 1;
            });
            $locNamHoc.find("option[value!='0']").each(function () {
                $(this).text($(this).text() + " (" + (dem[this.value] || 0) + ")");
            });
        },
        pageLength: 10,
        lengthMenu: [5, 10, 25, 50],
        language: {
            search: "Tìm kiếm:",
            lengthMenu: "Hiển thị _MENU_ dòng",
            info: "Hiển thị _START_ - _END_ trong tổng số _TOTAL_ lớp học phần",
            infoEmpty: "Không có dữ liệu",
            infoFiltered: "(lọc từ _MAX_ lớp học phần)",
            zeroRecords: "Không tìm thấy lớp học phần phù hợp",
            emptyTable: "Chưa có lớp học phần nào",
            paginate: {
                first: "Đầu",
                last: "Cuối",
                next: "Sau",
                previous: "Trước"
            }
        }
    });

    // Đổi bộ lọc: lọc lại bảng và ghi lên URL (tải lại trang / quay lại / chia sẻ link vẫn giữ bộ lọc)
    $locNamHoc.add($locHocKy).on("change", function () {
        dataTable.draw();
        var url = new URL(window.location.href);
        url.searchParams.set("namHocId", $locNamHoc.val() || "0"); // luôn ghi để "tất cả" không bị đổi về năm hiện tại
        if ($locHocKy.val()) {
            url.searchParams.set("hocKy", $locHocKy.val());
        } else {
            url.searchParams.delete("hocKy");
        }
        history.replaceState(null, "", url);
    });

    // Event delegation để nút Xóa hoạt động ở mọi trang của DataTable
    $table.on("click", ".btn-delete", function () {
        var btn = this;

        // Lớp học phần còn sinh viên: báo luôn, không gọi server (server vẫn kiểm tra lại)
        if (Number(btn.dataset.count) > 0) {
            Swal.fire({
                title: "Không thể xóa",
                text: "Lớp học phần \"" + btn.dataset.name + "\" còn " + btn.dataset.count +
                      " sinh viên. Hãy bỏ sinh viên khỏi lớp trước.",
                icon: "info",
                confirmButtonText: "Đã hiểu"
            });
            return;
        }

        Swal.fire({
            title: "Bạn có chắc chắn?",
            text: "Lớp học phần \"" + btn.dataset.name + "\" và các phân công giảng viên của nó sẽ bị xóa, không thể hoàn tác!",
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
                toastr.error("Có lỗi xảy ra khi xóa lớp học phần");
            });
        });
    });
});
