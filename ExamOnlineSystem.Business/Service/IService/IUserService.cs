using ExamOnlineSystem.Models.ViewModels;

namespace ExamOnlineSystem.Business.Service.IService;

public interface IUserService
{
    // Danh sách tài khoản kèm vai trò, khoa và hồ sơ nghiệp vụ (sinh viên / giảng viên) đang gắn
    Task<List<UserListItemVM>> GetAllUserAsync();

    // Dữ liệu đổ vào form sửa; null nếu không tìm thấy tài khoản
    Task<UserEditVM?> GetUserForEditAsync(string id);

    // Email đã được tài khoản khác dùng chưa (bỏ qua chính tài khoản đang sửa)
    Task<bool> EmailExistsAsync(string email, string? excludeUserId = null);

    Task<(bool Success, IEnumerable<string> Errors)> CreateUserAsync(UserCreateVM vm);

    // currentUserId: tài khoản đang đăng nhập – không cho tự hạ quyền chính mình
    Task<(bool Success, IEnumerable<string> Errors)> UpdateUserAsync(UserEditVM vm, string currentUserId);

    // khoa = true: khóa tài khoản (không đăng nhập được nữa), false: mở khóa
    Task<(bool Success, string Message)> SetTrangThaiAsync(string id, bool khoa, string currentUserId);
}
