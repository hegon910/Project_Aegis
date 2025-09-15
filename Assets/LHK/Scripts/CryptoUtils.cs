// -------------------------------
// CryptoUtils.cs
// -------------------------------
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class CryptoUtils
{
    // [중요] 첫 실행 시 생성해서 안전한 저장소(Keystore/Keychain)에 보관할 키 길이
    public const int AesKeySizeBytes  = 32; // 256-bit
    public const int HmacKeySizeBytes = 32; // 256-bit

    // 임의 키 생성 유틸
    public static byte[] GenerateRandomBytes(int length)
    {
        // 보안 강도 높은 RNG로 바이트 배열 생성
        using (var rng = RandomNumberGenerator.Create())
        {
            var bytes = new byte[length];
            rng.GetBytes(bytes);
            return bytes;
        }
    }

    // AES-256-CBC + PKCS7 패딩으로 암호화
    // - 매번 랜덤 IV 생성
    // - out iv: 복호화에 필요한 초기화 벡터
    public static byte[] AesEncrypt(byte[] plaintext, byte[] key, out byte[] iv)
    {
        using (var aes = Aes.Create())
        {
            aes.KeySize = 256;                   // 256-bit
            aes.Key     = key;                   // 공유 비밀키
            aes.Mode    = CipherMode.CBC;        // CBC 모드
            aes.Padding = PaddingMode.PKCS7;     // PKCS7 패딩
            aes.GenerateIV();                    // 매번 새 IV 생성
            iv = aes.IV;                         // 호출자에게 IV 전달

            using (var ms = new MemoryStream())
            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                cs.Write(plaintext, 0, plaintext.Length); // 평문을 스트림에 기록
                cs.FlushFinalBlock();                      // 암호문 최종 블록 플러시
                return ms.ToArray();                       // 암호문 바이트 반환
            }
        }
    }

    // AES-256-CBC + PKCS7 패딩 복호화
    public static byte[] AesDecrypt(byte[] ciphertext, byte[] key, byte[] iv)
    {
        using (var aes = Aes.Create())
        {
            aes.KeySize = 256;
            aes.Key     = key;
            aes.IV      = iv;
            aes.Mode    = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using (var ms = new MemoryStream())
            using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
            {
                cs.Write(ciphertext, 0, ciphertext.Length); // 암호문 기록
                cs.FlushFinalBlock();                        // 평문 최종 블록 플러시
                return ms.ToArray();                         // 평문 바이트 반환
            }
        }
    }

    // HMAC-SHA256 생성 (무결성 검증용)
    // - 데이터 변조를 판별하기 위해 사용
    public static byte[] ComputeHmacSha256(byte[] data, byte[] hmacKey)
    {
        using (var hmac = new HMACSHA256(hmacKey))
        {
            return hmac.ComputeHash(data); // data 전체에 대한 MAC 생성
        }
    }

    // 타임 인컨스턴트(상수 시간) 비교로 MAC 검증 (타이밍 공격 완화)
    public static bool ConstantTimeEquals(byte[] a, byte[] b)
    {
        if (a == null || b == null || a.Length != b.Length) return false;
        int diff = 0;
        for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
        return diff == 0;
    }
}