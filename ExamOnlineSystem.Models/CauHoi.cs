using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamOnlineSystem.Models;

public class CauHoi
{
    [Key]
    public int Id { get; set; }
    public int DanhMucCauHoiId { get; set; }
    [ForeignKey(nameof(DanhMucCauHoiId))]
    public DanhMucCauHoi DanhMucCauHoi { get; set; }
    public int GiaoVienId { get; set; }
    [ForeignKey(nameof(GiaoVienId))]
    public GiaoVien GiaoVien { get; set; }
    public string NoiDung { get; set; }
    public int LoaiCauHoi { get; set; } // 1: Trắc nghiệm 2:Tự luận
    public int MucDo { get; set; } // 1: dễ 2: trung bình 3:khó 4:rất khó
    public double DiemMacDinh { get; set; }
    public string GiaiThich { get; set; }

    // Tên file ảnh minh họa (lưu trong App_Data/HinhAnhCauHoi, ngoài wwwroot); null = không có ảnh
    public string? HinhAnh { get; set; }
}