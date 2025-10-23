using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class OpeningEventDataApplier
{
	private const string CutsceneFolder = "Assets/PHG/CutsceneSO";
	private const string ResourcesFolder = "Assets/Resources";
	private const string OpeningPrefix = "OpeningEventData 0.0.1 - ";

	[MenuItem("PHG/Cutscene/Apply OpeningEventData CSVs")] 
	public static void ApplyAll()
	{
		try
		{
			EnsureFoldersExist();

			// 1) 매핑 CSV 로드 (BG/SFX/OpeningCutScene) - 접두사 유무 모두 허용
			var bgMap = LoadBGMap(Path.Combine(ResourcesFolder, OpeningPrefix + "BGData.csv"));
			var sfxMap = LoadSFXMap(Path.Combine(ResourcesFolder, OpeningPrefix + "SFXData.csv"));
			var imageMap = LoadOpeningCutSceneMap(Path.Combine(ResourcesFolder, OpeningPrefix + "OpeningCutScene.csv"));

			// 2) 대상 OpeningEventData_* CSV들 탐색 (접두사 유무 모두 허용)
			var openingCsvPaths = Directory.GetFiles(ResourcesFolder, OpeningPrefix + "OpeningEventData_*.csv", SearchOption.TopDirectoryOnly);
			if (openingCsvPaths.Length == 0)
			{
				openingCsvPaths = Directory.GetFiles(ResourcesFolder, "OpeningEventData_*.csv", SearchOption.TopDirectoryOnly);
			}
			// AssetDatabase는 '/' 구분자를 기대하므로 경로 정규화
			openingCsvPaths = openingCsvPaths.Select(ToAssetPath).ToArray();
			if (openingCsvPaths.Length == 0)
			{
				EditorUtility.DisplayDialog("적용 불가", "OpeningEventData CSV 파일을 찾지 못했습니다.", "확인");
				return;
			}

			// 3) 사용 가능한 리소스(배경/SFX) 인덱스 구축 (폴백 매칭용)
			var availableBackgrounds = BuildBackgroundIndex();
			var availableSfx = BuildSfxIndex();

			int appliedCount = 0;
			foreach (var csvPath in openingCsvPaths)
			{
				var rows = LoadOpeningRows(csvPath);
				if (rows == null || rows.Count == 0) continue;

				// CSV 파일명에서 캐릭터/분기 명 추출 → Opening_{Name}.asset로 생성/갱신
				var fileNoExt = Path.GetFileNameWithoutExtension(csvPath);
				string nameToken;
				{
					int idx = fileNoExt.LastIndexOf('_');
					nameToken = idx >= 0 ? fileNoExt.Substring(idx + 1) : fileNoExt; // Lucas, Bris, Percy 등
				}
				var targetAssetFile = Path.Combine(CutsceneFolder, $"Opening_{nameToken}.asset").Replace('\\', '/');
				var existing = AssetDatabase.LoadAssetAtPath<CutsceneData>(targetAssetFile);
				var data = existing != null ? existing : ScriptableObject.CreateInstance<CutsceneData>();
				data.steps = BuildStepsFromRows(rows, bgMap, sfxMap, imageMap, availableBackgrounds, availableSfx);
				if (existing == null)
				{
					AssetDatabase.CreateAsset(data, targetAssetFile);
				}
				else
				{
					EditorUtility.SetDirty(data);
				}
				appliedCount++;
				Debug.Log($"[OpeningEventDataApplier] '{Path.GetFileName(csvPath)}' -> '{targetAssetFile}' 적용 완료 (스텝 {rows.Count}개)");
			}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			EditorUtility.DisplayDialog("완료", $"OpeningEventData CSV 적용 완료: {appliedCount}개 자산 업데이트", "확인");
		}
		catch (Exception ex)
		{
			Debug.LogError($"[OpeningEventDataApplier] 적용 중 예외: {ex.Message}\n{ex}");
			EditorUtility.DisplayDialog("오류", "CSV 적용 중 예외가 발생했습니다. 콘솔 로그를 확인하세요.", "확인");
		}
	}

	private static void EnsureFoldersExist()
	{
		if (!AssetDatabase.IsValidFolder("Assets/PHG"))
		{
			AssetDatabase.CreateFolder("Assets", "PHG");
		}
		if (!AssetDatabase.IsValidFolder(CutsceneFolder))
		{
			AssetDatabase.CreateFolder("Assets/PHG", "CutsceneSO");
		}
	}

	private static List<(CutsceneData asset, string path)> LoadOpeningCutsceneAssets()
	{
		var guids = AssetDatabase.FindAssets("t:CutsceneData", new[] { CutsceneFolder });
		var list = new List<(CutsceneData, string)>();
		foreach (var guid in guids)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			if (!Path.GetFileName(path).StartsWith("Opening_", StringComparison.OrdinalIgnoreCase)) continue;
			var asset = AssetDatabase.LoadAssetAtPath<CutsceneData>(path);
			if (asset != null) list.Add((asset, path));
		}
		return list;
	}

	private class OpeningRow
	{
		public int ID { get; set; }
		public int StoryNum { get; set; }
		public int BG_ID { get; set; }
		public int SFX_ID { get; set; }
		public int OpeningCutScene { get; set; }
		public string Text_kr { get; set; }
		public string Text_en { get; set; }
		public int Font_Direction { get; set; }
	}

	private class OpeningBG
	{
		public int BG_ID { get; set; }
		public string BGName { get; set; }
		public string Explanation { get; set; }
	}

	private class OpeningSFX
	{
		public int SFX_ID { get; set; }
		public string SFXName { get; set; }
		public string Explanation { get; set; }
	}

	private class OpeningImage
	{
		public int OpeningCutScene_ID { get; set; }
		public string IMGName { get; set; }
		public string Explanation { get; set; }
	}

	private static List<OpeningRow> LoadOpeningRows(string csvAssetPath)
	{
		var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(ToAssetPath(csvAssetPath));
		if (ta == null) return new List<OpeningRow>();
		return Csvparser.Parse<OpeningRow>(ta.text);
	}

	private static Dictionary<int, string> LoadBGMap(string csvAssetPath)
	{
		// 접두사 있는 파일 우선, 없으면 무접두사 파일 시도
		var ta = TryLoadCsv(csvAssetPath, Path.Combine(ResourcesFolder, "BGData.csv"));
		if (ta == null) return new Dictionary<int, string>();
		var list = Csvparser.Parse<OpeningBG>(ta.text);
		return list.Where(x => x != null).GroupBy(x => x.BG_ID).ToDictionary(g => g.Key, g => (g.First().BGName ?? string.Empty));
	}

	private static Dictionary<int, string> LoadSFXMap(string csvAssetPath)
	{
		// 접두사 있는 파일 우선, 없으면 무접두사 파일 시도 + OpeningEvent 전용명도 허용
		var ta = TryLoadCsv(
			csvAssetPath,
			Path.Combine(ResourcesFolder, "SFXData.csv"),
			Path.Combine(ResourcesFolder, "OpeningEvent_SFXData.csv")
		);
		if (ta == null) return new Dictionary<int, string>();
		var list = Csvparser.Parse<OpeningSFX>(ta.text);
		return list.Where(x => x != null).GroupBy(x => x.SFX_ID).ToDictionary(g => g.Key, g => (g.First().SFXName ?? string.Empty));
	}

	private static Dictionary<int, string> LoadOpeningCutSceneMap(string csvAssetPath)
	{
		// 접두사 있는 파일 우선, 없으면 무접두사 파일 시도
		var ta = TryLoadCsv(csvAssetPath, Path.Combine(ResourcesFolder, "OpeningCutScene.csv"));
		if (ta == null) return new Dictionary<int, string>();
		var list = Csvparser.Parse<OpeningImage>(ta.text);
		return list.Where(x => x != null).GroupBy(x => x.OpeningCutScene_ID).ToDictionary(g => g.Key, g => (g.First().IMGName ?? string.Empty));
	}

	private static TextAsset TryLoadCsv(params string[] candidateAssetPaths)
	{
		foreach (var p in candidateAssetPaths)
		{
			var normalized = ToAssetPath(p);
			var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(normalized);
			if (ta != null) return ta;
		}
		return null;
	}

	private static string ToAssetPath(string path)
	{
		return path.Replace('\\','/');
	}

	private static List<CutsceneStep> BuildStepsFromRows(List<OpeningRow> rows, Dictionary<int, string> bgMap, Dictionary<int, string> sfxMap, Dictionary<int, string> imageMap, Dictionary<string, Sprite> availableBackgrounds, Dictionary<string, AudioClip> availableSfx)
	{
		var steps = new List<CutsceneStep>(rows.Count);
		int bgAssigned = 0;
		int sfxAssigned = 0;
		int bgmAssigned = 0;
		for (int i = 0; i < rows.Count; i++)
		{
			var row = rows[i];

			// Background Image
            Sprite bgSprite = null;
            string bgSpriteAddress = null;
			if (imageMap.TryGetValue(row.OpeningCutScene, out var imgName) && !string.IsNullOrEmpty(imgName))
			{
				var imgKey = Path.GetFileNameWithoutExtension(imgName);
                // 주소 규칙: Backgrounds/<IMGName>
                bgSpriteAddress = $"Backgrounds/{imgKey}";
                bgSprite = TryLoadBackground(imgKey, availableBackgrounds);
				if (bgSprite == null)
				{
					Debug.LogWarning($"[OpeningEventDataApplier] BG 스프라이트 로드 실패 (rowID:{row.ID}) key:{imgKey}");
				}
			}
			else
			{
				Debug.LogWarning($"[OpeningEventDataApplier] OpeningCutScene 매핑 없음 (rowID:{row.ID}) key:{row.OpeningCutScene}");
			}

			// SFX (효과음)
            AudioClip sfxClip = null;
            string sfxAddress = null;
			if (row.SFX_ID != 0 && sfxMap.TryGetValue(row.SFX_ID, out var sfxName) && !string.IsNullOrEmpty(sfxName))
			{
				var sfxKey = Path.GetFileNameWithoutExtension(sfxName);
                // 주소 규칙: Audio/SFX/<SFXName>
                sfxAddress = $"Audio/SFX/{sfxKey}";
                sfxClip = TryLoadSfx(sfxKey, availableSfx);
				if (sfxClip == null)
				{
					Debug.LogWarning($"[OpeningEventDataApplier] SFX 로드 실패 (rowID:{row.ID}) key:{sfxKey}");
				}
			}
			else if (row.SFX_ID != 0)
			{
				Debug.LogWarning($"[OpeningEventDataApplier] SFX 매핑 없음 (rowID:{row.ID}) id:{row.SFX_ID}");
			}

			string bgName = null;
			if (row.BG_ID != 0 && bgMap.TryGetValue(row.BG_ID, out var bgFile) && !string.IsNullOrEmpty(bgFile))
			{
				bgName = Path.GetFileNameWithoutExtension(bgFile);
			}

			// BGM: BG_ID를 BGM 파일명으로 사용 (Main과 동일 경로 규칙)
            AudioClip bgmClip = null;
            string bgmAddress = null;
            if (!string.IsNullOrEmpty(bgName))
            {
                // 주소 규칙: Audio/BGM/<BGName>
                bgmAddress = $"Audio/BGM/{bgName}";
                bgmClip = ProjectAegis.Addressables.AddressableLoader.LoadSync<AudioClip>(bgmAddress) ?? Resources.Load<AudioClip>("Audio/BGM/" + bgName);
				if (bgmClip == null)
				{
					// 확장자 추정 경로 시도
					var directMp3 = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Resources/Audio/BGM/{bgName}.mp3".Replace('\\','/'));
					var directWav = directMp3 ?? AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Resources/Audio/BGM/{bgName}.wav".Replace('\\','/'));
					bgmClip = directWav;
				}
			}

			var step = new CutsceneStep
			{
				stepName = $"StoryNum:{row.StoryNum} | ID:{row.ID}{(bgName != null ? " | BGM:" + bgName : string.Empty)}",
				enableDialogueEffect = true,
                dialogueData = new DialogueEffectData
                {
                    dialogue = row.Text_kr ?? string.Empty,
                    typewriterSpeed = 0.03f
                },
                enableImageEffect = bgSprite != null || !string.IsNullOrEmpty(bgSpriteAddress),
                imageData = new ImageEffectData
                {
                    image = bgSprite,
                    imageAddress = bgSpriteAddress,
                    fadeDuration = 0f
                },
                enableSoundEffect = sfxClip != null || !string.IsNullOrEmpty(sfxAddress),
                soundData = new SoundEffectData { soundClip = sfxClip, soundAddress = sfxAddress },
                enableBgmEffect = bgmClip != null || !string.IsNullOrEmpty(bgmAddress),
                bgmData = new BgmEffectData { bgmClip = bgmClip, bgmAddress = bgmAddress, loop = true, volume = 1f },
				enableVideoEffect = false,
				enableDayTextEffect = false,
				waitTime = 0f,
				waitForClick = true
			};

			if (bgSprite != null) bgAssigned++;
			if (sfxClip != null) sfxAssigned++;
			if (bgmClip != null) bgmAssigned++;

			steps.Add(step);
		}
		Debug.Log($"[OpeningEventDataApplier] 매핑 결과 - 배경 {bgAssigned}/{rows.Count}, SFX {sfxAssigned}/{rows.Count}, BGM {bgmAssigned}/{rows.Count}");
		return steps;
	}

	private static Dictionary<string, Sprite> BuildBackgroundIndex()
	{
		var dict = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
		var guids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Resources/Backgrounds" });
		foreach (var guid in guids)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);
			var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
			if (sprite == null) continue;
			var key = Path.GetFileNameWithoutExtension(path);
			if (!dict.ContainsKey(key)) dict[key] = sprite;
		}
		return dict;
	}

	private static Dictionary<string, AudioClip> BuildSfxIndex()
	{
		var dict = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
		var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Resources/Audio/SFX" });
		foreach (var guid in guids)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);
			var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
			if (clip == null) continue;
			var key = Path.GetFileNameWithoutExtension(path);
			if (!dict.ContainsKey(key)) dict[key] = clip;
		}
		return dict;
	}

	private static Sprite TryLoadBackground(string baseName, Dictionary<string, Sprite> index)
	{
		// 1) Resources 경로 시도
		var fromResources = Resources.Load<Sprite>("Backgrounds/" + baseName);
		if (fromResources != null) return fromResources;
		// direct path guess
		var directPath = $"Assets/Resources/Backgrounds/{baseName}.png".Replace('\\','/');
		var fromPath = AssetDatabase.LoadAssetAtPath<Sprite>(directPath);
		if (fromPath != null) return fromPath;
		// 2) 인덱스에서 정확도 높은 폴백
		if (index.TryGetValue(baseName, out var exact)) return exact;
		// 3) prefix/contains 폴백 (예: ThroneRoomKing -> ThroneRoom)
		var candidate = index.Keys.FirstOrDefault(k =>
			baseName.StartsWith(k, StringComparison.OrdinalIgnoreCase)
			|| k.StartsWith(baseName, StringComparison.OrdinalIgnoreCase)
			|| baseName.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0
			|| k.IndexOf(baseName, StringComparison.OrdinalIgnoreCase) >= 0);
		if (candidate != null) return index[candidate];
		// 4) 에셋 데이터베이스에서 광역 검색 (Sprite)
		var guidCandidates = AssetDatabase.FindAssets($"t:Sprite {baseName}");
		foreach (var guid in guidCandidates)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);
			var name = Path.GetFileNameWithoutExtension(path);
			if (name.IndexOf(baseName, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
				if (sprite != null) return sprite;
			}
		}
		// 5) 에셋 데이터베이스에서 텍스처 검색 후 서브에셋 Sprite 찾기
		var texGuids = AssetDatabase.FindAssets($"t:Texture2D {baseName}");
		foreach (var guid in texGuids)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);
			var name = Path.GetFileNameWithoutExtension(path);
			if (name.IndexOf(baseName, StringComparison.OrdinalIgnoreCase) < 0) continue;
			var subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
			foreach (var sub in subAssets)
			{
				if (sub is Sprite s) return s;
			}
		}
		Debug.LogWarning($"[OpeningEventDataApplier] 배경 스프라이트를 찾을 수 없음: {baseName}");
		return null;
	}

	private static AudioClip TryLoadSfx(string baseName, Dictionary<string, AudioClip> index)
	{
		// 1) Resources 경로 시도
		var fromResources = Resources.Load<AudioClip>("Audio/SFX/" + baseName);
		if (fromResources != null) return fromResources;
		// direct path guess
		var directPath = $"Assets/Resources/Audio/SFX/{baseName}.mp3".Replace('\\','/');
		var fromPath = AssetDatabase.LoadAssetAtPath<AudioClip>(directPath);
		if (fromPath != null) return fromPath;
		// 2) 인덱스에서 정확도 높은 폴백
		if (index.TryGetValue(baseName, out var exact)) return exact;
		// 3) contains 매칭 폴백
		var candidate = index.Keys.FirstOrDefault(k => string.Equals(k, baseName, StringComparison.OrdinalIgnoreCase) || k.StartsWith(baseName, StringComparison.OrdinalIgnoreCase) || baseName.StartsWith(k, StringComparison.OrdinalIgnoreCase));
		if (candidate != null) return index[candidate];
		// 4) 에셋 데이터베이스에서 광역 검색
		var guidCandidates = AssetDatabase.FindAssets($"t:AudioClip {baseName}");
		foreach (var guid in guidCandidates)
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);
			var name = Path.GetFileNameWithoutExtension(path);
			if (name.IndexOf(baseName, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
				if (clip != null) return clip;
			}
		}
		Debug.LogWarning($"[OpeningEventDataApplier] SFX 클립을 찾을 수 없음: {baseName}");
		return null;
	}
}


