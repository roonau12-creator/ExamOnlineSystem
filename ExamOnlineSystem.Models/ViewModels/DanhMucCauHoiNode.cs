namespace ExamOnlineSystem.Models.ViewModels;

// Một nút trong cây danh mục câu hỏi nhiều cấp
public class DanhMucCauHoiNode
{
    public DanhMucCauHoi DanhMuc { get; set; } = null!;

    // Cấp của danh mục: 1 = danh mục gốc, 2 = con của gốc, ...
    public int Cap { get; set; }

    // Các danh mục tổ tiên, theo thứ tự từ gốc xuống cha trực tiếp
    public List<DanhMucCauHoi> ToTien { get; set; } = new();

    // Đường dẫn danh mục cha, vd: "Ngôn ngữ SQL › Truy vấn nâng cao"
    public string DuongDanCha => string.Join(" › ", ToTien.Select(t => t.TenDanhMuc));

    // Đường dẫn đầy đủ gồm cả chính nó, vd: "Ngôn ngữ SQL › Truy vấn nâng cao"
    public string DuongDanDayDu => string.Join(" › ", ToTien.Select(t => t.TenDanhMuc).Append(DanhMuc.TenDanhMuc));

    // Khóa lọc "|gốc|...|cha|chính nó|": câu hỏi thuộc danh mục X hoặc danh mục con/cháu của X
    // khi khóa lọc của danh mục câu hỏi chứa "|X|" (lọc danh mục cha gồm cả các danh mục con)
    public string KhoaLoc => "|" + string.Join("|", ToTien.Select(t => t.Id).Append(DanhMuc.Id)) + "|";
}
