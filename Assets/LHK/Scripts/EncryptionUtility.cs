using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>
/// 간단한 암호화 유틸리티
/// AES + Base64 암호화 방식을 사용
/// </summary>
public static class EncryptionUtility
{
    // 암호화 키 (TODO : 코드가 아닌 다른 저장방법 필요)
    // AES-256을 위한 32바이트 키와 16바이트 IV
    private static readonly byte[] ENCRYPTION_KEY = {
        0x4D, 0x79, 0x47, 0x61, 0x6D, 0x65, 0x53, 0x65, 0x63, 0x72, 0x65, 0x74, 0x4B, 0x65, 0x79, 0x31,
        0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37
    }; // 32바이트 (256비트)
    
    private static readonly byte[] ENCRYPTION_IV = {
        0x4D, 0x79, 0x47, 0x61, 0x6D, 0x65, 0x53, 0x65, 0x63, 0x72, 0x65, 0x74, 0x49, 0x56, 0x31, 0x32
    }; // 16바이트 (128비트)

    /// <summary>
    /// 문자열을 AES로 암호화한 후 Base64로 인코딩합니다.
    /// </summary>
    /// <param name="plainText">암호화할 문자열</param>
    /// <returns>암호화된 Base64 문자열</returns>
    public static string EncryptString(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            Debug.LogWarning("[EncryptionUtility] 암호화할 텍스트가 비어있습니다.");
            return plainText;
        }

        try
        {
            // AES 암호화 객체 생성
            using (Aes aes = Aes.Create())
            {
                aes.Key = ENCRYPTION_KEY; // 32바이트 키 직접 사용
                aes.IV = ENCRYPTION_IV;   // 16바이트 IV 직접 사용
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // 암호화 수행
                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                using (MemoryStream msEncrypt = new MemoryStream())
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                {
                    swEncrypt.Write(plainText);
                    swEncrypt.Close();
                    
                    // Base64로 인코딩하여 반환
                    return Convert.ToBase64String(msEncrypt.ToArray());
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EncryptionUtility] 암호화 실패: {ex.Message}");
            // 보안상 암호화 실패 시 null 반환 (원본 반환하지 않음)
            return null;
        }
    }

    /// <summary>
    /// Base64로 인코딩된 암호화 문자열을 복호화합니다.
    /// </summary>
    /// <param name="cipherText">복호화할 Base64 문자열</param>
    /// <returns>복호화된 원본 문자열</returns>
    public static string DecryptString(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            Debug.LogWarning("[EncryptionUtility] 복호화할 텍스트가 비어있습니다.");
            return cipherText;
        }

        try
        {
            // Base64 디코딩
            byte[] cipherBytes = Convert.FromBase64String(cipherText);

            // AES 복호화 객체 생성
            using (Aes aes = Aes.Create())
            {
                aes.Key = ENCRYPTION_KEY; // 32바이트 키 직접 사용
                aes.IV = ENCRYPTION_IV;   // 16바이트 IV 직접 사용
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // 복호화 수행
                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                using (MemoryStream msDecrypt = new MemoryStream(cipherBytes))
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                {
                    return srDecrypt.ReadToEnd();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EncryptionUtility] 복호화 실패: {ex.Message}");
            // 보안상 복호화 실패 시 null 반환 (원본 반환하지 않음)
            return null;
        }
    }

    /// <summary>
    /// 파일 내용을 암호화하여 저장합니다.
    /// </summary>
    /// <param name="filePath">저장할 파일 경로</param>
    /// <param name="content">저장할 내용</param>
    /// <returns>저장 성공 여부</returns>
    public static bool SaveEncryptedFile(string filePath, string content)
    {
        try
        {
            if (string.IsNullOrEmpty(content))
            {
                Debug.LogWarning("[EncryptionUtility] 저장할 내용이 비어있습니다.");
                return false;
            }

            string encryptedContent = EncryptString(content);
            if (string.IsNullOrEmpty(encryptedContent))
            {
                Debug.LogError("[EncryptionUtility] 암호화 실패로 파일 저장을 중단합니다.");
                return false;
            }

            File.WriteAllText(filePath, encryptedContent, Encoding.UTF8);
            Debug.Log($"[EncryptionUtility] 암호화된 파일 저장 완료: {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EncryptionUtility] 암호화 파일 저장 실패: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 암호화된 파일을 읽어서 복호화합니다.
    /// </summary>
    /// <param name="filePath">읽을 파일 경로</param>
    /// <returns>복호화된 내용</returns>
    public static string LoadEncryptedFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[EncryptionUtility] 파일이 존재하지 않습니다: {filePath}");
                return null;
            }

            string encryptedContent = File.ReadAllText(filePath, Encoding.UTF8);
            return DecryptString(encryptedContent);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EncryptionUtility] 암호화 파일 로드 실패: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 간단한 XOR 암호화 (참고용 - 보안성이 낮음)
    /// </summary>
    /// <param name="text">암호화할 텍스트</param>
    /// <param name="key">XOR 키</param>
    /// <returns>XOR 암호화된 텍스트</returns>
    public static string SimpleXOREncrypt(string text, string key)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(key))
            return text;

        StringBuilder result = new StringBuilder();
        for (int i = 0; i < text.Length; i++)
        {
            result.Append((char)(text[i] ^ key[i % key.Length]));
        }
        return result.ToString();
    }

    /// <summary>
    /// 간단한 XOR 복호화 (참고용 - 보안성이 낮음)
    /// </summary>
    /// <param name="encryptedText">복호화할 텍스트</param>
    /// <param name="key">XOR 키</param>
    /// <returns>복호화된 텍스트</returns>
    public static string SimpleXORDecrypt(string encryptedText, string key)
    {
        // XOR은 암호화와 복호화가 동일합니다
        return SimpleXOREncrypt(encryptedText, key);
    }
}

