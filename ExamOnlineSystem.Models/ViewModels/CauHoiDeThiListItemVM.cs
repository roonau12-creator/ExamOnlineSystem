namespace ExamOnlineSystem.Models.ViewModels;

// Một dòng trong bảng danh sách câu hỏi đề thi (bảng CauHoiDeThis: câu nào nằm ở vị trí nào, bao nhiêu điểm trong đề nào)
public class CauHoiDeThiListItemVM
{
    public int Id { get; set; }
    public int DeThiId { get; set; }
    public string MaDeThi { get; set; } = string.Empty;
    public string TenDeThi { get; set; } = string.Empty;
    public bool DaPhatHanh { get; set; }
    public string TenMonHoc { get; set; } = string.Empty;
    public int ThuTu { get; set; }          // câu số mấy trong đề
    public int CauHoiId { get; set; }
    public string NoiDung { get; set; } = string.Empty;
    public int LoaiCauHoi { get; set; }
    public int MucDo { get; set; }
    public double Diem { get; set; }        // điểm của câu trong đề này
    public string? ThuTuDapAn { get; set; } // thứ tự đáp án riêng của đề (null = thứ tự gốc)
}
