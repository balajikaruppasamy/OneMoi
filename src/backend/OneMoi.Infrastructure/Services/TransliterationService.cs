using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneMoi.Application.Common;

namespace OneMoi.Infrastructure.Services;

/// <summary>
/// English (Tanglish) → Tamil.
///  1. A small dictionary of common names (so "Ram" → "ராம்", not "ரேம்").
///  2. Google Input Tools (free, online) for everything else — several options per word.
///  3. If offline, a built-in phonetic converter.
/// The operator can always edit the Tamil text before saving.
/// </summary>
public partial class TransliterationService(HttpClient http, IMemoryCache cache, IConfiguration config, ILogger<TransliterationService> log)
    : ITransliterationService
{
    private static readonly Dictionary<string, string> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ram"] = "ராம்", ["raman"] = "ராமன்", ["rama"] = "ராமா", ["geetha"] = "கீதா", ["gita"] = "கீதா", ["sita"] = "சீதா",
        ["murugan"] = "முருகன்", ["murugesan"] = "முருகேசன்", ["lakshmi"] = "லட்சுமி", ["senthil"] = "செந்தில்", ["kumar"] = "குமார்",
        ["ravi"] = "ரவி", ["meena"] = "மீனா", ["meenakshi"] = "மீனாட்சி", ["arun"] = "அருண்", ["divya"] = "திவ்யா", ["surya"] = "சூர்யா",
        ["priya"] = "பிரியா", ["karthik"] = "கார்த்திக்", ["saranya"] = "சரண்யா", ["vignesh"] = "விக்னேஷ்", ["anitha"] = "அனிதா",
        ["kavitha"] = "கவிதா", ["kavya"] = "காவ்யா", ["manikandan"] = "மணிகண்டன்", ["selvi"] = "செல்வி", ["muthu"] = "முத்து",
        ["pollachi"] = "பொள்ளாச்சி", ["coimbatore"] = "கோயம்புத்தூர்", ["tiruppur"] = "திருப்பூர்", ["madurai"] = "மதுரை",
        ["udumalpet"] = "உடுமலைப்பேட்டை", ["palani"] = "பழனி", ["erode"] = "ஈரோடு", ["salem"] = "சேலம்", ["karur"] = "கரூர்",
        ["chennai"] = "சென்னை", ["trichy"] = "திருச்சி", ["thanjavur"] = "தஞ்சாவூர்", ["kinathukadavu"] = "கிணத்துக்கடவு",
        ["thaimaman"] = "தாய்மாமன்", ["moi"] = "மொய்", ["teacher"] = "ஆசிரியர்", ["farmer"] = "விவசாயி", ["business"] = "வியாபாரம்",
        ["illa"] = "இல்லா", ["villa"] = "வில்லா"
    };

    public async Task<TransliterationResult> ToTamilAsync(string text, int maxOptions = 4, CancellationToken ct = default)
    {
        text = (text ?? "").Trim();
        if (text.Length == 0) return new TransliterationResult("", "", Array.Empty<WordSuggestions>(), "none");
        if (text.Length > 200) text = text[..200];

        var tokens = TokenRegex().Matches(text).Select(m => m.Value).ToList();
        var words = new List<WordSuggestions>();
        var output = new StringBuilder();
        var source = "dictionary";

        foreach (var token in tokens)
        {
            if (!WordRegex().IsMatch(token)) { output.Append(token); continue; }   // spaces, dots, digits stay as they are

            var options = new List<string>();
            if (Names.TryGetValue(token, out var known)) options.Add(known);

            var google = await GoogleAsync(token, maxOptions, ct);
            if (google != null) { source = "google"; options.AddRange(google.Where(g => !options.Contains(g))); }
            if (options.Count == 0) { source = "phonetic"; options.Add(TamilPhonetic.Convert(token)); }

            options = options.Take(Math.Max(1, maxOptions)).ToList();
            words.Add(new WordSuggestions(token, options));
            output.Append(options[0]);
        }
        return new TransliterationResult(text, output.ToString(), words, source);
    }

    private async Task<List<string>?> GoogleAsync(string word, int max, CancellationToken ct)
    {
        if (!bool.TryParse(config["Transliteration:UseGoogle"] ?? "true", out var use) || !use) return null;
        var key = "tl:" + word.ToLowerInvariant();
        if (cache.TryGetValue(key, out List<string>? cached)) return cached;
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(3));
            var url = $"https://inputtools.google.com/request?text={Uri.EscapeDataString(word)}&itc=ta-t-i0-und&num={max}&cp=0&cs=1&ie=utf-8&oe=utf-8";
            using var doc = JsonDocument.Parse(await http.GetStringAsync(url, cts.Token));
            var root = doc.RootElement;
            if (root[0].GetString() != "SUCCESS") return null;
            var list = root[1][0][1].EnumerateArray().Select(e => e.GetString()!).Where(s => !string.IsNullOrEmpty(s)).ToList();
            cache.Set(key, list, TimeSpan.FromDays(7));
            return list;
        }
        catch (Exception ex)
        {
            log.LogDebug(ex, "Google transliteration unavailable for {Word}", word);
            return null;
        }
    }

    [GeneratedRegex(@"[A-Za-z]+|[^A-Za-z]+")] private static partial Regex TokenRegex();
    [GeneratedRegex(@"^[A-Za-z]+$")] private static partial Regex WordRegex();
}

