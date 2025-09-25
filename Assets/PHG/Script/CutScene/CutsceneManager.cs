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

        // 엔딩 연출용 클릭 로직
        if (isStepActive && !isVideoPlaying && Input.GetMouseButtonDown(0))
        {
            if (isSkippable)
            {
                // 연출 진행 중일 때 클릭 -> 현재 페이지 연출 즉시 완료
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

    // 엔딩 연출용: 현재 페이지 연출 즉시 완료
    private void SkipCurrentStepEffects()
    {
        StopAllRunningCoroutines();
        ApplyFinalState(currentCutscene.steps[currentStepIndex]);
        isSkippable = false; // 연출 완료 후 '다음' 상태로 변경
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
        isStepActive = true;
        isSkippable = true; // 연출 시작, 클릭으로 현재 페이지 연출 완료 가능

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

        // 연출이 끝났으므로 클릭 대기 상태로 변경
        isSkippable = false;

        // 엔딩 연출에서는 자동으로 다음으로 넘어가지 않고 클릭을 기다림
        // 마지막 스텝이 아닌 경우에만 클릭 대기
        if (currentStepIndex < currentCutscene.steps.Count - 1)
        {
            // 다음 스텝이 있으면 클릭 대기
            yield return null; // 한 프레임 대기 후 클릭 대기 상태로 전환
        }
        else
        {
            // 마지막 스텝이면 자동으로 종료
            yield return new WaitForSeconds(1.0f); // 잠깐 대기 후 자동 종료
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