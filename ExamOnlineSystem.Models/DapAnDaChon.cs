using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamOnlineSystem.Models;

// Đáp án mà sinh viên đã chọn cho một câu hỏi trong bài làm
public class DapAnDaChon
{
    [Key]
    public int Id { get; set; }

    public int BaiLamId { get; set; }
    [ForeignKey(nameof(BaiLamId))]
    public BaiLam? BaiLam { get; set; }

    public int DapAnLanThiId { get; set; }
    [ForeignKey(nameof(DapAnLanThiId))]
    public DapAnLanThi? DapAnLanThi { get; set; }
}
