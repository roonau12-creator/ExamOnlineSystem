using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamOnlineSystem.Models;

public class Lop
{
    [Key]
    public int Id { get; set; }

    [Display(Name = "Mã lớp")]
    [Required(ErrorMessage = "Vui lòng nhập mã lớp")]
    public string MaLop { get; set; } = string.Empty;

    [Display(Name = "Tên lớp")]
    [Required(ErrorMessage = "Vui lòng nhập tên lớp")]
    public string TenLop { get; set; } = string.Empty;

    [Display(Name = "Khoa")]
    [Required(ErrorMessage = "Vui lòng chọn khoa")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn khoa")]
    public int KhoaId { get; set; }

    // Nullable để form không bắt buộc gửi kèm đối tượng Khoa (chỉ cần KhoaId)
    [ForeignKey(nameof(KhoaId))]
    public Khoa? Khoa { get; set; }
}
