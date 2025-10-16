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

        float zoom = _camera?.Zoom.X ?? 1.0f;
        Vector2 viewportSize = GetViewportRect().Size;
        Vector2 visibleSize = viewportSize / zoom;

        // Draw black background around the camera origin (node follows the camera)
        float margin = visibleSize.Length();
        Vector2 bgSize = visibleSize + new Vector2(margin, margin);
        Vector2 bgPos = -bgSize * 0.5f;
        DrawRect(new Rect2(bgPos, bgSize), Colors.Black);

        // Parallax offset is based on how far the camera has travelled from the origin
        Vector2 cameraDelta = (_camera?.GlobalPosition ?? Vector2.Zero) - _parallaxOrigin;
        float parallaxFactor = Mathf.Clamp(_farLayer.ParallaxSpeed, 0.0f, 1.0f);
        Vector2 parallaxOffset = cameraDelta * parallaxFactor;

        // Wrap bounds in local space (centered on the camera position)
        float wrapXMin = -visibleSize.X * 1.5f;
        float wrapXMax = visibleSize.X * 1.5f;
        float wrapYMin = -visibleSize.Y * 1.5f;
        float wrapYMax = visibleSize.Y * 1.5f;

        for (int i = 0; i < _farLayer.Stars.Length; i++)
        {
            Vector2 starPos = _farLayer.Stars[i] - parallaxOffset;

            Vector2 wrappedPos = new Vector2(
                Wrap(starPos.X, wrapXMin, wrapXMax),
                Wrap(starPos.Y, wrapYMin, wrapYMax)
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
        GlobalPosition = cameraPos;

        // Always redraw when the camera moves to keep parallax smooth
        if ((cameraPos - _lastCameraPos).LengthSquared() > 0.01f)
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
