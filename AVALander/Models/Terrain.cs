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

    // Mountain boundary positions (ship crashes if it goes beyond these)
    public double LeftMountainX { get; private set; }
    public double RightMountainX { get; private set; }

    public void Generate(double screenWidth, double screenHeight, int level)
    {
        ScreenWidth = screenWidth;
        ScreenHeight = screenHeight;

        _points.Clear();
        _landingPads.Clear();

        double baseHeight = screenHeight * 0.7;
        int numSegments = 50 + level * 5;
        double segmentWidth = screenWidth / numSegments;

        // Extend terrain beyond screen edges for horizon effect
        int extraSegments = numSegments / 2; // 50% extra on each side
        int totalSegments = numSegments + extraSegments * 2;
        int startOffset = extraSegments; // Offset for the main terrain area

        // Generate base terrain heights with peaks and valleys using multiple sine waves
        var baseHeights = new double[totalSegments + 1];
        double phase1 = _random.NextDouble() * Math.PI * 2;
        double phase2 = _random.NextDouble() * Math.PI * 2;
        double phase3 = _random.NextDouble() * Math.PI * 2;

        // Mountain rise starts this many segments from each edge
        int mountainStartSegments = 8;

        // Set mountain boundary X positions
        LeftMountainX = (-extraSegments + mountainStartSegments) * segmentWidth;
        RightMountainX = (numSegments + extraSegments - mountainStartSegments) * segmentWidth;

        for (int i = 0; i <= totalSegments; i++)
        {
            double x = (double)(i - startOffset) / numSegments;
            // Large rolling hills
            double wave1 = Math.Sin(x * Math.PI * 2 + phase1) * 80;
            // Medium features
            double wave2 = Math.Sin(x * Math.PI * 5 + phase2) * 40;
            // Small details
            double wave3 = Math.Sin(x * Math.PI * 12 + phase3) * 20;
            // Random noise (use seeded position to keep it consistent)
            double noise = (Math.Sin(i * 127.1 + phase1 * 311.7) * 0.5 + 0.5 - 0.5) * 30;

            double height = baseHeight + wave1 + wave2 + wave3 + noise;
            // Keep within bounds
            height = Math.Max(screenHeight * 0.5, Math.Min(screenHeight * 0.88, height));

            // Add mountain rise at extreme edges
            int distFromLeft = i;
            int distFromRight = totalSegments - i;

            if (distFromLeft < mountainStartSegments)
            {
                // Rising mountain on left edge
                double mountainProgress = 1.0 - (double)distFromLeft / mountainStartSegments;
                double mountainHeight = mountainProgress * mountainProgress * screenHeight * 0.8;
                height = Math.Min(height - mountainHeight, screenHeight * 0.1);
            }
            else if (distFromRight < mountainStartSegments)
            {
                // Rising mountain on right edge
                double mountainProgress = 1.0 - (double)distFromRight / mountainStartSegments;
                double mountainHeight = mountainProgress * mountainProgress * screenHeight * 0.8;
                height = Math.Min(height - mountainHeight, screenHeight * 0.1);
            }

            baseHeights[i] = height;
        }

        // Decide landing pad positions - pads in main area plus extended areas
        // Main area gets 2-4 pads, each extended area gets 1-2 pads
        int mainPads = Math.Min(2 + level / 2, 4);
        int extendedPads = 1 + level / 3; // 1-2 pads per extended area
        int totalPads = mainPads + extendedPads * 2;

        var padPositions = new List<int>();
        var padSegmentCounts = new List<int>();
        var padMultipliers = new List<int>();
        var padHeights = new List<double>();

        // Helper to add a pad
        void TryAddPad(int minPos, int maxPos, int padIndex)
        {
            // Smaller pads = higher multiplier (in segments)
            int padSegments = (padIndex % 4) switch
            {
                0 => 5,  // 1X - widest
                1 => 4,  // 2X
                2 => 3,  // 3X
                _ => 3   // 5X - narrowest
            };

            int minDistance = padSegments + 4;
            int pos;
            int attempts = 0;
            do
            {
                pos = _random.Next(minPos, maxPos - padSegments);
                attempts++;
                if (attempts > 100) break;
            } while (IsTooCloseToOtherPads(pos, padPositions, padSegmentCounts, minDistance));

            if (attempts > 100) return;

            padPositions.Add(pos);
            padSegmentCounts.Add(padSegments);

            // Find the highest point in the pad area (adjust for array offset)
            double maxHeightInPadArea = 0;
            for (int j = pos; j <= pos + padSegments; j++)
            {
                int arrayIndex = j + startOffset;
                if (arrayIndex >= 0 && arrayIndex <= totalSegments)
                    maxHeightInPadArea = Math.Max(maxHeightInPadArea, baseHeights[arrayIndex]);
            }
            padHeights.Add(maxHeightInPadArea);

            int multiplier = (padIndex % 4) switch
            {
                0 => 1,
                1 => 2,
                2 => 3,
                _ => 5
            };
            padMultipliers.Add(multiplier);
        }

        // Add pads in main screen area
        for (int i = 0; i < mainPads; i++)
        {
            TryAddPad(4, numSegments - 3, i);
        }

        // Add pads in left extended area (negative segment indices)
        for (int i = 0; i < extendedPads; i++)
        {
            TryAddPad(-extraSegments + 4, -4, mainPads + i);
        }

        // Add pads in right extended area (beyond numSegments)
        for (int i = 0; i < extendedPads; i++)
        {
            TryAddPad(numSegments + 4, numSegments + extraSegments - 3, mainPads + extendedPads + i);
        }

        // Sort all pad data by position
        var padData = new List<(int pos, int segments, int multiplier, double height)>();
        for (int i = 0; i < padPositions.Count; i++)
        {
            padData.Add((padPositions[i], padSegmentCounts[i], padMultipliers[i], padHeights[i]));
        }
        padData.Sort((a, b) => a.pos.CompareTo(b.pos));

        padPositions.Clear();
        padSegmentCounts.Clear();
        padMultipliers.Clear();
        padHeights.Clear();
        foreach (var (pos, segments, multiplier, height) in padData)
        {
            padPositions.Add(pos);
            padSegmentCounts.Add(segments);
            padMultipliers.Add(multiplier);
            padHeights.Add(height);
        }

        // Create the landing pads
        for (int i = 0; i < padPositions.Count; i++)
        {
            double padX = padPositions[i] * segmentWidth;
            double padWidth = padSegmentCounts[i] * segmentWidth;
            _landingPads.Add(new LandingPad(padX, padHeights[i], padWidth, padMultipliers[i]));
        }

        // Generate terrain points including extended areas
        for (int i = 0; i <= totalSegments; i++)
        {
            // Calculate actual X position (can be negative or beyond screenWidth)
            double x = (i - startOffset) * segmentWidth;

            // Calculate segment index in main terrain coordinates for pad checking
            int mainSegmentIndex = i - startOffset;
            var (padIndex, _) = GetPadIndexAt(mainSegmentIndex, padPositions, padSegmentCounts);

            if (padIndex >= 0)
            {
                // Use pad height for all segments in pad area
                _points.Add(new Point(x, padHeights[padIndex]));
            }
            else
            {
                double height = baseHeights[i];

                // Ensure terrain transitions smoothly near pads (only in main area)
                for (int p = 0; p < padPositions.Count; p++)
                {
                    int padStart = padPositions[p];
                    int padEnd = padStart + padSegmentCounts[p];
                    double padY = padHeights[p];

                    if (mainSegmentIndex >= padStart - 2 && mainSegmentIndex < padStart)
                    {
                        double t = (double)(mainSegmentIndex - (padStart - 2)) / 2.0;
                        height = Math.Max(height, padY - (1 - t) * 40);
                    }
                    else if (mainSegmentIndex > padEnd && mainSegmentIndex <= padEnd + 2)
                    {
                        double t = (double)(mainSegmentIndex - padEnd) / 2.0;
                        height = Math.Max(height, padY - t * 40);
                    }
                }

                _points.Add(new Point(x, height));
            }
        }

        // Close the terrain polygon at the bottom (extended to cover full terrain width)
        double leftEdge = -extraSegments * segmentWidth;
        double rightEdge = screenWidth + extraSegments * segmentWidth;
        _points.Add(new Point(rightEdge, screenHeight + 10));
        _points.Add(new Point(leftEdge, screenHeight + 10));
    }

    private bool IsTooCloseToOtherPads(int pos, List<int> existingPads, List<int> padSegmentCounts, int minDistance)
    {
        for (int i = 0; i < existingPads.Count; i++)
        {
            int padStart = existingPads[i];
            int padEnd = padStart + padSegmentCounts[i]; // Inclusive endpoint
            // Check if new pad would overlap or be too close
            if (pos >= padStart - minDistance && pos <= padEnd + minDistance)
                return true;
        }
        return false;
    }

    private (int padIndex, bool isFirstSegment) GetPadIndexAt(int segmentIndex, List<int> padPositions, List<int> padSegmentCounts)
    {
        for (int i = 0; i < padPositions.Count; i++)
        {
            int padStart = padPositions[i];
            // Include the endpoint segment so flat terrain matches pad width exactly
            int padEnd = padStart + padSegmentCounts[i];
            if (segmentIndex >= padStart && segmentIndex <= padEnd)
            {
                return (i, segmentIndex == padStart);
            }
        }
        return (-1, false);
    }

    public double GetHeightAt(double x)
    {
        if (_points.Count < 2) return ScreenHeight;

        // The last 2 points are the polygon closing points at the bottom, skip them
        int terrainPointCount = _points.Count - 2;

        for (int i = 0; i < terrainPointCount - 1; i++)
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

        // If x is outside terrain bounds, return the height at the nearest edge
        if (terrainPointCount > 0)
        {
            if (x < _points[0].X)
                return _points[0].Y;
            if (x > _points[terrainPointCount - 1].X)
                return _points[terrainPointCount - 1].Y;
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

    public bool IsInMountainZone(double x)
    {
        return x <= LeftMountainX || x >= RightMountainX;
    }
}
