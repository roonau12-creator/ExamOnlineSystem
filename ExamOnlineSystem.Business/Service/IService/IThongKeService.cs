using ExamOnlineSystem.Models.ViewModels;

namespace ExamOnlineSystem.Business.Service.IService;

public interface IThongKeService
{
    // Dashboard tổng quan: gộp 12 nhóm số liệu của toàn hệ thống (tài khoản, giảng viên, sinh viên,
    // khoa, bộ môn, môn học, đề thi, lịch thi, lượt dự thi, kết quả chấm), biểu đồ theo
    // tháng – học kỳ – năm học và danh sách hoạt động gần đây
    Task<ThongKeDashboardVM> GetDashboardAsync();

    // Thống kê toàn bộ giảng viên: câu hỏi, đề thi, lịch thi, lớp học phần, kết quả thi;
    // kèm số liệu gộp theo khoa và bộ môn
    Task<ThongKeGiaoVienTongQuanVM> GetThongKeGiaoVienAsync();

    // Thống kê chi tiết một giảng viên kèm danh sách đề thi, lịch thi, lớp học phần, danh mục câu hỏi.
    // null khi không tìm thấy giảng viên.
    Task<ThongKeGiaoVienChiTietVM?> GetThongKeGiaoVienByIdAsync(int giaoVienId);

    // Thống kê toàn bộ sinh viên: lịch thi được giao, bài đã nộp, điểm trung bình, xếp loại,
    // lớp học phần; kèm số liệu gộp theo khoa và lớp
    Task<ThongKeSinhVienTongQuanVM> GetThongKeSinhVienAsync();

    // Thống kê chi tiết một sinh viên kèm kết quả từng lịch thi và lớp học phần đã đăng ký.
    // null khi không tìm thấy sinh viên.
    Task<ThongKeSinhVienChiTietVM?> GetThongKeSinhVienByIdAsync(int sinhVienId);

    // Thống kê toàn bộ khoa: tổ chức / nhân sự, chương trình - ngân hàng đề và kết quả thi
    Task<ThongKeKhoaTongQuanVM> GetThongKeKhoaAsync();

    // Thống kê chi tiết một khoa kèm danh sách bộ môn, lớp và môn học.
    // null khi không tìm thấy khoa.
    Task<ThongKeKhoaChiTietVM?> GetThongKeKhoaByIdAsync(int khoaId);

    // Thống kê toàn bộ bộ môn: nhân sự, chương trình - ngân hàng đề và kết quả thi
    Task<ThongKeBoMonTongQuanVM> GetThongKeBoMonAsync();

    // Thống kê chi tiết một bộ môn kèm danh sách giảng viên và môn học.
    // null khi không tìm thấy bộ môn.
    Task<ThongKeBoMonChiTietVM?> GetThongKeBoMonByIdAsync(int boMonId);
}
