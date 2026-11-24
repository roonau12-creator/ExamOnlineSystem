using ExamOnlineSystem.Models;

namespace ExamOnlineSystem.Business.Service.IService;

public interface INamHocService
{
    // Danh sách năm học, năm mới nhất lên trước (theo ngày bắt đầu giảm dần)
    Task<IEnumerable<NamHoc>> GetAllNamHocAsync();

    // Lấy 1 năm học; null nếu không có
    Task<NamHoc?> GetNamHocByIdAsync(int id);

    // Năm học đang được đánh dấu là hiện tại; null nếu chưa đặt
    Task<NamHoc?> GetNamHocHienTaiAsync();

    // Thêm năm học. Nếu LaNamHienTai = true thì bỏ cờ hiện tại ở các năm học khác.
    Task<NamHoc> CreateNamHocAsync(NamHoc namHoc);

    // Cập nhật năm học. Trả về false nếu năm học không còn tồn tại.
    Task<bool> UpdateNamHocAsync(NamHoc namHoc);

    // Đặt một năm học làm năm học hiện tại (bỏ cờ ở các năm khác)
    Task<(bool Success, string Message)> DatNamHienTaiAsync(int id);

    // Xóa năm học
    Task<(bool Success, string Message)> DeleteNamHocAsync(int id);

    // Kiểm tra trùng mã năm học (không phân biệt hoa thường). excludeId: bỏ qua chính năm đang sửa.
    Task<bool> MaNamHocExistsAsync(string maNamHoc, int? excludeId = null);

    // Kiểm tra khoảng thời gian có giao với năm học khác không. excludeId: bỏ qua chính năm đang sửa.
    Task<NamHoc?> GetNamHocTrungThoiGianAsync(DateOnly batDau, DateOnly ketThuc, int? excludeId = null);
}
