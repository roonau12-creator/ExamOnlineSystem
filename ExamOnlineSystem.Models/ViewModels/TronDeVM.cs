using System.ComponentModel.DataAnnotations;

namespace ExamOnlineSystem.Models.ViewModels;

// Form Trộn đề: từ 1 đề gốc tạo nhiều đề mới cùng câu hỏi/điểm nhưng thứ tự câu được xáo ngẫu nhiên
public class TronDeVM
{
    public const int SoDeToiDa = 20;

    public int DeThiGocId { get; set; }

    [Display(Name = "Số đề cần tạo")]
    [Range(1, SoDeToiDa, ErrorMessage = "Số đề phải từ 1 đến 20")]
    public int SoDe { get; set; } = 4;

    // Mã đề mới = TienToMa + số thứ tự (bỏ qua các số đã có), vd: CSDL-GK-01-T1
    [Display(Name = "Tiền tố mã đề")]
    [Required(ErrorMessage = "Vui lòng nhập tiền tố mã đề")]
    [StringLength(45, ErrorMessage = "Tiền tố tối đa 45 ký tự")]
    public string TienToMa { get; set; } = string.Empty;

    // Trộn cả thứ tự đáp án (A/B/C/D) của các câu trắc nghiệm trong mỗi đề mới
    [Display(Name = "Trộn cả thứ tự đáp án của câu trắc nghiệm")]
    public bool TronDapAn { get; set; } = true;
}
