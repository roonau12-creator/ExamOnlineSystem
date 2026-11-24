namespace ExamOnlineSystem.Models.ViewModels;

// Trang "Gán giảng viên vào bộ môn" (/Admin/BoMon/PhanGiaoVien/{id}):
// giảng viên đang thuộc bộ môn + giảng viên ở bộ môn khác (để gán vào) + các bộ môn khác (để chuyển sang)
public class PhanGiaoVienVaoBoMonVM
{
    public BoMon BoMon { get; set; } = null!; // kèm Khoa
    public List<GiaoVienListItemVM> TrongBoMon { get; set; } = new();
    public List<GiaoVienListItemVM> NgoaiBoMon { get; set; } = new();
    public List<BoMon> BoMonKhac { get; set; } = new(); // kèm Khoa
}
