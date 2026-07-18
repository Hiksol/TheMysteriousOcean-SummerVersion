using System;

[Serializable]
public abstract class TutorialState
{
    public string tutorialText;

    public virtual void OnEnter() {}
    public virtual void OnExit() {}

    public virtual void OnUpdate() {}

    public abstract bool IsComplete();
}
