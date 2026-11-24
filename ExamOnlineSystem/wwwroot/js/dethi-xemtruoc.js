// Xem trước đề thi:
//  - "Làm thử": chọn đáp án như sinh viên, "Nộp thử" để chấm (câu nhiều đáp án đúng phải chọn đúng đủ mới được điểm;
//    câu tự luận không chấm tự động)
//  - "Hiện đáp án": đánh dấu đáp án đúng + hiện giải thích
$(function () {
    var $cau = $("#deThi .cau");
    var daNop = false;

    function lamTron(x) {
        return Math.round(x * 100) / 100;
    }

    function hienDapAn(hien) {
        $cau.find(".lua-chon").each(function () {
            var dung = this.dataset.dung === "1";
            $(this).toggleClass("bg-success-subtle", hien && dung);
            $(this).find(".dau-dung").toggleClass("d-none", !(hien && dung));
        });
        $cau.find(".giai-thich").toggleClass("d-none", !hien);
    }

    function cheDo() {
        var dapAn = $("#cheDoDapAn").is(":checked");
        hienDapAn(dapAn || daNop);
        $cau.find("input, textarea").prop("disabled", dapAn || daNop);
        $("#btnNopThu").toggleClass("d-none", dapAn || daNop);
    }

    $("input[name=cheDo]").on("change", cheDo);

    $("#btnNopThu").on("click", function () {
        var diem = 0, tong = 0, dungHet = 0, soTracNghiem = 0, chuaLam = 0, coTuLuan = false;
        $cau.each(function () {
            var $c = $(this);
            var diemCau = parseFloat(this.dataset.diem) || 0;
            var $kq = $c.find(".ket-qua-cau");
            if (this.dataset.loai !== "1") {
                coTuLuan = true;
                $kq.removeClass("text-success text-danger").addClass("text-secondary").text("Tự luận — chấm tay");
                return;
            }
            soTracNghiem++;
            tong += diemCau;
            var dung = true, coChon = false;
            $c.find(".lua-chon").each(function () {
                var chon = $(this).find("input").is(":checked");
                if (chon) coChon = true;
                if (chon !== (this.dataset.dung === "1")) dung = false;
                // Tô đỏ lựa chọn sai đã chọn
                $(this).toggleClass("bg-danger-subtle", chon && this.dataset.dung !== "1");
            });
            if (!coChon) chuaLam++;
            if (dung) {
                diem += diemCau;
                dungHet++;
            }
            $kq.removeClass("text-secondary text-success text-danger").addClass(dung ? "text-success" : "text-danger")
               .html(dung ? '<i class="bi bi-check-circle-fill"></i> Đúng +' + diemCau
                          : '<i class="bi bi-x-circle-fill"></i> ' + (coChon ? "Sai" : "Chưa trả lời") + " 0/" + diemCau);
        });
        daNop = true;
        cheDo();
        $("#btnLamLai").removeClass("d-none");
        $("#ketQua").html("Kết quả làm thử: <span class='text-primary'>" + lamTron(diem) + "/" + lamTron(tong) +
                          "</span> điểm trắc nghiệm (" + dungHet + "/" + soTracNghiem + " câu đúng" +
                          (chuaLam > 0 ? ", " + chuaLam + " câu chưa trả lời" : "") + ")" +
                          (coTuLuan ? " · phần tự luận chấm tay" : ""));
        window.scrollTo({ top: 0, behavior: "smooth" });
    });

    $("#btnLamLai").on("click", function () {
        daNop = false;
        $cau.find("input").prop("checked", false);
        $cau.find("textarea").val("");
        $cau.find(".lua-chon").removeClass("bg-danger-subtle");
        $cau.find(".ket-qua-cau").text("");
        $("#ketQua").text("");
        $(this).addClass("d-none");
        $("#cheDoLamThu").prop("checked", true);
        cheDo();
    });

    // Xác nhận trước khi phát hành
    $("#frmPhatHanh").on("submit", function (e) {
        var form = this;
        if (form.dataset.ok) return;
        e.preventDefault();
        Swal.fire({
            title: "Phát hành đề thi?",
            text: "Đề sẽ được dùng để tổ chức thi. Sau khi phát hành, đề và các câu hỏi/đáp án trong đề bị khóa chỉnh sửa.",
            icon: "question", showCancelButton: true, confirmButtonText: "Phát hành", cancelButtonText: "Hủy", reverseButtons: true
        }).then(function (r) { if (r.isConfirmed) { form.dataset.ok = "1"; form.submit(); } });
    });

    cheDo();
});
