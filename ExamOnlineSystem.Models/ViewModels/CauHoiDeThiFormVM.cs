using System.ComponentModel.DataAnnotations;

namespace ExamOnlineSystem.Models.ViewModels;

// Form Thêm câu hỏi vào đề thi
public class CauHoiDeThiFormVM
{
    // 0 = thêm mới; > 0 = đang sửa dòng câu-đề này (đổi câu / điểm / vị trí)
    public int Id { get; set; }

    // Khi sửa: trang quay về sau khi lưu (chỉ chấp nhận URL nội bộ)
    public string? ReturnUrl { get; set; }

    [Display(Name = "Đề thi")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn đề thi")]
    public int DeThiId { get; set; }

    [Display(Name = "Câu hỏi")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn câu hỏi")]
    public int CauHoiId { get; set; }

    [Display(Name = "Điểm trong đề")]
    [Range(0.25, 1000, ErrorMessage = "Điểm phải lớn hơn 0")]
    public double Diem { get; set; } = 1;

    // Vị trí câu trong đề: null/0 = cuối đề; k = chèn thành câu số k (các câu từ k trở đi lùi xuống)
    [Display(Name = "Vị trí")]
    public int? ViTri { get; set; }

    // "Lưu & thêm câu khác": mở lại form, giữ đề vừa chọn
    public bool ThemTiep { get; set; }
}

// Form Thêm NHIỀU câu hỏi vào đề cùng lúc (thứ tự trong danh sách = thứ tự thêm vào đề)
public class ThemNhieuCauVM
{
    [Display(Name = "Đề thi")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn đề thi")]
    public int DeThiId { get; set; }

    // Vị trí chèn cả nhóm: null/0 = cuối đề; k = nhóm câu mới bắt đầu từ câu số k
    [Display(Name = "Vị trí")]
    public int? ViTri { get; set; }

    public List<CauChonVM> Cau { get; set; } = new();

    public bool ThemTiep { get; set; }
}

public class CauChonVM
{
    public int CauHoiId { get; set; }
    public double Diem { get; set; }
}
