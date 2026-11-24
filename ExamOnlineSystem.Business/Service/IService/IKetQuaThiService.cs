using ExamOnlineSystem.Models.ViewModels;

namespace ExamOnlineSystem.Business.Service.IService;

public interface IKetQuaThiService
{
    // Tổng hợp kết quả của tất cả lịch thi (mới nhất trước): số sinh viên theo trạng thái,
    // điểm trung bình / cao nhất / thấp nhất và số bài đạt
    Task<List<KetQuaThiListItemVM>> GetTongHopKetQuaAsync();

    // Kết quả chi tiết của một lịch thi: danh sách sinh viên kèm điểm và phân bố điểm.
    // null khi không tìm thấy lịch thi.
    Task<KetQuaThiChiTietVM?> GetKetQuaByLichThiAsync(int lichThiId);

    // Bài làm của một lần thi: từng câu hỏi, đáp án sinh viên đã chọn và đáp án đúng.
    // null khi không tìm thấy lần thi.
    Task<BaiThiChiTietVM?> GetBaiThiAsync(int lanThiId);
}
