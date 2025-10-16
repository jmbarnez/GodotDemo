using Godot;

public partial class Player : RigidBody2D
{
    [Export]
    public float Thrust { get; set; } = 500.0f;
    [Export]
    public float MaxSpeed { get; set; } = 400.0f;
    [Export]
    public float RotationSpeed { get; set; } = 4.0f;
    [Export]
    public float LinearDamping { get; set; } = 0.1f; // Space friction
    [Export]
    public float AngularDamping { get; set; } = 1.0f; // Rotation damping

    private Node2D _turret;

    public override void _Ready()
    {
        _turret = GetNode<Node2D>("Turret");

        // Set damping for space physics
        this.LinearDamp = LinearDamping;
        this.AngularDamp = AngularDamping;

        // Disable gravity for space physics
        this.GravityScale = 0.0f;
    }

    public override void _PhysicsProcess(double delta)
    {
        // Turret follows mouse cursor
        if (_turret != null)
        {
            Vector2 mousePosition = GetGlobalMousePosition();
            _turret.LookAt(mousePosition);
            // Adjust rotation since turret points upward by default (LookAt assumes pointing right)
            _turret.Rotation += Mathf.Pi / 2;
        }

        // Space physics movement - relative to ship orientation
        Vector2 thrustDirection = Vector2.Zero;

        // Forward/Backward movement relative to ship facing (W/S keys)
        if (Input.IsKeyPressed(Key.W) || Input.IsActionPressed("up"))
            thrustDirection.Y -= 1; // Forward (up relative to ship)
        if (Input.IsKeyPressed(Key.S) || Input.IsActionPressed("down"))
            thrustDirection.Y += 1; // Backward (down relative to ship)

        // Strafing left/right relative to ship facing (A/D keys)
        if (Input.IsKeyPressed(Key.A) || Input.IsActionPressed("left"))
            thrustDirection.X -= 1; // Left strafe
        if (Input.IsKeyPressed(Key.D) || Input.IsActionPressed("right"))
            thrustDirection.X += 1; // Right strafe

        // Debug: Check input detection
        if (thrustDirection != Vector2.Zero)
        {
            GD.Print($"Thrust direction: {thrustDirection}, Velocity: {LinearVelocity}");
        }

        // Apply thrust force relative to ship rotation
        if (thrustDirection != Vector2.Zero)
        {
            Vector2 worldThrustDirection = thrustDirection.Rotated(Rotation);
            ApplyCentralForce(worldThrustDirection.Normalized() * Thrust);
        }

        // Limit maximum speed
        if (LinearVelocity.Length() > MaxSpeed)
        {
            LinearVelocity = LinearVelocity.Normalized() * MaxSpeed;
        }
    }
}