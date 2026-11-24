namespace ExamOnlineSystem.Models.ViewModels;

// Trang chi tiết câu hỏi: thông tin câu hỏi + đáp án + các đề thi đang dùng câu hỏi
public class CauHoiDetailVM
{
    public CauHoi CauHoi { get; set; } = null!;
    public string TenMonHoc { get; set; } = string.Empty;

    // Đường dẫn danh mục từ gốc tới danh mục chứa câu hỏi (dùng cho breadcrumb)
    public List<DanhMucCauHoi> DuongDanDanhMuc { get; set; } = new();

    public List<DapAn> DapAns { get; set; } = new();
    public List<CauHoiDeThi> CauHoiDeThis { get; set; } = new(); // kèm DeThi

    // Người đang xem có được sửa/xóa câu hỏi này không (Admin hoặc giáo viên tạo câu hỏi)
    public bool CoQuyenSua { get; set; }
}
