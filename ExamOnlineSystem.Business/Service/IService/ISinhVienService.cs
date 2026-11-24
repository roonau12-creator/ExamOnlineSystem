using ExamOnlineSystem.Models;
using ExamOnlineSystem.Models.ViewModels;

namespace ExamOnlineSystem.Business.Service.IService;

public interface ISinhVienService
{
    // Tất cả sinh viên kèm Lớp và Khoa (KHÔNG kèm ApplicationUser), sắp theo mã sinh viên
    Task<List<SinhVien>> GetAllSinhVienAsync();

    Task<SinhVien?> GetSinhVienByIdAsync(int id);

    // Chi tiết sinh viên: hồ sơ, tài khoản, lớp học phần, lịch thi và kết quả. Trả về null nếu không có.
    Task<SinhVienDetailVM?> GetSinhVienDetailAsync(int id);

    // Id hồ sơ sinh viên gắn với tài khoản đăng nhập (null nếu tài khoản không có hồ sơ sinh viên)
    Task<int?> GetSinhVienIdByUserIdAsync(string userId);

    // Các môn sinh viên học (qua lớp học phần) hoặc có lịch thi, kèm lớp học phần và kết quả thi của từng môn
    Task<List<MonHocCuaToiItem>> GetMonHocCuaSinhVienAsync(int sinhVienId);

    // Chi tiết một kỳ thi của sinh viên; null nếu sinh viên không có trong lịch thi đó
    Task<KyThiChiTietVM?> GetKyThiCuaSinhVienAsync(int sinhVienId, int lichThiId);

    // Sinh viên tự cập nhật số điện thoại (đồng bộ sang tài khoản đăng nhập)
    Task<bool> CapNhatSoDienThoaiAsync(int sinhVienId, string soDienThoai);

    // excludeId: bỏ qua chính sinh viên đang sửa
    Task<bool> MaSinhVienExistsAsync(string maSinhVien, int? excludeId = null);

    // Email đã dùng cho sinh viên khác hoặc cho một tài khoản đăng nhập khác.
    // excludeId: bỏ qua chính sinh viên đang sửa và tài khoản của sinh viên đó.
    Task<bool> EmailExistsAsync(string email, int? excludeId = null);

    // Tạo tài khoản đăng nhập (vai trò Student) + bản ghi sinh viên trong cùng một transaction.
    // Lỗi -> rollback cả hai, trả về danh sách lỗi.
    Task<(bool Success, IEnumerable<string> Errors)> CreateSinhVienAsync(SinhVienCreateVM vm);

    // Cập nhật sinh viên và đồng bộ tài khoản (email = tên đăng nhập, họ tên, SĐT, khoa) trong một transaction
    Task<(bool Success, IEnumerable<string> Errors)> UpdateSinhVienAsync(SinhVienEditVM vm);

    // Xóa sinh viên + tài khoản đăng nhập của sinh viên (trong một transaction).
    // Tài khoản có vai trò khác ngoài Student (vd Admin) thì chỉ xóa sinh viên, giữ tài khoản.
    // Không cho xóa sinh viên đã có lượt làm bài thi (tránh mất kết quả thi).
    Task<(bool Success, string Message)> DeleteSinhVienAsync(int id);

    // Nhập nhiều sinh viên (mỗi người kèm tài khoản Student). Mã SV đã có -> bỏ qua.
    // Kiểm tra hết trước; có lỗi thì không nhập dòng nào. Ghi trong một transaction.
    Task<SinhVienImportResult> ImportSinhVienAsync(List<SinhVienImportRow> rows, string? matKhauMacDinh);
}
