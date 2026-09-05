using Godot;

public partial class GameCamera : Camera2D
{
    [ExportGroup("Seguimiento")]
    [Export] private Node2D _target;
    [Export] private float _followSpeed = 5.0f;

    [ExportGroup("Camera Shake")]
    [Export] private float _shakeDecay = 5.0f;

    private float _shakeIntensity = 0.0f;
    private float _lockedY;
    private bool _isYLocked = false;
    private readonly RandomNumberGenerator _rng = new();

    public override void _Ready()
    {
        _rng.Randomize();

        if (EventBus.Instance != null)
        {
            EventBus.Instance.CameraShakeRequested += OnCameraShakeRequested;
        }

        // Posicionar inmediatamente sobre el objetivo al iniciar
        if (_target != null)
        {
            GlobalPosition = _target.GlobalPosition;
            _lockedY = _target.GlobalPosition.Y;
            _isYLocked = true;
        }
    }

    public override void _ExitTree()
    {
        if (EventBus.Instance != null)
        {
            EventBus.Instance.CameraShakeRequested -= OnCameraShakeRequested;
        }
    }

    public override void _Process(double delta)
    {
        if (_target == null) return;

        // Comprobar si el objetivo salió de los márgenes verticales de la pantalla
        CheckTargetOutOfBounds();

        // Calcular la posición objetivo
        float targetY = _isYLocked ? _lockedY : _target.GlobalPosition.Y;
        Vector2 desiredPosition = new Vector2(_target.GlobalPosition.X, targetY);

        // Interpolación suave
        GlobalPosition = GlobalPosition.Lerp(desiredPosition, (float)delta * _followSpeed);

        // Si la cámara ya alcanzó la Y del objetivo tras reajustarse, volver a bloquear Y
        if (!_isYLocked && Mathf.Abs(GlobalPosition.Y - _target.GlobalPosition.Y) < 5.0f)
        {
            _lockedY = _target.GlobalPosition.Y;
            _isYLocked = true;
        }

        // Aplicar vibración (Shake)
        if (_shakeIntensity > 0)
        {
            _shakeIntensity = Mathf.Max(0, _shakeIntensity - (float)delta * _shakeDecay);
            Offset = new Vector2(
                _rng.RandfRange(-_shakeIntensity, _shakeIntensity),
                _rng.RandfRange(-_shakeIntensity, _shakeIntensity)
            );
        }
    }

    private void CheckTargetOutOfBounds()
    {
        // Obtener el alto visible del Viewport en coordenadas del mundo
        float halfScreenHeight = GetViewportRect().Size.Y / (2.0f * Zoom.Y);
        float topEdge = GlobalPosition.Y - halfScreenHeight;
        float bottomEdge = GlobalPosition.Y + halfScreenHeight;

        // Si el objetivo sale del borde superior o inferior, desbloquear Y para reajustar la vista
        if (_target.GlobalPosition.Y < topEdge || _target.GlobalPosition.Y > bottomEdge)
        {
            _isYLocked = false;
        }
    }

    private void OnCameraShakeRequested(float intensity)
    {
        _shakeIntensity = intensity;
    }
}