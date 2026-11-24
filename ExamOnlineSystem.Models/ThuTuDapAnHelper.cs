namespace ExamOnlineSystem.Models;

// Xử lý thứ tự đáp án riêng của từng câu trong đề (CauHoiDeThi.ThuTuDapAn)
public static class ThuTuDapAnHelper
{
    // Sắp đáp án theo thứ tự đã lưu của đề. Bền với việc sửa câu hỏi sau khi trộn:
    // Id không còn tồn tại thì bỏ qua, đáp án mới (chưa có trong chuỗi) nối vào cuối theo ThuTu gốc.
    public static List<DapAn> SapXep(IEnumerable<DapAn> dapAns, string? thuTuDapAn)
    {
        var goc = dapAns.OrderBy(d => d.ThuTu).ThenBy(d => d.Id).ToList();
        if (string.IsNullOrWhiteSpace(thuTuDapAn))
        {
            return goc;
        }

        var theoId = goc.ToDictionary(d => d.Id);
        var ketQua = new List<DapAn>();
        foreach (var phan in thuTuDapAn.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(phan, out var id) && theoId.Remove(id, out var dapAn))
            {
                ketQua.Add(dapAn);
            }
        }
        ketQua.AddRange(goc.Where(d => theoId.ContainsKey(d.Id)));
        return ketQua;
    }

    // Trộn ngẫu nhiên (Fisher–Yates) danh sách đáp án, cố gắng khác thứ tự hiện tại.
    // Trả về chuỗi Id để lưu vào CauHoiDeThi.ThuTuDapAn; ít hơn 2 đáp án -> null (không cần trộn).
    public static string? Tron(IReadOnlyList<DapAn> thuTuHienTai)
    {
        if (thuTuHienTai.Count < 2)
        {
            return null;
        }
        var hienTai = thuTuHienTai.Select(d => d.Id).ToList();
        List<int> tron;
        do
        {
            tron = hienTai.ToList();
            for (var k = tron.Count - 1; k > 0; k--)
            {
                var j = Random.Shared.Next(k + 1);
                (tron[k], tron[j]) = (tron[j], tron[k]);
            }
        } while (tron.SequenceEqual(hienTai)); // n >= 2 nên luôn có thứ tự khác
        return string.Join(",", tron);
    }

    public static string Nhan(int viTri) => viTri < 26 ? ((char)('A' + viTri)).ToString() : (viTri + 1).ToString();
}
