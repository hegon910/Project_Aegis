using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

static class CsvUtil
{
    public static List<string[]> ReadAll(string path)
    {
        var rows = new List<string[]>();
        using var sr = new StreamReader(path, Encoding.UTF8);
        var sb = new StringBuilder();
        bool inQuotes = false;
        string line;
        while ((line = sr.ReadLine()) != null)
        {
            if (sb.Length > 0) sb.Append("\n");
            sb.Append(line);

            int quotes = line.Count(c => c == '"');
            if (inQuotes)
            {
                inQuotes = (quotes % 2 == 0) ? !inQuotes : inQuotes;
            }
            else
            {
                inQuotes = (quotes % 2 == 1);
            }

            if (!inQuotes)
            {
                rows.Add(ParseLine(sb.ToString()));
                sb.Length = 0;
            }
        }
        if (sb.Length > 0) rows.Add(ParseLine(sb.ToString()));
        return rows;
    }

    static string[] ParseLine(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == ',' && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Length = 0;
            }
            else
            {
                sb.Append(ch);
            }
        }
        result.Add(sb.ToString());
        return result.ToArray();
    }

    public static void WriteAll(string path, List<string[]> rows)
    {
        using var sw = new StreamWriter(path, false, Encoding.UTF8);
        foreach (var row in rows)
        {
            var fields = new List<string>(row.Length);
            foreach (var cell in row)
            {
                fields.Add(Escape(cell ?? string.Empty));
            }
            sw.WriteLine(string.Join(",", fields));
        }
    }

    static string Escape(string s)
    {
        bool needQuotes = s.Contains(',') || s.Contains('\n') || s.Contains('"');
        if (needQuotes)
        {
            s = s.Replace("\"", "\"\"");
            return $"\"{s}\"";
        }
        return s;
    }
}

public class CharacterImgAutoSetter : EditorWindow
{
    const string MainCsvPath = "Assets/Resources/MainEventData.csv";
    const string ImgCsvPath  = "Assets/Resources/MainCharacterImgData.csv";
    const string CharCsvPath = "Assets/Resources/MainCharacterData.csv";
    const string SuggestPath = "Assets/Resources/MainEventData_CharacterImg_Suggestions.csv";
    const string BackupPath  = "Assets/Resources/MainEventData_backup_before_apply.csv";

    class ImgRow { public int CharacterImg_ID; public string IMGName; public int Chr_ID; public string Mood; }

    Dictionary<int, string> chrIdToName;
    Dictionary<string, int> chrNameToId;
    Dictionary<(int chr, string mood), int> moodMap;
    Dictionary<int, List<ImgRow>> chrToImgs;

    [MenuItem("Tools/CharacterImg/Build Suggestions")]
    public static void BuildSuggestionsMenu()
    {
        var w = GetWindow<CharacterImgAutoSetter>();
        w.BuildSuggestions();
    }

    [MenuItem("Tools/CharacterImg/Apply Suggestions")]
    public static void ApplySuggestionsMenu()
    {
        var w = GetWindow<CharacterImgAutoSetter>();
        w.ApplySuggestions();
    }

    [MenuItem("Tools/CharacterImg/Apply Suggestions (Strict, preserve formatting)")]
    public static void ApplySuggestionsStrictMenu()
    {
        var w = GetWindow<CharacterImgAutoSetter>();
        w.ApplySuggestionsStrict();
    }

    [MenuItem("Tools/CharacterImg/Restore From Backup")] 
    public static void RestoreFromBackupMenu() 
    {
        var w = GetWindow<CharacterImgAutoSetter>();
        w.RestoreFromBackup();
    }

    [MenuItem("Tools/CharacterImg/Validate MainEventData.csv")] 
    public static void ValidateMenu() 
    {
        var w = GetWindow<CharacterImgAutoSetter>();
        w.ValidateMainCsv();
    }

