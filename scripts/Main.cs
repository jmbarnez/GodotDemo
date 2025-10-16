using Godot;

public partial class Main : Node2D
{
	private Node2D _player;
	private StarfieldGenerator _starfieldGenerator;
	private CameraController _camera;

	[Export] public Vector2 WorldBounds = new Vector2(5000, 3750); // World boundaries

	public override void _Ready()
	{
		// Set to fullscreen mode for native desktop resolution
		DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);

		// Get and log the native screen resolution
		Vector2I screenSize = DisplayServer.ScreenGetSize();
		GD.Print($"Native screen resolution: {screenSize.X}x{screenSize.Y}");

		_player = GetNode<Node2D>("Player");
		_starfieldGenerator = GetNode<StarfieldGenerator>("StarfieldGenerator");
		_camera = GetNode<CameraController>("Camera2D");

		// Center the player on the screen
		Vector2 viewportSize = GetViewportRect().Size;
		_player.GlobalPosition = viewportSize / 2;
	}

	public override void _PhysicsProcess(double delta)
	{
		// Make camera follow the player smoothly
		if (_camera != null)
		{
			_camera.GlobalPosition = _player.GlobalPosition;
		}

		// Implement world boundaries - clamp player position
		if (_player != null)
		{
			Vector2 halfBounds = WorldBounds / 2;
			_player.GlobalPosition = new Vector2(
				Mathf.Clamp(_player.GlobalPosition.X, -halfBounds.X, halfBounds.X),
				Mathf.Clamp(_player.GlobalPosition.Y, -halfBounds.Y, halfBounds.Y)
			);
		}

		// Starfield parallax is now handled automatically
	}
}
