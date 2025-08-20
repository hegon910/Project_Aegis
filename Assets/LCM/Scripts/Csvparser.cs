using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

public static class Csvparser
{
    // 따옴표 안의 쉼표는 무시하고, 따옴표 밖의 쉼표만 기준으로 분리하는 정규식
    // (?=...)은 앞을 내다보는 정규식으로, 따옴표가 짝수 개(즉, 따옴표 밖에 있는) 
    // 위치의 쉼표만 찾습니다.
    private static readonly Regex CsvSplitRegex = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

    public static List<T> Parse<T>(TextAsset csvFile) where T : new()
    {
        var dataList = new List<T>();

        // 인코딩 문제로 인한 오류를 방지하기 위해 UTF-8로 파일을 읽습니다.
        string[] lines = csvFile.text.Split('\n');

        string[] headers = CsvSplitRegex.Split(lines[0]);
        var headerMap = new Dictionary<string, int>();
        for (int i = 0; i < headers.Length; i++)
        {
            string headerName = headers[i].Trim().Trim('"');
            headerMap[headerName] = i;
        }

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] values = CsvSplitRegex.Split(lines[i]);

            if (values.Length < headers.Length)
            {
                Debug.LogError($"'{lines[i]}' 행에 오류가 있습니다. 데이터 열 개수: {values.Length}, 헤더 열 개수: {headers.Length}");
                continue;
            }

            T data = new T();
            foreach (var property in typeof(T).GetProperties())
            {
                if (headerMap.ContainsKey(property.Name))
                {
                    int index = headerMap[property.Name];
                    string rawValue = values[index];
                    string trimmedValue = rawValue.Trim().Trim('"');

                    // 로그를 추가하여 값 변환 전/후를 직접 확인
                    Debug.Log($"컬럼: '{property.Name}', 원본 값: '{rawValue}', 처리된 값: '{trimmedValue}'");

                    try
                    {
                        object convertedValue = System.Convert.ChangeType(trimmedValue, property.PropertyType);
                        property.SetValue(data, convertedValue);
                    }
                    catch
                    {
                        Debug.LogError($"'{property.Name}' 변환 오류. 값: '{rawValue}', 처리된 값: '{trimmedValue}', 타입: '{property.PropertyType}'");
                    }
                }
            }
            dataList.Add(data);
        }
        return dataList;
    }
}