    void BuildSuggestions()
    {
        if (!File.Exists(MainCsvPath) || !File.Exists(ImgCsvPath) || !File.Exists(CharCsvPath))
        {
            Debug.LogError("필요한 CSV가 없습니다. 경로를 확인하세요.");
            return;
        }

        var main = CsvUtil.ReadAll(MainCsvPath);
        var imgs = CsvUtil.ReadAll(ImgCsvPath);
        var chars = CsvUtil.ReadAll(CharCsvPath);

        var mainHead = Index(main[0]);
        var imgHead  = Index(imgs[0]);
        var chrHead  = Index(chars[0]);

        BuildCharMaps(chars, chrHead);
        BuildImgMaps(imgs, imgHead);

        var outRows = new List<string[]>();
        outRows.Add(new[] { "ID", "CharacterName(Chr_ID)", "Text_kr", "Current_CharacterImg_ID", "Suggested_CharacterImg_ID", "Reason" });

        for (int i = 1; i < main.Count; i++)
        {
            var row = main[i];
            int id = ToInt(row, mainHead, "ID");
            if (id <= 0) continue;
            int chrId = ToInt(row, mainHead, "CharacterName");
            string text = Get(row, mainHead, "Text_kr");
            int cur = ToInt(row, mainHead, "CharacterImg_ID");

            string mood = InferMood(text);
            int sug = Suggest(chrId, mood, cur, out string reason);

            string chrName = chrIdToName != null && chrIdToName.TryGetValue(chrId, out var nm) ? nm : "";
            outRows.Add(new[]
            {
                id.ToString(),
                string.IsNullOrEmpty(chrName) ? chrId.ToString() : $"{chrName}({chrId})",
                Trim(text, 100),
                cur > 0 ? cur.ToString() : string.Empty,
                sug > 0 ? sug.ToString() : string.Empty,
                reason
            });
        }

        CsvUtil.WriteAll(SuggestPath, outRows);
        AssetDatabase.Refresh();
        Debug.Log($"제안 생성 완료: {SuggestPath}");
    }

    void ApplySuggestions()
    {
        if (!File.Exists(SuggestPath) || !File.Exists(MainCsvPath))
        {
            Debug.LogError("제안 파일 또는 메인 CSV가 없습니다.");
            return;
        }
        var main = CsvUtil.ReadAll(MainCsvPath);
        var sugg = CsvUtil.ReadAll(SuggestPath);
        var mH = Index(main[0]);
        var sH = Index(sugg[0]);

        File.Copy(MainCsvPath, BackupPath, true);

        var map = new Dictionary<int, int>();
        for (int i = 1; i < sugg.Count; i++)
        {
            int id = ToInt(sugg[i], sH, "ID");
            int sug = ToInt(sugg[i], sH, "Suggested_CharacterImg_ID");
            if (id > 0 && sug > 0) map[id] = sug;
        }

        for (int i = 1; i < main.Count; i++)
        {
            int id = ToInt(main[i], mH, "ID");
            if (id > 0 && map.TryGetValue(id, out int sug))
            {
                Set(main[i], mH, "CharacterImg_ID", sug.ToString());
            }
        }

        CsvUtil.WriteAll(MainCsvPath, main);
        AssetDatabase.Refresh();
        Debug.Log($"적용 완료. 백업 파일: {BackupPath}");
    }

    // 기존 라인 구조/따옴표/공백/개행을 유지하며 CharacterImg_ID 열만 교체
    void ApplySuggestionsStrict()
    {
        if (!File.Exists(SuggestPath) || !File.Exists(MainCsvPath))
        {
            Debug.LogError("제안 파일 또는 메인 CSV가 없습니다.");
            return;
        }

        // 백업 생성 후 시작
        File.Copy(MainCsvPath, BackupPath, true);

        var suggRows = CsvUtil.ReadAll(SuggestPath);
        var sH = Index(suggRows[0]);
        var map = new Dictionary<int, int>();
        for (int i = 1; i < suggRows.Count; i++)
        {
            int id = ToInt(suggRows[i], sH, "ID");
            int sug = ToInt(suggRows[i], sH, "Suggested_CharacterImg_ID");
            if (id > 0 && sug > 0) map[id] = sug;
        }

        // 원본 전체 텍스트 읽기(개행 유지)
        string text = File.ReadAllText(MainCsvPath, Encoding.UTF8);
        var records = SplitCsvRecords(text);
        if (records.Count == 0) { Debug.LogError("CSV 파싱 실패"); return; }

        // 헤더 분석
        var header = ParseFieldsWithSpans(records[0].record);
        var headerValues = header.fields.Select(f => f.value).ToArray();
        var hIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headerValues.Length; i++) hIndex[headerValues[i]] = i;

