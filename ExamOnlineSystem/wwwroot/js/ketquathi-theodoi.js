// Trang Theo dõi kết quả thi của một lịch thi:
// - DataTable cho bảng kết quả sinh viên + lọc theo trạng thái / kết quả / lớp
// - Làm mới số liệu qua Ajax (tự động 15 giây khi lịch thi đang diễn ra)
$(function () {
    var $table = $("#tblKetQuaSinhVien");
    var lichThiId = parseInt($("#btnLamMoi").data("id"), 10);
    if (isNaN(lichThiId)) {
        return;
    }

    function escapeHtml(value) {
        return $("<div>").text(value == null ? "" : value).html();
    }

    function soThapPhan(d) {
        return d == null ? "—" : (Math.round(d * 100) / 100).toString();
    }

    var dataTable = null;
    if ($table.length > 0) {
        dataTable = $table.DataTable({
            order: [[1, "asc"]],
            pageLength: 25,
            lengthMenu: [10, 25, 50, 100],
            columnDefs: [{ targets: [0, 12], orderable: false, searchable: false }],
            language: {
                search: "Tìm kiếm:",
                lengthMenu: "Hiển thị _MENU_ dòng",
                info: "Hiển thị _START_ - _END_ trong tổng số _TOTAL_ sinh viên",
                infoEmpty: "Không có dữ liệu",
                infoFiltered: "(lọc từ _MAX_ sinh viên)",
                zeroRecords: "Không tìm thấy sinh viên phù hợp",
                emptyTable: "Chưa có sinh viên dự thi",
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
            var tt = $("#locTrangThaiSv").val();
            var kq = $("#locKetQua").val();
            var lop = $("#locLopSv").val();
            return (!tt || tr.getAttribute("data-trang-thai") === tt) &&
                   (!kq || tr.getAttribute("data-ket-qua") === kq) &&
                   (!lop || tr.getAttribute("data-lop") === lop);
        });

        $("#locTrangThaiSv, #locKetQua, #locLopSv").on("change", function () { dataTable.draw(); });
    }

    // Dựng lại một dòng kết quả từ dữ liệu Ajax (khớp với bản dựng phía máy chủ)
    function taoDong(sv, stt) {
        var ketQua = sv.diemHe10 == null ? "chuacham" : (sv.diemHe10 >= 5 ? "dat" : "khongdat");
        var he10 = sv.diemHe10 == null
            ? '<span class="text-muted">—</span>'
            : '<span class="badge text-bg-' + sv.mauXepLoai + '">' + soThapPhan(sv.diemHe10) + '</span>';
        var baiLam = sv.lanThiId
            ? '<a href="/Admin/KetQuaThi/BaiLam/' + sv.lanThiId + '" class="btn btn-sm btn-outline-primary"' +
              ' title="Xem bài làm"><i class="bi bi-eye"></i> Xem</a>'
            : '<span class="text-muted">—</span>';

        return '<tr data-trang-thai="' + sv.trangThai + '" data-lop="' + escapeHtml(sv.maLop) + '" data-ket-qua="' + ketQua + '">' +
            '<td class="text-center">' + stt + '</td>' +
            '<td>' + escapeHtml(sv.maSinhVien) + '</td>' +
            '<td>' + escapeHtml(sv.hoTen) + '</td>' +
            '<td>' + escapeHtml(sv.maLop) + '</td>' +
            '<td class="text-center"><span class="badge text-bg-' + sv.mauTrangThai + '">' + escapeHtml(sv.tenTrangThai) + '</span></td>' +
            '<td class="text-center">' + (sv.soLanThi ? sv.soLanThi : "—") + '</td>' +
            '<td class="text-center text-nowrap" data-order="' + sv.batDauSort + '">' +
                (sv.batDau ? escapeHtml(sv.batDau.substring(0, 5) + " " + sv.batDau.substring(11)) : "—") + '</td>' +
            '<td class="text-center text-nowrap" data-order="' + sv.nopBaiSort + '">' +
                (sv.nopBai ? escapeHtml(sv.nopBai.substring(0, 5) + " " + sv.nopBai.substring(11)) : "—") + '</td>' +
            '<td class="text-center" data-order="' + (sv.soPhutLamBai == null ? -1 : sv.soPhutLamBai) + '">' +
                (sv.soPhutLamBai == null ? "—" : sv.soPhutLamBai + " phút") + '</td>' +
            '<td class="text-center fw-semibold" data-order="' + (sv.diem == null ? -1 : sv.diem) + '">' + soThapPhan(sv.diem) + '</td>' +
            '<td class="text-center" data-order="' + (sv.diemHe10 == null ? -1 : sv.diemHe10) + '">' + he10 + '</td>' +
            '<td class="text-center"><span class="text-' + sv.mauXepLoai + '">' + escapeHtml(sv.xepLoai) + '</span></td>' +
            '<td class="text-center">' + baiLam + '</td>' +
            '</tr>';
    }

    function veLaiPhanBo(phanBo, soCoDiem) {
        var $bo = $("#phanBoDiem");
        if (!soCoDiem) {
            $bo.html('<p class="text-muted mb-0"><i class="bi bi-info-circle"></i> Chưa có bài thi nào được chấm điểm.</p>');
            return;
        }
        var html = "";
        phanBo.forEach(function (k) {
            html += '<div class="d-flex align-items-center gap-2 mb-2">' +
                '<div class="text-nowrap small" style="width:150px">' +
                '<span class="badge text-bg-' + k.mau + '">' + escapeHtml(k.xepLoai) + '</span> ' +
                '<span class="text-muted">' + escapeHtml(k.nhan) + '</span></div>' +
                '<div class="progress flex-grow-1" role="progressbar" aria-valuenow="' + k.tiLe +
                '" aria-valuemin="0" aria-valuemax="100" style="height:1.25rem">' +
                '<div class="progress-bar bg-' + k.mau + '" style="width:' + k.tiLe + '%">' +
                (k.tiLe > 8 ? k.tiLe + "%" : "") + '</div></div>' +
                '<div class="text-nowrap small text-muted" style="width:90px">' + k.soLuong + ' bài (' + k.tiLe + '%)</div>' +
                '</div>';
        });
        $bo.html(html);
    }

    function lamMoi(hienThongBao) {
        var $nut = $("#btnLamMoi");
        $nut.prop("disabled", true);
        $.getJSON("/Admin/KetQuaThi/TheoDoi/" + lichThiId)
            .done(function (res) {
                if (!res.success) {
                    toastr.error(res.message || "Không tải được kết quả");
                    return;
                }
                var t = res.tongHop;
                $('[data-tk="soChuaThi"]').text(t.soChuaThi);
                $('[data-tk="soDangThi"]').text(t.soDangThi);
                $('[data-tk="soDaNop"]').text(t.soDaNop);
                $('[data-tk="soDaNopLon"]').text(t.soDaNop);
                $('[data-tk="soChuaCham"]').text(t.soChuaCham);
                $('[data-tk="soDat"]').text(t.soDat);
                $('[data-tk="soCoDiem"]').text(t.soCoDiem);
                $('[data-tk="diemTrungBinh"]').text(soThapPhan(t.diemTrungBinh));
                $('[data-tk="diemCaoNhat"]').text(soThapPhan(t.diemCaoNhat));
                $('[data-tk="diemThapNhat"]').text(soThapPhan(t.diemThapNhat));
                $('[data-tk="tiLeDat"]').text(t.soCoDiem ? t.tiLeDat + "%" : "—");
                $("#thanhTienDo").css("width", t.tiLeNop + "%").text(t.tiLeNop + "%")
                    .closest(".progress").attr("aria-valuenow", t.tiLeNop);

                veLaiPhanBo(res.phanBo, t.soCoDiem);

                if (dataTable) {
                    var html = res.sinhViens.map(function (sv, i) { return taoDong(sv, i + 1); }).join("");
                    dataTable.clear();
                    dataTable.rows.add($(html));
                    dataTable.draw(false);
                }
                if (hienThongBao) {
                    toastr.success("Đã cập nhật kết quả lúc " + new Date().toLocaleTimeString("vi-VN"));
                }
            })
            .fail(function () {
                toastr.error("Có lỗi xảy ra khi tải kết quả");
            })
            .always(function () {
                $nut.prop("disabled", false);
            });
    }

    $("#btnLamMoi").on("click", function () { lamMoi(true); });

    // Tự động làm mới khi lịch thi đang diễn ra
    var hen = null;
    function datHen() {
        if (hen) {
            clearInterval(hen);
            hen = null;
        }
        if ($("#tuDongLamMoi").prop("checked")) {
            hen = setInterval(function () { lamMoi(false); }, 15000);
        }
    }
    $("#tuDongLamMoi").on("change", datHen);
    datHen();
});
