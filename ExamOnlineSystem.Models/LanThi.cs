using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamOnlineSystem.Models;

// Lần thi của một sinh viên dự thi (lưu từng lượt làm bài: thời gian, trạng thái và điểm)
public class LanThi
{
    [Key]
    public int Id { get; set; }

    public int SinhVienDuThiId { get; set; }
    [ForeignKey(nameof(SinhVienDuThiId))]
    public SinhVienDuThi? SinhVienDuThi { get; set; }

    public int LanThu { get; set; } = 1;

    // Thời điểm bắt đầu và nộp bài của lần thi (UTC)
    public DateTime ThoiGianBatDau { get; set; } = DateTime.UtcNow;
    public DateTime? ThoiGianNopBai { get; set; }

    // 0: Đang làm bài, 1: Đã nộp bài, 2: Hết giờ
    public int TrangThai { get; set; }

    public double? Diem { get; set; }
}
