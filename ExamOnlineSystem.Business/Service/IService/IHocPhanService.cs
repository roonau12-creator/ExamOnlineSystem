using ExamOnlineSystem.Models;

namespace ExamOnlineSystem.Business.Service.IService;

public interface IHocPhanService
{
    // Lấy tất cả học phần (kèm Khoa, MonHoc), sắp theo STT khoa, STT môn học rồi mã học phần
    Task<IEnumerable<HocPhan>> GetAllHocPhanAsync();
    Task<HocPhan?> GetHocPhanByIdAsync(int id);
    Task<HocPhan> CreateHocPhanAsync(HocPhan hocPhan);
    Task UpdateHocPhanAsync(HocPhan hocPhan);
    Task DeleteHocPhanAsync(int id);
    Task<bool> ExistsAsync(int id);

    // Kiểm tra mã học phần đã được dùng bởi học phần khác (không phân biệt hoa thường)
    Task<bool> IsMaHocPhanDuplicateAsync(string maHocPhan, int excludeId = 0);

    // Tra cứu môn học phục vụ học phần
    Task<IEnumerable<MonHoc>> GetMonHocByKhoaAsync(int khoaId);
    Task<IEnumerable<MonHoc>> GetAllMonHocAsync();
    Task<bool> MonHocThuocKhoaAsync(int monHocId, int khoaId);

    // Nhập nhiều học phần: mã học phần đã tồn tại -> cập nhật, chưa có -> thêm mới. Lưu một lần.
    Task<(int Added, int Updated)> ImportHocPhanAsync(IEnumerable<HocPhan> hocPhans);
}
