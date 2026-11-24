using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamOnlineSystem.Models;

public class DapAn
{
    [Key]
    public int Id { get; set; }
    public int CauHoiId { get; set; }
    [ForeignKey(nameof(CauHoiId))]
    public CauHoi CauHoi { get; set; }
    public string NoiDung { get; set; }
    public bool DapAnDung { get; set; }
    public int ThuTu { get; set; }
}