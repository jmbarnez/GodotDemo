using Godot;
using System;

public partial class StarfieldGenerator : Node2D
{
    [Export] public int StarCount = 400;
    [Export(PropertyHint.Range, "0.001,0.1,0.001")] public float DistantParallaxFactor = 0.02f;
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

        GenerateFarLayer();
        _lastCameraPos = _camera?.GlobalPosition ?? Vector2.Zero;
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
            // Generate stars within a large area that covers the visible space plus margin
            float zoom = _camera?.Zoom.X ?? 1.0f;
            Vector2 viewportSize = GetViewportRect().Size;
            Vector2 visibleSize = viewportSize / zoom;

            _farLayer.Stars[starIndex] = new Vector2(
                (float)random.NextDouble() * visibleSize.X * 4 - visibleSize.X * 2,
                (float)random.NextDouble() * visibleSize.Y * 4 - visibleSize.Y * 2
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

        Vector2 cameraPos = _camera?.GlobalPosition ?? Vector2.Zero;

        // Draw black background that covers the entire visible area
        // Use a much larger area to ensure full coverage when stretched
        float zoom = _camera?.Zoom.X ?? 1.0f;
        Vector2 viewportSize = GetViewportRect().Size;
        Vector2 visibleSize = viewportSize / zoom;

        // Make background large enough to cover the entire visible area plus margin
        float margin = visibleSize.Length() * 2.0f;
        Vector2 bgSize = visibleSize + new Vector2(margin, margin);
        Vector2 bgPos = cameraPos - bgSize * 0.5f;
        DrawRect(new Rect2(bgPos, bgSize), Colors.Black);

        // Draw stars for each layer
        Vector2 parallaxOffset = cameraPos * _farLayer.ParallaxSpeed;

        for (int i = 0; i < _farLayer.Stars.Length; i++)
        {
            Vector2 starPos = _farLayer.Stars[i] + parallaxOffset;

            // Wrap stars around the visible area for infinite scrolling effect
            Vector2 wrappedPos = new Vector2(
                Wrap(starPos.X, cameraPos.X - visibleSize.X * 1.5f, cameraPos.X + visibleSize.X * 1.5f),
                Wrap(starPos.Y, cameraPos.Y - visibleSize.Y * 1.5f, cameraPos.Y + visibleSize.Y * 1.5f)
            );

            // Draw star
            DrawCircle(wrappedPos, _farLayer.StarSizes[i], _farLayer.StarColors[i]);
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

        // Only redraw when camera moves significantly to improve performance
        if ((cameraPos - _lastCameraPos).Length() > 10.0f)
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
