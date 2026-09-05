using Godot;

public partial class HitboxComponent : Area2D
{
    [Export] public int Damage { get; set; } = 10;

    public override void _Ready()
    {
        // Por defecto en colisiones, deshabilitamos la máscara para no colisionar consigo mismo
        // La detección la gestionará el Hurtbox.
    }
}