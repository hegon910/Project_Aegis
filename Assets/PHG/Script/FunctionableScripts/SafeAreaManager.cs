using System;
using UnityEngine;
using UnityEngine.UI;

public class SafeAreaManager : MonoBehaviour
{
	private static SafeAreaManager _instance;
	public static SafeAreaManager Instance
	{
		get { return _instance; }
	}

	// 매니저가 준비되었음을 알리는 정적 이벤트 (바인딩이 먼저 생성된 경우 대비)
	public static event Action OnReady;

	[SerializeField] private Color fillerColor = Color.black;
	[SerializeField] private Image topFiller;
	[SerializeField] private Image bottomFiller;
	[SerializeField] private Image leftFiller;
	[SerializeField] private Image rightFiller;

	private Transform _fillersParent;
	private Canvas _overlayCanvas;

	private Rect _lastSafeArea = new Rect(0, 0, 0, 0);
	private Vector2 _lastScreenSize = Vector2.zero;
	private ScreenOrientation _lastOrientation = ScreenOrientation.AutoRotation;

	// 이벤트: 정규화된 앵커(min/max)가 변경될 때 통지
	public event Action<Vector2, Vector2> OnSafeAreaChanged;

	public Rect CurrentSafeAreaPixels { get; private set; }
	public Vector2 CurrentAnchorMin { get; private set; }
	public Vector2 CurrentAnchorMax { get; private set; }

	void Awake()
	{
		if (_instance != null && _instance != this)
		{
			Destroy(gameObject);
			return;
		}
		_instance = this;
		DontDestroyOnLoad(gameObject);

		var parentCanvas = GetComponentInParent<Canvas>();
		_overlayCanvas = CreateOrGetOverlayCanvas(parentCanvas);
		_fillersParent = _overlayCanvas != null ? _overlayCanvas.transform : (parentCanvas != null ? parentCanvas.transform : transform);
		EnsureFillers();
		ApplyAndBroadcastSafeArea();
		OnReady?.Invoke();
	}

	void Update()
	{
		bool sizeChanged = _lastScreenSize.x != Screen.width || _lastScreenSize.y != Screen.height;
		bool orientationChanged = _lastOrientation != Screen.orientation;
		bool safeAreaChanged = _lastSafeArea != Screen.safeArea;

		if (sizeChanged || orientationChanged || safeAreaChanged)
		{
			ApplyAndBroadcastSafeArea();
		}
	}

	private void ApplyAndBroadcastSafeArea()
	{
		EnsureFillers();
		Rect safeArea = Screen.safeArea;
		_lastSafeArea = safeArea;
		_lastScreenSize = new Vector2(Screen.width, Screen.height);
		_lastOrientation = Screen.orientation;

		Vector2 anchorMin = safeArea.position;
		Vector2 anchorMax = safeArea.position + safeArea.size;
		anchorMin.x /= Screen.width;
		anchorMin.y /= Screen.height;
		anchorMax.x /= Screen.width;
		anchorMax.y /= Screen.height;

		CurrentSafeAreaPixels = safeArea;
		CurrentAnchorMin = anchorMin;
		CurrentAnchorMax = anchorMax;

		UpdateFillers(anchorMin, anchorMax);
		OnSafeAreaChanged?.Invoke(anchorMin, anchorMax);
	}

	private Canvas CreateOrGetOverlayCanvas(Canvas parent)
	{
		// 이미 만들어졌다면 반환
		if (_overlayCanvas != null) return _overlayCanvas;

		var go = new GameObject("SafeAreaFillerCanvas", typeof(RectTransform), typeof(Canvas));
		go.transform.SetParent(transform, false);
		var canvas = go.GetComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 전역 최상단 오버레이
		canvas.overrideSorting = true;
		canvas.sortingOrder = 32760; // 매우 높은 정렬 순서로 항상 최상단에 위치
		if (parent != null)
		{
			canvas.sortingLayerID = parent.sortingLayerID; // 레이어 정렬 일치
		}
		var raycaster = go.GetComponent<UnityEngine.UI.GraphicRaycaster>();
		if (raycaster != null) raycaster.enabled = false;
		var rt = go.GetComponent<RectTransform>();
		rt.anchorMin = Vector2.zero;
		rt.anchorMax = Vector2.one;
		rt.offsetMin = Vector2.zero;
		rt.offsetMax = Vector2.zero;
		_overlayCanvas = canvas;
		return _overlayCanvas;
	}

	private void EnsureFillers()
	{
		EnsureFiller(ref topFiller, "TopFiller");
		EnsureFiller(ref bottomFiller, "BottomFiller");
		EnsureFiller(ref leftFiller, "LeftFiller");
		EnsureFiller(ref rightFiller, "RightFiller");
	}

	private void EnsureFiller(ref Image img, string name)
	{
		if (img == null)
		{
			var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
			go.transform.SetParent(_fillersParent, false);
			img = go.GetComponent<Image>();
			img.raycastTarget = false;
		}
		img.color = fillerColor;
		BringToFront(img);
	}

	private void BringToFront(Image img)
	{
		if (img != null)
		{
			img.transform.SetAsLastSibling();
		}
	}

	private void UpdateFillers(Vector2 anchorMin, Vector2 anchorMax)
	{
		// 화면을 0..1 정규화 기준으로, 안전영역 밖의 여백만 채우는 이미지 패널들 크기 조정
		if (topFiller != null)
		{
			var rt = topFiller.rectTransform;
			rt.anchorMin = new Vector2(0f, anchorMax.y);
			rt.anchorMax = new Vector2(1f, 1f);
			rt.offsetMin = Vector2.zero;
			rt.offsetMax = Vector2.zero;
			topFiller.color = fillerColor;
			BringToFront(topFiller);
		}
		if (bottomFiller != null)
		{
			var rt = bottomFiller.rectTransform;
			rt.anchorMin = new Vector2(0f, 0f);
			rt.anchorMax = new Vector2(1f, anchorMin.y);
			rt.offsetMin = Vector2.zero;
			rt.offsetMax = Vector2.zero;
			bottomFiller.color = fillerColor;
			BringToFront(bottomFiller);
		}
		if (leftFiller != null)
		{
			var rt = leftFiller.rectTransform;
			rt.anchorMin = new Vector2(0f, anchorMin.y);
			rt.anchorMax = new Vector2(anchorMin.x, anchorMax.y);
			rt.offsetMin = Vector2.zero;
			rt.offsetMax = Vector2.zero;
			leftFiller.color = fillerColor;
			BringToFront(leftFiller);
		}
		if (rightFiller != null)
		{
			var rt = rightFiller.rectTransform;
			rt.anchorMin = new Vector2(anchorMax.x, anchorMin.y);
			rt.anchorMax = new Vector2(1f, anchorMax.y);
			rt.offsetMin = Vector2.zero;
			rt.offsetMax = Vector2.zero;
			rightFiller.color = fillerColor;
			BringToFront(rightFiller);
		}
	}
}

 
