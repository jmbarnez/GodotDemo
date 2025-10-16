using Godot;
using System.Collections.Generic;
using System;

public partial class EngineTrail : Node2D
{
    [Export] public int MaxParticles = 100;
    [Export] public float ParticleLifetime = 2.0f;
    [Export] public float ParticleSpeed = 200.0f;
    [Export] public float ParticleSize = 2.0f;
    [Export] public Color ParticleColor = Colors.Orange;
    [Export] public float EmissionRate = 30.0f; // particles per second
    [Export] public float MinVelocityThreshold = 10.0f; // minimum velocity to emit particles
    
    private struct Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Lifetime;
        public float MaxLifetime;
        public float Size;
        public Color Color;
    }
    
    private List<Particle> _particles = new List<Particle>();
    private RigidBody2D _player;
    private float _emissionTimer = 0.0f;
    private Random _random = new Random();
    
    public override void _Ready()
    {
        _player = GetParent<RigidBody2D>();
        if (_player == null)
        {
            GD.PrintErr("EngineTrail: Parent must be a RigidBody2D");
            return;
        }
    }
    
    public override void _Process(double delta)
    {
        if (_player == null) return;
        
        float deltaTime = (float)delta;
        
        // Update existing particles
        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var particle = _particles[i];
            particle.Lifetime -= deltaTime;
            
            if (particle.Lifetime <= 0)
            {
                _particles.RemoveAt(i);
                continue;
            }
            
            // Update particle position
            particle.Position += particle.Velocity * deltaTime;
            
            // Fade out over time
            float alpha = particle.Lifetime / particle.MaxLifetime;
            particle.Color = ParticleColor;
            particle.Color.A = alpha;
            
            _particles[i] = particle;
        }
        
        // Emit new particles based on player movement
        Vector2 playerVelocity = _player.LinearVelocity;
        float velocityMagnitude = playerVelocity.Length();
        
        if (velocityMagnitude > MinVelocityThreshold)
        {
            _emissionTimer += deltaTime;
            float emissionInterval = 1.0f / EmissionRate;
            
            while (_emissionTimer >= emissionInterval && _particles.Count < MaxParticles)
            {
                EmitParticle(playerVelocity);
                _emissionTimer -= emissionInterval;
            }
        }
        else
        {
            _emissionTimer = 0.0f;
        }
        
        QueueRedraw();
    }
    
    private void EmitParticle(Vector2 playerVelocity)
    {
        // Calculate the opposite direction of movement
        Vector2 oppositeDirection = -playerVelocity.Normalized();
        
        // Add some randomness to the direction
        float angleVariation = (float)(_random.NextDouble() - 0.5) * 0.5f; // ±0.25 radians
        oppositeDirection = oppositeDirection.Rotated(angleVariation);
        
        // Calculate particle position (behind the player)
        Vector2 playerPos = _player.GlobalPosition;
        Vector2 offset = oppositeDirection * 20.0f; // Start particles behind the ship
        Vector2 particlePos = playerPos + offset;
        
        // Calculate particle velocity (opposite to player movement with some randomness)
        float speedVariation = (float)(_random.NextDouble() * 0.5 + 0.75); // 0.75 to 1.25 multiplier
        Vector2 particleVelocity = oppositeDirection * ParticleSpeed * speedVariation;
        
        // Add some random drift
        Vector2 drift = new Vector2(
            (float)(_random.NextDouble() - 0.5) * 50.0f,
            (float)(_random.NextDouble() - 0.5) * 50.0f
        );
        particleVelocity += drift;
        
        // Create particle
        var particle = new Particle
        {
            Position = particlePos,
            Velocity = particleVelocity,
            Lifetime = ParticleLifetime,
            MaxLifetime = ParticleLifetime,
            Size = ParticleSize * (0.8f + (float)_random.NextDouble() * 0.4f), // Size variation
            Color = ParticleColor
        };
        
        _particles.Add(particle);
    }
    
    public override void _Draw()
    {
        foreach (var particle in _particles)
        {
            // Convert world position to local position
            Vector2 localPos = ToLocal(particle.Position);
            DrawCircle(localPos, particle.Size, particle.Color);
        }
    }
}
