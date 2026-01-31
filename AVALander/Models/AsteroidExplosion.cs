using System;
using System.Collections.Generic;
using Avalonia;

namespace AVALander.Models;

public class AsteroidExplosion
{
    private readonly List<Particle> _particles = new();
    private readonly List<EjectaParticle> _ejecta = new();
    private readonly List<(Point start, Point end, Vector velocity)> _debrisLines = new();
    private double _time;
    private const double Duration = 2.5;

    public Point Position { get; }
    public double AsteroidRadius { get; }
    public bool IsFinished => _time >= Duration;

    private static readonly Random Random = new();

    public AsteroidExplosion(Point position, double asteroidRadius, int particleCount = 25)
    {
        Position = position;
        AsteroidRadius = asteroidRadius;

        // Create ground explosion particles (spread horizontally)
        for (int i = 0; i < particleCount; i++)
        {
            _particles.Add(CreateGroundParticle(position));
        }

        // Create ejecta particles that fly upward (dangerous to lander)
        int ejectaCount = 8 + Random.Next(8); // 8-15 ejecta particles
        for (int i = 0; i < ejectaCount; i++)
        {
            _ejecta.Add(CreateEjectaParticle(position, asteroidRadius));
        }

        // Create debris lines (rock fragments flying outward)
        for (int i = 0; i < 6; i++)
        {
            double angle = -Math.PI / 2 + (Random.NextDouble() - 0.5) * Math.PI; // Upward bias
            double length = 3 + Random.NextDouble() * 8;
            double speed = 60 + Random.NextDouble() * 100;

            var direction = new Vector(Math.Cos(angle), Math.Sin(angle));
            var start = new Point(
                position.X + direction.X * 5,
                position.Y + direction.Y * 5
            );
            var end = new Point(
                start.X + direction.X * length,
                start.Y + direction.Y * length
            );

            _debrisLines.Add((start, end, direction * speed));
        }
    }

    private static Particle CreateGroundParticle(Point origin)
    {
        // Particles spread horizontally and slightly upward
        double angle = -Math.PI / 2 + (Random.NextDouble() - 0.5) * Math.PI * 0.8;
        double speed = 30 + Random.NextDouble() * 80;

        return new Particle(
            origin,
            new Vector(Math.Cos(angle) * speed, Math.Sin(angle) * speed),
            0.5 + Random.NextDouble() * 1.0,
            2 + Random.NextDouble() * 4
        );
    }

    private static EjectaParticle CreateEjectaParticle(Point origin, double asteroidRadius)
    {
        // Ejecta flies upward with some horizontal spread
        double angle = -Math.PI / 2 + (Random.NextDouble() - 0.5) * 0.8; // Mostly upward
        double speed = 100 + Random.NextDouble() * 150; // Fast upward

        return new EjectaParticle(
            new Point(origin.X + (Random.NextDouble() - 0.5) * asteroidRadius, origin.Y),
            new Vector(Math.Cos(angle) * speed, Math.Sin(angle) * speed),
            1.5 + Random.NextDouble() * 1.5, // Longer life
            3 + Random.NextDouble() * 4
        );
    }

    public void Update(double deltaTime)
    {
        _time += deltaTime;

        // Update regular particles
        foreach (var particle in _particles)
        {
            particle.Update(deltaTime);
        }
        _particles.RemoveAll(p => !p.IsAlive);

        // Update ejecta particles
        foreach (var ejecta in _ejecta)
        {
            ejecta.Update(deltaTime);
        }
        _ejecta.RemoveAll(e => !e.IsAlive);

        // Update debris lines
        for (int i = 0; i < _debrisLines.Count; i++)
        {
            var (start, end, velocity) = _debrisLines[i];

            // Apply gravity
            var newVelocity = new Vector(velocity.X * 0.99, velocity.Y + 80 * deltaTime);

            var offset = new Vector(
                newVelocity.X * deltaTime,
                newVelocity.Y * deltaTime
            );

            _debrisLines[i] = (
                new Point(start.X + offset.X, start.Y + offset.Y),
                new Point(end.X + offset.X, end.Y + offset.Y),
                newVelocity
            );
        }
    }

    public double GetOpacity()
    {
        return Math.Max(0, 1.0 - _time / Duration);
    }

    public IReadOnlyList<Particle> GetParticles() => _particles;

    public IReadOnlyList<EjectaParticle> GetEjecta() => _ejecta;

    public IReadOnlyList<(Point start, Point end, Vector velocity)> GetDebrisLines() => _debrisLines;

    /// <summary>
    /// Check if any ejecta is within dangerous range of the given position
    /// </summary>
    public bool IsEjectaDangerousTo(Point landerPosition, double landerRadius)
    {
        foreach (var ejecta in _ejecta)
        {
            if (!ejecta.IsAlive || !ejecta.IsDangerous) continue;

            double dx = ejecta.Position.X - landerPosition.X;
            double dy = ejecta.Position.Y - landerPosition.Y;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            if (distance < ejecta.Size + landerRadius)
            {
                return true;
            }
        }
        return false;
    }
}

/// <summary>
/// Special particle type for asteroid ejecta that can damage the lander
/// </summary>
public class EjectaParticle
{
    public Point Position { get; private set; }
    public Vector Velocity { get; private set; }
    public double Life { get; private set; }
    public double MaxLife { get; }
    public double Size { get; }

    /// <summary>
    /// Ejecta is only dangerous while moving upward (negative Y velocity)
    /// </summary>
    public bool IsDangerous => Velocity.Y < 0;

    public EjectaParticle(Point position, Vector velocity, double life, double size)
    {
        Position = position;
        Velocity = velocity;
        Life = life;
        MaxLife = life;
        Size = size;
    }

    public void Update(double deltaTime)
    {
        // Apply gravity (stronger than regular particles - these are rocks)
        Velocity = new Vector(Velocity.X * 0.995, Velocity.Y + 120 * deltaTime);

        Position = new Point(
            Position.X + Velocity.X * deltaTime,
            Position.Y + Velocity.Y * deltaTime
        );

        Life -= deltaTime;
    }

    public bool IsAlive => Life > 0;

    public double GetOpacity()
    {
        return Math.Max(0, Life / MaxLife);
    }
}
