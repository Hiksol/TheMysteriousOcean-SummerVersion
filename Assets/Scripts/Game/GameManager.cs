using System;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : SingletonNetworkBehaviour<GameManager>
{
    [SyncVar(hook = nameof(OnChangeSeed))] public int seed = 0;
    public Rng Rng { get; private set; }
    public bool gameIsRunning = true;

    protected override void AwakeNew() {
        if (seed == 0)
            seed = DateTime.Now.GetHashCode();
        Rng = new(seed);
    }

    void OnChangeSeed(int _, int newVal) {
        Rng = new(newVal);
    }

    void Update() {
        if (!isServer) return;
        if (!gameIsRunning) return;
        if (YachtManager.I.currentSinkingProgress >= YachtManager.I.maxSinkingProgress) {
            gameIsRunning = false;
            FindObjectsByType<Player>().ToList().ForEach(player => player.SetPlayerState(PlayerState.Dead));
            RpcSendNotificationToEveryone("You've lost", NotificationInstance.NotificationType.Danger);
            Invoke(nameof(ExitGame), 5f);
        } else if (YachtManager.I.breaches.Count == 0) {
            gameIsRunning = false;
            FindObjectsByType<Player>().ToList().ForEach(player => player.SetPlayerState(PlayerState.Dead));
            RpcSendNotificationToEveryone("You've win", NotificationInstance.NotificationType.Info);
            Invoke(nameof(ExitGame), 5f);
        }
    }

    [ClientRpc]
    void RpcSendNotificationToEveryone(string text, NotificationInstance.NotificationType notificationType) {
        NotificationManager.I.PrintNotification(text, notificationType);
    }

    [Server]
    void ExitGame() {
        RpcResetGame(true);
        // NetworkManager.singleton.ServerChangeScene(mainMenuScene);
        ResetGame(true);
    }

    [ClientRpc]
    void RpcResetGame(bool disconnect) {
        ResetGame(disconnect);
    }

    void ResetGame(bool disconnect = false) {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        if (disconnect) {
            if (NetworkServer.active) NetworkManager.singleton.StopHost();
            else NetworkManager.singleton.StopClient();
        } else if (isServer) NetworkManager.singleton.ServerChangeScene(SceneManager.GetActiveScene().name);
    }

    public void Exit() {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
