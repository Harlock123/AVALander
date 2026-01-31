using System;
using Avalonia;

namespace AVALander.Models;

public class Asteroid : GameObject
{
    private static readonly Random Random = new();

    private readonly Point[] _vertices;
    private readonly double _rotationSpeed;
    private readonly double _speed;
    private readonly int _direction; // 1 = moving right, -1 = moving left

    public Asteroid(Point position, double radius, int direction)
        : base(position, radius)
    {
        _direction = direction;
        _speed = 40 + Random.NextDouble() * 60; // 40-100 px/sec
        _rotationSpeed = 0.5 + Random.NextDouble() * 1.5; // 0.5-2.0 rad/sec

        Velocity = new Vector(_speed * _direction, 0);

        // Generate irregular polygon shape (6-10 vertices)
        int vertexCount = 6 + Random.Next(5); // 6-10 vertices
        _vertices = GenerateIrregularPolygon(vertexCount, radius);
    }

    private static Point[] GenerateIrregularPolygon(int vertexCount, double baseRadius)
    {
        var vertices = new Point[vertexCount];
        double angleStep = Math.PI * 2 / vertexCount;

        for (int i = 0; i < vertexCount; i++)
        {
            double angle = i * angleStep;
            // Randomize radius between 60% and 100% of base radius for jagged edges
            double vertexRadius = baseRadius * (0.6 + Random.NextDouble() * 0.4);

            vertices[i] = new Point(
                Math.Cos(angle) * vertexRadius,
                Math.Sin(angle) * vertexRadius
            );
        }

        return vertices;
    }

    public override void Update(double deltaTime, double screenWidth, double screenHeight)
    {
        // Update rotation
        Rotation += _rotationSpeed * deltaTime;

        // Update position via base class
        base.Update(deltaTime, screenWidth, screenHeight);

        // Mark as dead if exited screen on opposite side
        if (_direction > 0 && Position.X - Radius > screenWidth + 100)
        {
            IsAlive = false;
        }
        else if (_direction < 0 && Position.X + Radius < -100)
        {
            IsAlive = false;
        }
    }

    public override Point[] GetPolygonPoints()
    {
        var points = new Point[_vertices.Length];
        double cos = Math.Cos(Rotation);
        double sin = Math.Sin(Rotation);

        for (int i = 0; i < _vertices.Length; i++)
        {
            // Rotate vertex around origin
            double rotatedX = _vertices[i].X * cos - _vertices[i].Y * sin;
            double rotatedY = _vertices[i].X * sin + _vertices[i].Y * cos;

            // Translate to world position
            points[i] = new Point(
                Position.X + rotatedX,
                Position.Y + rotatedY
            );
        }

        return points;
    }

    /// <summary>
    /// Creates an asteroid spawning from the left edge
    /// </summary>
    public static Asteroid CreateFromLeft(double y, double radius)
    {
        return new Asteroid(new Point(-radius - 50, y), radius, 1);
    }

    /// <summary>
    /// Creates an asteroid spawning from the right edge
    /// </summary>
    public static Asteroid CreateFromRight(double screenWidth, double y, double radius)
    {
        return new Asteroid(new Point(screenWidth + radius + 50, y), radius, -1);
    }
}