        if (!hIndex.ContainsKey("ID") || !hIndex.ContainsKey("CharacterImg_ID") || !hIndex.ContainsKey("CharacterName"))
        {
            Debug.LogError("헤더에 필요한 컬럼(ID/CharacterImg_ID/CharacterName)이 없습니다.");
            return;
        }

        int idCol = hIndex["ID"], imgCol = hIndex["CharacterImg_ID"], nameCol = hIndex["CharacterName"];

        var sbOut = new StringBuilder(text.Length + 1024);
        sbOut.Append(records[0].record);
        sbOut.Append(records[0].newline);

        for (int r = 1; r < records.Count; r++)
        {
            var rec = records[r];
            var parsed = ParseFieldsWithSpans(rec.record);
            var fields = parsed.fields; // list of (start,end,value,quoted)

            if (fields.Count <= Math.Max(Math.Max(idCol, imgCol), nameCol))
            {
                // 구조가 부족하면 원본 그대로
                sbOut.Append(rec.record).Append(rec.newline);
                continue;
            }

            int idVal = SafeToInt(fields[idCol].value);
            int chrIdVal = SafeToInt(fields[nameCol].value);

            // 나레이션은 변경 금지
            if (chrIdVal <= 0 || !map.TryGetValue(idVal, out int suggested))
            {
                sbOut.Append(rec.record).Append(rec.newline);
                continue;
            }

            // 대상 필드만 교체. 따옴표 존재 여부 유지
            var recSB = new StringBuilder(rec.record);
            var span = fields[imgCol];
            string replacement = suggested.ToString();
            if (span.quoted) replacement = Quote(replacement);
            recSB.Remove(span.start, span.length);
            recSB.Insert(span.start, replacement);

            sbOut.Append(recSB.ToString()).Append(rec.newline);
        }

        File.WriteAllText(MainCsvPath, sbOut.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();
        Debug.Log("Strict 적용 완료. 라인/따옴표/구조 보존");
    }

    void RestoreFromBackup()
    {
        if (!File.Exists(BackupPath)) { Debug.LogError("백업 파일이 없습니다."); return; }
        File.Copy(BackupPath, MainCsvPath, true);
        AssetDatabase.Refresh();
        Debug.Log("백업본으로 복구 완료");
    }

    void ValidateMainCsv()
    {
        if (!File.Exists(MainCsvPath)) { Debug.LogError("메인 CSV 없음"); return; }
        string text = File.ReadAllText(MainCsvPath, Encoding.UTF8);
        var records = SplitCsvRecords(text);
        if (records.Count == 0) { Debug.LogError("CSV 비어있음/파싱 실패"); return; }

        var headerParsed = ParseFieldsWithSpans(records[0].record);
        int expected = headerParsed.fields.Count;
        int errors = 0;
        for (int i = 1; i < records.Count; i++)
        {
            var rec = records[i];
            var parsed = ParseFieldsWithSpans(rec.record);
            if (parsed.fields.Count != expected)
            {
                errors++;
                Debug.LogError($"[행 {i+1}] 컬럼 수 불일치: {parsed.fields.Count} != {expected} | 샘플: {Trunc(rec.record, 120)}");
                if (errors >= 20) break;
            }
        }
        if (errors == 0) Debug.Log("형식 검증 통과: 모든 행의 컬럼 수 일치");
    }

