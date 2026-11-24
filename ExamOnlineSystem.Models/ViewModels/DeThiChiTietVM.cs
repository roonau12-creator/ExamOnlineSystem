namespace ExamOnlineSystem.Models.ViewModels;

// Trang chi tiết đề thi: đề theo đúng thứ tự câu và thứ tự đáp án của đề (sau khi trộn) + bảng đáp án
public class DeThiChiTietVM
{
    public DeThi DeThi { get; set; } = null!;
    public string TenMonHoc { get; set; } = string.Empty;
    public string TenGiaoVien { get; set; } = string.Empty;
    public bool CoQuyenSua { get; set; }

    // Các vấn đề khiến đề chưa phát hành được (chỉ tính khi đề đang là nháp); rỗng = đủ điều kiện
    public List<string> VanDePhatHanh { get; set; } = new();

    // Lưu ý khi xem trước (không chặn phát hành), vd: câu nhiều đáp án đúng, thiếu giải thích...
    public List<string> LuuY { get; set; } = new();
    public List<CauTrongDeVM> CauHois { get; set; } = new();

    public double TongDiemCau => Math.Round(CauHois.Sum(c => c.Diem), 2);
}

public class CauTrongDeVM
{
    public int Id { get; set; } // Id dòng CauHoiDeThi (dùng để bỏ câu khỏi đề)
    public int ThuTu { get; set; }
    public int CauHoiId { get; set; }
    public string NoiDung { get; set; } = string.Empty;
    public int LoaiCauHoi { get; set; }
    public int MucDo { get; set; }
    public double Diem { get; set; }
    public bool CoHinhAnh { get; set; }
    public string GiaiThich { get; set; } = string.Empty;

    // Đáp án theo thứ tự của đề (đã áp dụng trộn đáp án nếu có)
    public List<DapAn> DapAns { get; set; } = new();

    // Vị trí (A, B, C...) của mỗi đáp án trong câu hỏi gốc — để đối chiếu sau khi trộn
    public Dictionary<int, string> NhanGoc { get; set; } = new();

    public bool DaTronDapAn { get; set; }

    // Đáp án đúng theo nhãn của đề, vd "C" hoặc "A, C"
    public string DapAnDung => LoaiCauHoi != 1
        ? "Tự luận"
        : string.Join(", ", DapAns.Select((d, i) => (d, i)).Where(x => x.d.DapAnDung).Select(x => ThuTuDapAnHelper.Nhan(x.i)));
}
