using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Reflection;

public static class Csvparser
{
    public static List<T> Parse<T>(TextAsset csvFile) where T : new()
    {
        List<T> dataList = new List<T>();

        if (csvFile == null)
        {
            Debug.LogError($"CSV 파일을 찾을 수 없습니다: {typeof(T).Name}");
            return dataList;
        }

        string[] lines = csvFile.text.Split('\n');
        if (lines.Length <= 1) return dataList;

        string[] headers = lines[0].Split(',');
        Dictionary<string, int> headerMap = new Dictionary<string, int>();
        for (int i = 0; i < headers.Length; i++)
        {
            headerMap[headers[i].Trim()] = i;
        }

        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = lines[i].Split(',');
            if (values.Length < headers.Length) continue;

            T data = new T();
            foreach (var property in typeof(T).GetProperties())
            {
                if (headerMap.ContainsKey(property.Name))
                {
                    int index = headerMap[property.Name];
                    string value = values[index].Trim().Trim('"');

                    try
                    {
                        object convertedValue = System.Convert.ChangeType(value, property.PropertyType);
                        property.SetValue(data, convertedValue);
                    }
                    catch
                    {
                        Debug.LogError($"'{property.Name}' 변환 오류. 값: '{value}', 타입: '{property.PropertyType}'");
                    }
                }
            }
            dataList.Add(data);
        }

        return dataList;
    }
}