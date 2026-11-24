using ExamOnlineSystem.Models;
using ExamOnlineSystem.Models.ViewModels;

namespace ExamOnlineSystem.Business.Service.IService;

public interface ICauHoiService
{
    // Danh sách câu hỏi kèm tên danh mục, môn học, giáo viên và số đáp án; sắp theo môn học rồi Id
    Task<List<CauHoiListItemVM>> GetAllCauHoiAsync();

    // Thêm câu hỏi cùng các đáp án (ThuTu đánh theo thứ tự trong danh sách), lưu một lần
    Task<CauHoi> CreateCauHoiAsync(CauHoi cauHoi, IEnumerable<DapAn> dapAns);

    // Câu hỏi kèm giáo viên và danh mục (không theo dõi), dùng cho form sửa
    Task<CauHoi?> GetCauHoiByIdAsync(int id);
    Task<List<DapAn>> GetDapAnsAsync(int cauHoiId); // sắp theo ThuTu

    // Tất cả đáp án kèm thông tin câu hỏi, sắp theo môn học, câu hỏi rồi thứ tự đáp án
    Task<List<DapAnListItemVM>> GetAllDapAnAsync();

    // Câu hỏi trắc nghiệm cho dropdown; giaoVienId != null => chỉ lấy câu của giáo viên đó
    Task<List<CauHoiLuaChonVM>> GetCauHoiTracNghiemAsync(int? giaoVienId);

    // Thêm 1 đáp án vào cuối danh sách đáp án của câu hỏi (ThuTu = lớn nhất + 1)
    Task<DapAn> AddDapAnAsync(DapAn dapAn);

    Task<DapAn?> GetDapAnByIdAsync(int id); // không theo dõi
    // Sửa nội dung và đúng/sai của 1 đáp án (giữ nguyên câu hỏi và thứ tự)
    Task UpdateDapAnAsync(int id, string noiDung, bool dapAnDung);

    // Đáp án của nhiều câu hỏi (dùng khi nhập Excel), nhóm theo câu hỏi, mỗi nhóm sắp theo ThuTu
    Task<Dictionary<int, List<DapAn>>> GetDapAnsTheoCauHoiAsync(IEnumerable<int> cauHoiIds);

    // Thêm nhiều đáp án: với mỗi câu hỏi, đáp án mới nối tiếp sau đáp án cuối (ThuTu tăng dần). Lưu một lần.
    Task<int> ImportDapAnAsync(List<DapAn> dapAns);

    // Xóa 1 đáp án và đánh lại ThuTu 1..n cho các đáp án còn lại của câu hỏi (lưu một lần)
    Task DeleteDapAnAsync(int id);
    Task<int> CountDeThiDangDungAsync(int cauHoiId);

    // Mã các đề ĐÃ PHÁT HÀNH đang chứa từng câu hỏi (câu nào không có thì không có trong dictionary).
    // Câu hỏi trong đề đã phát hành bị khóa: không sửa câu hỏi, không thêm/sửa/xóa đáp án.
    Task<Dictionary<int, List<string>>> GetDePhatHanhChuaCauAsync(IEnumerable<int> cauHoiIds);
    Task<List<CauHoiDeThi>> GetCauHoiDeThisAsync(int cauHoiId); // kèm DeThi, sắp theo mã đề

    // Cập nhật câu hỏi (giữ nguyên giáo viên). Đáp án: Id > 0 -> sửa đáp án cũ, Id = 0 -> thêm mới,
    // đáp án cũ không còn trong danh sách -> xóa. ThuTu đánh lại theo thứ tự trong danh sách. Lưu một lần.
    Task UpdateCauHoiAsync(CauHoi cauHoi, List<DapAn> dapAns);

    // Khóa nhận diện câu hỏi đã có: "{DanhMucCauHoiId}|{nội dung chuẩn hóa}" (dùng để bỏ qua câu trùng khi nhập)
    Task<HashSet<string>> GetCauHoiKeysAsync();
    static string TaoKhoa(int danhMucId, string noiDung) =>
        $"{danhMucId}|{string.Join(' ', noiDung.Normalize().ToLowerInvariant()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))}";

    // Thêm nhiều câu hỏi (kèm đáp án) của cùng một giáo viên, lưu một lần
    Task<int> ImportCauHoiAsync(List<CauHoiImportRow> rows, int giaoVienId);

    // Xóa câu hỏi cùng các đáp án của nó
    Task DeleteCauHoiAsync(int id);

    // Giáo viên gắn với tài khoản đăng nhập (null nếu tài khoản không phải giáo viên)
    Task<GiaoVien?> GetGiaoVienByUserIdAsync(string userId);
    Task<List<GiaoVien>> GetAllGiaoVienAsync();
    Task<bool> GiaoVienExistsAsync(int id);
}
