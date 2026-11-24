using System.ComponentModel.DataAnnotations;

namespace ExamOnlineSystem.Models.ViewModels;

// Form Phân bổ điểm: tổng điểm đề + điểm từng câu trong đề
public class PhanBoDiemVM
{
    public int DeThiId { get; set; }

    [Display(Name = "Tổng điểm đề")]
    [Range(0.25, 1000, ErrorMessage = "Tổng điểm đề phải lớn hơn 0 và không quá 1000")]
    public double TongDiem { get; set; }

    public List<DiemCauVM> Cau { get; set; } = new();
}

public class DiemCauVM
{
    public int Id { get; set; }          // Id dòng câu-đề
    public double Diem { get; set; }

    // Chỉ để hiển thị (không gửi lên/không tin khi lưu)
    public int ThuTu { get; set; }
    public int CauHoiId { get; set; }
    public string NoiDung { get; set; } = string.Empty;
    public int LoaiCauHoi { get; set; }
    public int MucDo { get; set; }
    public double DiemMacDinh { get; set; }
}
