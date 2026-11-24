using ExamOnlineSystem.Models;
using ExamOnlineSystem.Models.ViewModels;

namespace ExamOnlineSystem.Business.Service.IService;

public interface IDeThiService
{
    // Danh sách đề thi kèm môn học, giáo viên, số câu và tổng điểm các câu; sắp theo môn học rồi mã đề
    Task<List<DeThiListItemVM>> GetAllDeThiAsync();

    // Mã đề đã tồn tại chưa (không phân biệt hoa thường); boQuaId: bỏ qua chính đề đang sửa
    Task<bool> MaDeThiExistsAsync(string maDeThi, int? boQuaId = null);

    // Câu hỏi trong ngân hàng thuộc một môn học (để chọn vào đề)
    Task<List<CauHoiChonVaoDeVM>> GetCauHoiTheoMonHocAsync(int monHocId);

    // Thông tin các câu hỏi theo Id (kèm môn học, loại, điểm mặc định, số đáp án) — dùng khi nhập Excel
    Task<List<CauHoiChonVaoDeVM>> GetCauHoiTheoIdsAsync(IEnumerable<int> ids);

    // Tất cả mã đề hiện có (đã chuẩn hóa chữ thường) — dùng khi nhập Excel
    Task<HashSet<string>> GetAllMaDeThiAsync();

    // Thêm nhiều đề thi cùng câu hỏi của từng đề (ThuTu theo thứ tự danh sách), lưu một lần
    Task<int> ImportDeThiAsync(List<(DeThi DeThi, List<CauHoiDeThi> CauHois)> deThis);

    // Thêm đề thi cùng các câu hỏi (ThuTu theo thứ tự danh sách), lưu một lần
    Task<DeThi> CreateDeThiAsync(DeThi deThi, List<CauHoiDeThi> cauHois);

    // Đề thi kèm giáo viên (không theo dõi)
    Task<DeThi?> GetDeThiByIdAsync(int id);
    Task<List<CauHoiDeThi>> GetCauHoiDeThisAsync(int deThiId); // sắp theo ThuTu

    // Cập nhật thông tin đề (giữ nguyên giáo viên) và đồng bộ câu hỏi theo CauHoiId:
    // câu giữ lại -> cập nhật điểm/thứ tự, câu mới -> thêm, câu bị bỏ -> xóa. Lưu một lần.
    Task UpdateDeThiAsync(DeThi deThi, List<CauHoiDeThi> cauHois);

    // Tất cả câu hỏi trong các đề (bảng CauHoiDeThis), sắp theo mã đề rồi câu số
    Task<List<CauHoiDeThiListItemVM>> GetAllCauHoiDeThiAsync();

    // Thêm 1 câu vào đề: viTri null/<=0/lớn hơn số câu => cuối đề; ngược lại chèn thành câu số viTri
    // (các câu từ viTri trở đi lùi xuống 1). Trả về câu số thực tế. Lưu một lần.
    Task<int> AddCauHoiVaoDeAsync(int deThiId, int cauHoiId, double diem, int? viTri);

    // Sửa 1 câu trong đề: đổi câu hỏi (thứ tự đáp án đã trộn bị bỏ vì câu mới có đáp án khác),
    // đổi điểm, chuyển sang vị trí viTri (các câu khác dồn lại, đánh số 1..n). Lưu một lần.
    Task UpdateCauHoiDeThiAsync(int id, int cauHoiId, double diem, int viTri);

    // Sắp xếp lại toàn bộ câu trong đề theo danh sách Id dòng câu-đề (thứ tự mới = thứ tự trong danh sách).
    // Danh sách phải gồm đúng các câu đang có trong đề (không thiếu, không thừa, không trùng); sai => false, không lưu.
    Task<bool> SapXepCauHoiAsync(int deThiId, List<int> cauHoiDeThiIds);

    // Phân bổ điểm: đặt tổng điểm đề và điểm từng câu (theo Id dòng câu-đề) trong một lần lưu.
    // diemTheoDong phải gồm đúng các câu đang có trong đề; sai => false, không lưu.
    Task<bool> PhanBoDiemAsync(int deThiId, double tongDiem, Dictionary<int, double> diemTheoDong);

    // Một dòng câu-đề kèm đề thi (không theo dõi)
    Task<CauHoiDeThi?> GetCauHoiDeThiByIdAsync(int id);

    // Bỏ 1 câu khỏi đề (câu hỏi trong ngân hàng giữ nguyên) và đánh lại thứ tự 1..n các câu còn lại. Lưu một lần.
    Task RemoveCauHoiKhoiDeAsync(int cauHoiDeThiId);

    // Thêm nhiều câu vào đề cùng lúc (giữ thứ tự danh sách): viTri null/<=0/lớn hơn số câu => cuối đề,
    // ngược lại nhóm câu mới bắt đầu từ câu số viTri (các câu cũ từ đó trở đi lùi xuống). Trả về câu số đầu tiên. Lưu một lần.
    Task<int> AddNhieuCauHoiVaoDeAsync(int deThiId, List<(int CauHoiId, double Diem)> cauHois, int? viTri);

    // ===== Kiểm tra tổng điểm =====
    // Đặt tổng điểm đề = tổng điểm các câu (đề phải có câu). Trả về tổng điểm mới, null nếu đề không có câu.
    Task<double?> DatTongDiemTheoCauAsync(int deThiId);
    // Co giãn điểm các câu theo tỷ lệ hiện tại để tổng = tổng điểm đề. Trả về false nếu đề không có câu.
    Task<bool> CoGianDiemCauAsync(int deThiId);

    // ===== Phát hành =====
    // Các vấn đề khiến đề chưa phát hành được (rỗng = đủ điều kiện)
    Task<List<string>> KiemTraPhatHanhAsync(int deThiId);
    Task DatPhatHanhAsync(int deThiId, bool phatHanh);
    // Đề đã phát hành (được dùng để tổ chức thi), lọc theo môn học nếu có
    Task<List<DeThi>> GetDeThiDaPhatHanhAsync(int? monHocId = null);

    // Trộn lại thứ tự đáp án của các câu trắc nghiệm trong đề (mỗi câu khác thứ tự đang dùng). Trả về số câu đã trộn.
    Task<int> TronDapAnAsync(int deThiId);

    // Xóa đề thi và các liên kết câu hỏi của đề (câu hỏi trong ngân hàng giữ nguyên)
    Task DeleteDeThiAsync(int id);
}
