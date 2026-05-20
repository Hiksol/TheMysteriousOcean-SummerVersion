using Mirror;

public class SingletonNetworkBehaviour<T> : NetworkBehaviour where T: SingletonNetworkBehaviour<T>
{
    public static T I { get; private set; }
    protected virtual bool ShouldDontDestroyOnLoad => true;

    void Awake() {
        if (I != null) {
            Destroy(gameObject);
            return;
        }
        I = (T)this;
        AwakeNew();
    }

    protected virtual void Start() {
        if (ShouldDontDestroyOnLoad) DontDestroyOnLoad(gameObject.transform.root.gameObject);
    }

    protected virtual void OnDestroy() {
        if (I == this) I = null;
    }

    /// <summary>
    /// Use this instead of Awake()
    /// </summary>
    protected virtual void AwakeNew() {}
}