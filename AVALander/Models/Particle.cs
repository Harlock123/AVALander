using System;
using Avalonia;

namespace AVALander.Models;

public class Particle
{
    public Point Position { get; private set; }
    public Vector Velocity { get; private set; }
    public double Life { get; private set; }
    public double MaxLife { get; }
    public double Size { get; }

    private static readonly Random Random = new();

    public Particle(Point position, Vector velocity, double life, double size = 2)
    {
        Position = position;
        Velocity = velocity;
        Life = life;
        MaxLife = life;
        Size = size;
    }

    public void Update(double deltaTime)
    {
        // Apply slight gravity to particles
        Velocity = new Vector(Velocity.X * 0.99, Velocity.Y + 20 * deltaTime);

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

    public static Particle CreateExplosionParticle(Point origin)
    {
        double angle = Random.NextDouble() * Math.PI * 2;
        double speed = 50 + Random.NextDouble() * 150;

        return new Particle(
            origin,
            new Vector(Math.Cos(angle) * speed, Math.Sin(angle) * speed),
            0.5 + Random.NextDouble() * 1.0,
            2 + Random.NextDouble() * 3
        );
    }

    public static Particle CreateDebrisParticle(Point origin)
    {
        double angle = Random.NextDouble() * Math.PI * 2;
        double speed = 30 + Random.NextDouble() * 100;

        return new Particle(
            origin,
            new Vector(Math.Cos(angle) * speed, Math.Sin(angle) * speed - 50),
            1.0 + Random.NextDouble() * 1.5,
            3 + Random.NextDouble() * 5
        );
    }
}
