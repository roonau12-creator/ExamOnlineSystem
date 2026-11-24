namespace ExamOnlineSystem.Models.ViewModels;

// Một dòng trong bảng danh sách câu hỏi (chỉ các trường cần hiển thị)
public class CauHoiListItemVM
{
    public int Id { get; set; }
    public string NoiDung { get; set; } = string.Empty;
    public int DanhMucCauHoiId { get; set; }
    public string TenDanhMuc { get; set; } = string.Empty;
    public int MonHocId { get; set; }
    public string TenMonHoc { get; set; } = string.Empty;
    public int LoaiCauHoi { get; set; }
    public int MucDo { get; set; }
    public double DiemMacDinh { get; set; }
    public string TenGiaoVien { get; set; } = string.Empty;
    public int SoDapAn { get; set; }
    public bool CoHinhAnh { get; set; }
}
