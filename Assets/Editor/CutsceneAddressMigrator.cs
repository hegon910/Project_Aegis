using System.IO;
using UnityEditor;
using UnityEngine;

public static class CutsceneAddressMigrator
{
	private const string CutsceneFolder = "Assets/PHG/CutsceneSO";

	[MenuItem("PHG/Cutscene/Migrate: Direct refs → Address fields", priority = 2100)]
	public static void MigrateCutsceneAssets()
	{
		var guids = AssetDatabase.FindAssets("t:CutsceneData", new[] { CutsceneFolder });
		int migrated = 0;
		foreach (var guid in guids)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			var asset = AssetDatabase.LoadAssetAtPath<CutsceneData>(path);
			if (asset == null || asset.steps == null) continue;

			bool changed = false;
			for (int i = 0; i < asset.steps.Count; i++)
			{
				var step = asset.steps[i];
				// Image
				if (step.enableImageEffect)
				{
					if (step.imageData.image != null && string.IsNullOrEmpty(step.imageData.imageAddress))
					{
						string addr = BuildDefaultSpriteAddress(step.imageData.image);
						if (!string.IsNullOrEmpty(addr))
						{
							step.imageData.imageAddress = addr;
							changed = true;
						}
					}
					// 직접 참조 제거
					if (step.imageData.image != null)
					{
						step.imageData.image = null;
						changed = true;
					}
				}
				// SFX
				if (step.enableSoundEffect)
				{
					if (step.soundData.soundClip != null && string.IsNullOrEmpty(step.soundData.soundAddress))
					{
						string addr = BuildDefaultAudioAddress(step.soundData.soundClip, isBgm:false);
						if (!string.IsNullOrEmpty(addr))
						{
							step.soundData.soundAddress = addr;
							changed = true;
						}
					}
					if (step.soundData.soundClip != null)
					{
						step.soundData.soundClip = null;
						changed = true;
					}
				}
				// BGM
				if (step.enableBgmEffect)
				{
					if (step.bgmData.bgmClip != null && string.IsNullOrEmpty(step.bgmData.bgmAddress))
					{
						string addr = BuildDefaultAudioAddress(step.bgmData.bgmClip, isBgm:true);
						if (!string.IsNullOrEmpty(addr))
						{
							step.bgmData.bgmAddress = addr;
							changed = true;
						}
					}
					if (step.bgmData.bgmClip != null)
					{
						step.bgmData.bgmClip = null;
						changed = true;
					}
				}
				asset.steps[i] = step;
			}

			if (changed)
			{
				EditorUtility.SetDirty(asset);
				migrated++;
			}
		}
		if (migrated > 0)
		{
			AssetDatabase.SaveAssets();
		}
		EditorUtility.DisplayDialog("컷신 마이그레이션", $"주소로 변환된 자산: {migrated}개", "확인");
	}

	private static string BuildDefaultSpriteAddress(Sprite sprite)
	{
		var path = AssetDatabase.GetAssetPath(sprite);
		if (string.IsNullOrEmpty(path)) return null;
		var file = Path.GetFileNameWithoutExtension(path);
		// 기본 규칙: Backgrounds/<name> 또는 Portraits/<name> 추정
		if (path.IndexOf("Background", System.StringComparison.OrdinalIgnoreCase) >= 0)
			return $"Backgrounds/{file}";
		if (path.IndexOf("Portrait", System.StringComparison.OrdinalIgnoreCase) >= 0)
			return $"Portraits/{file}";
		return file;
	}

	private static string BuildDefaultAudioAddress(AudioClip clip, bool isBgm)
	{
		var path = AssetDatabase.GetAssetPath(clip);
		if (string.IsNullOrEmpty(path)) return null;
		var file = Path.GetFileNameWithoutExtension(path);
		return isBgm ? $"Audio/BGM/{file}" : $"Audio/SFX/{file}";
	}
}
