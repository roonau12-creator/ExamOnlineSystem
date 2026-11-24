using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamOnlineSystem.Models;

// Câu hỏi được sao chép (snapshot) cho một lần thi cụ thể
public class CauHoiLanThi
{
    [Key]
    public int Id { get; set; }

    public int LanThiId { get; set; }
    [ForeignKey(nameof(LanThiId))]
    public LanThi? LanThi { get; set; }

    public int CauHoiGocId { get; set; }
    [ForeignKey(nameof(CauHoiGocId))]
    public CauHoi? CauHoiGoc { get; set; }

    public string NoiDung { get; set; } = string.Empty;

    // 1: Trắc nghiệm, 2: Tự luận
    public short LoaiCauHoi { get; set; }

    [Column(TypeName = "numeric(5,2)")]
    public decimal Diem { get; set; }

    public int ThuTu { get; set; }
}
