using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

[Serializable]
public class GameSettings
{
    // 사운드, 언어 등 환경설정 값
    public float masterVolume = 1.0f;
    public float bgmVolume = 1.0f;
    public float sfxVolume = 1.0f;
}

[Serializable]
public class SettingsData
{
    public List<int> selectedSubEventPackIDs; // 여러 ID를 저장할 수 있도록 List<int>로 변경
    public GameSettings settings;
    // 지휘관 해금 목록을 설정에 보관하여 새 게임 시에도 보존되도록 함
    public List<int> unlockedCommanderTraitIds;
    // 서브스토리팩 해금 목록 (구매로 해금된 팩 ID 저장)
    public List<int> unlockedStoryPackIds;
    // 일반 상점 아이템 구매 내역 (itemId 저장)
    public List<string> purchasedShopItemIds;
    // 재화 데이터 (새 게임으로 초기화되지 않음)
    public int currencyAmount; // 업적 보상으로 획득한 재화

    /// <summary>
    /// 새 설정 파일 생성 시 기본값
    /// </summary>
    public SettingsData()
    {
        selectedSubEventPackIDs = new List<int> { 1 }; // 기본적으로 1번 서브이벤트팩 선택
        settings = new GameSettings();
        // 기본값: Devost(0)만 해금
        unlockedCommanderTraitIds = new List<int> { 0 };
        // 기본값: 스토리팩 1000001만 해금
        unlockedStoryPackIds = new List<int> { 1000001 };
        // 기본값: 구매 내역 비어 있음
        purchasedShopItemIds = new List<string>();
        // 기본 재화 초기화
        currencyAmount = 0;
      // Debug.Log($"[SettingsData] 새 설정 데이터 생성 - 기본 서브이벤트팩: {string.Join(", ", selectedSubEventPackIDs)}");
    }
}