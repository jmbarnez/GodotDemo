using Godot;

public partial class CameraController : Camera2D
{
    [Export] public float MinZoom = 0.5f;
    [Export] public float MaxZoom = 3.0f;
    [Export] public float ZoomFactor = 0.1f;
    [Export] public float ZoomDuration = 0.2f;

    private Tween _tween;
    private Vector2 _targetZoom = Vector2.One;
    private StarfieldGenerator _starfieldGenerator;

    public override void _Ready()
    {
        _tween = CreateTween();
        _tween.SetTrans(Tween.TransitionType.Sine);
        _tween.SetEase(Tween.EaseType.Out);

        // Get reference to starfield generator for parallax updates
        _starfieldGenerator = GetNode<StarfieldGenerator>("../StarfieldGenerator");

        // Make camera follow the player
        // We'll implement this in Main.cs instead to keep camera logic centralized
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            // Invert zoom controls: wheel up zooms out, wheel down zooms in
            if (mouseEvent.ButtonIndex == MouseButton.WheelUp && mouseEvent.Pressed)
            {
                ZoomOut(); // Wheel up = zoom out
            }
            else if (mouseEvent.ButtonIndex == MouseButton.WheelDown && mouseEvent.Pressed)
            {
                ZoomIn(); // Wheel down = zoom in
            }
        }
    }

    private void ZoomIn()
    {
        SetZoomLevel(Zoom.X - ZoomFactor);
    }

    private void ZoomOut()
    {
        SetZoomLevel(Zoom.X + ZoomFactor);
    }

    private void SetZoomLevel(float zoomLevel)
    {
        // Clamp zoom level within limits
        zoomLevel = Mathf.Clamp(zoomLevel, MinZoom, MaxZoom);
        _targetZoom = new Vector2(zoomLevel, zoomLevel);

        // Kill any existing tween
        if (_tween != null && _tween.IsRunning())
        {
            _tween.Kill();
        }

        // Create smooth zoom tween
        _tween = CreateTween();
        _tween.SetTrans(Tween.TransitionType.Sine);
        _tween.SetEase(Tween.EaseType.Out);
        _tween.TweenProperty(this, "zoom", _targetZoom, ZoomDuration);

        // Update parallax when zoom changes
        if (_starfieldGenerator != null)
        {
            _starfieldGenerator.UpdateParallax();
        }
    }

    // Method to set zoom instantly (useful for initialization)
    public void SetZoomInstant(float zoomLevel)
    {
        zoomLevel = Mathf.Clamp(zoomLevel, MinZoom, MaxZoom);
        Zoom = new Vector2(zoomLevel, zoomLevel);
        _targetZoom = Zoom;

        // Update parallax when zoom changes
        if (_starfieldGenerator != null)
        {
            _starfieldGenerator.UpdateParallax();
        }
    }
}
