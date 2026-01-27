using System;
using Avalonia;

namespace AVALander.Models;

public abstract class GameObject
{
    public Point Position { get; set; }
    public Vector Velocity { get; set; }
    public double Rotation { get; set; }
    public double Radius { get; protected set; }
    public bool IsAlive { get; set; } = true;

    protected GameObject(Point position, double radius)
    {
        Position = position;
        Radius = radius;
        Velocity = new Vector(0, 0);
        Rotation = 0;
    }

    public virtual void Update(double deltaTime, double screenWidth, double screenHeight)
    {
        Position = new Point(
            Position.X + Velocity.X * deltaTime,
            Position.Y + Velocity.Y * deltaTime
        );
    }

    public bool CollidesWith(GameObject other)
    {
        double dx = Position.X - other.Position.X;
        double dy = Position.Y - other.Position.Y;
        double distance = Math.Sqrt(dx * dx + dy * dy);
        return distance < Radius + other.Radius;
    }

    public abstract Point[] GetPolygonPoints();
}
