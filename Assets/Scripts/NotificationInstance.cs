using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
[RequireComponent(typeof(CanvasGroup))]
public class NotificationInstance : MonoBehaviour
{
    public float timeToShow = 2f;
    public float timeToFade = 1f;
    public Color defaultColor = Color.white;
    public Color warningColor = Color.yellowNice;
    public Color dangerColor = Color.darkRed;
    public TMP_Text notificationText;
    
    Image image;
    CanvasGroup canvasGroup;

    void Awake() {
        image = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void SetNotification(string text, NotificationType notificationType) {
        notificationText.text = text;
        image.color = notificationType switch {
            NotificationType.Warning => warningColor,
            NotificationType.Danger => dangerColor,
            _ => defaultColor
        };
    }

    IEnumerator FadeAndDestroy() {
        yield return new WaitForSeconds(timeToShow);
        while (canvasGroup.alpha > 0) {
            canvasGroup.alpha -= timeToFade <= 0 ? 1 : Time.deltaTime / timeToFade;
            yield return null;
        }
        Destroy(gameObject);
    }

    public enum NotificationType {
        Info,
        Warning,
        Danger
    }
}
