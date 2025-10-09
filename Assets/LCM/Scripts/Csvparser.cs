using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using System.Threading;

public static class Csvparser
{
    // CSV의 쉼표(,)를 큰따옴표 밖에서만 분리하는 정규식
    private static readonly Regex CsvSplitRegex = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

    // CSV의 행(줄)을 큰따옴표 규칙에 따라 올바르게 분리하는 정규식
    private static readonly Regex CsvLineSplitter = new Regex("(?<=[\"](?:[\"\"].*?[\"\"].*?|[^\"].*?)[\"]|[^\"]+)(\\r\\n|\\n)", RegexOptions.Compiled);


    public static List<T> Parse<T>(string csvContent) where T : new()
    {
        var dataList = new List<T>();

        // Windows/Linux 줄바꿈 문자를 통일
        csvContent = csvContent.Replace("\r\n", "\n");

        // 첫 번째 줄(헤더) 분리
        string[] headerLine = csvContent.Split(new[] { '\n' }, 2);
        if (headerLine.Length < 2) return dataList;

        string[] headers = CsvSplitRegex.Split(headerLine[0]);
        var headerMap = new Dictionary<string, int>();
        for (int i = 0; i < headers.Length; i++)
        {
            string headerName = headers[i].Trim().Trim('"');
            headerMap[headerName] = i;
        }

        // --- 수정된 부분: CSV 내용을 올바르게 행 단위로 분리하는 새로운 로직 ---
        var dataLines = new List<string>();
        var currentLine = new System.Text.StringBuilder();
        bool inQuotes = false;

        foreach (char c in headerLine[1])
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == '\n' && !inQuotes)
            {
                dataLines.Add(currentLine.ToString().Trim());
                currentLine.Clear();
                continue;
            }
            currentLine.Append(c);
        }
        if (currentLine.Length > 0)
        {
            dataLines.Add(currentLine.ToString().Trim());
        }
        // ----------------------------------------------------------------------

        for (int i = 0; i < dataLines.Count; i++)
        {
            string line = dataLines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] values = CsvSplitRegex.Split(line);

            if (values.Length < headers.Length)
            {
                Debug.LogError($"'{line}' 행에 오류가 있습니다. 데이터 열 개수: {values.Length}, 헤더 열 개수: {headers.Length}");
                continue;
            }

            T data = new T();
            foreach (var property in typeof(T).GetProperties())
            {
                if (headerMap.ContainsKey(property.Name))
                {
                    int index = headerMap[property.Name];
                    string rawValue = values[index];
                    string trimmedValue = rawValue;

                    // 문자열이 큰따옴표로 시작하고 끝나는지 확인하고 제거
                    if (trimmedValue.StartsWith("\"") && trimmedValue.EndsWith("\""))
                    {
                        trimmedValue = trimmedValue.Substring(1, trimmedValue.Length - 2);
                    }

                    // 이중 큰따옴표를 단일 큰따옴표로 변환
                    trimmedValue = trimmedValue.Replace("\"\"", "\"");

                    try
                    {
                        object convertedValue = null;
                        if (property.PropertyType == typeof(int))
                        {
                            if (string.IsNullOrWhiteSpace(trimmedValue))
                            {
                                convertedValue = 0;
                            }
                            else
                            {
                                // 'Null' 문자열은 0으로 조용히 처리 (로그 노이즈 방지)
                                if (string.Equals(trimmedValue, "null", System.StringComparison.OrdinalIgnoreCase))
                                {
                                    convertedValue = 0;
                                }
                                else
                                {
                                    convertedValue = int.Parse(trimmedValue);
                                }
                            }
                        }
                        else if (property.PropertyType == typeof(bool))
                        {
                            if (string.IsNullOrWhiteSpace(trimmedValue))
                            {
                                convertedValue = null;
                            }
                            else
                            {
                                convertedValue = bool.Parse(trimmedValue.ToLower());
                            }
                        }
                        else
                        {
                            convertedValue = System.Convert.ChangeType(trimmedValue, property.PropertyType);
                        }
                        property.SetValue(data, convertedValue);
                    }
                    catch (System.FormatException)
                    {
                        if (property.PropertyType == typeof(int))
                        {
                            property.SetValue(data, 0);
                        }
                        else if (property.PropertyType == typeof(bool))
                        {
                            property.SetValue(data, false);
                        }
                        // int/bool에 대해서만 기본값 설정. 불필요한 에러 로그는 억제합니다.
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"'{property.Name}' 변환 오류. 값: '{rawValue}', 타입: '{property.PropertyType}' - 예외: {ex.Message}");
                    }
                }
            }
            dataList.Add(data);
        }
        return dataList;
    }

    public static async UniTask<List<T>> ParseAsync<T>(string assetName, CancellationToken token = default) where T : new()
    {
        ResourceRequest resourceRequest = Resources.LoadAsync<TextAsset>(assetName);

        await resourceRequest.ToUniTask(cancellationToken: token);

        if (resourceRequest.asset == null || !(resourceRequest.asset is TextAsset csvFile))
        {
            Debug.LogError($"Resources 로드 실패 또는 타입 불일치: {assetName}");
            return null;
        }

        string csvContent = csvFile.text;

        List<T> resultList = await UniTask.RunOnThreadPool(() =>
        {
            return Parse<T>(csvContent);
        }, cancellationToken: token);

        return resultList;
    }
}