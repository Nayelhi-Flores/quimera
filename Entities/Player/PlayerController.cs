using Godot;

public partial class PlayerController : CharacterBody2D
{
    [ExportGroup("Movimiento Base")]
    [Export] public float Speed { get; set; } = 250.0f;
    [Export] public float JumpVelocity { get; set; } = -400.0f;

    [ExportGroup("Daño")]
    [Export] public float KnockbackForce { get; set; } = 300.0f;
    [Export] public float InvulnerabilityTime { get; set; } = 0.8f;

    [ExportGroup("Ataque")]
    [Export] public Area2D AttackHitbox { get; private set; }
    [Export] public CollisionShape2D AttackCollision { get; private set; }
    [Export] public float AttackOffset { get; set; } = 40.0f;

    private bool _isAttacking = false;

    [ExportGroup("Referencias")]
    [Export] public Sprite2D DisplaySprite { get; private set; }
    [Export] public HealthComponent Health { get; private set; }
    [Export] public AnimationPlayer AnimPlayer { get; private set; }
    [Export] public AnimationTree AnimTree { get; private set; }

    public float Gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();

    private bool _isInvulnerable = false;
    private float _invulnerabilityTimer = 0.0f;
    private float _knockbackVelocityX = 0.0f;

    public override void _Ready()
    {
        if (Health != null)
        {
            // Suscribirse a las señales de vida para responder al daño
            Health.HealthChanged += OnHealthChanged;
            Health.Died += OnDied;

            if (EventBus.Instance != null)
            {
                EventBus.Instance.EmitSignal(
                    EventBus.SignalName.PlayerHealthChanged, 
                    Health.CurrentHealth, 
                    Health.MaxHealth
                );
            }
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

        // Manejar tiempo de invulnerabilidad y parpadeo
        if (_isInvulnerable)
        {
            _invulnerabilityTimer -= (float)delta;
            DisplaySprite.Modulate = new Color(1, 1, 1, Mathf.PingPong((float)Time.GetTicksMsec() / 100.0f, 1.0f));

            if (_invulnerabilityTimer <= 0.0f)
            {
                _isInvulnerable = false;
                DisplaySprite.Modulate = new Color(1, 1, 1, 1); // Restablecer opacidad
            }
        }

        // Gravedad
        if (!IsOnFloor())
        {
            velocity.Y += Gravity * (float)delta;
        }

        // Salto
        if (Input.IsActionJustPressed("jump") && IsOnFloor())
        {
            velocity.Y = JumpVelocity;
        }

        // Movimiento horizontal + Desaceleración del Knockback
        float direction = Input.GetAxis("move_left", "move_right");
        _knockbackVelocityX = Mathf.MoveToward(_knockbackVelocityX, 0, Speed * 2.0f * (float)delta);

        if (direction != 0)
        {
            velocity.X = (direction * Speed) + _knockbackVelocityX;
            
            if (DisplaySprite != null)
            {
                DisplaySprite.FlipH = direction < 0;
            }
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed) + _knockbackVelocityX;
        }

        Velocity = velocity;
        MoveAndSlide();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("attack") && !_isAttacking)
        {
            Attack();
        }
    }

    private async void Attack()
    {
        _isAttacking = true;

        // Colocar el área de ataque hacia la derecha (+X) o izquierda (-X) según el Flip del Sprite
        if (AttackHitbox != null)
        {
            float direction = DisplaySprite.FlipH ? -1.0f : 1.0f;
            
            // Mueve la posición X del AttackHitbox según la dirección del personaje
            AttackHitbox.Position = new Vector2(Mathf.Abs(AttackOffset) * direction, AttackHitbox.Position.Y);
        }

        // Activar colisión
        if (AttackCollision != null)
        {
            AttackCollision.Disabled = false;
        }

        // Forzar comprobación de solapamiento instantánea
        if (AttackHitbox != null)
        {
            var overlappingAreas = AttackHitbox.GetOverlappingAreas();
            foreach (var area in overlappingAreas)
            {
                if (area is HurtboxComponent hurtbox)
                {
                    hurtbox.CheckAndTakeDamage(AttackHitbox as HitboxComponent);
                }
            }
        }

        // Duración del ataque activo
        await ToSignal(GetTree().CreateTimer(0.15f), SceneTreeTimer.SignalName.Timeout);

        if (AttackCollision != null)
        {
            AttackCollision.Disabled = true;
        }

        // Cooldown antes de poder volver a atacar
        await ToSignal(GetTree().CreateTimer(0.10f), SceneTreeTimer.SignalName.Timeout);
        _isAttacking = false;
    }

    private void OnHealthChanged(int currentHealth, int maxHealth)
    {
        if (_isInvulnerable) return;

        GD.Print($"[DEBUG PLAYER] Vida restante: {currentHealth} / {maxHealth}");

        // Aplicar Invulnerabilidad
        _isInvulnerable = true;
        _invulnerabilityTimer = InvulnerabilityTime;

        // Empujón hacia atrás según la orientación del sprite
        float knockbackDir = DisplaySprite.FlipH ? 1.0f : -1.0f;
        _knockbackVelocityX = knockbackDir * KnockbackForce;
        Velocity = new Vector2(Velocity.X, -150.0f); // Pequeño salto al recibir impacto
    }

    private void OnDied()
    {
        GD.Print("[DEBUG PLAYER] El jugador ha muerto.");
    }
}