    static string Trunc(string s, int n)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Length > n ? s.Substring(0, n) + "..." : s;
    }

    static string Quote(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";

    struct FieldSpan { public int start; public int length; public string value; public bool quoted; }
    struct ParsedRecord { public List<FieldSpan> fields; }
    struct RawRecord { public string record; public string newline; }

    static List<RawRecord> SplitCsvRecords(string text)
    {
        var list = new List<RawRecord>();
        var sb = new StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < text.Length; i++)
        {
            char ch = text[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < text.Length && text[i + 1] == '"') { sb.Append('"'); i++; }
                else { inQuotes = !inQuotes; sb.Append(ch); }
            }
            else if ((ch == '\n' || ch == '\r') && !inQuotes)
            {
                // 줄 끝 감지(\r\n, \n, \r 모두 처리)
                string nl = ch.ToString();
                if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') { nl = "\r\n"; i++; }
                list.Add(new RawRecord { record = sb.ToString(), newline = nl });
                sb.Length = 0;
            }
            else
            {
                sb.Append(ch);
            }
        }
        if (sb.Length > 0) list.Add(new RawRecord { record = sb.ToString(), newline = string.Empty });
        return list;
    }

    static ParsedRecord ParseFieldsWithSpans(string record)
    {
        var fields = new List<FieldSpan>();
        int start = 0;
        bool inQuotes = false;
        bool fieldQuoted = false;
        var valueSB = new StringBuilder();
        for (int i = 0; i < record.Length; i++)
        {
            char ch = record[i];
            if (ch == '"')
            {
                if (!inQuotes && valueSB.Length == 0) { fieldQuoted = true; inQuotes = true; }
                else if (inQuotes && i + 1 < record.Length && record[i + 1] == '"') { valueSB.Append('"'); i++; }
                else { inQuotes = !inQuotes; }
            }
            else if (ch == ',' && !inQuotes)
            {
                int end = i; // exclusive
                fields.Add(new FieldSpan { start = start, length = end - start, value = valueSB.ToString(), quoted = fieldQuoted });
                start = i + 1;
                valueSB.Length = 0;
                fieldQuoted = false;
            }
            else
            {
                valueSB.Append(ch);
            }
        }
        // last field
        fields.Add(new FieldSpan { start = start, length = record.Length - start, value = valueSB.ToString(), quoted = fieldQuoted });
        return new ParsedRecord { fields = fields };
    }

    static int SafeToInt(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0;
        if (int.TryParse(s.Trim(), out int v)) return v;
        return 0;
    }

    void BuildCharMaps(List<string[]> chars, Dictionary<string, int> head)
    {
        chrIdToName = new Dictionary<int, string>();
        chrNameToId = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 1; i < chars.Count; i++)
        {
            string name = Get(chars[i], head, "Chr_Name");
            int id = ToInt(chars[i], head, "Chr_ID");
            if (id <= 0 || string.IsNullOrEmpty(name)) continue;
            chrIdToName[id] = name;
            if (!chrNameToId.ContainsKey(name)) chrNameToId[name] = id;
        }
    }

    void BuildImgMaps(List<string[]> imgs, Dictionary<string, int> head)
    {
        chrToImgs = new Dictionary<int, List<ImgRow>>();
        moodMap = new Dictionary<(int, string), int>(new MoodKeyComparer());
        for (int i = 1; i < imgs.Count; i++)
        {
            int imgId = ToInt(imgs[i], head, "CharacterImg_ID");
            string name = Get(imgs[i], head, "IMGName");
            if (imgId <= 0 || string.IsNullOrEmpty(name)) continue;

            int chrId = GuessChrIdFromImgName(name);
            string mood = GuessMoodFromImgName(name);

            var row = new ImgRow { CharacterImg_ID = imgId, IMGName = name, Chr_ID = chrId, Mood = mood };
            if (!chrToImgs.TryGetValue(chrId, out var list)) { list = new List<ImgRow>(); chrToImgs[chrId] = list; }
            list.Add(row);

            if (chrId > 0 && !string.IsNullOrEmpty(mood)) moodMap[(chrId, mood)] = imgId;
        }
    }

    int GuessChrIdFromImgName(string imgName)
    {
        // IMGName의 접두사로 캐릭터 매칭 (예: Pierre_*, Schrodinger_*, Jang_*)
        // chrIdToName 사전의 한글 이름은 직접 매칭이 어려우므로 영문 접두사를 하드 매핑
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Pierre", "부관" },
            { "Schrodinger", "슈뢰딩거" },
            { "Jang", "장 미셸" },
            { "King", "루이2세" },
            { "Spy", "스파이" },
            { "Boy", "아이" },
        };
        foreach (var kv in map)
        {
            if (imgName.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase))
            {
                if (chrNameToId != null && chrNameToId.TryGetValue(kv.Value, out int id)) return id;
            }
        }
        return 0;
    }

    string GuessMoodFromImgName(string imgName)
    {
        var moods = new[] { "Smile", "Angry", "Worry", "Sad", "Surprise", "Quest", "Idle", "Hate", "Bad" };
        foreach (var m in moods)
        {
            if (imgName.IndexOf(m, StringComparison.OrdinalIgnoreCase) >= 0) return m;
        }
        return "Idle";
    }

    string InferMood(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "Idle";
        var t = text.Trim();
        if (Regex.IsMatch(t, @"[!！]{2,}")) return "Angry";
        if (Regex.IsMatch(t, @"\?$")) return "Quest";
        if (Regex.IsMatch(t, @"\b(웃|하하|ㅎㅎ|ㅋㅋ)\b")) return "Smile";
        if (Regex.IsMatch(t, @"\b(미안|죄송|유감)\b")) return "Sad";
        if (Regex.IsMatch(t, @"\b(분노|화가|열받)\b")) return "Angry";
        if (Regex.IsMatch(t, @"\b(놀랐|깜짝|헉|엇)\b")) return "Surprise";
        if (Regex.IsMatch(t, @"\b(흠|고민|우려|걱정)\b")) return "Worry";
        return "Idle";
    }

    int Suggest(int chrId, string mood, int current, out string reason)
    {
        // CharacterName이 공란(나레이션/독백)일 때는 절대 채우지 않음
        if (chrId <= 0)
        {
            reason = "narration (no character)";
            return 0;
        }
        if (chrId > 0 && moodMap.TryGetValue((chrId, mood), out int id)) { reason = $"mood:{mood}"; return id; }
        if (chrId > 0 && moodMap.TryGetValue((chrId, "Idle"), out id)) { reason = $"fallback:Idle for {mood}"; return id; }
        if (chrToImgs != null && chrToImgs.TryGetValue(chrId, out var list) && list.Count > 0)
        {
            reason = "fallback:first image";
            return list[0].CharacterImg_ID;
        }
        reason = "keep current";
        return current;
    }

    static Dictionary<string, int> Index(string[] header)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < header.Length; i++) map[header[i]] = i;
        return map;
    }
    static string Get(string[] row, Dictionary<string, int> head, string key)
    {
        return head.TryGetValue(key, out int idx) && idx < row.Length ? row[idx] : string.Empty;
    }
    static void Set(string[] row, Dictionary<string, int> head, string key, string val)
    {
        if (head.TryGetValue(key, out int idx) && idx < row.Length) row[idx] = val;
    }
    static int ToInt(string[] row, Dictionary<string, int> head, string key)
    {
        if (!head.TryGetValue(key, out int idx) || idx >= row.Length) return 0;
        int.TryParse(row[idx], out int v); return v;
    }
    static string Trim(string s, int len)
    {
        if (string.IsNullOrEmpty(s)) return string.Empty;
        s = Regex.Replace(s, @"\s+", " ").Trim();
        if (s.Length > len) s = s.Substring(0, len) + "...";
        return s;
    }

    class MoodKeyComparer : IEqualityComparer<(int, string)>
    {
        public bool Equals((int, string) x, (int, string) y) => x.Item1 == y.Item1 && string.Equals(x.Item2, y.Item2, StringComparison.OrdinalIgnoreCase);
        public int GetHashCode((int, string) obj) => (obj.Item1, obj.Item2?.ToLowerInvariant()).GetHashCode();
    }
}


