namespace ExamOnlineSystem.Models.ViewModels;

// Trang chi tiết danh mục câu hỏi: thông tin danh mục + danh mục con + câu hỏi thuộc danh mục
public class DanhMucCauHoiDetailVM
{
    public DanhMucCauHoi DanhMuc { get; set; } = null!;
    public int Cap { get; set; } = 1;
    // Các danh mục tổ tiên từ gốc xuống cha trực tiếp (dùng cho breadcrumb)
    public List<DanhMucCauHoi> ToTien { get; set; } = new();
    public List<DanhMucCauHoi> DanhMucCons { get; set; } = new();
    public List<CauHoi> CauHois { get; set; } = new();

}
