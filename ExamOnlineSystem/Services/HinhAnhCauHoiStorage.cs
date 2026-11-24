namespace ExamOnlineSystem.Services;

// Lưu ảnh minh họa của câu hỏi vào {ContentRoot}/App_Data/HinhAnhCauHoi (ngoài wwwroot):
// ảnh không truy cập trực tiếp được qua URL tĩnh, chỉ trả về qua action có [Authorize]
// => sinh viên không xem trước được nội dung đề.
public class HinhAnhCauHoiStorage
{
    public const long KichThuocToiDa = 5 * 1024 * 1024; // 5 MB

    // Chỉ nhận ảnh raster phổ biến (không nhận SVG vì có thể chứa script)
    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp"
    };

    private readonly string _thuMuc;

    public HinhAnhCauHoiStorage(IWebHostEnvironment env)
    {
        _thuMuc = Path.Combine(env.ContentRootPath, "App_Data", "HinhAnhCauHoi");
        Directory.CreateDirectory(_thuMuc);
    }

    // Trả về thông báo lỗi, hoặc null nếu file hợp lệ
    public static string? KiemTra(IFormFile file)
    {
        if (file.Length == 0)
        {
            return "File ảnh rỗng";
        }
        if (file.Length > KichThuocToiDa)
        {
            return "Ảnh không được lớn hơn 5 MB";
        }
        if (!ContentTypes.ContainsKey(Path.GetExtension(file.FileName)))
        {
            return "Chỉ hỗ trợ ảnh .jpg, .jpeg, .png, .gif, .webp";
        }

        // Kiểm tra nội dung thật của file (magic bytes), không tin vào đuôi file
        var dau = new byte[12];
        using var s = file.OpenReadStream();
        var n = s.Read(dau, 0, dau.Length);
        if (!LaAnhHopLe(dau.AsSpan(0, n)))
        {
            return "File không phải là ảnh hợp lệ";
        }
        return null;
    }

    private static bool LaAnhHopLe(ReadOnlySpan<byte> b) =>
        (b.Length >= 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) ||                        // JPEG
        (b.Length >= 8 && b[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A })) || // PNG
        (b.Length >= 4 && b[0] == 'G' && b[1] == 'I' && b[2] == 'F' && b[3] == '8') ||           // GIF
        (b.Length >= 12 && b[0] == 'R' && b[1] == 'I' && b[2] == 'F' && b[3] == 'F' &&
                           b[8] == 'W' && b[9] == 'E' && b[10] == 'B' && b[11] == 'P');           // WEBP

    // Lưu file với tên ngẫu nhiên (không dùng tên gốc của người dùng), trả về tên file đã lưu
    public async Task<string> LuuAsync(IFormFile file)
    {
        var tenFile = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
        await using var fs = new FileStream(Path.Combine(_thuMuc, tenFile), FileMode.CreateNew);
        await file.CopyToAsync(fs);
        return tenFile;
    }

    public void Xoa(string? tenFile)
    {
        var duongDan = DuongDan(tenFile);
        if (duongDan != null && File.Exists(duongDan))
        {
            File.Delete(duongDan);
        }
    }

    // Đường dẫn vật lý của file; null nếu tên file không hợp lệ (chặn path traversal)
    public string? DuongDan(string? tenFile)
    {
        if (string.IsNullOrEmpty(tenFile) || Path.GetFileName(tenFile) != tenFile ||
            !ContentTypes.ContainsKey(Path.GetExtension(tenFile)))
        {
            return null;
        }
        return Path.Combine(_thuMuc, tenFile);
    }

    public static string ContentType(string tenFile) =>
        ContentTypes.GetValueOrDefault(Path.GetExtension(tenFile), "application/octet-stream");
}
