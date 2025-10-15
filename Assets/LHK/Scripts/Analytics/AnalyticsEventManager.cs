using UnityEngine;
using System.Collections.Generic;

#if !UNITY_EDITOR
using Firebase.Analytics;
#endif

/// <summary>
/// Firebase Analytics 이벤트 관리자
/// FirebaseManager의 초기화를 활용하여 Analytics 이벤트를 전송합니다.
/// </summary>
public class AnalyticsEventManager : MonoBehaviour
{
    public static AnalyticsEventManager Instance { get; private set; }
    
    private bool isInitialized = false;
    private Dictionary<string, bool> oneTimeEventsSent = new Dictionary<string, bool>();
    
    private const string ONE_TIME_EVENT_PREFIX = "Analytics_OneTime_";
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadOneTimeEventFlags();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        // FirebaseManager 초기화 완료 이벤트 구독
        FirebaseManager.OnFirebaseManagerInitialized += OnFirebaseInitialized;
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            FirebaseManager.OnFirebaseManagerInitialized -= OnFirebaseInitialized;
        }
    }
    
    /// <summary>
    /// Firebase 초기화 완료 시 호출
    /// </summary>
    private void OnFirebaseInitialized()
    {
        isInitialized = true;
        Debug.Log("[AnalyticsEventManager] Firebase Analytics 준비 완료");
        
#if !UNITY_EDITOR
        // Analytics 설정
        FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
        Debug.Log("[AnalyticsEventManager] Firebase Analytics 활성화");
#else
        Debug.Log("[AnalyticsEventManager] 에디터 환경: Analytics 이벤트는 로그만 출력됩니다");
#endif
    }
    
    /// <summary>
    /// 1회성 이벤트 플래그 로드
    /// </summary>
    private void LoadOneTimeEventFlags()
    {
        oneTimeEventsSent.Clear();
        
        foreach (LogEventType eventType in System.Enum.GetValues(typeof(LogEventType)))
        {
            if (eventType.IsOneTimeEvent())
            {
                string key = ONE_TIME_EVENT_PREFIX + eventType.ToString();
                bool hasSent = PlayerPrefs.GetInt(key, 0) == 1;
                oneTimeEventsSent[eventType.ToString()] = hasSent;
            }
        }
    }
    
    /// <summary>
    /// 파라미터 없이 이벤트 로그 전송
    /// </summary>
    public void LogEvent(LogEventType eventType)
    {
        // 1회성 이벤트 체크
        if (eventType.IsOneTimeEvent())
        {
            if (HasSentOneTimeEvent(eventType))
            {
                Debug.Log($"[AnalyticsEventManager] 이미 전송된 1회성 이벤트: {eventType}");
                return;
            }
            
            MarkOneTimeEventAsSent(eventType);
        }
        
        string eventName = eventType.ToEventName();
        
#if UNITY_EDITOR
        Debug.Log($"[AnalyticsEventManager] 이벤트 로그 (에디터): {eventName}");
#else
        if (isInitialized)
        {
            FirebaseAnalytics.LogEvent(eventName);
            Debug.Log($"[AnalyticsEventManager] 이벤트 전송: {eventName}");
        }
        else
        {
            Debug.LogWarning($"[AnalyticsEventManager] Firebase 초기화 전에 이벤트 호출됨: {eventName}");
        }
#endif
    }
    
    /// <summary>
    /// 정수 파라미터와 함께 이벤트 로그 전송
    /// </summary>
    public void LogEvent(LogEventType eventType, string parameterName, int parameterValue)
    {
        // 1회성 이벤트 체크
        if (eventType.IsOneTimeEvent())
        {
            if (HasSentOneTimeEvent(eventType))
            {
                Debug.Log($"[AnalyticsEventManager] 이미 전송된 1회성 이벤트: {eventType}");
                return;
            }
            
            MarkOneTimeEventAsSent(eventType);
        }
        
        string eventName = eventType.ToEventName();
        
#if UNITY_EDITOR
        Debug.Log($"[AnalyticsEventManager] 이벤트 로그 (에디터): {eventName}, {parameterName}={parameterValue}");
#else
        if (isInitialized)
        {
            FirebaseAnalytics.LogEvent(eventName, parameterName, parameterValue);
            Debug.Log($"[AnalyticsEventManager] 이벤트 전송: {eventName}, {parameterName}={parameterValue}");
        }
        else
        {
            Debug.LogWarning($"[AnalyticsEventManager] Firebase 초기화 전에 이벤트 호출됨: {eventName}");
        }
#endif
    }
    
    /// <summary>
    /// 문자열 파라미터와 함께 이벤트 로그 전송
    /// </summary>
    public void LogEvent(LogEventType eventType, string parameterName, string parameterValue)
    {
        // 1회성 이벤트 체크
        if (eventType.IsOneTimeEvent())
        {
            if (HasSentOneTimeEvent(eventType))
            {
                Debug.Log($"[AnalyticsEventManager] 이미 전송된 1회성 이벤트: {eventType}");
                return;
            }
            
            MarkOneTimeEventAsSent(eventType);
        }
        
        string eventName = eventType.ToEventName();
        
#if UNITY_EDITOR
        Debug.Log($"[AnalyticsEventManager] 이벤트 로그 (에디터): {eventName}, {parameterName}={parameterValue}");
#else
        if (isInitialized)
        {
            FirebaseAnalytics.LogEvent(eventName, parameterName, parameterValue);
            Debug.Log($"[AnalyticsEventManager] 이벤트 전송: {eventName}, {parameterName}={parameterValue}");
        }
        else
        {
            Debug.LogWarning($"[AnalyticsEventManager] Firebase 초기화 전에 이벤트 호출됨: {eventName}");
        }
#endif
    }
    
    /// <summary>
    /// 1회성 이벤트가 이미 전송되었는지 확인
    /// </summary>
    private bool HasSentOneTimeEvent(LogEventType eventType)
    {
        string key = eventType.ToString();
        return oneTimeEventsSent.ContainsKey(key) && oneTimeEventsSent[key];
    }
    
    /// <summary>
    /// 1회성 이벤트를 전송됨으로 마킹
    /// </summary>
    private void MarkOneTimeEventAsSent(LogEventType eventType)
    {
        string key = eventType.ToString();
        oneTimeEventsSent[key] = true;
        
        // PlayerPrefs에 저장
        string prefsKey = ONE_TIME_EVENT_PREFIX + key;
        PlayerPrefs.SetInt(prefsKey, 1);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// 1회성 이벤트 플래그 초기화 (테스트용)
    /// </summary>
    public void ResetOneTimeEvent(LogEventType eventType)
    {
        string key = eventType.ToString();
        if (oneTimeEventsSent.ContainsKey(key))
        {
            oneTimeEventsSent[key] = false;
        }
        
        string prefsKey = ONE_TIME_EVENT_PREFIX + key;
        PlayerPrefs.DeleteKey(prefsKey);
        PlayerPrefs.Save();
        
        Debug.Log($"[AnalyticsEventManager] 1회성 이벤트 플래그 초기화: {eventType}");
    }
    
    /// <summary>
    /// 모든 1회성 이벤트 플래그 초기화 (테스트용)
    /// </summary>
    public void ResetAllOneTimeEvents()
    {
        foreach (LogEventType eventType in System.Enum.GetValues(typeof(LogEventType)))
        {
            if (eventType.IsOneTimeEvent())
            {
                string key = eventType.ToString();
                if (oneTimeEventsSent.ContainsKey(key))
                {
                    oneTimeEventsSent[key] = false;
                }
                
                string prefsKey = ONE_TIME_EVENT_PREFIX + key;
                PlayerPrefs.DeleteKey(prefsKey);
            }
        }
        
        PlayerPrefs.Save();
        Debug.Log("[AnalyticsEventManager] 모든 1회성 이벤트 플래그 초기화");
    }
}

