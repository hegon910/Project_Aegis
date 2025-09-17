using UnityEngine;

/// <summary>
    /// 암호화 기능을 테스트하는 스크립트
    /// 개발 초심자가 암호화가 제대로 작동하는지 확인할 수 있습니다.
    /// </summary>
public class EncryptionTest : MonoBehaviour
{
    [Header("테스트 설정")]
    [SerializeField] private bool runTestOnStart = true;
    [SerializeField] private string testString = "안녕하세요! 이것은 테스트 문자열입니다.";

    private void Start()
    {
        if (runTestOnStart)
        {
            RunEncryptionTest();
        }
    }

    /// <summary>
    /// 암호화/복호화 테스트를 실행합니다.
    /// </summary>
    [ContextMenu("암호화 테스트 실행")]
    public void RunEncryptionTest()
    {
        Debug.Log("=== 암호화 테스트 시작 ===");
        
        // 1. 원본 텍스트 출력
        Debug.Log($"원본 텍스트: {testString}");
        
        // 2. 암호화
        string encrypted = EncryptionUtility.EncryptString(testString);
        Debug.Log($"암호화된 텍스트: {encrypted}");
        
        // 3. 복호화
        string decrypted = EncryptionUtility.DecryptString(encrypted);
        Debug.Log($"복호화된 텍스트: {decrypted}");
        
        // 4. 결과 검증
        bool isSuccess = testString == decrypted;
        Debug.Log($"테스트 결과: {(isSuccess ? "성공" : "실패")}");
        
        if (isSuccess)
        {
            Debug.Log("✅ 암호화/복호화가 정상적으로 작동합니다!");
        }
        else
        {
            Debug.LogError("❌ 암호화/복호화에 문제가 있습니다!");
        }
        
        Debug.Log("=== 암호화 테스트 완료 ===");
    }

    /// <summary>
    /// 파일 암호화 테스트를 실행합니다.
    /// </summary>
    [ContextMenu("파일 암호화 테스트 실행")]
    public void RunFileEncryptionTest()
    {
        Debug.Log("=== 파일 암호화 테스트 시작 ===");
        
        string testFilePath = System.IO.Path.Combine(Application.persistentDataPath, "encryption_test.txt");
        string testContent = "이것은 파일 암호화 테스트입니다.\n한글도 잘 작동하는지 확인해보세요!";
        
        try
        {
            // 1. 암호화된 파일 저장
            EncryptionUtility.SaveEncryptedFile(testFilePath, testContent);
            Debug.Log($"암호화된 파일 저장 완료: {testFilePath}");
            
            // 2. 암호화된 파일 읽기
            string loadedContent = EncryptionUtility.LoadEncryptedFile(testFilePath);
            Debug.Log($"파일에서 읽은 내용: {loadedContent}");
            
            // 3. 결과 검증
            bool isSuccess = testContent == loadedContent;
            Debug.Log($"파일 테스트 결과: {(isSuccess ? "성공" : "실패")}");
            
            if (isSuccess)
            {
                Debug.Log("✅ 파일 암호화/복호화가 정상적으로 작동합니다!");
            }
            else
            {
                Debug.LogError("❌ 파일 암호화/복호화에 문제가 있습니다!");
            }
            
            // 4. 테스트 파일 삭제
            if (System.IO.File.Exists(testFilePath))
            {
                System.IO.File.Delete(testFilePath);
                Debug.Log("테스트 파일 삭제 완료");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"파일 암호화 테스트 실패: {ex.Message}");
        }
        
        Debug.Log("=== 파일 암호화 테스트 완료 ===");
    }

    /// <summary>
    /// XOR 암호화 테스트 (참고용)
    /// </summary>
    [ContextMenu("XOR 암호화 테스트 실행")]
    public void RunXORTest()
    {
        Debug.Log("=== XOR 암호화 테스트 시작 ===");
        
        string testText = "XOR 테스트 문자열";
        string xorKey = "MySecretKey";
        
        // XOR 암호화
        string xorEncrypted = EncryptionUtility.SimpleXOREncrypt(testText, xorKey);
        Debug.Log($"XOR 암호화된 텍스트: {xorEncrypted}");
        
        // XOR 복호화
        string xorDecrypted = EncryptionUtility.SimpleXORDecrypt(xorEncrypted, xorKey);
        Debug.Log($"XOR 복호화된 텍스트: {xorDecrypted}");
        
        // 결과 검증
        bool isSuccess = testText == xorDecrypted;
        Debug.Log($"XOR 테스트 결과: {(isSuccess ? "성공" : "실패")}");
        
        Debug.Log("=== XOR 암호화 테스트 완료 ===");
    }

    /// <summary>
    /// 게임 데이터 암호화 테스트
    /// </summary>
    [ContextMenu("게임 데이터 암호화 테스트 실행")]
    public void RunGameDataEncryptionTest()
    {
        Debug.Log("=== 게임 데이터 암호화 테스트 시작 ===");
        
        try
        {
            // 테스트용 GameData 생성
            GameData testGameData = new GameData();
            testGameData.playthroughCount = 5;
            testGameData.currentChapter = 3;
            testGameData.politics = 75;
            testGameData.militaryPower = 60;
            testGameData.supplies = 80;
            testGameData.leadership = 70;
            testGameData.warSituation = 55;
            testGameData.karma = 65;
            
            // JSON으로 변환
            string jsonData = JsonUtility.ToJson(testGameData, true);
            Debug.Log($"원본 JSON 데이터:\n{jsonData}");
            
            // 암호화
            string encryptedJson = EncryptionUtility.EncryptString(jsonData);
            Debug.Log($"암호화된 JSON 데이터: {encryptedJson}");
            
            // 복호화
            string decryptedJson = EncryptionUtility.DecryptString(encryptedJson);
            Debug.Log($"복호화된 JSON 데이터:\n{decryptedJson}");
            
            // GameData로 다시 변환
            GameData restoredGameData = JsonUtility.FromJson<GameData>(decryptedJson);
            
            // 결과 검증
            bool isSuccess = testGameData.playthroughCount == restoredGameData.playthroughCount &&
                           testGameData.currentChapter == restoredGameData.currentChapter &&
                           testGameData.politics == restoredGameData.politics &&
                           testGameData.militaryPower == restoredGameData.militaryPower &&
                           testGameData.supplies == restoredGameData.supplies &&
                           testGameData.leadership == restoredGameData.leadership &&
                           testGameData.warSituation == restoredGameData.warSituation &&
                           testGameData.karma == restoredGameData.karma;
            
            Debug.Log($"게임 데이터 테스트 결과: {(isSuccess ? "성공" : "실패")}");
            
            if (isSuccess)
            {
                Debug.Log("✅ 게임 데이터 암호화/복호화가 정상적으로 작동합니다!");
                Debug.Log($"복원된 데이터 - 회차: {restoredGameData.playthroughCount}, 챕터: {restoredGameData.currentChapter}");
                Debug.Log($"복원된 데이터 - 정치력: {restoredGameData.politics}, 병력: {restoredGameData.militaryPower}");
            }
            else
            {
                Debug.LogError("❌ 게임 데이터 암호화/복호화에 문제가 있습니다!");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"게임 데이터 암호화 테스트 실패: {ex.Message}");
        }
        
        Debug.Log("=== 게임 데이터 암호화 테스트 완료 ===");
    }
}

