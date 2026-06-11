using System;
using System.Linq;
using Mirror;
using UnityEngine.SceneManagement;

public class GameManager : SingletonNetworkBehaviour<GameManager>
{
    [SyncVar(hook = nameof(OnChangeSeed))] public int seed = 0;
    public Rng Rng { get; private set; }
    public bool gameIsRunning = true;

    public override void OnStartServer() {
        base.Start();
        if (seed == 0)
            seed = DateTime.Now.GetHashCode();
        Rng = new(seed);
    }

    void OnChangeSeed(int _, int newVal) {
        Rng = new(newVal);
    }

    void Update() {
        if (!isServer) return;
        YachtManager ym = YachtManager.I;
        if (ym.currentSinkingProgress >= ym.maxSinkingProgress && gameIsRunning) {
            gameIsRunning = false;
            FindObjectsByType<Player>().ToList().ForEach(player => player.SetPlayerState(PlayerState.Dead));
            RpcSendNotificationToEveryone("You've lost", NotificationInstance.NotificationType.Danger);
            Invoke(nameof(RestartGame), 5f);
        }
    }

    [ClientRpc]
    void RpcSendNotificationToEveryone(string text, NotificationInstance.NotificationType notificationType) {
        NotificationManager.I.PrintNotification(text, notificationType);
    }

    [Server]
    void RestartGame() {
        NetworkManager.singleton.ServerChangeScene(SceneManager.GetActiveScene().name);
        NetworkManager.singleton.StopHost();
    }

    [ClientRpc]
    void RpcRestartGame() {
        RestartGame();
    }
}
