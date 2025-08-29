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

                    try
                    {
                        object convertedValue = null;
                        // Ÿ�Կ� ���� ������ ��ȯ ���� �߰�
                        if (property.PropertyType == typeof(int))
                        {
                            // int Ÿ���̰� ���� ��������� 0���� ��ȯ
                            if (string.IsNullOrWhiteSpace(trimmedValue))
                            {
                                convertedValue = 0;
                            }
                            else
                            {
                                convertedValue = int.Parse(trimmedValue);
                            }
                        }
                        else if (property.PropertyType == typeof(bool))
                        {
                            // bool Ÿ���̰� ���� ��������� false�� ��ȯ
                            if (string.IsNullOrWhiteSpace(trimmedValue))
                            {
                                convertedValue = null;
                            }
                            else
                            {
                                // "TRUE" �Ǵ� "FALSE"�� �ν��ϵ��� ó��
                                convertedValue = bool.Parse(trimmedValue.ToLower());
                            }
                        }
                        else
                        {
                            // �� �� Ÿ���� �⺻ ChangeType ���
                            convertedValue = System.Convert.ChangeType(trimmedValue, property.PropertyType);
                        }

                        property.SetValue(data, convertedValue);
                    }
                    catch (System.FormatException)
                    {
                        // ��ȯ ���� �� 0�̳� false�� �⺻������ ����
                        if (property.PropertyType == typeof(int))
                        {
                            property.SetValue(data, 0);
                        }
                        else if (property.PropertyType == typeof(bool))
                        {
                            property.SetValue(data, false);
                        }
                        Debug.LogError($"'{property.Name}' ��ȯ ����. ��: '{rawValue}', Ÿ��: '{property.PropertyType}' - �⺻������ �����մϴ�.");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"'{property.Name}' ��ȯ ����. ��: '{rawValue}', Ÿ��: '{property.PropertyType}' - ����: {ex.Message}");
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
            Debug.LogError($"Resources �ε� ���� �Ǵ� Ÿ�� ����ġ: {assetName}");
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