using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamOnlineSystem.Models;

public class SinhVien
{
    [Key]
    public int Id { get; set; }
    // Id của IdentityUser là string (GUID) nên khóa ngoại cũng phải là string
    public string ApplicationUserId { get; set; }
    [ForeignKey(nameof(ApplicationUserId))]
    public ApplicationUser? ApplicationUser { get; set; }
    public string MaSinhVien { get; set; }
    public string HoTen { get; set; }
    // DateOnly -> cột "date": chỉ lưu ngày, tránh lỗi Npgsql bắt DateTime phải là UTC
    public DateOnly NgaySinh { get; set; }
    public bool GioiTinh { get; set; }
    public int KhoaId { get; set; }
    [ForeignKey(nameof(KhoaId))]
    public Khoa? Khoa { get; set; }
    public int LopId { get; set; }
    [ForeignKey(nameof(LopId))]
    public Lop? Lop { get; set; }
    public string Email { get; set; }
    public string SoDienThoai { get; set; }
    
}