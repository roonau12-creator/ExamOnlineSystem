using ExamOnlineSystem.Models;
using ExamOnlineSystem.Models.ViewModels;

namespace ExamOnlineSystem.Business.Service.IService;

public interface IKhoaService
{
    Task<IEnumerable<Khoa>> GetAllKhoaByAsync();
    Task<Khoa> CreateKhoaByAsync(Khoa khoa);
    Task UpdateKhoaAsync(Khoa khoa);
    Task DeleteKhoaAsync(int id);
    Task<Khoa?> GetKhoaByIdAsync(int id);

    // Chi tiết khoa: bộ môn, lớp, môn học, giảng viên và số liệu tổng hợp. Trả về null nếu không có khoa.
    Task<KhoaDetailVM?> GetKhoaDetailAsync(int id);

    // Nhập nhiều khoa: mã khoa đã tồn tại -> cập nhật, chưa có -> thêm mới. Lưu một lần.
    Task<(int Added, int Updated)> ImportKhoaAsync(IEnumerable<Khoa> khoas);
}
