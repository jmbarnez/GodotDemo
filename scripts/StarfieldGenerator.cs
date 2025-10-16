using Godot;
using System;
using System.Collections.Generic;

public partial class StarfieldGenerator : Node2D
{
    [Export] public int NumberOfLayers = 3;
    [Export] public int StarsPerLayer = 150;
    [Export] public float BaseParallaxSpeed = 0.3f;
    [Export] public float LayerSpeedMultiplier = 0.5f;
    [Export] public int BackgroundZIndex = -10000;
    [Export] public bool UseAbsoluteZIndex = true;

    private class StarLayer
    {
        public Vector2[] Stars;
        public float ParallaxSpeed;
        public Color StarColor;
        public float StarSize;
        public Vector2 Offset;
    }

    private StarLayer[] _layers;
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

        GenerateStarLayers();
        _lastCameraPos = _camera?.GlobalPosition ?? Vector2.Zero;
    }

    private void GenerateStarLayers()
    {
        _layers = new StarLayer[NumberOfLayers];

        for (int layerIndex = 0; layerIndex < NumberOfLayers; layerIndex++)
        {
            var layer = new StarLayer();
            layer.Stars = new Vector2[StarsPerLayer];
            layer.ParallaxSpeed = BaseParallaxSpeed * (layerIndex + 1) * LayerSpeedMultiplier;
            layer.Offset = Vector2.Zero;

            // Vary color and size by layer (farther layers are dimmer and smaller)
            float layerBrightness = 1.0f - (layerIndex * 0.3f);
            layer.StarColor = new Color(layerBrightness, layerBrightness, layerBrightness, 1.0f);
            layer.StarSize = Mathf.Max(1.0f, 3.0f - (layerIndex * 1.0f)); // Crisp pixel sizes

            Random random = new Random(layerIndex); // Different seed per layer

            for (int starIndex = 0; starIndex < StarsPerLayer; starIndex++)
            {
                // Generate stars within a large area that covers the visible space plus margin
                float zoom = _camera?.Zoom.X ?? 1.0f;
                Vector2 viewportSize = GetViewportRect().Size;
                Vector2 visibleSize = viewportSize / zoom;

                layer.Stars[starIndex] = new Vector2(
                    (float)random.NextDouble() * visibleSize.X * 4 - visibleSize.X * 2,
                    (float)random.NextDouble() * visibleSize.Y * 4 - visibleSize.Y * 2
                );
            }

            _layers[layerIndex] = layer;
        }
    }

    public override void _Draw()
    {
        if (_layers == null) return;

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
        foreach (var layer in _layers)
        {
            Vector2 parallaxOffset = cameraPos * layer.ParallaxSpeed;

            for (int i = 0; i < layer.Stars.Length; i++)
            {
                Vector2 starPos = layer.Stars[i] + parallaxOffset;

                // Wrap stars around the visible area for infinite scrolling effect
                Vector2 wrappedPos = new Vector2(
                    Wrap(starPos.X, cameraPos.X - visibleSize.X * 1.5f, cameraPos.X + visibleSize.X * 1.5f),
                    Wrap(starPos.Y, cameraPos.Y - visibleSize.Y * 1.5f, cameraPos.Y + visibleSize.Y * 1.5f)
                );

                // Draw star
                DrawCircle(wrappedPos, layer.StarSize, layer.StarColor);
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
