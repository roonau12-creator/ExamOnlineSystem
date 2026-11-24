namespace ExamOnlineSystem.Models;

// Phân bổ tổng điểm cho các câu theo trọng số, làm tròn theo bước bằng phương pháp "phần dư lớn nhất":
// tổng các câu luôn đúng bằng tổng điểm (cùng thuật toán với trang Phân bổ điểm - cauhoidethi-phanbodiem.js).
public static class PhanBoDiemHelper
{
    // Bước làm tròn phù hợp: 0.25 nếu đủ cho mỗi câu ít nhất 1 bước, ngược lại 0.01
    public static double BuocPhuHop(double tongDiem, int soCau) =>
        soCau * 0.25 <= tongDiem + 1e-9 ? 0.25 : 0.01;

    // trongSo: trọng số >= 0 của từng câu (tổng = 0 thì chia đều). Trả về điểm từng câu, tổng = tongDiem.
    public static List<double> PhanBo(IReadOnlyList<double> trongSo, double tongDiem, double buoc)
    {
        var n = trongSo.Count;
        if (n == 0)
        {
            return new List<double>();
        }
        var ts = trongSo.Select(w => Math.Max(0, w)).ToList();
        var tongTs = ts.Sum();
        if (tongTs <= 0)
        {
            ts = Enumerable.Repeat(1.0, n).ToList();
            tongTs = n;
        }

        var soDonVi = (int)Math.Round(tongDiem / buoc);
        var lech = Math.Round(tongDiem - soDonVi * buoc, 2); // phần lẻ khi tổng không chia hết cho bước
        var tho = ts.Select(w => w / tongTs * soDonVi).ToList();
        var nguyen = tho.Select(x => (int)Math.Floor(x)).ToList();
        var conLai = soDonVi - nguyen.Sum();
        foreach (var i in Enumerable.Range(0, n)
                     .OrderByDescending(i => tho[i] - nguyen[i]).ThenBy(i => i)
                     .Take(conLai))
        {
            nguyen[i]++;
        }

        var diem = nguyen.Select(u => Math.Round(u * buoc, 2)).ToList();
        if (lech != 0)
        {
            diem[n - 1] = Math.Round(diem[n - 1] + lech, 2); // dồn phần lẻ vào câu cuối
        }
        return diem;
    }
}