/// <summary>Offline Tanglish → Tamil phonetic converter (simple rules, good enough as a fallback).</summary>
public static class TamilPhonetic
{
    private static readonly (string en, string ta)[] Consonants =
    {
        ("ksh","க்ஷ"),("sh","ஷ"),("zh","ழ"),("ng","ங"),("nj","ஞ"),("ch","ச"),("th","த"),("dh","த"),("kh","க"),("gh","க"),
        ("ph","ப"),("bh","ப"),("tr","ற்ற"),
        ("k","க"),("g","க"),("c","ச"),("s","ச"),("j","ஜ"),("t","ட"),("d","ட"),("n","ந"),("p","ப"),("b","ப"),("m","ம"),
        ("y","ய"),("r","ர"),("l","ல"),("v","வ"),("w","வ"),("h","ஹ"),("f","ஃப"),("z","ஸ"),("x","க்ஸ"),("q","க")
    };
    private static readonly (string en, string vowel, string sign)[] Vowels =
    {
        ("aa","ஆ","ா"),("ai","ஐ","ை"),("au","ஔ","ௌ"),("ee","ஈ","ீ"),("ii","ஈ","ீ"),("oo","ஊ","ூ"),("uu","ஊ","ூ"),("ae","ஏ","ே"),
        ("oa","ஓ","ோ"),("a","அ",""),("i","இ","ி"),("u","உ","ு"),("e","எ","ெ"),("o","ஒ","ொ")
    };
    private const string Pulli = "்";

    public static string Convert(string word)
    {
        var w = word.ToLowerInvariant();
        var sb = new StringBuilder();
        var i = 0;
        while (i < w.Length)
        {
            var con = Consonants.FirstOrDefault(c => w.AsSpan(i).StartsWith(c.en));
            if (con.en != null)
            {
                var ta = con.ta;
                if (con.en == "n" && i > 0) ta = "ன";                 // word-internal n → ன
                if (con.en == "n" && i + 1 < w.Length && (w[i + 1] == 'd' || w[i + 1] == 't')) ta = "ண";
                i += con.en.Length;
                var vow = Vowels.FirstOrDefault(v => w.AsSpan(i).StartsWith(v.en));
                if (vow.en != null)
                {
                    i += vow.en.Length;
                    var sign = vow.sign;
                    if (vow.en == "a" && i == w.Length && w.Length > 2) sign = "ா";   // name-final "a" → ா (Geetha → கீதா)
                    sb.Append(ta).Append(sign);
                }
                else sb.Append(ta).Append(Pulli);
                continue;
            }
            var v0 = Vowels.FirstOrDefault(v => w.AsSpan(i).StartsWith(v.en));
            if (v0.en != null) { sb.Append(v0.vowel); i += v0.en.Length; continue; }
            sb.Append(w[i]); i++;
        }
        return sb.ToString();
    }
}
