namespace ExamOnlineSystem.Models.ViewModels;

// Một dòng danh mục câu hỏi đọc từ file Excel (đã kiểm tra định dạng). Dong = số dòng trong file để báo lỗi.
public class DanhMucCauHoiImportRow
{
    public int Dong { get; set; }
    public int MonHocId { get; set; }
    public string TenDanhMuc { get; set; } = string.Empty;

    // Đường dẫn danh mục cha từ gốc xuống cha trực tiếp; rỗng = danh mục gốc
    public List<string> DuongDanCha { get; set; } = new();
    public string MoTa { get; set; } = string.Empty;
}

public class DanhMucCauHoiImportResult
{
    public int Added { get; set; }
    public int Updated { get; set; }
    public List<string> Errors { get; set; } = new();    // có lỗi -> không nhập dòng nào
}
