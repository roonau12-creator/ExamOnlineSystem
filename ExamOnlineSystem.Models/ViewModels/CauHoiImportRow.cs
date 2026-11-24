namespace ExamOnlineSystem.Models.ViewModels;

// Một câu hỏi đọc từ file Excel (đã kiểm tra hợp lệ). Dong = số dòng trong file để báo lỗi.
public class CauHoiImportRow
{
    public int Dong { get; set; }
    public int DanhMucCauHoiId { get; set; }
    public string NoiDung { get; set; } = string.Empty;
    public int LoaiCauHoi { get; set; }   // 1: Trắc nghiệm 2: Tự luận
    public int MucDo { get; set; }        // 1..4
    public double DiemMacDinh { get; set; }
    public string GiaiThich { get; set; } = string.Empty;
    public List<DapAn> DapAns { get; set; } = new(); // theo thứ tự A, B, C...
}
