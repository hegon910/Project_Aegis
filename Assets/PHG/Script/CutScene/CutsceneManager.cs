using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class CutsceneManager : MonoBehaviour
{
    [Header("UI Component References")]
    [SerializeField] private GameObject cutsceneCanvas;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage videoPlayerScreen;
    [SerializeField] private GameObject clickIndicator;

    public static event Action OnCutsceneFinished;

    private CutsceneData currentCutscene;
    private int currentStepIndex;

    private bool isStepActive = false;
    private bool isSkippable = false;
    private bool isVideoPlaying = false;
    private Coroutine stepProcessCoroutine;
    private bool isCutsceneActive = false;

    private List<Coroutine> runningEffectCoroutines = new List<Coroutine>();

    private void Awake()
    {
        Debug.Log("CutsceneManager가 여기서 깨어났습니다!", this.gameObject);
    }
    private void Start()
    {
        cutsceneCanvas.SetActive(false);
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoFinished;
        }
    }

    public void StartCutscene(CutsceneData cutsceneData)
    {
        Debug.Log($"[CutsceneManager] StartCutscene 호출. CutsceneData 스텝 수: {cutsceneData?.steps.Count ?? 0}");
        if (isCutsceneActive) return;
        if (cutsceneData == null || cutsceneData.steps.Count == 0)
        {
            Debug.LogWarning("[CutsceneManager] 유효한 CutsceneData가 없어 컷신을 시작할 수 없습니다.");
            OnCutsceneFinished?.Invoke();
            return;
        }
        isCutsceneActive = true;
        currentCutscene = cutsceneData;
        currentStepIndex = -1;
        isStepActive = false;
        isSkippable = false;
        cutsceneCanvas.SetActive(true);
        PlayNextStep();
    }

    private void Update()
    {
        if (clickIndicator != null)
        {
            clickIndicator.SetActive(isStepActive && !isVideoPlaying);
        }

        // <<<<<<< [복원] '스킵'과 '다음'을 구분하는 로직
        if (isStepActive && !isVideoPlaying && Input.GetMouseButtonDown(0))
        {
            if (isSkippable)
            {
                // 연출 진행 중일 때 클릭 -> 연출 스킵
                SkipCurrentStepEffects();
            }
            else
            {
                // 연출 종료 후 대기 상태일 때 클릭 -> 다음 스텝으로
                PlayNextStep();
            }
        }
    }

    private void StopAllRunningCoroutines()
    {
        if (stepProcessCoroutine != null)
        {
            StopCoroutine(stepProcessCoroutine);
            stepProcessCoroutine = null;
        }

        foreach (var coroutine in runningEffectCoroutines)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
        runningEffectCoroutines.Clear();
    }

    private void PlayNextStep()
    {
        StopAllRunningCoroutines();

        currentStepIndex++;

        if (currentStepIndex >= currentCutscene.steps.Count)
        {
            FinishCutscene();
            return;
        }

        stepProcessCoroutine = StartCoroutine(ProcessStep(currentCutscene.steps[currentStepIndex]));
    }

    // <<<<<<< [복원] 연출 스킵을 위한 메서드
    private void SkipCurrentStepEffects()
    {
        StopAllRunningCoroutines();
        ApplyFinalState(currentCutscene.steps[currentStepIndex]);
        isSkippable = false; // 스킵 후에는 '다음' 상태가 되어야 하므로 false로 변경
    }

    // <<<<<<< [복원] 스킵 시 최종 상태를 즉시 적용하는 메서드
    private void ApplyFinalState(CutsceneStep step)
    {
        if (step.enableImageEffect)
        {
            backgroundImage.sprite = step.imageData.image;
            backgroundImage.color = new Color(1, 1, 1, 0.4f);
            backgroundImage.gameObject.SetActive(true);
        }
        if (step.enableDialogueEffect)
        {
            dialogueText.text = step.dialogueData.dialogue;
            dialoguePanel.SetActive(true);
        }
        if (step.enableVideoEffect)
        {
            if (videoPlayer.isPlaying) videoPlayer.Stop();
            videoPlayerScreen.gameObject.SetActive(false);
            isVideoPlaying = false;
        }
        if (step.enableDayTextEffect)
        {
            if (dayText != null)
            {
                dayText.text = step.dayTextData.text;
                dayText.color = new Color(dayText.color.r, dayText.color.g, dayText.color.b, 1);
                dayText.gameObject.SetActive(true);
            }
        }
    }

    private IEnumerator ProcessStep(CutsceneStep step)
    {
        Debug.Log($"[CutsceneManager] ProcessStep 호출. 현재 스텝 인덱스: {currentStepIndex}");
        Debug.Log($"[CutsceneManager] ImageEffect: {step.enableImageEffect}, DialogueEffect: {step.enableDialogueEffect}, VideoEffect: {step.enableVideoEffect}, DayTextEffect: {step.enableDayTextEffect}");

        if (step.enableImageEffect) Debug.Log($"[CutsceneManager] Image: {step.imageData.image?.name}, FadeDuration: {step.imageData.fadeDuration}");
        if (step.enableDialogueEffect) Debug.Log($"[CutsceneManager] Dialogue: {step.dialogueData.dialogue}");
        if (step.enableVideoEffect) Debug.Log($"[CutsceneManager] Video: {step.videoData.videoClip?.name}");
        if (step.enableDayTextEffect) Debug.Log($"[CutsceneManager] DayText: {step.dayTextData.text}");

        isStepActive = true;
        isSkippable = true; // 연출 시작, '스킵'이 가능한 상태

        runningEffectCoroutines.Clear();

        if (dayText != null)
        {
            dayText.gameObject.SetActive(false);
        }

        if (step.enableImageEffect) runningEffectCoroutines.Add(StartCoroutine(ImageEffectCoroutine(step.imageData)));
        if (step.enableDialogueEffect) runningEffectCoroutines.Add(StartCoroutine(DialogueEffectCoroutine(step.dialogueData)));
        if (step.enableSoundEffect) runningEffectCoroutines.Add(StartCoroutine(SoundEffectCoroutine(step.soundData)));
        if (step.enableVideoEffect)
        {
            var videoCoroutine = StartCoroutine(VideoEffectCoroutine(step.videoData));
            runningEffectCoroutines.Add(videoCoroutine);
        }
        if (step.enableDayTextEffect)
        {
            runningEffectCoroutines.Add(StartCoroutine(DayTextEffectCoroutine(step.dayTextData)));
        }

        // 모든 '연출' 코루틴이 끝날 때까지 기다림
        foreach (var coroutine in runningEffectCoroutines)
        {
            yield return coroutine;
        }

        // <<<<<<< [핵심 수정]
        // 모든 연출이 끝났으므로, 이제부터의 클릭은 '다음'으로 넘기는 역할을 해야 함.
        // 따라서 waitTime 이전에 isSkippable 상태를 false로 변경.
        isSkippable = false;

        if (step.waitTime > 0)
        {
            // 이 waitTime 동안 클릭하면 isSkippable가 false이므로 PlayNextStep()이 호출됨
            yield return new WaitForSeconds(step.waitTime);
        }

        if (!step.waitForClick)
        {
            PlayNextStep();
        }
    }

    private void FinishCutscene()
    {
        isStepActive = false;
        isSkippable = false;

        Debug.Log("컷신 종료.");
        if (clickIndicator != null) clickIndicator.SetActive(false);
        backgroundImage.gameObject.SetActive(false);
        dialoguePanel.SetActive(false);
        videoPlayerScreen.gameObject.SetActive(false);
        cutsceneCanvas.SetActive(false);

        if (isCutsceneActive)
        {
            isCutsceneActive = false;
            OnCutsceneFinished?.Invoke();
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        isVideoPlaying = false;
    }

    // --- 각 효과를 처리하는 코루틴들 (이하 변경 없음) ---
    private IEnumerator ImageEffectCoroutine(ImageEffectData data)
    {
        backgroundImage.sprite = data.image;
        backgroundImage.gameObject.SetActive(true);

        if (data.fadeDuration > 0)
        {
            backgroundImage.color = new Color(1, 1, 1, 0);
            float timer = 0f;
            while (timer < data.fadeDuration)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(0, 0.4f, timer / data.fadeDuration);
                backgroundImage.color = new Color(1, 1, 1, alpha);
                yield return null;
            }
            backgroundImage.color = new Color(1, 1, 1, 0.4f);
        }
        else
        {
            backgroundImage.color = new Color(1, 1, 1, 0.4f);
        }
    }

    private IEnumerator DialogueEffectCoroutine(DialogueEffectData data)
    {
        dialoguePanel.SetActive(true);
        dialogueText.text = "";

        if (data.typewriterSpeed > 0)
        {
            foreach (char letter in data.dialogue.ToCharArray())
            {
                dialogueText.text += letter;
                yield return new WaitForSeconds(data.typewriterSpeed);
            }
        }
        else
        {
            dialogueText.text = data.dialogue;
        }
    }

    private IEnumerator SoundEffectCoroutine(SoundEffectData data)
    {
        if (data.soundClip != null && sfxAudioSource != null)
        {
            sfxAudioSource.PlayOneShot(data.soundClip);
        }
        yield break;
    }

    private IEnumerator VideoEffectCoroutine(VideoEffectData data)
    {
        if (videoPlayer == null || data.videoClip == null)
        {
            Debug.LogError("비디오 플레이어나 비디오 클립이 할당되지 않았습니다!");
            yield break;
        }

        backgroundImage.gameObject.SetActive(false);
        dialoguePanel.SetActive(false);

        videoPlayer.clip = data.videoClip;
        videoPlayer.Prepare();

        while (!videoPlayer.isPrepared)
        {
            yield return null;
        }

        videoPlayerScreen.texture = videoPlayer.texture;
        videoPlayerScreen.gameObject.SetActive(true);
        videoPlayer.Play();

        isVideoPlaying = true;

        while (videoPlayer.isPlaying)
        {
            yield return null;
        }

        isVideoPlaying = false;
        videoPlayerScreen.gameObject.SetActive(false);
    }

    private IEnumerator DayTextEffectCoroutine(DayTextEffectData data)
    {
        if (dayText == null)
        {
            Debug.LogError("DayText UI가 할당되지 않았습니다!");
            yield break;
        }

        dayText.text = data.text;
        dayText.gameObject.SetActive(true);

        // --- 페이드인 로직 ---
        // animationDuration 전체를 페이드인 시간으로 사용합니다.
        float fadeInDuration = data.animationDuration;
        float timer = 0f;
        Color startColor = new Color(dayText.color.r, dayText.color.g, dayText.color.b, 0);
        Color endColor = new Color(dayText.color.r, dayText.color.g, dayText.color.b, 1);

        // 페이드인 루프
        while (timer < fadeInDuration)
        {
            timer += Time.deltaTime;
            dayText.color = Color.Lerp(startColor, endColor, timer / fadeInDuration);
            yield return null;
        }
        dayText.color = endColor; // 페이드인 완료

        // Hold 및 페이드아웃 로직을 모두 제거하여 텍스트가 사라지지 않도록 합니다.
    }
}