using Godot;

public partial class HurtboxComponent : Area2D
{
    [Export] private HealthComponent _healthComponent;

    public override void _Ready()
    {
        AreaEntered += OnAreaEntered;
    }

    public override void _ExitTree()
    {
        AreaEntered -= OnAreaEntered;
    }

    private void OnAreaEntered(Area2D area)
    {
        CheckAndTakeDamage(area);
    }

    public void CheckAndTakeDamage(Area2D area)
    {
        if (area is HitboxComponent hitbox)
        {
            GD.Print($"[HURTBOX] {GetParent().Name} recibió {hitbox.Damage} de daño desde {hitbox.Name}.");
            _healthComponent?.TakeDamage(hitbox.Damage);
            EventBus.Instance?.EmitSignal(EventBus.SignalName.CameraShakeRequested, 8.0f);
        }
    }
}