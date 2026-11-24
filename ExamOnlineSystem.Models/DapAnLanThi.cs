using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamOnlineSystem.Models;

// Đáp án được sao chép (snapshot) cho một câu hỏi trong lần thi
public class DapAnLanThi
{
    [Key]
    public int Id { get; set; }

    public int CauHoiLanThiId { get; set; }
    [ForeignKey(nameof(CauHoiLanThiId))]
    public CauHoiLanThi? CauHoiLanThi { get; set; }

    public int DapAnGocId { get; set; }
    [ForeignKey(nameof(DapAnGocId))]
    public DapAn? DapAnGoc { get; set; }

    public string NoiDung { get; set; } = string.Empty;

    public bool LaDapAnDung { get; set; }

    public int ThuTu { get; set; }
}
