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

    // --- 스킵 및 상태 관리를 위한 변수들 ---
    private bool isStepActive = false;      // 현재 스텝이 진행 중인지 (클릭 입력을 받을지)
    private bool isSkippable = false;     // 현재 스텝의 '연출'을 스킵할 수 있는지
    private bool isVideoPlaying = false;    // 비디오가 재생중인지 (비디오는 별도 스킵 로직 필요)
    private Coroutine stepProcessCoroutine; // 현재 진행 중인 스텝 코루틴의 참조
    private bool isCutsceneActive = false;

    private void Awake()
    {
        // 게임이 시작될 때 자신의 이름과 위치를 콘솔에 알립니다.
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
        // 클릭 인디케이터는 스텝이 활성화되어 유저의 입력을 기다릴 때 항상 표시
        if (clickIndicator != null)
        {
            // 비디오 재생 중에는 인디케이터를 숨깁니다.
            clickIndicator.SetActive(isStepActive && !isVideoPlaying);
        }

        // 스텝이 활성화된 상태 & 비디오 재생 중이 아닐 때만 클릭 감지
        if (isStepActive && !isVideoPlaying && Input.GetMouseButtonDown(0))
        {
            // 스킵 가능한 상태(연출 진행 중)라면 -> 연출 스킵
            if (isSkippable)
            {
                SkipCurrentStepEffects();
            }
            // 스킵 불가능한 상태(연출 완료)라면 -> 다음 스텝으로
            else
            {
                PlayNextStep();
            }
        }
    }

    private void PlayNextStep()
    {
        // 이전에 실행되던 스텝 코루틴이 있다면 확실히 중지
        if (stepProcessCoroutine != null)
        {
            StopCoroutine(stepProcessCoroutine);
        }

        currentStepIndex++;

        if (currentStepIndex >= currentCutscene.steps.Count)
        {
            FinishCutscene();
            return;
        }

        // 새로운 스텝 처리 코루틴을 시작하고 참조를 저장
        stepProcessCoroutine = StartCoroutine(ProcessStep(currentCutscene.steps[currentStepIndex]));
    }

    private void SkipCurrentStepEffects()
    {
        if (stepProcessCoroutine != null)
        {
            StopCoroutine(stepProcessCoroutine);
        }

        // 현재 스텝의 최종 상태를 즉시 적용
        ApplyFinalState(currentCutscene.steps[currentStepIndex]);

        isSkippable = false; // 스킵했으므로 더 이상 스킵 가능한 상태가 아님
    }

    private void ApplyFinalState(CutsceneStep step)
    {
        if (step.enableImageEffect)
        {
            backgroundImage.sprite = step.imageData.image;
            backgroundImage.color = Color.white;
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
                // 숨기는 대신, 최종 상태로 즉시 표시합니다.
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
        if (dayText != null)
        {
            dayText.gameObject.SetActive(false);
        }
        List<Coroutine> runningCoroutines = new List<Coroutine>();

        if (step.enableImageEffect) runningCoroutines.Add(StartCoroutine(ImageEffectCoroutine(step.imageData)));
        if (step.enableDialogueEffect) runningCoroutines.Add(StartCoroutine(DialogueEffectCoroutine(step.dialogueData)));
        if (step.enableSoundEffect) StartCoroutine(SoundEffectCoroutine(step.soundData));
        if (step.enableVideoEffect)
        {
            var videoCoroutine = StartCoroutine(VideoEffectCoroutine(step.videoData));
            if (step.videoData.waitForCompletion)
            {
                runningCoroutines.Add(videoCoroutine);
            }
        }
        if (step.enableDayTextEffect)
        {
            runningCoroutines.Add(StartCoroutine(DayTextEffectCoroutine(step.dayTextData)));
        }

        foreach (var coroutine in runningCoroutines)
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
        OnCutsceneFinished?.Invoke();

        isCutsceneActive = false;
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        isVideoPlaying = false;
    }

    // --- 각 효과를 처리하는 코루틴들 ---

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

        while (isVideoPlaying)
        {
            yield return null;
        }

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

        // --- 1. 페이드 인 ---
        timer = 0f;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            dayText.color = Color.Lerp(startColor, endColor, timer / halfDuration);
            yield return null;
        }
        dayText.color = endColor;

        // --- 2. 대기 ---
        yield return new WaitForSeconds(data.holdDuration);

        // --- 3. 페이드 아웃 ---
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