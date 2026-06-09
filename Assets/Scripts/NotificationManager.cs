public class NotificationManager : SingletonMonoBehaviour<NotificationManager>
{
    public NotificationInstance notificationInstancePrefab;

    public void PrintNotification(string text, NotificationInstance.NotificationType notificationType = NotificationInstance.NotificationType.Info) {
        NotificationInstance notificationInstance = Instantiate(notificationInstancePrefab, transform);
        notificationInstance.SetNotification(text, notificationType);
    }
}
