using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaBinding : MonoBehaviour
{
	[SerializeField] private bool applyX = true;
	[SerializeField] private bool applyY = true;

	private RectTransform _rectTransform;

	void Awake()
	{
		_rectTransform = GetComponent<RectTransform>();
		SubscribeIfReady();
	}

	void OnEnable()
	{
		SubscribeIfReady();
	}

	void OnDestroy()
	{
		if (SafeAreaManager.Instance != null)
		{
			SafeAreaManager.Instance.OnSafeAreaChanged -= HandleSafeAreaChanged;
		}
	}

	private void SubscribeIfReady()
	{
		if (SafeAreaManager.Instance != null)
		{
			SafeAreaManager.Instance.OnSafeAreaChanged -= HandleSafeAreaChanged;
			SafeAreaManager.Instance.OnSafeAreaChanged += HandleSafeAreaChanged;
			HandleSafeAreaChanged(SafeAreaManager.Instance.CurrentAnchorMin, SafeAreaManager.Instance.CurrentAnchorMax);
		}
		else
		{
			SafeAreaManager.OnReady -= SubscribeIfReady;
			SafeAreaManager.OnReady += SubscribeIfReady;
		}
	}

	private void HandleSafeAreaChanged(Vector2 anchorMin, Vector2 anchorMax)
	{
		var currentMin = _rectTransform.anchorMin;
		var currentMax = _rectTransform.anchorMax;
		if (applyX)
		{
			currentMin.x = anchorMin.x;
			currentMax.x = anchorMax.x;
		}
		if (applyY)
		{
			currentMin.y = anchorMin.y;
			currentMax.y = anchorMax.y;
		}
		_rectTransform.anchorMin = currentMin;
		_rectTransform.anchorMax = currentMax;
		_rectTransform.offsetMin = Vector2.zero;
		_rectTransform.offsetMax = Vector2.zero;
		_rectTransform.anchoredPosition = Vector2.zero;
	}
}


