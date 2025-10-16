using Godot;
using System;

public partial class StarfieldGenerator : Node2D
{
    [Export] public int StarCount = 400;
    [Export(PropertyHint.Range, "0.0,0.1,0.001")] public float DistantParallaxFactor = 0.0f;
    [Export(PropertyHint.Range, "1.0,4.0,0.1")] public float DistantStarSize = 2.5f;
    [Export(PropertyHint.Range, "0.0,1.0,0.01")] public float DistantStarSizeJitter = 0.2f;
    [Export(PropertyHint.Range, "0.0,0.5,0.01")] public float DistantStarBrightnessJitter = 0.15f;
    [Export] public Color DistantStarColor = Colors.White;
    [Export] public int BackgroundZIndex = -10000;
    [Export] public bool UseAbsoluteZIndex = true;

    private class StarLayer
    {
        public Vector2[] Stars = Array.Empty<Vector2>();
        public Color[] StarColors = Array.Empty<Color>();
        public float[] StarSizes = Array.Empty<float>();
        public float ParallaxSpeed;
    }

    private StarLayer _farLayer;
    private CameraController _camera;
    private Vector2 _lastCameraPos;
    private Vector2 _parallaxOrigin;

    public override void _Ready()
    {
        if (UseAbsoluteZIndex)
        {
            ZAsRelative = false;
            ZIndex = BackgroundZIndex;
        }

        // Try to get camera reference - make it more robust
        _camera = GetNodeOrNull<CameraController>("../Camera2D");
        if (_camera == null)
        {
            // Fallback: search for camera in the scene
            var cameras = GetTree().GetNodesInGroup("camera");
            if (cameras.Count > 0 && cameras[0] is CameraController cam)
            {
                _camera = cam;
            }
        }

        _parallaxOrigin = _camera?.GlobalPosition ?? Vector2.Zero;
        GlobalPosition = _parallaxOrigin;

        GenerateFarLayer();
        _lastCameraPos = _parallaxOrigin;
        QueueRedraw();
    }

    private void GenerateFarLayer()
    {
        _farLayer = new StarLayer
        {
            Stars = new Vector2[StarCount],
            StarColors = new Color[StarCount],
            StarSizes = new float[StarCount],
            ParallaxSpeed = DistantParallaxFactor
        };

        Random random = new Random(1);

        for (int starIndex = 0; starIndex < StarCount; starIndex++)
        {
            // Generate stars within a very large area for distant static stars
            float zoom = _camera?.Zoom.X ?? 1.0f;
            Vector2 viewportSize = GetViewportRect().Size;
            Vector2 visibleSize = viewportSize / zoom;

            // Create a much larger area for stars to avoid wrapping
            float starFieldSize = Mathf.Max(visibleSize.X, visibleSize.Y) * 8;
            _farLayer.Stars[starIndex] = new Vector2(
                (float)random.NextDouble() * starFieldSize - starFieldSize * 0.5f,
                (float)random.NextDouble() * starFieldSize - starFieldSize * 0.5f
            );

            float sizeVariation = Mathf.Clamp(DistantStarSizeJitter, 0.0f, 1.0f) * DistantStarSize;
            float sizeMin = Mathf.Max(1.0f, DistantStarSize - sizeVariation);
            float sizeMax = DistantStarSize + sizeVariation;
            _farLayer.StarSizes[starIndex] = Mathf.Lerp(sizeMin, sizeMax, (float)random.NextDouble());

            float brightnessVariation = Mathf.Clamp(DistantStarBrightnessJitter, 0.0f, 1.0f);
            float brightnessMin = Mathf.Max(0.0f, 1.0f - brightnessVariation);
            float brightnessMax = 1.0f + brightnessVariation;
            float brightness = Mathf.Lerp(brightnessMin, brightnessMax, (float)random.NextDouble());
            _farLayer.StarColors[starIndex] = DistantStarColor * brightness;
        }
    }

    public override void _Draw()
    {
        if (_farLayer?.Stars == null || _farLayer.Stars.Length == 0) return;

        float zoom = _camera?.Zoom.X ?? 1.0f;
        Vector2 viewportSize = GetViewportRect().Size;
        Vector2 visibleSize = viewportSize / zoom;

        // Draw black background covering the entire visible area
        Vector2 bgSize = visibleSize * 2; // Make it larger to ensure full coverage
        Vector2 bgPos = -bgSize * 0.5f;
        DrawRect(new Rect2(bgPos, bgSize), Colors.Black);

        // Since parallax factor is 0, stars are completely static
        // No need for complex wrapping or parallax calculations
        for (int i = 0; i < _farLayer.Stars.Length; i++)
        {
            Vector2 starPos = _farLayer.Stars[i];
            
            // Only draw stars that are within the visible area
            if (Mathf.Abs(starPos.X) <= visibleSize.X * 0.6f && 
                Mathf.Abs(starPos.Y) <= visibleSize.Y * 0.6f)
            {
                DrawCircle(starPos, _farLayer.StarSizes[i], _farLayer.StarColors[i]);
            }
        }
    }

    private float Wrap(float value, float min, float max)
    {
        float range = max - min;
        while (value < min) value += range;
        while (value > max) value -= range;
        return value;
    }

    public override void _Process(double delta)
    {
        if (_camera == null) return;

        Vector2 cameraPos = _camera.GlobalPosition;
        GlobalPosition = cameraPos;

        // Since stars are static, we only need to redraw when the camera moves significantly
        // to update which stars are visible
        if ((cameraPos - _lastCameraPos).LengthSquared() > 100.0f) // Reduced frequency
        {
            QueueRedraw();
            _lastCameraPos = cameraPos;
        }
    }

    public void UpdateParallax()
    {
        QueueRedraw();
    }
}
