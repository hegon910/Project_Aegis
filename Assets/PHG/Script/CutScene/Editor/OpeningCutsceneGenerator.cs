using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class OpeningCutsceneGenerator
{
    private const string RootFolder = "Assets/PHG/CutsceneSO";
    private const string SourceFolder = RootFolder + "/SourceText";

    [MenuItem("PHG/Cutscene/Generate Opening Cutscenes From TextAssets")] 
    public static void GenerateFromTextAssets()
    {
        EnsureFolders();

        // 1) 원본 텍스트 로드 (Lisard 명칭을 우선, 없으면 Risard)
        var devostText = LoadTextAsset(Path.Combine(SourceFolder, "Devost.txt"));
        var willeText  = LoadTextAsset(Path.Combine(SourceFolder, "Wille.txt"));
        var lisardText = LoadTextAsset(Path.Combine(SourceFolder, "Lisard.txt"));
        var risardText = lisardText ?? LoadTextAsset(Path.Combine(SourceFolder, "Risard.txt"));

        if (devostText == null || willeText == null || risardText == null)
        {
            EditorUtility.DisplayDialog(
                "Cutscene 생성 실패",
                "SourceText 폴더에 Devost.txt, Wille.txt, (Lisard.txt 또는 Risard.txt) 파일이 필요합니다.",
                "확인");
            return;
        }

        // 2) 텍스트 → CutsceneData 변환 및 저장/갱신
        var devostAsset = CreateOrUpdateCutsceneAsset("Opening_Devost.asset", devostText);
        var willeAsset  = CreateOrUpdateCutsceneAsset("Opening_Wille.asset",  willeText);
        var risardAsset = CreateOrUpdateCutsceneAsset("Opening_Risard.asset", risardText);

        // 3) 열린 씬의 GameManager에 자동 할당(없으면 프리팹/에셋에 할당 시도)
        AssignToGameManager(devostAsset, willeAsset, risardAsset);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("완료", "오프닝 컷씬 SO 생성 및 GameManager 자동 할당이 완료되었습니다.", "확인");
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/PHG"))
        {
            AssetDatabase.CreateFolder("Assets", "PHG");
        }
        if (!AssetDatabase.IsValidFolder(RootFolder))
        {
            AssetDatabase.CreateFolder("Assets/PHG", "CutsceneSO");
        }
        if (!AssetDatabase.IsValidFolder(SourceFolder))
        {
            AssetDatabase.CreateFolder(RootFolder, "SourceText");
        }
    }

    private static string LoadTextAsset(string assetPath)
    {
        var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
        return ta != null ? ta.text : null;
    }

    private static CutsceneData CreateOrUpdateCutsceneAsset(string fileName, string fullText)
    {
        string assetPath = Path.Combine(RootFolder, fileName).Replace('\\', '/');
        var existing = AssetDatabase.LoadAssetAtPath<CutsceneData>(assetPath);

        var data = existing != null ? existing : ScriptableObject.CreateInstance<CutsceneData>();
        data.steps = BuildStepsFromText(fullText);

        if (existing == null)
        {
            AssetDatabase.CreateAsset(data, assetPath);
        }
        else
        {
            EditorUtility.SetDirty(data);
        }
        return data;
    }

    private static List<CutsceneStep> BuildStepsFromText(string fullText)
    {
        var steps = new List<CutsceneStep>();
        if (string.IsNullOrWhiteSpace(fullText)) return steps;

        var lines = fullText.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        int index = 0;
        foreach (var raw in lines)
        {
            var line = raw?.Trim();
            if (string.IsNullOrEmpty(line)) continue;
            // 화자 표시/구분자 라인 등은 스킵
            if (line.StartsWith("-")) continue;

            var step = new CutsceneStep
            {
                stepName = $"Line {++index}",
                enableDialogueEffect = true,
                dialogueData = new DialogueEffectData
                {
                    dialogue = line,
                    typewriterSpeed = 0f
                },
                enableImageEffect = false,
                enableSoundEffect = false,
                enableVideoEffect = false,
                enableDayTextEffect = false,
                waitTime = 0f,
                waitForClick = true
            };
            steps.Add(step);
        }
        return steps;
    }

    private static void AssignToGameManager(CutsceneData devost, CutsceneData wille, CutsceneData risard)
    {
        // 우선 씬 인스턴스를 선호
        var managers = Resources.FindObjectsOfTypeAll<GameManager>();
        GameManager target = managers.FirstOrDefault(m => m != null && !EditorUtility.IsPersistent(m));
        if (target == null)
        {
            // 씬에 없으면, 에셋(프리팹/스크립터블 오브젝트 상의 임베디드) 중 아무거나
            target = managers.FirstOrDefault(m => m != null);
        }
        if (target == null)
        {
            Debug.LogWarning("[CutsceneGenerator] GameManager 인스턴스를 찾지 못하여 자동 할당을 건너뜁니다.");
            return;
        }

        Undo.RecordObject(target, "Assign Opening Cutscenes");
        var so = new SerializedObject(target);
        so.FindProperty("openingCutscene_Devost").objectReferenceValue = devost;
        so.FindProperty("openingCutscene_Wille").objectReferenceValue = wille;
        so.FindProperty("openingCutscene_Risard").objectReferenceValue = risard;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);

        // 프리팹 자산일 경우 저장
        if (PrefabUtility.IsPartOfPrefabAsset(target))
        {
            var path = AssetDatabase.GetAssetPath(target);
            AssetDatabase.SaveAssets();
            Debug.Log($"[CutsceneGenerator] 프리팹 자산에 오프닝 컷씬을 할당했습니다: {path}");
        }
        else
        {
            Debug.Log("[CutsceneGenerator] 열린 씬의 GameManager에 오프닝 컷씬을 할당했습니다.");
        }
    }
}


