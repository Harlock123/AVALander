using System;
using System.Collections.Generic;
using Avalonia;

namespace AVALander.Models;

public class Explosion
{
    private readonly List<Particle> _particles = new();
    private readonly List<(Point start, Point end)> _debrisLines = new();
    private double _time;
    private const double Duration = 2.0;

    public Point Position { get; }
    public bool IsFinished => _time >= Duration;

    private static readonly Random Random = new();

    public Explosion(Point position, int particleCount = 30)
    {
        Position = position;

        // Create explosion particles
        for (int i = 0; i < particleCount; i++)
        {
            _particles.Add(Particle.CreateExplosionParticle(position));
        }

        // Create debris lines (pieces of the lander)
        for (int i = 0; i < 8; i++)
        {
            double angle = Random.NextDouble() * Math.PI * 2;
            double length = 5 + Random.NextDouble() * 15;
            double speed = 40 + Random.NextDouble() * 80;

            var direction = new Vector(Math.Cos(angle), Math.Sin(angle));
            var start = new Point(
                position.X + direction.X * 5,
                position.Y + direction.Y * 5
            );
            var end = new Point(
                start.X + direction.X * length,
                start.Y + direction.Y * length
            );

            _debrisLines.Add((start, end));
        }
    }

    public void Update(double deltaTime)
    {
        _time += deltaTime;

        foreach (var particle in _particles)
        {
            particle.Update(deltaTime);
        }

        _particles.RemoveAll(p => !p.IsAlive);

        // Update debris lines (expand outward and fall)
        for (int i = 0; i < _debrisLines.Count; i++)
        {
            var (start, end) = _debrisLines[i];

            // Calculate center and move it
            var center = new Point((start.X + end.X) / 2, (start.Y + end.Y) / 2);
            var direction = new Vector(center.X - Position.X, center.Y - Position.Y);
            double dist = Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y);
            if (dist > 0)
            {
                direction = new Vector(direction.X / dist, direction.Y / dist);
            }

            double moveSpeed = 30 * deltaTime;
            double gravity = 50 * deltaTime * _time;

            var offset = new Vector(
                direction.X * moveSpeed,
                direction.Y * moveSpeed + gravity
            );

            _debrisLines[i] = (
                new Point(start.X + offset.X, start.Y + offset.Y),
                new Point(end.X + offset.X, end.Y + offset.Y)
            );
        }
    }

    public double GetOpacity()
    {
        return Math.Max(0, 1.0 - _time / Duration);
    }

    public IReadOnlyList<Particle> GetParticles() => _particles;

    public IReadOnlyList<(Point start, Point end)> GetDebrisLines() => _debrisLines;
}
