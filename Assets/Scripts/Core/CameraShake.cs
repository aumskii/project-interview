using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class CameraShake : MonoBehaviour
{
    [SerializeField] private RectTransform overlayBackground;

    private Coroutine activeShake;
    private Vector3 cameraOrigin;
    private Vector2 backgroundOrigin;

    public void Configure(RectTransform background) => overlayBackground = background;

    public void Shake(float duration, float amplitude)
    {
        StopShake();
        if (!isActiveAndEnabled || duration <= 0f || amplitude <= 0f)
            return;

        cameraOrigin = transform.localPosition;
        if (overlayBackground != null)
            backgroundOrigin = overlayBackground.anchoredPosition;

        activeShake = StartCoroutine(ShakeRoutine(duration, amplitude));
    }

    public void StopShake()
    {
        if (activeShake == null)
            return;

        StopCoroutine(activeShake);
        RestorePosition();
    }

    private IEnumerator ShakeRoutine(float duration, float amplitude)
    {
        float overlayScale = overlayBackground == null ? 0f :
            overlayBackground.rect.height / (2f * GetComponent<Camera>().orthographicSize);
        float startedAt = Time.unscaledTime;
        for (float elapsed = 0f; elapsed < duration; elapsed = Time.unscaledTime - startedAt)
        {
            Vector2 offset = Random.insideUnitCircle * (amplitude * (1f - elapsed / duration));
            transform.localPosition = cameraOrigin + (Vector3)offset;
            if (overlayBackground != null)
                overlayBackground.anchoredPosition = backgroundOrigin - offset * overlayScale;

            yield return null;
        }

        RestorePosition();
    }

    private void RestorePosition()
    {
        transform.localPosition = cameraOrigin;
        if (overlayBackground != null)
            overlayBackground.anchoredPosition = backgroundOrigin;

        activeShake = null;
    }

    private void OnDisable() => StopShake();
}
