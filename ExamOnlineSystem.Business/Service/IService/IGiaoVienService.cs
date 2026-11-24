using ExamOnlineSystem.Models.ViewModels;

namespace ExamOnlineSystem.Business.Service.IService;

public interface IGiaoVienService
{
    // Danh sách giáo viên kèm tài khoản, bộ môn/khoa, số câu hỏi và số đề thi đã tạo; sắp theo khoa, bộ môn, mã GV
    Task<List<GiaoVienListItemVM>> GetAllGiaoVienAsync();

    // Chi tiết giảng viên: bộ môn/khoa công tác và các giảng viên cùng bộ môn. Trả về null nếu không có.
    Task<GiaoVienDetailVM?> GetGiaoVienDetailAsync(int id);

    // excludeId: bỏ qua chính giảng viên đang sửa
    Task<bool> MaGiaoVienExistsAsync(string maGiaoVien, int? excludeId = null);

    // Email đã được dùng cho tài khoản khác chưa (excludeId: bỏ qua tài khoản của giảng viên đang sửa)
    Task<bool> EmailExistsAsync(string email, int? excludeId = null);

    // Dữ liệu cho form sửa (kèm email, chức danh từ tài khoản). Trả về null nếu không có.
    Task<GiaoVienEditVM?> GetGiaoVienForEditAsync(int id);

    // Tạo tài khoản (vai trò Teacher) + hồ sơ giảng viên trong một transaction. Trả về Id giảng viên mới.
    Task<(bool Success, int GiaoVienId, IEnumerable<string> Errors)> CreateGiaoVienAsync(GiaoVienCreateVM vm);

    // Cập nhật hồ sơ giảng viên + đồng bộ tài khoản đăng nhập trong một transaction
    Task<(bool Success, IEnumerable<string> Errors)> UpdateGiaoVienAsync(GiaoVienEditVM vm);

    // Gán các giảng viên vào một bộ môn (đổi khoa của tài khoản theo khoa của bộ môn). Lưu một lần.
    Task<(bool Success, string Message)> GanGiaoVienVaoBoMonAsync(int boMonId, IReadOnlyCollection<int> giaoVienIds);

    // Xóa giảng viên + tài khoản đăng nhập (nếu tài khoản chỉ có vai trò Teacher).
    // Không cho xóa khi giảng viên còn câu hỏi, đề thi, lịch thi hoặc được phân công lớp học phần,
    // hoặc khi đó là tài khoản đang đăng nhập.
    Task<(bool Success, string Message)> DeleteGiaoVienAsync(int id, string? currentUserId);
}
