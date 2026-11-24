// Danh sách sinh viên: DataTable tải dữ liệu qua Ajax từ /Admin/SinhVien/GetAllSinhVien + lọc theo khoa / lớp (ghi lên URL)
$(function () {
    var $table = $("#tblSinhVien");
    if ($table.length === 0) {
        return;
    }

    // Escape dữ liệu trước khi chèn vào HTML
    function escapeHtml(value) {
        return $("<div>").text(value == null ? "" : value).html();
    }
    function text(d) { return escapeHtml(d); }

    // Lọc theo khoa (so theo khoaId) và lớp (so đúng mã lớp); chỉ áp dụng cho bảng sinh viên
    var $locKhoa = $("#locKhoa"), $locLop = $("#locLop");
    $.fn.dataTable.ext.search.push(function (settings, data, dataIndex, row) {
        if (settings.nTable !== $table[0]) {
            return true;
        }
        var khoaId = $locKhoa.val(), maLop = $locLop.val();
        return (!khoaId || String(row.khoaId) === khoaId) && (!maLop || row.maLop === maLop);
    });

    var dataTable = $table.DataTable({
        ajax: { url: "/Admin/SinhVien/GetAllSinhVien", dataSrc: "data" },
        columns: [
            {
                data: null,
                orderable: false,
                searchable: false,
                className: "text-center",
                // STT theo thứ tự đang hiển thị (sau khi sắp xếp/lọc/phân trang)
                render: function (d, type, row, meta) { return meta.row + meta.settings._iDisplayStart + 1; }
            },
            { data: "maSinhVien", render: text },
            { data: "hoTen", render: text },
            {
                data: "ngaySinh",
                // Hiển thị dd/MM/yyyy nhưng sắp xếp theo yyyy-MM-dd
                render: function (d, type, row) { return type === "sort" ? row.ngaySinhSort : escapeHtml(d); }
            },
            { data: "gioiTinh", render: text },
            { data: "maLop", render: text },
            { data: "tenKhoa", render: text },
            { data: "email", render: text },
            { data: "soDienThoai", render: text },
            {
                data: "id",
                orderable: false,
                searchable: false,
                className: "text-center text-nowrap",
                render: function (id, type, row) {
                    return '<a href="/Admin/SinhVien/Details/' + id + '" class="btn btn-sm btn-outline-info me-1">' +
                               '<i class="bi bi-eye"></i> Xem</a>' +
                           '<a href="/Admin/SinhVien/Details/' + id + '#lich-su-thi" class="btn btn-sm btn-outline-secondary me-1" title="Lịch sử thi">' +
                               '<i class="bi bi-clock-history"></i></a>' +
                           '<a href="/Admin/SinhVien/Edit/' + id + '" class="btn btn-sm btn-outline-primary me-1">' +
                               '<i class="bi bi-pencil-square"></i> Sửa</a>' +
                           '<button type="button" class="btn btn-sm btn-outline-danger btn-delete"' +
                               ' data-url="/Admin/SinhVien/DeleteSinhVien/' + id + '"' +
                               ' data-name="' + escapeHtml(row.maSinhVien + " – " + row.hoTen) + '">' +
                               '<i class="bi bi-trash3"></i> Xóa</button>';
                }
            }
        ],
        order: [[1, "asc"]],
        initComplete: function () {
            // Ghi số sinh viên vào từng lựa chọn, vd "Khoa Công nghệ thông tin (8)"
            var demKhoa = {}, demLop = {};
            this.api().rows().data().each(function (r) {
                demKhoa[r.khoaId] = (demKhoa[r.khoaId] || 0) + 1;
                demLop[r.maLop] = (demLop[r.maLop] || 0) + 1;
            });
            $locKhoa.find("option[value!='']").each(function () {
                $(this).text($(this).text() + " (" + (demKhoa[this.value] || 0) + ")");
            });
            $locLop.find("option[value!='']").each(function () {
                $(this).text($(this).text() + " (" + (demLop[this.value] || 0) + ")");
            });
        },
        pageLength: 10,
        lengthMenu: [5, 10, 25, 50, 100],
        language: {
            search: "Tìm kiếm:",
            lengthMenu: "Hiển thị _MENU_ dòng",
            info: "Hiển thị _START_ - _END_ trong tổng số _TOTAL_ sinh viên",
            infoEmpty: "Không có dữ liệu",
            infoFiltered: "(lọc từ _MAX_ sinh viên)",
            zeroRecords: "Không tìm thấy sinh viên phù hợp",
            emptyTable: "Chưa có sinh viên nào",
            paginate: {
                first: "Đầu",
                last: "Cuối",
                next: "Sau",
                previous: "Trước"
            }
        }
    });

    // Chỉ hiện lớp thuộc khoa đang chọn; lớp đang chọn không thuộc khoa thì bỏ chọn
    function capNhatLopTheoKhoa() {
        var khoaId = $locKhoa.val();
        $locLop.find("option[data-khoa]").each(function () {
            var hop = !khoaId || this.dataset.khoa === khoaId;
            $(this).prop("hidden", !hop).prop("disabled", !hop);
        });
        if ($locLop.find("option:selected").prop("disabled")) {
            $locLop.val("");
        }
    }

    // Ghi bộ lọc lên URL (tải lại trang / quay lại / chia sẻ link vẫn giữ bộ lọc)
    function ghiUrl() {
        var url = new URL(window.location.href);
        [["khoaId", $locKhoa.val()], ["lop", $locLop.val()]].forEach(function (p) {
            if (p[1]) {
                url.searchParams.set(p[0], p[1]);
            } else {
                url.searchParams.delete(p[0]);
            }
        });
        history.replaceState(null, "", url);
    }

    // Mở từ trang chi tiết lớp: /Admin/SinhVien?lop=MaLop -> chọn luôn khoa của lớp
    var lopTuUrl = new URLSearchParams(window.location.search).get("lop");
    var $lopTuUrl = $locLop.find("option").filter(function () { return lopTuUrl && this.value === lopTuUrl; });
    if ($lopTuUrl.length) {
        if (!$locKhoa.val()) {
            $locKhoa.val($lopTuUrl.attr("data-khoa"));
        }
        capNhatLopTheoKhoa();
        $locLop.val(lopTuUrl);
    } else {
        capNhatLopTheoKhoa();
    }

    $locKhoa.on("change", function () {
        capNhatLopTheoKhoa();
        ghiUrl();
        dataTable.draw();
    });
    $locLop.on("change", function () {
        ghiUrl();
        dataTable.draw();
    });

    // Event delegation để nút Xóa hoạt động ở mọi trang của DataTable
    $table.on("click", ".btn-delete", function () {
        var btn = this;
        Swal.fire({
            title: "Bạn có chắc chắn?",
            text: "Sinh viên \"" + btn.dataset.name + "\" và tài khoản đăng nhập của sinh viên sẽ bị xóa, không thể hoàn tác!",
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
                toastr.error("Có lỗi xảy ra khi xóa sinh viên");
            });
        });
    });
});
