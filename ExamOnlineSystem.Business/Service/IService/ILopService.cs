using ExamOnlineSystem.Models;
using ExamOnlineSystem.Models.ViewModels;

namespace ExamOnlineSystem.Business.Service.IService;

public interface ILopService
{
    // Lấy tất cả lớp kèm Khoa, sắp theo STT khoa rồi mã lớp
    Task<IEnumerable<Lop>> GetAllLopAsync();

    // Lấy 1 lớp kèm Khoa; null nếu không có
    Task<Lop?> GetLopByIdAsync(int id);

    // Sinh viên của lớp, sắp theo mã sinh viên
    Task<List<SinhVien>> GetSinhViensByLopAsync(int lopId);

    Task<Lop> CreateLopAsync(Lop lop);

    // Cập nhật mã, tên, khoa của lớp. Đổi khoa thì khoa của các sinh viên trong lớp cũng đổi theo.
    // Trả về false nếu lớp không còn tồn tại.
    Task<bool> UpdateLopAsync(Lop lop);

    // Xóa lớp. Không xóa nếu lớp còn sinh viên (DB đang cascade: xóa lớp sẽ xóa luôn sinh viên).
    Task<(bool Success, string Message)> DeleteLopAsync(int id);

    // Kiểm tra trùng mã lớp (không phân biệt hoa thường, bỏ khoảng trắng đầu/cuối).
    // excludeId: bỏ qua chính lớp đang sửa.
    Task<bool> MaLopExistsAsync(string maLop, int? excludeId = null);

    // Số sinh viên của từng lớp: key = LopId
    Task<Dictionary<int, int>> CountSinhVienByLopAsync();

    // --- Phân sinh viên vào lớp ---

    // Sinh viên của lớp dưới dạng dòng chọn (kèm khoa), sắp theo mã sinh viên
    Task<List<SinhVienChonVM>> GetSinhVienChonByLopAsync(int lopId);

    // Ứng viên để phân vào lớp lopId: sinh viên đang thuộc lớp khác (kèm lớp + khoa hiện tại).
    // Sắp theo khoa, mã lớp rồi mã sinh viên.
    Task<List<SinhVienChonVM>> GetSinhVienNgoaiLopAsync(int lopId);

    // Các lớp khác lopId (kèm khoa, sĩ số) – để chọn lớp chuyển sinh viên sang
    Task<List<LopChonVM>> GetLopKhacAsync(int lopId);

    // Chuyển các sinh viên đã chọn vào lớp lopDichId: đổi LopId, đồng bộ KhoaId của sinh viên
    // và khoa công tác trên tài khoản đăng nhập. Sinh viên đã thuộc lớp đích thì bỏ qua.
    // Ghi trong một transaction.
    Task<PhanLopResult> ChuyenSinhVienVaoLopAsync(int lopDichId, IEnumerable<int> sinhVienIds);
}
