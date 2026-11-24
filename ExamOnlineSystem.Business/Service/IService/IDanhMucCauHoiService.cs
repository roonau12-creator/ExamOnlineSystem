using ExamOnlineSystem.Models;
using ExamOnlineSystem.Models.ViewModels;

namespace ExamOnlineSystem.Business.Service.IService;

public interface IDanhMucCauHoiService
{
    // Lấy tất cả danh mục câu hỏi (kèm Môn học và Danh mục cha), sắp theo tên môn học rồi tên danh mục
    Task<IEnumerable<DanhMucCauHoi>> GetAllDanhMucCauHoiAsync();
    Task<DanhMucCauHoi?> GetDanhMucCauHoiByIdAsync(int id);

    // Toàn bộ danh mục dạng cây (duyệt theo chiều sâu): mỗi danh mục cha đứng ngay trước các danh mục con của nó
    Task<List<DanhMucCauHoiNode>> GetDanhMucTreeAsync();

    // Dùng cho trang chi tiết
    Task<DanhMucCauHoi?> GetDanhMucCauHoiDetailAsync(int id); // kèm Môn học và Danh mục cha
    Task<List<DanhMucCauHoi>> GetDanhMucConAsync(int id);
    Task<List<CauHoi>> GetCauHoisByDanhMucAsync(int id); // kèm Giáo viên
    Task<DanhMucCauHoi> CreateDanhMucCauHoiAsync(DanhMucCauHoi danhMuc);
    Task UpdateDanhMucCauHoiAsync(DanhMucCauHoi danhMuc);
    Task<bool> ExistsAsync(int id);
    Task<bool> HasChildrenAsync(int id);
    Task<bool> HasCauHoiAsync(int id);
    Task DeleteDanhMucCauHoiAsync(int id);

    // Id của danh mục và toàn bộ danh mục con cháu (không được chọn làm danh mục cha khi sửa)
    Task<HashSet<int>> GetSelfAndDescendantIdsAsync(int id);

    // Nhập nhiều danh mục từ Excel. Danh mục được nhận diện theo (môn học, danh mục cha, tên):
    // đã tồn tại -> cập nhật mô tả, chưa có -> thêm mới. Có lỗi thì không lưu gì. Lưu một lần.
    Task<DanhMucCauHoiImportResult> ImportDanhMucCauHoiAsync(List<DanhMucCauHoiImportRow> rows);

    // Danh sách môn học cho dropdown, sắp theo số thứ tự
    Task<IEnumerable<MonHoc>> GetAllMonHocAsync();
    Task<bool> MonHocExistsAsync(int monHocId);
}
