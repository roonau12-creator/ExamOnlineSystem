using ExamOnlineSystem.Models;
using ExamOnlineSystem.Models.ViewModels;

namespace ExamOnlineSystem.Business.Service.IService;

public interface ILopHocPhanService
{
    // Danh sách lớp học phần kèm học phần/môn/khoa, năm học, số giảng viên, giảng viên phụ trách và số sinh viên;
    // sắp theo năm học mới nhất, học kỳ rồi mã lớp học phần
    Task<List<LopHocPhanListItemVM>> GetAllLopHocPhanAsync();

    // Lớp học phần kèm học phần (môn học, khoa) và năm học; null nếu không có
    Task<LopHocPhan?> GetLopHocPhanByIdAsync(int id);

    // Chi tiết lớp học phần: thông tin + giảng viên đã phân công + sinh viên trong lớp
    Task<LopHocPhanDetailVM?> GetChiTietAsync(int id);

    Task<LopHocPhan> CreateLopHocPhanAsync(LopHocPhan lopHocPhan);

    // Cập nhật thông tin lớp học phần (không đụng tới phân công). Trả về false nếu lớp không còn tồn tại.
    Task<bool> UpdateLopHocPhanAsync(LopHocPhan lopHocPhan);

    // Xóa lớp học phần. Không xóa nếu còn sinh viên (DB đang cascade: xóa lớp sẽ xóa luôn phân công).
    Task<(bool Success, string Message)> DeleteLopHocPhanAsync(int id);

    // Kiểm tra trùng mã lớp học phần (không phân biệt hoa thường). excludeId: bỏ qua chính lớp đang sửa.
    Task<bool> MaLopHocPhanExistsAsync(string maLopHocPhan, int? excludeId = null);

    // Số sinh viên hiện có trong lớp học phần
    Task<int> DemSinhVienAsync(int lopHocPhanId);

    // --- Phân công giảng viên ---

    // Giảng viên đã phân công, phụ trách lên trước rồi đến mã giảng viên
    Task<List<GiaoVienPhanCongVM>> GetGiaoVienTrongLopAsync(int lopHocPhanId);

    // Giảng viên chưa có trong lớp học phần (kèm bộ môn, khoa, số lớp đang dạy)
    Task<List<GiaoVienPhanCongVM>> GetGiaoVienNgoaiLopAsync(int lopHocPhanId);

    // Phân công các giảng viên đã chọn với cùng một vai trò.
    // Chỉ cho tối đa 1 người phụ trách: đã có người phụ trách thì phải đổi vai trò người đó trước.
    Task<PhanCongResult> PhanCongGiaoVienAsync(int lopHocPhanId, IEnumerable<int> giaoVienIds, int vaiTro);

    // Đổi vai trò của một giảng viên đã phân công. Đặt làm phụ trách thì người phụ trách cũ thành trợ giảng.
    Task<PhanCongResult> DoiVaiTroGiaoVienAsync(int lopHocPhanId, int giaoVienId, int vaiTro);

    // Bỏ phân công các giảng viên đã chọn khỏi lớp học phần
    Task<PhanCongResult> BoGiaoVienAsync(int lopHocPhanId, IEnumerable<int> giaoVienIds);

    // --- Phân sinh viên ---

    // Sinh viên đã có trong lớp học phần (kèm lớp, khoa), sắp theo mã sinh viên
    Task<List<SinhVienChonVM>> GetSinhVienTrongLopAsync(int lopHocPhanId);

    // Sinh viên chưa có trong lớp học phần (kèm lớp, khoa) – ứng viên để phân vào
    Task<List<SinhVienChonVM>> GetSinhVienNgoaiLopAsync(int lopHocPhanId);

    // Thêm các sinh viên đã chọn vào lớp học phần. Từ chối nếu vượt sĩ số tối đa.
    Task<PhanCongResult> ThemSinhVienAsync(int lopHocPhanId, IEnumerable<int> sinhVienIds);

    // Bỏ các sinh viên đã chọn khỏi lớp học phần
    Task<PhanCongResult> BoSinhVienAsync(int lopHocPhanId, IEnumerable<int> sinhVienIds);
}
