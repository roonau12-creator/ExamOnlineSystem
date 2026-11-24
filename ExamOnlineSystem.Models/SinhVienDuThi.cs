using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamOnlineSystem.Models;

// Sinh viên dự thi trong một lịch thi (theo dõi trạng thái làm bài, thời gian và điểm)
public class SinhVienDuThi
{
    [Key]
    public int Id { get; set; }

    public int LichThiId { get; set; }
    [ForeignKey(nameof(LichThiId))]
    public LichThi? LichThi { get; set; }

    public int SinhVienId { get; set; }
    [ForeignKey(nameof(SinhVienId))]
    public SinhVien? SinhVien { get; set; }

    // Thời điểm sinh viên bắt đầu và nộp bài (UTC)
    public DateTime? ThoiGianBatDau { get; set; }
    public DateTime? ThoiGianNopBai { get; set; }

    // 0: Chưa thi, 1: Đang làm bài, 2: Đã nộp bài
    public int TrangThai { get; set; }

    public double? Diem { get; set; }
}