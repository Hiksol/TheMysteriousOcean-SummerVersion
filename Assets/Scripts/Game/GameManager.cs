using System;
using Mirror;

public class GameManager : SingletonNetworkBehaviour<GameManager>
{
    [SyncVar(hook = nameof(OnChangeSeed))] public int seed = 0;
    public Rng Rng { get; private set; }

    public override void OnStartServer() {
        base.Start();
        if (seed == 0)
            seed = DateTime.Now.GetHashCode();
        Rng = new(seed);
    }

    void OnChangeSeed(int _, int newVal) {
        Rng = new(newVal);
    }
}
