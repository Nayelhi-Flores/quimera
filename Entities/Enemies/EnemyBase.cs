using Godot;

public partial class EnemyBase : CharacterBody2D
{
    [ExportGroup("Movimiento")]
    [Export] public float Speed { get; set; } = 80.0f;
    [Export] public float PatrolDistance { get; set; } = 150.0f;

    [ExportGroup("Referencias")]
    [Export] public Sprite2D DisplaySprite { get; set; }
    [Export] public HealthComponent Health { get; set; }
    [Export] public HurtboxComponent Hurtbox { get; set; }
    [Export] public HitboxComponent Hitbox { get; set; }
    [Export] public RayCast2D FloorDetector { get; set; }

    public float Gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

    private Vector2 _startPosition;
    private int _direction = 1;

    public override void _Ready()
    {
        _startPosition = GlobalPosition;

        if (Health != null)
        {
            Health.HealthChanged += OnHealthChanged;
            Health.Died += OnDied;
        }
    }

    public override void _ExitTree()
    {
        if (Health != null)
        {
            Health.HealthChanged -= OnHealthChanged;
            Health.Died -= OnDied;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 velocity = Velocity;

        // Aplicar gravedad
        if (!IsOnFloor())
        {
            velocity.Y += Gravity * (float)delta;
        }

        // 1. Límite de distancia de patrulla desde la posición inicial
        float currentOffset = GlobalPosition.X - _startPosition.X;
        if (_direction > 0 && currentOffset >= PatrolDistance)
        {
            TurnAround();
        }
        else if (_direction < 0 && currentOffset <= -PatrolDistance)
        {
            TurnAround();
        }

        // 2. Giro si detecta una pared
        if (IsOnWall())
        {
            TurnAround();
        }

        // 3. Giro si el RayCast detecta que NO hay suelo enfrente (solo si está tocando el piso)
        if (IsOnFloor() && FloorDetector != null && !FloorDetector.IsColliding())
        {
            TurnAround();
        }

        velocity.X = _direction * Speed;

        if (DisplaySprite != null)
        {
            DisplaySprite.FlipH = _direction < 0;
        }

        Velocity = velocity;
        MoveAndSlide();
    }

    private void TurnAround()
    {
        _direction *= -1;

        // Invertir la posición X del RayCast2D para que siempre apunte hacia el frente según la dirección
        if (FloorDetector != null)
        {
            FloorDetector.Position = new Vector2(Mathf.Abs(FloorDetector.Position.X) * _direction, FloorDetector.Position.Y);
        }
    }

    private void OnHealthChanged(int currentHealth, int maxHealth)
    {
        GD.Print($"[DEBUG ENEMIGO] {Name} recibió daño. Vida: {currentHealth}/{maxHealth}");
        if (DisplaySprite != null)
        {
            DisplaySprite.Modulate = Colors.Red;
            GetTree().CreateTimer(0.15f).Timeout += () => DisplaySprite.Modulate = Colors.White;
        }
    }

    private void OnDied()
    {
        GD.Print($"[DEBUG ENEMIGO] {Name} ha sido derrotado.");
        QueueFree();
    }
}