using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamOnlineSystem.Models;

// Lịch thi (ca thi): một đề thi đã phát hành, tổ chức trong khung giờ [ThoiGianBatDau, ThoiGianKetThuc]
// cho một hoặc nhiều lớp (LichThiLop). Thời gian làm bài = DeThi.ThoiLuong.
public class LichThi
{
    [Key]
    public int Id { get; set; }

    public string TenLichThi { get; set; } = string.Empty;

    public int DeThiId { get; set; }
    [ForeignKey(nameof(DeThiId))]
    public DeThi? DeThi { get; set; }

    // Khung giờ được vào thi (UTC)
    public DateTime ThoiGianBatDau { get; set; }
    public DateTime ThoiGianKetThuc { get; set; }

    // Giáo viên phụ trách / tạo lịch thi
    public int GiaoVienId { get; set; }
    [ForeignKey(nameof(GiaoVienId))]
    public GiaoVien? GiaoVien { get; set; }

    public string GhiChu { get; set; } = string.Empty;
    public DateTime NgayTao { get; set; } = DateTime.UtcNow;

    public List<LichThiLop> LichThiLops { get; set; } = new();
}

// Lớp tham gia lịch thi
public class LichThiLop
{
    [Key]
    public int Id { get; set; }

    public int LichThiId { get; set; }
    [ForeignKey(nameof(LichThiId))]
    public LichThi? LichThi { get; set; }

    public int LopId { get; set; }
    [ForeignKey(nameof(LopId))]
    public Lop? Lop { get; set; }
}
