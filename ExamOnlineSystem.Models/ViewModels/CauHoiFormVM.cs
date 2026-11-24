using System.ComponentModel.DataAnnotations;

namespace ExamOnlineSystem.Models.ViewModels;

// Dữ liệu form Thêm câu hỏi (kèm danh sách đáp án cho câu trắc nghiệm)
public class CauHoiFormVM
{
    public int Id { get; set; }

    // Chỉ dùng để lọc dropdown danh mục trên form, không lưu vào câu hỏi
    [Display(Name = "Môn học")]
    public int? MonHocId { get; set; }

    [Display(Name = "Danh mục")]
    [Required(ErrorMessage = "Vui lòng chọn danh mục")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn danh mục")]
    public int DanhMucCauHoiId { get; set; }

    // Chỉ dùng khi người nhập không phải giáo viên (vd: Admin) => phải chọn giáo viên
    [Display(Name = "Giáo viên")]
    public int? GiaoVienId { get; set; }

    // Khi sửa: tên giáo viên đã tạo câu hỏi (chỉ hiển thị, không đổi)
    public string? TenGiaoVien { get; set; }

    // Khi sửa: số đề thi đang dùng câu hỏi này (để cảnh báo)
    public int SoDeThiDangDung { get; set; }

    [Display(Name = "Nội dung câu hỏi")]
    [Required(ErrorMessage = "Vui lòng nhập nội dung câu hỏi")]
    public string NoiDung { get; set; } = string.Empty;

    [Display(Name = "Loại câu hỏi")]
    [Range(1, 2, ErrorMessage = "Vui lòng chọn loại câu hỏi")]
    public int LoaiCauHoi { get; set; } = 1; // 1: Trắc nghiệm 2: Tự luận

    [Display(Name = "Mức độ")]
    [Range(1, 4, ErrorMessage = "Vui lòng chọn mức độ")]
    public int MucDo { get; set; } = 1; // 1: dễ 2: trung bình 3: khó 4: rất khó

    [Display(Name = "Điểm mặc định")]
    [Range(0.01, 100, ErrorMessage = "Điểm mặc định phải lớn hơn 0 và không quá 100")]
    public double DiemMacDinh { get; set; } = 1;

    [Display(Name = "Giải thích / đáp án gợi ý")]
    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public string? GiaiThich { get; set; } = string.Empty;

    // Ảnh minh họa: HinhAnh = tên file ảnh hiện tại (khi sửa), HinhAnhFile = ảnh mới tải lên,
    // XoaHinhAnh = bỏ ảnh hiện tại
    public string? HinhAnh { get; set; }

    [Display(Name = "Ảnh minh họa")]
    public Microsoft.AspNetCore.Http.IFormFile? HinhAnhFile { get; set; }

    public bool XoaHinhAnh { get; set; }

    public List<DapAnInputVM> DapAns { get; set; } = new();
}

public class DapAnInputVM
{
    // 0 = đáp án mới; > 0 = đáp án đã có (khi sửa câu hỏi)
    public int Id { get; set; }
    [DisplayFormat(ConvertEmptyStringToNull = false)]
    public string? NoiDung { get; set; } = string.Empty;
    public bool DapAnDung { get; set; }
}
