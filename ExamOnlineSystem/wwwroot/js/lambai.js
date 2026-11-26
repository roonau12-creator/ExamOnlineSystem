// Trang làm bài thi: đếm ngược, tự động lưu từng câu, bảng câu hỏi, đánh dấu xem lại, nộp bài
// và đình chỉ khi sinh viên mở / chuyển sang tab khác (kể cả mở thêm cửa sổ).
$(function () {
    var $trang = $("#trangLamBai");
    if (!$trang.length) {
        return;
    }

    var lanThiId = parseInt($trang.data("lan-thi-id"), 10);
    var soCau = parseInt($trang.data("so-cau"), 10) || 0;
    var token = $('#frmNopBai input[name="__RequestVerificationToken"]').val();
    var dangNopBai = false;      // đang nộp -> không chặn rời trang nữa
    var hetGio = false;
    var biDinhChi = false;       // đã mở / chuyển sang tab khác
    var dangRoiTrang = false;    // đang tải lại, đóng hoặc chuyển trang trong chính tab này
    var daDong = false;          // máy chủ đã khóa bài vì vi phạm

    var p2 = function (n) { return String(n).padStart(2, "0"); };

    // ===== Đồng hồ đếm ngược =====
    // Mốc hết giờ tính từ số giây còn lại do máy chủ cấp (không phụ thuộc đồng hồ máy sinh viên)
    var mocHetGio = Date.now() + (parseInt($trang.data("so-giay"), 10) || 0) * 1000;
    var daCanhBao = {};

    var dinhDangGiay = function (giay) {
        var gio = Math.floor(giay / 3600);
        var phut = Math.floor((giay % 3600) / 60);
        return (gio > 0 ? gio + ":" + p2(phut) : p2(phut)) + ":" + p2(giay % 60);
    };

    var veDongHo = function () {
        var giay = Math.max(0, Math.round((mocHetGio - Date.now()) / 1000));
        $("#dongHo").text(dinhDangGiay(giay))
            .toggleClass("lb-gan-het", giay > 60 && giay <= 300)
            .toggleClass("lb-sap-het", giay <= 60);

        [600, 300, 60].forEach(function (moc) {
            if (giay <= moc && giay > moc - 5 && !daCanhBao[moc]) {
                daCanhBao[moc] = true;
                toastr.warning("Còn " + (moc >= 60 ? moc / 60 + " phút" : moc + " giây") + " để làm bài.");
            }
        });

        if (giay <= 0 && !hetGio) {
            hetGio = true;
            tuNopBai();
        }
    };

    // ===== Lưu bài làm của một câu =====
    var $trangThaiLuu = $("#trangThaiLuu");
    var dangLuu = 0;

    var hienTrangThai = function (loai, chu) {
        var icon = loai === "luu" ? "bi-arrow-repeat" : loai === "xong" ? "bi-cloud-check" : "bi-exclamation-triangle";
        var mau = loai === "loi" ? "text-danger" : loai === "xong" ? "text-success" : "text-muted";
        $trangThaiLuu.removeClass("text-muted text-success text-danger").addClass(mau)
            .html('<i class="bi ' + icon + '"></i> ' + chu);
    };

    var capNhatTienDo = function (soCauDaLam) {
        if (typeof soCauDaLam !== "number") {
            return;
        }
        $("#soCauDaLam").text(soCauDaLam);
        $("#thanhTienDo").css("width", (soCau === 0 ? 0 : Math.round(soCauDaLam * 100 / soCau)) + "%");
    };

    // Câu đã làm chưa: tự luận có nội dung, trắc nghiệm có đáp án được chọn
    var daTraLoi = function ($cau) {
        return $cau.data("loai") === 2
            ? $.trim($cau.find(".lb-tu-luan").val() || "") !== ""
            : $cau.find(".lb-dap-an:checked").length > 0;
    };

    var veBangCau = function ($cau) {
        var xong = daTraLoi($cau);
        $('#bangCau .lb-o-cau[data-cau="' + $cau.data("cau") + '"]')
            .toggleClass("btn-success", xong)
            .toggleClass("btn-outline-secondary", !xong);
        $cau.find(".lb-bo-chon").toggleClass("d-none", !xong);
    };

    var luuCau = function ($cau) {
        if (hetGio || dangNopBai || biDinhChi) {
            return;
        }
        veBangCau($cau);
        dangLuu++;
        hienTrangThai("luu", "Đang lưu...");

        $.ajax({
            url: "/Customer/LamBai/LuuCau",
            type: "POST",
            traditional: true, // dapAnIds=1&dapAnIds=2 (đúng kiểu List<int> của ASP.NET Core)
            data: {
                lanThiId: lanThiId,
                cauHoiLanThiId: $cau.data("cau"),
                dapAnIds: $cau.find(".lb-dap-an:checked").map(function () { return this.value; }).get(),
                noiDungTraLoi: $cau.find(".lb-tu-luan").val() || "",
                __RequestVerificationToken: token
            },
            success: function (res) {
                capNhatTienDo(res.soCauDaLam);
                if (res.success) {
                    var gio = new Date();
                    hienTrangThai("xong", "Đã lưu " + p2(gio.getHours()) + ":" + p2(gio.getMinutes()) + ":" + p2(gio.getSeconds()));
                    return;
                }
                hienTrangThai("loi", "Chưa lưu được");
                toastr.error(res.message || "Không lưu được câu trả lời");
                // Hết giờ / bài đã nộp: về trang kết quả để không làm tiếp vô ích
                if (/hết giờ|đã nộp/i.test(res.message || "")) {
                    hetGio = true;
                    dangNopBai = true;
                    window.location.href = "/Customer/LamBai/KetQua/" + lanThiId;
                }
            },
            error: function () {
                hienTrangThai("loi", "Mất kết nối, chưa lưu");
                toastr.error("Không gửi được câu trả lời. Kiểm tra kết nối mạng rồi chọn lại câu trả lời.");
            },
            complete: function () {
                dangLuu--;
            }
        });
    };

    // ===== Sự kiện trên câu hỏi =====
    $(".lb-dap-an").on("change", function () {
        luuCau($(this).closest(".lb-cau"));
    });

    // Bấm vào cả dòng lựa chọn (không chỉ ô tròn / nhãn)
    $(".lb-lua-chon").on("click", function (e) {
        // Bấm trực tiếp vào ô chọn / nhãn thì trình duyệt đã tự đổi, không xử lý lại
        if ($(e.target).closest("input, label, a").length) {
            return;
        }
        $(this).find(".lb-dap-an").prop("checked", function (i, v) { return this.type === "radio" ? true : !v; })
            .trigger("change");
    });

    $(".lb-bo-chon").on("click", function () {
        var $cau = $(this).closest(".lb-cau");
        $cau.find(".lb-dap-an").prop("checked", false);
        luuCau($cau);
    });

    // Tự luận: lưu sau khi ngừng nhập 1,5 giây và khi rời ô nhập
    var hen = {};
    $(".lb-tu-luan").on("input", function () {
        var $cau = $(this).closest(".lb-cau");
        var ma = $cau.data("cau");
        veBangCau($cau);
        hienTrangThai("luu", "Chưa lưu...");
        clearTimeout(hen[ma]);
        hen[ma] = setTimeout(function () {
            delete hen[ma];
            luuCau($cau);
        }, 1500);
    }).on("blur", function () {
        var $cau = $(this).closest(".lb-cau");
        clearTimeout(hen[$cau.data("cau")]);
        delete hen[$cau.data("cau")];
        luuCau($cau);
    });

    // ===== Đánh dấu xem lại (lưu trên máy sinh viên, không gửi về máy chủ) =====
    var khoaDanhDau = "lb-danh-dau-" + lanThiId;
    var danhDau = {};
    try {
        danhDau = JSON.parse(localStorage.getItem(khoaDanhDau) || "{}");
    } catch (e) {
        danhDau = {};
    }

    var veDanhDau = function (ma) {
        var co = !!danhDau[ma];
        $('#bangCau .lb-o-cau[data-cau="' + ma + '"]').toggleClass("lb-co-dau-sao", co);
        var $nut = $('.lb-danh-dau[data-cau="' + ma + '"]');
        $nut.toggleClass("text-warning", co).toggleClass("text-secondary", !co);
        $nut.find("i").toggleClass("bi-flag-fill", co).toggleClass("bi-flag", !co);
    };

    $(".lb-danh-dau").on("click", function () {
        var ma = $(this).data("cau");
        danhDau[ma] = !danhDau[ma];
        try {
            localStorage.setItem(khoaDanhDau, JSON.stringify(danhDau));
        } catch (e) { /* bộ nhớ trình duyệt đầy / chế độ riêng tư: bỏ qua */ }
        veDanhDau(ma);
    });

    // ===== Bảng câu hỏi =====
    $("#bangCau").on("click", ".lb-o-cau", function () {
        var $cau = $("#cau-" + $(this).data("thu-tu"));
        if (!$cau.length) {
            return;
        }
        $("html, body").animate({ scrollTop: $cau.offset().top - 150 }, 200);
        $cau.addClass("border-primary");
        setTimeout(function () { $cau.removeClass("border-primary"); }, 1200);
    });

    // Ô câu đang xem sáng theo vị trí cuộn
    var danhDauDangXem = function () {
        var moc = $(window).scrollTop() + 160;
        var $hienTai = null;
        $(".lb-cau").each(function () {
            if ($(this).offset().top <= moc) {
                $hienTai = $(this);
            }
        });
        $("#bangCau .lb-o-cau").removeClass("lb-dang-xem");
        if ($hienTai) {
            $('#bangCau .lb-o-cau[data-cau="' + $hienTai.data("cau") + '"]').addClass("lb-dang-xem");
        }
    };

    // ===== Nộp bài =====
    var nopNgay = function (tuDong) {
        if (biDinhChi) {
            return;
        }
        dangNopBai = true;
        $("#nopTuDong").val(tuDong ? "true" : "false");
        $("#frmNopBai")[0].submit();
    };

    var xacNhanNopBai = function () {
        if (dangNopBai || biDinhChi) {
            return;
        }
        var chuaLam = [];
        $(".lb-cau").each(function () {
            if (!daTraLoi($(this))) {
                chuaLam.push($(this).data("thu-tu"));
            }
        });

        var noiDung = chuaLam.length === 0
            ? "Bạn đã làm hết " + soCau + " câu. Sau khi nộp, bài làm không sửa được nữa."
            : "Còn <strong>" + chuaLam.length + "</strong> câu chưa làm (câu " + chuaLam.join(", ") +
              "). Các câu bỏ trống được tính 0 điểm.";

        Swal.fire({
            title: "Nộp bài thi?",
            html: noiDung + (dangLuu > 0 ? "<br/><span class='text-danger'>Đang lưu câu trả lời, vui lòng đợi vài giây.</span>" : ""),
            icon: chuaLam.length === 0 ? "question" : "warning",
            showCancelButton: true,
            confirmButtonColor: "#198754",
            cancelButtonColor: "#6c757d",
            confirmButtonText: "Nộp bài",
            cancelButtonText: "Làm tiếp"
        }).then(function (kq) {
            if (!kq.isConfirmed) {
                return;
            }
            // Đợi các yêu cầu lưu đang chạy xong (tối đa ~3 giây) rồi mới nộp
            var cho = 0;
            (function doi() {
                if (dangLuu === 0 || cho >= 15) {
                    nopNgay(false);
                    return;
                }
                cho++;
                setTimeout(doi, 200);
            })();
        });
    };

    var tuNopBai = function () {
        if (dangNopBai || biDinhChi) {
            return;
        }
        $(".lb-dap-an, .lb-tu-luan").prop("disabled", true);
        Swal.fire({
            title: "Hết giờ làm bài",
            text: "Bài của bạn đang được nộp tự động...",
            icon: "info",
            allowOutsideClick: false,
            showConfirmButton: false,
            timer: 2000
        });
        setTimeout(function () { nopNgay(true); }, 2000);
    };

    $("#btnNopBai, #btnNopBaiDuoi, #btnNopBaiCuoi").on("click", xacNhanNopBai);

    // ===== Đình chỉ khi mở / chuyển sang tab khác =====
    // Ghi nhớ vi phạm trên máy: tải lại trang khi máy chủ chưa nhận được thì vẫn gửi lại
    var khoaViPham = "lb-vi-pham-" + lanThiId;

    var guiViPham = function () {
        var thuLai = function () { setTimeout(guiViPham, 3000); };
        $.ajax({
            url: "/Customer/LamBai/ViPham/" + lanThiId,
            type: "POST",
            data: { __RequestVerificationToken: token },
            success: function (res) {
                if (!res || !res.success) {
                    thuLai();
                    return;
                }
                daDong = true;
                try {
                    localStorage.removeItem(khoaViPham);
                } catch (e) { /* không dùng được localStorage: bỏ qua */ }
                Swal.fire({
                    title: "Không được tiếp tục thi",
                    text: res.message,
                    icon: "error",
                    allowOutsideClick: false,
                    allowEscapeKey: false,
                    confirmButtonText: "Xem kết quả"
                }).then(function () {
                    window.location.href = "/Customer/LamBai/KetQua/" + lanThiId;
                });
            },
            error: thuLai
        });
    };

    // lyDo: nội dung thông báo lỗi; choToiThieu: số mili giây chờ trước khi báo máy chủ
    var dinhChi = function (lyDo, choToiThieu) {
        lyDo = lyDo || "Bạn đã mở hoặc chuyển sang tab khác trong lúc làm bài.";
        choToiThieu = choToiThieu || 0;
        if (biDinhChi || dangNopBai || hetGio) {
            return;
        }
        // Lưu nốt phần tự luận vừa gõ trước khi khóa bài
        Object.keys(hen).forEach(function (ma) {
            clearTimeout(hen[ma]);
            delete hen[ma];
            luuCau($('.lb-cau[data-cau="' + ma + '"]'));
        });
        biDinhChi = true;
        try {
            localStorage.setItem(khoaViPham, "1");
        } catch (e) { /* không dùng được localStorage: bỏ qua */ }

        $(".lb-dap-an, .lb-tu-luan, .lb-bo-chon, #btnNopBai, #btnNopBaiDuoi, #btnNopBaiCuoi").prop("disabled", true);
        Swal.fire({
            title: "Vi phạm quy chế thi",
            text: lyDo + " Bài thi đang bị khóa...",
            icon: "error",
            allowOutsideClick: false,
            allowEscapeKey: false,
            showConfirmButton: false
        });

        // Đợi các yêu cầu lưu đang chạy xong (tối đa 3 giây) rồi mới khóa bài trên máy chủ
        var batDauCho = Date.now();
        (function doi() {
            var daCho = Date.now() - batDauCho;
            if (daCho >= choToiThieu && (dangLuu === 0 || daCho >= choToiThieu + 3000)) {
                guiViPham();
                return;
            }
            setTimeout(doi, 200);
        })();
    };

    // Tải lại / đóng / chuyển trang cũng làm trang bị ẩn, nhưng "pagehide" chạy trước nên phân biệt được
    $(window).on("pagehide", function () { dangRoiTrang = true; })
        .on("pageshow", function () { dangRoiTrang = false; });

    document.addEventListener("visibilitychange", function () {
        if (document.visibilityState === "hidden" && !dangRoiTrang) {
            dinhChi();
        }
    });

    // Nhắc trước khi rời trang trong lúc đang thi (đóng tab, bấm quay lại...)
    $(window).on("beforeunload", function () {
        if (!dangNopBai && !hetGio && !biDinhChi) {
            return "Bạn đang làm bài thi. Rời trang có thể làm mất câu trả lời chưa lưu.";
        }
    });

    $(window).on("scroll", danhDauDangXem);

    // ===== Khởi tạo =====
    Object.keys(danhDau).forEach(veDanhDau);
    $(".lb-cau").each(function () { veBangCau($(this)); });
    danhDauDangXem();
    veDongHo();
    setInterval(veDongHo, 1000);

    var conViPham = false;
    try {
        conViPham = localStorage.getItem(khoaViPham) === "1";
    } catch (e) { /* không dùng được localStorage: bỏ qua */ }
    if (conViPham) {
        dinhChi();
    }

    // ===== Chỉ cho phép một tab: phát hiện tab / cửa sổ khác của hệ thống trong cùng trình duyệt =====
    // Cần thiết vì hai cửa sổ đặt cạnh nhau đều đang hiển thị nên "visibilitychange" không chạy.
    // Tab vừa mở gửi "mo-tab", tab đang thi trả lời "dang-thi" (xem thêm tabthi.js).
    if (window.BroadcastChannel) {
        var kenhTab = new BroadcastChannel("lb-tab-thi");
        var maTab = Date.now() + "-" + Math.random();

        kenhTab.onmessage = function (e) {
            if (dangNopBai || hetGio || daDong) {
                return;
            }
            if (e.data.loai === "mo-tab") {
                kenhTab.postMessage({ loai: "dang-thi", den: e.data.tu });
                dinhChi("Bạn đã mở thêm tab hoặc cửa sổ khác trong lúc làm bài.");
            } else if (e.data.loai === "dang-thi" && e.data.den === maTab) {
                // Tab mở sau: chờ để tab đang thi kịp lưu bài rồi mới báo máy chủ
                dinhChi("Bài thi đang được mở ở một tab khác, không được mở nhiều tab cùng lúc.", 4000);
            }
        };
        kenhTab.postMessage({ loai: "mo-tab", tu: maTab });
    }
});
