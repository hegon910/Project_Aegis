using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

public static class ResourcesToAddressables
{
	private const string ResourcesFolder = "Assets/Resources";
	private const string DefaultGroupName = "Resources (Auto)";

	[MenuItem("Tools/Addressables/등록: Resources 전체 → Addressables (LZMA 압축)", priority = 2000)]
	public static void RegisterAllResources()
	{
		var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
		if (settings == null)
		{
			Debug.LogError("AddressableAssetSettings 로드에 실패했습니다.");
			return;
		}

		if (!AssetDatabase.IsValidFolder(ResourcesFolder))
		{
			EditorUtility.DisplayDialog("Resources 없음", "Assets/Resources 폴더가 없습니다.", "확인");
			return;
		}

		// 그룹 가져오거나 생성
		var group = GetOrCreateGroup(settings, DefaultGroupName, BundledAssetGroupSchema.BundleCompressionMode.LZMA);

		// Resources 아래 모든 에셋 경로 수집
		string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { ResourcesFolder });
		int added = 0;
		var entries = new List<AddressableAssetEntry>();

		AssetDatabase.StartAssetEditing();
		try
		{
			for (int i = 0; i < guids.Length; i++)
			{
				string guid = guids[i];
				string path = AssetDatabase.GUIDToAssetPath(guid);
				if (string.IsNullOrEmpty(path)) continue;

				if (Directory.Exists(path)) continue; // 폴더는 건너뜀
				if (path.EndsWith(".cs") || path.EndsWith(".dll") || path.EndsWith(".meta")) continue; // 스크립트/메타 제외

				// 이미 Addressable인 경우 스킵 (중복 등록 방지)
				var existing = settings.FindAssetEntry(guid);
				if (existing != null)
				{
					continue;
				}

				// 주소를 Resources 상대 경로 기반으로 지정 (확장자 제외)
				string address = BuildAddressFromResourcesPath(path);
				var entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
				entry.address = address;
				entries.Add(entry);
				added++;
			}
		}
		finally
		{
			AssetDatabase.StopAssetEditing();
		}

		if (added > 0)
		{
			settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entries, false, true);
			AssetDatabase.SaveAssets();
		}

		EditorUtility.DisplayDialog("완료", $"Addressables 등록 완료\n추가: {added}개\n그룹: {group.Name}\n압축: LZMA", "확인");
	}

	[MenuItem("Tools/Addressables/등록: 지정 폴더만 (Audio/BGM, Audio/SFX, Backgrounds, Portraits)", priority = 2000)]
	public static void RegisterSelectedResourceFolders()
	{
		var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
		if (settings == null) { Debug.LogError("AddressableAssetSettings 로드 실패"); return; }

		if (!AssetDatabase.IsValidFolder(ResourcesFolder))
		{
			EditorUtility.DisplayDialog("Resources 없음", "Assets/Resources 폴더가 없습니다.", "확인");
			return;
		}

		string[] targetFolders = new[]
		{
			"Assets/Resources/Audio/BGM",
			"Assets/Resources/Audio/SFX",
			"Assets/Resources/Backgrounds",
			"Assets/Resources/Portraits"
		};

		var group = GetOrCreateGroup(settings, DefaultGroupName, BundledAssetGroupSchema.BundleCompressionMode.LZMA);
		int added = 0;
		var entries = new List<AddressableAssetEntry>();

		AssetDatabase.StartAssetEditing();
		try
		{
			foreach (var folder in targetFolders)
			{
				if (!AssetDatabase.IsValidFolder(folder)) continue;
				string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { folder });
				for (int i = 0; i < guids.Length; i++)
				{
					string guid = guids[i];
					string path = AssetDatabase.GUIDToAssetPath(guid);
					if (string.IsNullOrEmpty(path)) continue;
					if (Directory.Exists(path)) continue;
					if (path.EndsWith(".cs") || path.EndsWith(".dll") || path.EndsWith(".meta")) continue;

					if (settings.FindAssetEntry(guid) != null) continue;

					string address = BuildAddressFromResourcesPath(path);
					var entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
					entry.address = address;
					entries.Add(entry);
					added++;
				}
			}
		}
		finally
		{
			AssetDatabase.StopAssetEditing();
		}

		if (added > 0)
		{
			settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entries, false, true);
			AssetDatabase.SaveAssets();
		}

		EditorUtility.DisplayDialog("완료", $"지정 폴더 등록 완료\n추가: {added}개\n그룹: {group.Name}\n압축: LZMA", "확인");
	}

	[MenuItem("Tools/Addressables/그룹 압축을 LZMA로 강제", priority = 2001)]
	public static void ForceLzmaCompressionOnAllGroups()
	{
		var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
		if (settings == null) { Debug.LogWarning("Addressables 설정이 없습니다."); return; }

		foreach (var group in settings.groups)
		{
			if (group == null) continue;
			var schema = group.GetSchema<BundledAssetGroupSchema>();
			if (schema == null) continue;
			if (schema.Compression != BundledAssetGroupSchema.BundleCompressionMode.LZMA)
			{
				schema.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZMA;
				EditorUtility.SetDirty(schema);
			}
		}
		AssetDatabase.SaveAssets();
		EditorUtility.DisplayDialog("적용됨", "모든 번들 그룹의 압축을 LZMA로 설정했습니다.", "확인");
	}

	private static AddressableAssetGroup GetOrCreateGroup(AddressableAssetSettings settings, string groupName, BundledAssetGroupSchema.BundleCompressionMode compression)
	{
		var group = settings.FindGroup(groupName);
		if (group == null)
		{
			group = settings.CreateGroup(groupName, false, false, true, null);
		}

		var bundled = group.GetSchema<BundledAssetGroupSchema>();
		if (bundled == null)
		{
			bundled = group.AddSchema<BundledAssetGroupSchema>();
		}
		bundled.Compression = compression;
		bundled.IncludeInBuild = true;
		EditorUtility.SetDirty(bundled);

		var contentUpdate = group.GetSchema<UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema>();
		if (contentUpdate == null)
		{
			contentUpdate = group.AddSchema<UnityEditor.AddressableAssets.Settings.GroupSchemas.ContentUpdateGroupSchema>();
			EditorUtility.SetDirty(contentUpdate);
		}

		EditorUtility.SetDirty(group);
		return group;
	}

	private static string BuildAddressFromResourcesPath(string assetPath)
	{
		// 예: Assets/Resources/Portraits/A.png -> Portraits/A
		int idx = assetPath.IndexOf("Assets/Resources/");
		string relative = idx >= 0 ? assetPath.Substring(idx + "Assets/Resources/".Length) : Path.GetFileName(assetPath);
		string noExt = Path.ChangeExtension(relative, null);
		return noExt.Replace('\\', '/');
	}
}


