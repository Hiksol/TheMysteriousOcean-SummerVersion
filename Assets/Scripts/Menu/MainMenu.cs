using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializedDictionary("SubMenu name", "SubMenu parent")]
    public SerializedDictionary<string, Transform> subMenus = new();
    [Scene] public string tutorialScene;

    public void SetSubMenu(string subMenuName) {
        if (subMenus.TryGetValue(subMenuName, out Transform _)) {
            foreach (KeyValuePair<string, Transform> subMenu in subMenus) {
                subMenu.Value.gameObject.SetActive(subMenu.Key == subMenuName);
            }
        }
    }

    public void StartGameHost() => NetworkManager.singleton.StartHost();

    public void StartGameClient() => NetworkManager.singleton.StartClient();

    public void StartTutorial() {
        Destroy(NetworkManager.singleton.gameObject);
        SceneManager.LoadScene(tutorialScene);
    }

    public void SetNetworkAddress(string address) {
        NetworkManager.singleton.networkAddress = address;
    }

    public void SetPort(string port) {
        if (Transport.active is PortTransport portTransport) {
            // use TryParse in case someone tries to enter non-numeric characters
            if (ushort.TryParse(port, out ushort uport))
                portTransport.Port = uport;
        }
    }

    public void Exit() => GameManager.I.Exit();
}
