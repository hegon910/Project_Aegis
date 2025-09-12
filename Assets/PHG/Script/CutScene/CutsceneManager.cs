using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System;
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

    // <<<<<<< [수정 1] 실행 중인 모든 자식(연출) 코루틴을 추적하기 위한 리스트
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
        if (isCutsceneActive) return;
        if (cutsceneData == null || cutsceneData.steps.Count == 0)
        {
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

        if (isStepActive && !isVideoPlaying && Input.GetMouseButtonDown(0))
        {
            if (isSkippable)
            {
                SkipCurrentStepEffects();
            }
            else
            {
                PlayNextStep();
            }
        }
    }

    // <<<<<<< [수정 2] 모든 실행 중인 코루틴(부모+자식)을 확실히 정리하는 헬퍼 함수
    private void StopAllRunningCoroutines()
    {
        // 메인 스텝 코루틴 중지
        if (stepProcessCoroutine != null)
        {
            StopCoroutine(stepProcessCoroutine);
            stepProcessCoroutine = null;
        }

        // 모든 개별 효과(자식) 코루틴들 중지
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
        // 다음 스텝으로 가기 전, 모든 코루틴을 확실히 정리
        StopAllRunningCoroutines();

        currentStepIndex++;

        if (currentStepIndex >= currentCutscene.steps.Count)
        {
            FinishCutscene();
            return;
        }

        stepProcessCoroutine = StartCoroutine(ProcessStep(currentCutscene.steps[currentStepIndex]));
    }

    private void SkipCurrentStepEffects()
    {
        // <<<<<<< [수정 3] 스킵 시에도 모든 코루틴을 정리하도록 변경
        StopAllRunningCoroutines();

        // 현재 스텝의 최종 상태를 즉시 적용
        ApplyFinalState(currentCutscene.steps[currentStepIndex]);

        isSkippable = false;
    }

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
        isStepActive = true;
        isSkippable = true;

        // <<<<<<< [수정 4] 새로운 스텝 시작 전, 추적 리스트를 초기화
        runningEffectCoroutines.Clear();

        if (dayText != null)
        {
            dayText.gameObject.SetActive(false);
        }

        // <<<<<<< [수정 5] 자식 코루틴들을 시작하고, 추적 리스트에 추가
        if (step.enableImageEffect) runningEffectCoroutines.Add(StartCoroutine(ImageEffectCoroutine(step.imageData)));
        if (step.enableDialogueEffect) runningEffectCoroutines.Add(StartCoroutine(DialogueEffectCoroutine(step.dialogueData)));
        if (step.enableSoundEffect) runningEffectCoroutines.Add(StartCoroutine(SoundEffectCoroutine(step.soundData)));
        if (step.enableVideoEffect)
        {
            var videoCoroutine = StartCoroutine(VideoEffectCoroutine(step.videoData));
            runningEffectCoroutines.Add(videoCoroutine); // 비디오 코루틴도 추적
        }
        if (step.enableDayTextEffect)
        {
            runningEffectCoroutines.Add(StartCoroutine(DayTextEffectCoroutine(step.dayTextData)));
        }

        // 모든 자식 코루틴이 끝날 때까지 기다립니다.
        foreach (var coroutine in runningEffectCoroutines)
        {
            yield return coroutine;
        }

        if (step.waitTime > 0)
        {
            yield return new WaitForSeconds(step.waitTime);
        }

        isSkippable = false;

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

        // 비디오 재생이 끝날 때까지 기다림
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

        float halfDuration = data.animationDuration / 2;
        float timer = 0f;
        Color startColor = new Color(dayText.color.r, dayText.color.g, dayText.color.b, 0);
        Color endColor = new Color(dayText.color.r, dayText.color.g, dayText.color.b, 1);

        timer = 0f;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            dayText.color = Color.Lerp(startColor, endColor, timer / halfDuration);
            yield return null;
        }
        dayText.color = endColor;

        yield return new WaitForSeconds(data.holdDuration);

        timer = 0f;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            dayText.color = Color.Lerp(endColor, startColor, timer / halfDuration);
            yield return null;
        }
        dayText.color = startColor;

        dayText.gameObject.SetActive(false);
    }
}