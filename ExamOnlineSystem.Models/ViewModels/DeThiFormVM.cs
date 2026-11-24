using System.ComponentModel.DataAnnotations;

namespace ExamOnlineSystem.Models.ViewModels;

// Dữ liệu form Thêm đề thi (kèm danh sách câu hỏi được chọn từ ngân hàng)
public class DeThiFormVM
{
    public int Id { get; set; }

    [Display(Name = "Mã đề thi")]
    [Required(ErrorMessage = "Vui lòng nhập mã đề thi")]
    [StringLength(50, ErrorMessage = "Mã đề thi tối đa 50 ký tự")]
    public string MaDeThi { get; set; } = string.Empty;

    [Display(Name = "Tên đề thi")]
    [Required(ErrorMessage = "Vui lòng nhập tên đề thi")]
    public string TenDeThi { get; set; } = string.Empty;

    [Display(Name = "Môn học")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn môn học")]
    public int MonHocId { get; set; }

    // Chỉ dùng khi người nhập không phải giáo viên (vd: Admin) => phải chọn giáo viên
    [Display(Name = "Giáo viên")]
    public int? GiaoVienId { get; set; }

    // Khi sửa: tên giáo viên đã tạo đề (chỉ hiển thị, không đổi)
    public string? TenGiaoVien { get; set; }

    [Display(Name = "Loại đề")]
    [Range(1, 3, ErrorMessage = "Vui lòng chọn loại đề")]
    public int LoaiDeThi { get; set; } = 1; // 1: Trắc nghiệm 2: Tự luận 3: Kết hợp

    [Display(Name = "Thời lượng (phút)")]
    [Range(1, 600, ErrorMessage = "Thời lượng phải từ 1 đến 600 phút")]
    public int ThoiLuong { get; set; } = 45;

    [Display(Name = "Tổng điểm")]
    [Range(0.25, 1000, ErrorMessage = "Tổng điểm phải lớn hơn 0")]
    public double TongDiem { get; set; } = 10;

    // Câu hỏi trong đề, theo đúng thứ tự (câu 1, câu 2, ...)
    public List<CauHoiDeThiInputVM> CauHois { get; set; } = new();
}

public class CauHoiDeThiInputVM
{
    public int CauHoiId { get; set; }
    public double Diem { get; set; }
}

// Một câu hỏi trong ngân hàng để chọn vào đề (trả về cho form qua Ajax)
public class CauHoiChonVaoDeVM
{
    public int Id { get; set; }
    public string NoiDung { get; set; } = string.Empty;
    public int MonHocId { get; set; }
    public int DanhMucCauHoiId { get; set; }
    public int LoaiCauHoi { get; set; }
    public int MucDo { get; set; }
    public double DiemMacDinh { get; set; }
    public bool CoHinhAnh { get; set; }
    public int SoDapAn { get; set; }
    public string GiaiThich { get; set; } = string.Empty;
}
