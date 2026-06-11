using UnityEngine;

public class SingletonMonoBehaviour<T> : MonoBehaviour where T: SingletonMonoBehaviour<T>
{
    public static T I { get; private set; }
    protected virtual bool ShouldDontDestroyOnLoad => false;

    void Awake() {
        if (I != null) {
            Destroy(gameObject.transform.root.gameObject);
            return;
        }
        I = (T)this;
        if (ShouldDontDestroyOnLoad) DontDestroyOnLoad(gameObject.transform.root.gameObject);
        AwakeNew();
    }

    protected virtual void OnDestroy() {
        if (I == this) I = null;
    }

    /// <summary>
    /// Use this instead of Awake()
    /// </summary>
    protected virtual void AwakeNew() {}
}