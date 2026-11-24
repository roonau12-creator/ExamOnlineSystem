namespace ExamOnlineSystem.Models.ViewModels;

// Trang chi tiết lớp: thông tin lớp + danh sách sinh viên của lớp
public class LopDetailVM
{
    public Lop Lop { get; set; } = null!;
    public List<SinhVien> SinhViens { get; set; } = new();
}
