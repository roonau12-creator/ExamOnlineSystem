namespace ExamOnlineSystem.Models.ViewModels;

// Một dòng trong bảng danh sách đề thi (chỉ các trường cần hiển thị)
public class DeThiListItemVM
{
    public int Id { get; set; }
    public string MaDeThi { get; set; } = string.Empty;
    public string TenDeThi { get; set; } = string.Empty;
    public int MonHocId { get; set; }
    public string TenMonHoc { get; set; } = string.Empty;
    public int LoaiDeThi { get; set; }
    public int ThoiLuong { get; set; }            // phút
    public double TongDiem { get; set; }          // tổng điểm khai báo của đề
    public int SoCauHoi { get; set; }
    public double TongDiemCauHoi { get; set; }    // tổng điểm các câu đã thêm vào đề
    public int GiaoVienId { get; set; }
    public string TenGiaoVien { get; set; } = string.Empty;
    public bool DaPhatHanh { get; set; }
    public DateTime? NgayPhatHanh { get; set; }
}

public enum TinhTrangDiem
{
    Lech,
    ChuaCoCau,
    Khop
}

// Một dòng trên trang Kiểm tra tổng điểm
public class KiemTraTongDiemVM
{
    public DeThiListItemVM DeThi { get; set; } = null!;
    public TinhTrangDiem TinhTrang { get; set; }
    public bool DuocSua { get; set; } // đề nháp + có quyền => hiện nút sửa nhanh
    public double ChenhLech => Math.Round(DeThi.TongDiemCauHoi - DeThi.TongDiem, 2);
}
