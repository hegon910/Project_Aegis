using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

public static class Csvparser
{
    // ����ǥ ���� ��ǥ�� �����ϰ�, ����ǥ ���� ��ǥ�� �������� �и��ϴ� ���Խ�
    // (?=...)�� ���� ���ٺ��� ���Խ�����, ����ǥ�� ¦�� ��(��, ����ǥ �ۿ� �ִ�) 
    // ��ġ�� ��ǥ�� ã���ϴ�.
    private static readonly Regex CsvSplitRegex = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

    public static List<T> Parse<T>(TextAsset csvFile) where T : new()
    {
        var dataList = new List<T>();

        // ���ڵ� ������ ���� ������ �����ϱ� ���� UTF-8�� ������ �н��ϴ�.
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
                Debug.LogError($"'{lines[i]}' �࿡ ������ �ֽ��ϴ�. ������ �� ����: {values.Length}, ��� �� ����: {headers.Length}");
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

                    // �α׸� �߰��Ͽ� �� ��ȯ ��/�ĸ� ���� Ȯ��
                    Debug.Log($"�÷�: '{property.Name}', ���� ��: '{rawValue}', ó���� ��: '{trimmedValue}'");

                    try
                    {
                        object convertedValue = System.Convert.ChangeType(trimmedValue, property.PropertyType);
                        property.SetValue(data, convertedValue);
                    }
                    catch
                    {
                        Debug.LogError($"'{property.Name}' ��ȯ ����. ��: '{rawValue}', ó���� ��: '{trimmedValue}', Ÿ��: '{property.PropertyType}'");
                    }
                }
            }
            dataList.Add(data);
        }
        return dataList;
    }
}