using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class UIHoverTransparency : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("通常時のアルファ値")]
    [Range(0, 1)]
    public float normalAlpha = 1.0f;

    [Tooltip("カーソルを乗せた時のアルファ値")]
    [Range(0, 1)]
    public float hoverAlpha = 0.5f;

    [Tooltip("変化にかかる時間（秒）")]
    public float duration = 0.3f;

    private CanvasGroup canvasGroup;
    private Coroutine fadeCoroutine;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = normalAlpha;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 既に実行中のフェード処理があれば停止し、新しい処理を開始する
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(Fade(hoverAlpha));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 既に実行中のフェード処理があれば停止し、新しい処理を開始する
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(Fade(normalAlpha));
    }

    // 指定したアルファ値まで時間をかけて変化させるコルーチン
    private IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float time = 0f;

        while (time < duration)
        {
            // 経過時間に合わせてアルファ値を線形補間
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            time += Time.deltaTime; // 経過時間を加算
            yield return null; // 1フレーム待機
        }

        // 最終的なアルファ値を設定
        canvasGroup.alpha = targetAlpha;
    }
}