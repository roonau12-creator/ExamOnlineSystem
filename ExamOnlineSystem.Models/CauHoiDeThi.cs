using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamOnlineSystem.Models;

public class CauHoiDeThi
{
    [Key]
    public int Id { get; set; }
    public int DeThiId { get; set; }
    [ForeignKey(nameof(DeThiId))]
    public  DeThi DeThi { get; set; }
    public int CauHoiId { get; set; }
    [ForeignKey(nameof(CauHoiId))]
    public CauHoi CauHoi { get; set; }
    public double Diem { get; set; }
    public int ThuTu { get; set; }

    // Thứ tự đáp án riêng của câu trong đề này (trộn đáp án): danh sách Id đáp án cách nhau bởi dấu phẩy,
    // vd "31,29,32,30". null = giữ thứ tự gốc của câu hỏi. Đọc bằng ThuTuDapAnHelper.SapXep.
    public string? ThuTuDapAn { get; set; }
}