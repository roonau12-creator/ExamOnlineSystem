using ExamOnlineSystem.Models;
using ExamOnlineSystem.Models.ViewModels;

namespace ExamOnlineSystem.Business.Service.IService;

public interface IBoMonService
{
    // Lấy tất cả bộ môn (kèm Khoa), sắp theo STT khoa rồi STT bộ môn
    Task<IEnumerable<BoMon>> GetAllBoMonAsync(bool includeKhoa = false);
    Task<BoMon?> GetBoMonByIdAsync(int id,bool includeKhoa = false);
    Task<BoMon> CreateBoMonAsync(BoMon boMon);
    Task UpdateBoMonAsync(BoMon boMon);
    Task DeleteBoMonAsync(int id);
    Task<bool> ExistsAsync(int id);

    // Chi tiết bộ môn: môn học, học phần, giảng viên và số liệu tổng hợp. Trả về null nếu không có bộ môn.
    Task<BoMonDetailVM?> GetBoMonDetailAsync(int id);

    // Nhập nhiều bộ môn: mã bộ môn đã tồn tại -> cập nhật, chưa có -> thêm mới. Lưu một lần.
    Task<(int Added, int Updated)> ImportBoMonAsync(IEnumerable<BoMon> boMons);
}
