using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamOnlineSystem.Models;

// Bài làm của sinh viên cho từng câu hỏi trong một lần thi
public class BaiLam
{
    [Key]
    public int Id { get; set; }

    public int LanThiId { get; set; }
    [ForeignKey(nameof(LanThiId))]
    public LanThi? LanThi { get; set; }

    public int CauHoiLanThiId { get; set; }
    [ForeignKey(nameof(CauHoiLanThiId))]
    public CauHoiLanThi? CauHoiLanThi { get; set; }

    // Nội dung trả lời tự luận
    public string? NoiDungTraLoi { get; set; }

    [Column(TypeName = "numeric(5,2)")]
    public decimal? Diem { get; set; }

    // Nhận xét của giảng viên
    public string? NhanXet { get; set; }

    // Ngày cập nhật (UTC -> timestamptz)
    public DateTime NgayCapNhat { get; set; } = DateTime.UtcNow;
}
