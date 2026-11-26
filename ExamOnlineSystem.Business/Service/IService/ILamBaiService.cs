using ExamOnlineSystem.Models.ViewModels;

namespace ExamOnlineSystem.Business.Service.IService;

// Làm bài thi của sinh viên: mở lượt thi, lưu bài theo từng câu và nộp bài (chấm tự động phần trắc nghiệm).
// Mọi phương thức đều nhận sinhVienId và chỉ làm việc trên dữ liệu của chính sinh viên đó.
public interface ILamBaiService
{
    // Mở lượt làm bài: kiểm tra lại điều kiện vào thi, tiếp tục lượt đang làm dở hoặc tạo lượt mới
    // (sao chép đề thành câu hỏi / đáp án của lần thi). Trả về Id lần thi khi thành công.
    Task<(bool Success, string Message, int LanThiId)> BatDauAsync(int sinhVienId, int lichThiId);

    // Dữ liệu trang làm bài (không chứa đáp án đúng). Hết giờ thì tự nộp trước khi trả về.
    // null = lần thi không thuộc sinh viên này hoặc không tồn tại.
    Task<LamBaiVM?> GetBaiLamAsync(int sinhVienId, int lanThiId);

    // Lưu bài làm của một câu: danh sách đáp án đã chọn (trắc nghiệm) hoặc nội dung trả lời (tự luận).
    // Trả về số câu đã làm sau khi lưu để trang cập nhật tiến độ.
    Task<(bool Success, string Message, int SoCauDaLam)> LuuCauAsync(int sinhVienId, int lanThiId,
        int cauHoiLanThiId, List<int> dapAnLanThiIds, string? noiDungTraLoi);

    // Nộp bài: chấm tự động các câu trắc nghiệm, đóng lượt thi và cập nhật trạng thái dự thi.
    // tuDong = true khi hết giờ (trạng thái lần thi là "Hết giờ" thay vì "Đã nộp bài").
    Task<(bool Success, string Message)> NopBaiAsync(int sinhVienId, int lanThiId, bool tuDong);

    // Đình chỉ lượt thi vì sinh viên mở / chuyển sang tab khác: khóa bài (trạng thái "Đình chỉ"),
    // chấm phần đã lưu và không cho vào thi lại.
    Task<(bool Success, string Message)> DinhChiAsync(int sinhVienId, int lanThiId);

    // Kết quả ngay sau khi nộp; null nếu lần thi không thuộc sinh viên hoặc chưa nộp
    Task<KetQuaLamBaiVM?> GetKetQuaAsync(int sinhVienId, int lanThiId);

    // Tên file ảnh minh họa của một câu trong lần thi của chính sinh viên (null = không có)
    Task<string?> GetTenFileHinhAnhAsync(int sinhVienId, int cauHoiLanThiId);
}
