using System.ComponentModel.DataAnnotations;

namespace ExamOnlineSystem.Models.ViewModels;

// Dữ liệu form Thêm đáp án cho một câu hỏi trắc nghiệm có sẵn
public class DapAnFormVM
{
    public const int SoDapAnToiDa = 10;

    // 0 = thêm mới; > 0 = đang sửa đáp án này
    public int Id { get; set; }

    // Chỉ dùng để lọc dropdown câu hỏi trên form
    [Display(Name = "Môn học")]
    public int? MonHocId { get; set; }

    [Display(Name = "Câu hỏi")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn câu hỏi")]
    public int CauHoiId { get; set; }

    [Display(Name = "Nội dung đáp án")]
    [Required(ErrorMessage = "Vui lòng nhập nội dung đáp án")]
    public string NoiDung { get; set; } = string.Empty;

    [Display(Name = "Là đáp án đúng")]
    public bool DapAnDung { get; set; }

    // Khi sửa: trang quay về sau khi lưu (chỉ chấp nhận URL nội bộ)
    public string? ReturnUrl { get; set; }
}

// Một câu hỏi trắc nghiệm trong dropdown chọn câu hỏi
public class CauHoiLuaChonVM
{
    public int Id { get; set; }
    public string NoiDung { get; set; } = string.Empty;
    public int MonHocId { get; set; }
    public int SoDapAn { get; set; }
}
