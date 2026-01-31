using System;
using Avalonia;

namespace AVALander.Models;

public class Lander : GameObject
{
    public const double RotationSpeed = 2.5;
    public const double ThrustPower = 120.0;
    public const double Gravity = 16.0;
    public const double MaxFuel = 1000.0;
    public const double FuelConsumption = 50.0;
    public const double MaxSafeLandingSpeed = 40.0;
    public const double MaxSafeLandingAngle = 15.0; // degrees from vertical

    // Throttle ramp-up settings
    public const double MinThrottle = 0.2;
    public const double ThrottleRampUpTime = 0.7; // seconds to reach full throttle

    public double Fuel { get; private set; }
    public bool IsThrusting { get; set; }
    public bool HasLanded { get; private set; }
    public bool HasCrashed { get; private set; }
    public double ThrottleLevel { get; private set; } = MinThrottle;

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

    public void RotateByAmount(double amount)
    {
        if (HasLanded || HasCrashed) return;
        Rotation += amount;
    }

    public void Thrust(double deltaTime)
    {
        if (HasLanded || HasCrashed || Fuel <= 0) return;

        IsThrusting = true;

        // Ramp up throttle over time
        double rampRate = (1.0 - MinThrottle) / ThrottleRampUpTime;
        ThrottleLevel = Math.Min(1.0, ThrottleLevel + rampRate * deltaTime);

        // Thrust direction is opposite to the rotation (pushing against gravity)
        // Rotation 0 = pointing up, so thrust is upward
        double thrustAngle = Rotation - Math.PI / 2;
        double effectiveThrust = ThrustPower * ThrottleLevel;
        var thrustVector = new Vector(
            Math.Cos(thrustAngle) * effectiveThrust * deltaTime,
            Math.Sin(thrustAngle) * effectiveThrust * deltaTime
        );

        Velocity += thrustVector;
        Fuel -= FuelConsumption * ThrottleLevel * deltaTime;
        if (Fuel < 0) Fuel = 0;
    }

    public override void Update(double deltaTime, double screenWidth, double screenHeight)
    {
        if (HasLanded || HasCrashed) return;

        // Apply gravity
        Velocity = new Vector(Velocity.X, Velocity.Y + Gravity * deltaTime);

        base.Update(deltaTime, screenWidth, screenHeight);

        // No horizontal bounds - mountains handle the boundaries now
        // The extended terrain allows the ship to fly beyond the original screen

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

    public void ResetThrottle()
    {
        ThrottleLevel = MinThrottle;
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
        ThrottleLevel = MinThrottle;
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
        // Main body outline - Apollo LM style ascent stage
        var points = new Point[12];
        double cos = Math.Cos(Rotation);
        double sin = Math.Sin(Rotation);

        // Ascent stage cabin (boxy shape with angled top)
        points[0] = TransformPoint(-4, -16, cos, sin);   // Top left of cabin
        points[1] = TransformPoint(4, -16, cos, sin);    // Top right of cabin
        points[2] = TransformPoint(8, -12, cos, sin);    // Upper right corner
        points[3] = TransformPoint(8, -4, cos, sin);     // Right side of cabin
        points[4] = TransformPoint(12, 0, cos, sin);     // Right side descent stage top
        points[5] = TransformPoint(12, 6, cos, sin);     // Right side descent stage bottom
        points[6] = TransformPoint(6, 8, cos, sin);      // Bottom right
        points[7] = TransformPoint(-6, 8, cos, sin);     // Bottom left
        points[8] = TransformPoint(-12, 6, cos, sin);    // Left side descent stage bottom
        points[9] = TransformPoint(-12, 0, cos, sin);    // Left side descent stage top
        points[10] = TransformPoint(-8, -4, cos, sin);   // Left side of cabin
        points[11] = TransformPoint(-8, -12, cos, sin);  // Upper left corner

        return points;
    }

    public Point[] GetCabinWindowPoints()
    {
        // Triangular window on the ascent stage
        var points = new Point[3];
        double cos = Math.Cos(Rotation);
        double sin = Math.Sin(Rotation);

        points[0] = TransformPoint(0, -14, cos, sin);    // Top of window
        points[1] = TransformPoint(-4, -8, cos, sin);    // Bottom left
        points[2] = TransformPoint(4, -8, cos, sin);     // Bottom right

        return points;
    }

    public Point[] GetLegPoints()
    {
        // Four landing legs with footpads - returns pairs of points for each leg segment
        var points = new Point[16];
        double cos = Math.Cos(Rotation);
        double sin = Math.Sin(Rotation);

        // Left leg - strut from body
        points[0] = TransformPoint(-10, 4, cos, sin);
        points[1] = TransformPoint(-18, 14, cos, sin);
        // Left leg - footpad
        points[2] = TransformPoint(-20, 14, cos, sin);
        points[3] = TransformPoint(-16, 14, cos, sin);

        // Right leg - strut from body
        points[4] = TransformPoint(10, 4, cos, sin);
        points[5] = TransformPoint(18, 14, cos, sin);
        // Right leg - footpad
        points[6] = TransformPoint(16, 14, cos, sin);
        points[7] = TransformPoint(20, 14, cos, sin);

        // Secondary struts (inner supports)
        // Left inner strut
        points[8] = TransformPoint(-6, 8, cos, sin);
        points[9] = TransformPoint(-18, 14, cos, sin);
        // Right inner strut
        points[10] = TransformPoint(6, 8, cos, sin);
        points[11] = TransformPoint(18, 14, cos, sin);

        // Cross braces
        // Left cross brace
        points[12] = TransformPoint(-12, 6, cos, sin);
        points[13] = TransformPoint(-14, 10, cos, sin);
        // Right cross brace
        points[14] = TransformPoint(12, 6, cos, sin);
        points[15] = TransformPoint(14, 10, cos, sin);

        return points;
    }

    private static readonly Random FlameRandom = new();

    public Point[] GetThrustFlamePoints()
    {
        if (!IsThrusting) return Array.Empty<Point>();

        var points = new Point[5];
        double cos = Math.Cos(Rotation);
        double sin = Math.Sin(Rotation);

        // Flickering flame length based on time and throttle level
        double baseFlicker = 15 + FlameRandom.NextDouble() * 15;
        double flicker = baseFlicker * ThrottleLevel;
        double width = 7 * ThrottleLevel;

        // Main flame triangle with inner detail
        points[0] = TransformPoint(-width, 8, cos, sin);
        points[1] = TransformPoint(-width * 0.43, 8 + flicker * 0.6, cos, sin);
        points[2] = TransformPoint(0, 8 + flicker, cos, sin);
        points[3] = TransformPoint(width * 0.43, 8 + flicker * 0.6, cos, sin);
        points[4] = TransformPoint(width, 8, cos, sin);

        return points;
    }

    public Point[] GetInnerFlamePoints()
    {
        if (!IsThrusting) return Array.Empty<Point>();

        var points = new Point[3];
        double cos = Math.Cos(Rotation);
        double sin = Math.Sin(Rotation);

        double baseFlicker = 8 + FlameRandom.NextDouble() * 10;
        double flicker = baseFlicker * ThrottleLevel;
        double width = 4 * ThrottleLevel;

        points[0] = TransformPoint(-width, 10, cos, sin);
        points[1] = TransformPoint(0, 10 + flicker, cos, sin);
        points[2] = TransformPoint(width, 10, cos, sin);

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

        // Match the new leg footpad positions
        return (
            TransformPoint(-18, 14, cos, sin),
            TransformPoint(18, 14, cos, sin)
        );
    }
}
