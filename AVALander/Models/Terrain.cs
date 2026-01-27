using System;
using System.Collections.Generic;
using Avalonia;

namespace AVALander.Models;

public class Terrain
{
    private readonly List<Point> _points = new();
    private readonly List<LandingPad> _landingPads = new();
    private readonly Random _random = new();

    public IReadOnlyList<Point> Points => _points;
    public IReadOnlyList<LandingPad> LandingPads => _landingPads;

    public double ScreenWidth { get; private set; }
    public double ScreenHeight { get; private set; }

    public void Generate(double screenWidth, double screenHeight, int level)
    {
        ScreenWidth = screenWidth;
        ScreenHeight = screenHeight;

        _points.Clear();
        _landingPads.Clear();

        double baseHeight = screenHeight * 0.75;
        double maxVariation = 100 + level * 20;
        int numSegments = 40 + level * 5;
        double segmentWidth = screenWidth / numSegments;

        // Decide landing pad positions (2-4 pads depending on level)
        int numPads = Math.Min(2 + level / 2, 4);
        var padPositions = new List<int>();
        var padWidths = new List<double>();
        var padMultipliers = new List<int>();

        for (int i = 0; i < numPads; i++)
        {
            int pos;
            do
            {
                pos = _random.Next(3, numSegments - 3);
            } while (IsTooCloseToOtherPads(pos, padPositions, 5));

            padPositions.Add(pos);

            // Smaller pads = higher multiplier
            double padWidth = (3 - i * 0.5) * segmentWidth;
            padWidth = Math.Max(padWidth, segmentWidth * 1.5);
            padWidths.Add(padWidth);

            int multiplier = i switch
            {
                0 => 1,
                1 => 2,
                2 => 3,
                _ => 5
            };
            padMultipliers.Add(multiplier);
        }

        // Generate terrain points
        double currentHeight = baseHeight;

        for (int i = 0; i <= numSegments; i++)
        {
            double x = i * segmentWidth;

            // Check if this is a landing pad position
            int padIndex = GetPadIndexAt(i, padPositions);

            if (padIndex >= 0)
            {
                // Flat landing pad area
                double padY = currentHeight;
                double padWidth = padWidths[padIndex];
                double padX = padPositions[padIndex] * segmentWidth;

                // Add pad if not already added
                bool padExists = false;
                foreach (var pad in _landingPads)
                {
                    if (Math.Abs(pad.X - padX) < 1)
                    {
                        padExists = true;
                        break;
                    }
                }

                if (!padExists)
                {
                    _landingPads.Add(new LandingPad(padX, padY, padWidth, padMultipliers[padIndex]));
                }

                _points.Add(new Point(x, padY));
            }
            else
            {
                // Random terrain variation
                double variation = (_random.NextDouble() - 0.5) * maxVariation * 0.3;
                currentHeight += variation;

                // Keep within bounds
                currentHeight = Math.Max(screenHeight * 0.5, Math.Min(screenHeight * 0.9, currentHeight));

                _points.Add(new Point(x, currentHeight));
            }
        }

        // Close the terrain polygon at the bottom
        _points.Add(new Point(screenWidth, screenHeight + 10));
        _points.Add(new Point(0, screenHeight + 10));
    }

    private bool IsTooCloseToOtherPads(int pos, List<int> existingPads, int minDistance)
    {
        foreach (var pad in existingPads)
        {
            if (Math.Abs(pos - pad) < minDistance)
                return true;
        }
        return false;
    }

    private int GetPadIndexAt(int segmentIndex, List<int> padPositions)
    {
        for (int i = 0; i < padPositions.Count; i++)
        {
            int padPos = padPositions[i];
            if (segmentIndex >= padPos && segmentIndex <= padPos + 2)
            {
                return i;
            }
        }
        return -1;
    }

    public double GetHeightAt(double x)
    {
        if (_points.Count < 2) return ScreenHeight;

        for (int i = 0; i < _points.Count - 1; i++)
        {
            var p1 = _points[i];
            var p2 = _points[i + 1];

            if (x >= p1.X && x <= p2.X)
            {
                // Linear interpolation
                double t = (x - p1.X) / (p2.X - p1.X);
                return p1.Y + t * (p2.Y - p1.Y);
            }
        }

        return ScreenHeight;
    }

    public (bool onPad, LandingPad? pad) CheckLanding(Point leftFoot, Point rightFoot)
    {
        foreach (var pad in _landingPads)
        {
            if (pad.IsLanderOnPad(leftFoot, rightFoot))
            {
                return (true, pad);
            }
        }
        return (false, null);
    }

    public bool IsCollidingWithTerrain(Point leftFoot, Point rightFoot)
    {
        double leftTerrainY = GetHeightAt(leftFoot.X);
        double rightTerrainY = GetHeightAt(rightFoot.X);

        return leftFoot.Y >= leftTerrainY || rightFoot.Y >= rightTerrainY;
    }
}
