using Godot;

public partial class EventBus : Node
{
    public static EventBus Instance { get; private set; }

    [Signal] public delegate void PlayerHealthChangedEventHandler(int currentHealth, int maxHealth);
    [Signal] public delegate void PlayerDiedEventHandler();
    [Signal] public delegate void CameraShakeRequestedEventHandler(float intensity);

    public override void _Ready()
    {
        Instance = this;
    }
}