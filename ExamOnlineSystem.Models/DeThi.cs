using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamOnlineSystem.Models;

public class DeThi
{
    [Key]
    public int Id { get; set; }
    public string MaDeThi { get; set; } = string.Empty;
    public string TenDeThi { get; set; } = string.Empty;
    public int MonHocId { get; set; }
    [ForeignKey(nameof(MonHocId))]
    public MonHoc MonHoc { get; set; } = null!;
    public int GiaoVienId { get; set; }
    [ForeignKey(nameof(GiaoVienId))]
    public GiaoVien GiaoVien { get; set; } = null!;
    public int ThoiLuong { get; set; }
    public double TongDiem { get; set; }
    public int LoaiDeThi { get; set; } //1: Trắc nghiệm 2 : Tư luận 3: kết hợp

    // Phát hành: chỉ đề đã phát hành mới được dùng để tổ chức thi. Đề đã phát hành bị khóa
    // (không sửa/xóa đề, không sửa câu hỏi/đáp án trong đề) cho tới khi hủy phát hành.
    public bool DaPhatHanh { get; set; }
    public DateTime? NgayPhatHanh { get; set; } // UTC

}