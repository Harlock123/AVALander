using System;
using Avalonia;

namespace AVALander.Models;

public class Lander : GameObject
{
    public const double RotationSpeed = 2.5;
    public const double ThrustPower = 120.0;
    public const double Gravity = 40.0;
    public const double MaxFuel = 1000.0;
    public const double FuelConsumption = 50.0;
    public const double MaxSafeLandingSpeed = 40.0;
    public const double MaxSafeLandingAngle = 15.0; // degrees from vertical

    public double Fuel { get; private set; }
    public bool IsThrusting { get; set; }
    public bool HasLanded { get; private set; }
    public bool HasCrashed { get; private set; }

    private const double LanderWidth = 30;
    private const double LanderHeight = 25;

    public Lander(Point position) : base(position, 15)
    {
        Rotation = 0; // 0 = pointing up
        Fuel = MaxFuel;
    }

    public void RotateLeft(double deltaTime)
    {
        if (HasLanded || HasCrashed) return;
        Rotation -= RotationSpeed * deltaTime;
    }

    public void RotateRight(double deltaTime)
    {
        if (HasLanded || HasCrashed) return;
        Rotation += RotationSpeed * deltaTime;
    }

    public void Thrust(double deltaTime)
    {
        if (HasLanded || HasCrashed || Fuel <= 0) return;

        IsThrusting = true;

        // Thrust direction is opposite to the rotation (pushing against gravity)
        // Rotation 0 = pointing up, so thrust is upward
        double thrustAngle = Rotation - Math.PI / 2;
        var thrustVector = new Vector(
            Math.Cos(thrustAngle) * ThrustPower * deltaTime,
            Math.Sin(thrustAngle) * ThrustPower * deltaTime
        );

        Velocity += thrustVector;
        Fuel -= FuelConsumption * deltaTime;
        if (Fuel < 0) Fuel = 0;
    }

    public override void Update(double deltaTime, double screenWidth, double screenHeight)
    {
        if (HasLanded || HasCrashed) return;

        // Apply gravity
        Velocity = new Vector(Velocity.X, Velocity.Y + Gravity * deltaTime);

        base.Update(deltaTime, screenWidth, screenHeight);

        // Keep within horizontal bounds
        double x = Position.X;
        if (x < LanderWidth / 2) x = LanderWidth / 2;
        if (x > screenWidth - LanderWidth / 2) x = screenWidth - LanderWidth / 2;
        Position = new Point(x, Position.Y);

        // Check if off screen top (allow it, but not too far)
        if (Position.Y < -100)
        {
            Position = new Point(Position.X, -100);
            Velocity = new Vector(Velocity.X, Math.Max(0, Velocity.Y));
        }
    }

    public void ClearThrustState()
    {
        IsThrusting = false;
    }

    public void Land()
    {
        HasLanded = true;
        Velocity = new Vector(0, 0);
    }

    public void Crash()
    {
        HasCrashed = true;
        Velocity = new Vector(0, 0);
    }

    public void Reset(Point position)
    {
        Position = position;
        Velocity = new Vector(0, 0);
        Rotation = 0;
        Fuel = MaxFuel;
        HasLanded = false;
        HasCrashed = false;
        IsThrusting = false;
    }

    public double GetSpeed()
    {
        return Math.Sqrt(Velocity.X * Velocity.X + Velocity.Y * Velocity.Y);
    }

    public double GetVerticalSpeed()
    {
        return Velocity.Y;
    }

    public double GetHorizontalSpeed()
    {
        return Math.Abs(Velocity.X);
    }

    public bool IsSafeLandingSpeed()
    {
        return GetSpeed() <= MaxSafeLandingSpeed;
    }

    public bool IsSafeLandingAngle()
    {
        // Normalize rotation to -PI to PI range
        double angle = Rotation;
        while (angle > Math.PI) angle -= 2 * Math.PI;
        while (angle < -Math.PI) angle += 2 * Math.PI;

        double angleInDegrees = Math.Abs(angle) * 180.0 / Math.PI;
        return angleInDegrees <= MaxSafeLandingAngle;
    }

    public override Point[] GetPolygonPoints()
    {
        var points = new Point[8];
        double cos = Math.Cos(Rotation);
        double sin = Math.Sin(Rotation);

        // Lander body (hexagon-ish shape)
        // Top point
        points[0] = TransformPoint(0, -12, cos, sin);
        // Upper right
        points[1] = TransformPoint(8, -6, cos, sin);
        // Lower right body
        points[2] = TransformPoint(10, 4, cos, sin);
        // Right leg
        points[3] = TransformPoint(14, 12, cos, sin);
        // Left leg
        points[4] = TransformPoint(-14, 12, cos, sin);
        // Lower left body
        points[5] = TransformPoint(-10, 4, cos, sin);
        // Upper left
        points[6] = TransformPoint(-8, -6, cos, sin);
        // Back to top
        points[7] = TransformPoint(0, -12, cos, sin);

        return points;
    }

    public Point[] GetLegPoints()
    {
        var points = new Point[4];
        double cos = Math.Cos(Rotation);
        double sin = Math.Sin(Rotation);

        // Left leg
        points[0] = TransformPoint(-10, 4, cos, sin);
        points[1] = TransformPoint(-14, 12, cos, sin);

        // Right leg
        points[2] = TransformPoint(10, 4, cos, sin);
        points[3] = TransformPoint(14, 12, cos, sin);

        return points;
    }

    private static readonly Random FlameRandom = new();

    public Point[] GetThrustFlamePoints()
    {
        if (!IsThrusting) return Array.Empty<Point>();

        var points = new Point[5];
        double cos = Math.Cos(Rotation);
        double sin = Math.Sin(Rotation);

        // Flickering flame length based on time
        double flicker = 15 + FlameRandom.NextDouble() * 15;

        // Main flame triangle with inner detail
        points[0] = TransformPoint(-7, 8, cos, sin);
        points[1] = TransformPoint(-3, 8 + flicker * 0.6, cos, sin);
        points[2] = TransformPoint(0, 8 + flicker, cos, sin);
        points[3] = TransformPoint(3, 8 + flicker * 0.6, cos, sin);
        points[4] = TransformPoint(7, 8, cos, sin);

        return points;
    }

    public Point[] GetInnerFlamePoints()
    {
        if (!IsThrusting) return Array.Empty<Point>();

        var points = new Point[3];
        double cos = Math.Cos(Rotation);
        double sin = Math.Sin(Rotation);

        double flicker = 8 + FlameRandom.NextDouble() * 10;

        points[0] = TransformPoint(-4, 10, cos, sin);
        points[1] = TransformPoint(0, 10 + flicker, cos, sin);
        points[2] = TransformPoint(4, 10, cos, sin);

        return points;
    }

    private Point TransformPoint(double localX, double localY, double cos, double sin)
    {
        return new Point(
            Position.X + localX * cos - localY * sin,
            Position.Y + localX * sin + localY * cos
        );
    }

    public (Point bottomLeft, Point bottomRight) GetLandingFeetPositions()
    {
        double cos = Math.Cos(Rotation);
        double sin = Math.Sin(Rotation);

        return (
            TransformPoint(-14, 12, cos, sin),
            TransformPoint(14, 12, cos, sin)
        );
    }
}
