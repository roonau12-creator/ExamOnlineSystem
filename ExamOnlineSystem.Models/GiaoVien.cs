using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamOnlineSystem.Models;

public class GiaoVien
{
    [Key]
    public int Id { get; set; }
    // Id của IdentityUser là string (GUID) nên khóa ngoại cũng phải là string
    public string ApplicationUserId { get; set; } = string.Empty;
    [ForeignKey(nameof(ApplicationUserId))]
    public ApplicationUser? ApplicationUser { get; set; }
    public string MaGiaoVien { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public int BoMonId { get; set; }
    [ForeignKey(nameof(BoMonId))]
    public BoMon? BoMon { get; set; }
    public string SoDienThoai { get; set; } = string.Empty;
}
