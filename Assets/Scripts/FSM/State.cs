// Dosya: Assets/Scripts/FSM/State.cs
// T: durumun sahibi olan sınıf (bu derste Guard)
public abstract class State<T>
{
    public virtual string Name => GetType().Name;

    public virtual void Enter(T owner) { }   // duruma girerken bir kez
    public abstract void Execute(T owner);   // durumdayken her karede
    public virtual void Exit(T owner) { }    // durumdan çıkarken bir kez
}
