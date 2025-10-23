// 파일 이름: CutsceneData.cs
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Video;

// [System.Serializable]을 사용해야 인스펙터에 노출됩니다.
// 각 기능별 데이터를 묶어주기 위한 작은 구조체들입니다.

[System.Serializable]
public struct ImageEffectData
{
    public Sprite image;
    public string imageAddress; // Addressables 주소가 있으면 이를 우선 사용
    [Tooltip("0이면 즉시 표시, 0보다 크면 해당 시간 동안 페이드인")]
    public float fadeDuration;
}

[System.Serializable]
public struct DialogueEffectData
{
    [TextArea(3, 10)]
    public string dialogue;
    [Tooltip("한 글자가 나타나는 시간. 0이면 즉시 표시")]
    public float typewriterSpeed;
}

[System.Serializable]
public struct SoundEffectData
{
    public AudioClip soundClip;
    public string soundAddress; // Addressables 주소가 있으면 이를 우선 사용
}

[System.Serializable]
public struct BgmEffectData
{
    public AudioClip bgmClip;
    public string bgmAddress; // Addressables 주소가 있으면 이를 우선 사용
    public bool loop;
    [Range(0f,1f)] public float volume;
}
[System.Serializable]
public struct VideoEffectData
{
    public VideoClip videoClip;
    [Tooltip("true면 영상이 끝날 때까지 기다리고, false면 바로 다음 효과 진행")]
    public bool waitForCompletion;
}

[System.Serializable]
public struct DayTextEffectData
{
    [Tooltip("예: Day 1, 제 1장, - 서울 -")]
    public string text;
    [Tooltip("연출 시간 (페이드, 이동 등)")]
    public float animationDuration;
    [Tooltip("화면에 완전히 표시된 후 사라지기 전까지 머무는 시간")]
    public float holdDuration;
}



[System.Serializable]
public class CutsceneStep
{
    [Tooltip("인스펙터에서 구분을 위한 이름")]
    public string stepName;

    [Header("--- 이미지 효과 ---")]
    public bool enableImageEffect; // 이 효과를 사용할지 체크박스
    public ImageEffectData imageData;

    [Header("--- 대사 효과 ---")]
    public bool enableDialogueEffect; // 이 효과를 사용할지 체크박스
    public DialogueEffectData dialogueData;

    [Header("--- 사운드 효과 ---")]
    public bool enableSoundEffect; // 이 효과를 사용할지 체크박스
    public SoundEffectData soundData;

    [Header("--- 배경음악 ---")]
    public bool enableBgmEffect;
    public BgmEffectData bgmData;

    [Header("--- 비디오 효과 ---")]
    public bool enableVideoEffect;
    public VideoEffectData videoData;

    [Header("--- 날짜 표시 효과 ---")]
    public bool enableDayTextEffect;
    public DayTextEffectData dayTextData;



    [Header("--- 진행 설정 ---")]
    [Tooltip("모든 효과가 끝난 후 다음으로 넘어가기 전까지 추가로 대기할 시간")]
    public float waitTime = 0.5f;
    [Tooltip("true면 유저의 클릭을 기다리고, false면 waitTime 이후 자동으로 넘어감")]
    public bool waitForClick = true;
}

[CreateAssetMenu(fileName = "NewCutscene", menuName = "Game/Cutscene Data")]
public class CutsceneData : ScriptableObject
{
    public List<CutsceneStep> steps;
}