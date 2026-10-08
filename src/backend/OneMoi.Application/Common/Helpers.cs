using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace OneMoi.Application.Common;

public static partial class Helpers
{
    /// <summary>"+91 98765-43210" → "9876543210". Returns null if not a valid Indian mobile.</summary>
    public static string? NormalizeMobile(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        var digits = new string(input.Where(char.IsDigit).ToArray());
        if (digits.Length == 12 && digits.StartsWith("91")) digits = digits[2..];
        if (digits.Length == 11 && digits.StartsWith("0")) digits = digits[1..];
        return MobileRegex().IsMatch(digits) ? digits : null;
    }

    public static bool IsEmail(string? input) => !string.IsNullOrWhiteSpace(input) && EmailRegex().IsMatch(input.Trim());

    public static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /// <summary>"n" / "N" / "N." → "N."; "SK" → "S.K."</summary>
    public static string? NormalizeInitial(string? initial)
    {
        if (string.IsNullOrWhiteSpace(initial)) return null;
        var letters = initial.Where(char.IsLetter).Select(char.ToUpperInvariant).ToArray();
        return letters.Length == 0 ? null : string.Join("", letters.Select(c => c + "."));
    }

    public static string MaskMobile(string? m) => string.IsNullOrEmpty(m) || m.Length < 10 ? (m ?? "") : $"{m[..2]}xxx xx{m[^3..]}";

    /// <summary>Random code without look-alike characters (no 0/O, 1/I).</summary>
    public static string RandomCode(int length)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return new string(Enumerable.Range(0, length).Select(_ => chars[RandomNumberGenerator.GetInt32(chars.Length)]).ToArray());
    }

    public static string RandomDigits(int length) =>
        string.Concat(Enumerable.Range(0, length).Select(_ => RandomNumberGenerator.GetInt32(10)));

    [GeneratedRegex("^[6-9][0-9]{9}$")] private static partial Regex MobileRegex();
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")] private static partial Regex EmailRegex();
}
