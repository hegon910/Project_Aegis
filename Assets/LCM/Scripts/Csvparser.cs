using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using System.Threading;

public static class Csvparser
{
    private static readonly Regex CsvSplitRegex = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

    public static List<T> Parse<T>(string csvContent) where T : new()
    {
        var dataList = new List<T>();

        string[] lines = csvContent.Split('\n');
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

                    try
                    {
                        object convertedValue = System.Convert.ChangeType(trimmedValue, property.PropertyType);
                        property.SetValue(data, convertedValue);
                    }
                    catch
                    {
                        Debug.LogError($"'{property.Name}' 변환 오류. 값: '{rawValue}', 타입: '{property.PropertyType}'");
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