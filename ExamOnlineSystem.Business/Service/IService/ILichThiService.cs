using ExamOnlineSystem.Models;
using ExamOnlineSystem.Models.ViewModels;

namespace ExamOnlineSystem.Business.Service.IService;

public interface ILichThiService
{
    // Danh sách lịch thi kèm đề, môn, giáo viên, các lớp tham gia và số sinh viên; sắp theo thời gian bắt đầu
    Task<List<LichThiListItemVM>> GetAllLichThiAsync();

    // Số lịch thi đang dùng đề (để chặn hủy phát hành / xóa đề)
    Task<int> CountLichThiTheoDeAsync(int deThiId);

    // Các lớp (kèm khoa, sĩ số) để chọn vào lịch thi; sắp theo khoa rồi tên lớp
    Task<List<LopChonVM>> GetLopChoLichThiAsync();

    // Lịch thi khác có khung giờ giao với [batDau, ketThuc] (UTC) và có chung lớp; boQuaId: bỏ qua chính lịch đang sửa
    Task<List<TrungLichVM>> GetLichTrungGioAsync(IEnumerable<int> lopIds, DateTime batDau, DateTime ketThuc, int? boQuaId = null);

    // Tạo lịch thi cùng các lớp tham gia, lưu một lần
    Task<LichThi> CreateLichThiAsync(LichThi lichThi, IEnumerable<int> lopIds);

    // Lịch thi kèm các dòng lớp tham gia (không theo dõi; thời gian Kind=Utc)
    Task<LichThi?> GetLichThiByIdAsync(int id);

    // Cập nhật lịch thi (giữ nguyên giáo viên phụ trách, ngày tạo) và đồng bộ lớp tham gia. Lưu một lần.
    Task UpdateLichThiAsync(LichThi lichThi, IEnumerable<int> lopIds);

    // Đồng bộ sinh viên còn thiếu từ các lớp tham gia vào SinhVienDuThi và trả về danh sách dự thi của lịch thi
    Task<List<SinhVienDuThi>> GetSinhVienDuThiByLichThiAsync(int lichThiId);

    // Xóa lịch thi
    Task DeleteLichThiAsync(int id);
}
