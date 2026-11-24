namespace ExamOnlineSystem.Models.ViewModels;

// Một dòng trong bảng danh sách đáp án (chỉ các trường cần hiển thị)
public class DapAnListItemVM
{
    public int Id { get; set; }
    public string NoiDung { get; set; } = string.Empty;
    public bool DapAnDung { get; set; }
    public int ThuTu { get; set; }
    public int CauHoiId { get; set; }
    public string NoiDungCauHoi { get; set; } = string.Empty;
    public int MucDo { get; set; }
    public int DanhMucCauHoiId { get; set; }
    public string TenMonHoc { get; set; } = string.Empty;
    public string TenGiaoVien { get; set; } = string.Empty;
}
