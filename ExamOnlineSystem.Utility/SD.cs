namespace ExamOnlineSystem.Utility;

public static class SD
{
    public const string Role_Admin = "Admin";
    public const string Role_Teacher = "Teacher";
    public const string Role_Student = "Student";
}

// Tên hiển thị tiếng Việt của các vai trò Identity
public static class VaiTroLabel
{
    // Thứ tự dùng cho dropdown chọn vai trò
    public static readonly string[] TatCa = { SD.Role_Admin, SD.Role_Teacher, SD.Role_Student };

    public static string Ten(string? vaiTro) => vaiTro switch
    {
        SD.Role_Admin => "Quản trị viên",
        SD.Role_Teacher => "Giảng viên",
        SD.Role_Student => "Sinh viên",
        _ => "Chưa phân quyền"
    };
}

// Tên hiển thị cho các giá trị số của câu hỏi
public static class CauHoiLabel
{
    // LoaiCauHoi: 1 = Trắc nghiệm, 2 = Tự luận
    public static string TenLoaiCauHoi(int loai) => loai switch
    {
        1 => "Trắc nghiệm",
        2 => "Tự luận",
        _ => "—"
    };

    // MucDo: 1 = Dễ, 2 = Trung bình, 3 = Khó, 4 = Rất khó
    public static string TenMucDo(int mucDo) => mucDo switch
    {
        1 => "Dễ",
        2 => "Trung bình",
        3 => "Khó",
        4 => "Rất khó",
        _ => "—"
    };
}

// Tên hiển thị cho các giá trị số của đề thi
public static class DeThiLabel
{
    // LoaiDeThi: 1 = Trắc nghiệm, 2 = Tự luận, 3 = Kết hợp
    public static string TenLoaiDeThi(int loai) => loai switch
    {
        1 => "Trắc nghiệm",
        2 => "Tự luận",
        3 => "Kết hợp",
        _ => "—"
    };
}

// Tên hiển thị cho các giá trị số của lớp học phần
public static class LopHocPhanLabel
{
    // HocKy: 1 = Học kỳ 1, 2 = Học kỳ 2, 3 = Học kỳ hè
    public static readonly int[] TatCaHocKy = { 1, 2, 3 };

    public static string TenHocKy(int hocKy) => hocKy switch
    {
        1 => "Học kỳ 1",
        2 => "Học kỳ 2",
        3 => "Học kỳ hè",
        _ => "—"
    };

    // VaiTro của giảng viên: 1 = Phụ trách, 2 = Trợ giảng
    public static string TenVaiTro(int vaiTro) => vaiTro switch
    {
        1 => "Phụ trách",
        2 => "Trợ giảng",
        _ => "—"
    };
}
