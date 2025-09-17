using UnityEngine;

/// <summary>
/// 빠른 암호화 테스트를 위한 임시 스크립트
/// </summary>
public class QuickEncryptionTest : MonoBehaviour
{
    [ContextMenu("빠른 암호화 테스트")]
    public void QuickTest()
    {
        Debug.Log("=== 빠른 암호화 테스트 시작 ===");
        
        string testString = "안녕하세요! 테스트입니다.";
        Debug.Log($"원본: {testString}");
        
        // 암호화 테스트
        string encrypted = EncryptionUtility.EncryptString(testString);
        if (encrypted != null)
        {
            Debug.Log($"암호화 성공: {encrypted}");
            
            // 복호화 테스트
            string decrypted = EncryptionUtility.DecryptString(encrypted);
            if (decrypted != null)
            {
                Debug.Log($"복호화 성공: {decrypted}");
                
                if (testString == decrypted)
                {
                    Debug.Log("✅ 암호화/복호화 테스트 성공!");
                }
                else
                {
                    Debug.LogError("❌ 복호화 결과가 원본과 다릅니다!");
                }
            }
            else
            {
                Debug.LogError("❌ 복호화 실패!");
            }
        }
        else
        {
            Debug.LogError("❌ 암호화 실패!");
        }
        
        Debug.Log("=== 빠른 암호화 테스트 완료 ===");
    }
}
