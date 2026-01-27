using Avalonia;

namespace AVALander.Models;

public class LandingPad
{
    public double X { get; }
    public double Y { get; }
    public double Width { get; }
    public int Multiplier { get; }

    public LandingPad(double x, double y, double width, int multiplier)
    {
        X = x;
        Y = y;
        Width = width;
        Multiplier = multiplier;
    }

    public double Left => X;
    public double Right => X + Width;

    public bool ContainsX(double pointX)
    {
        return pointX >= Left && pointX <= Right;
    }

    public bool IsLanderOnPad(Point leftFoot, Point rightFoot)
    {
        return ContainsX(leftFoot.X) && ContainsX(rightFoot.X);
    }

    public Point[] GetPolygonPoints()
    {
        return new Point[]
        {
            new Point(X, Y),
            new Point(X + Width, Y),
            new Point(X + Width, Y + 4),
            new Point(X, Y + 4)
        };
    }
}
