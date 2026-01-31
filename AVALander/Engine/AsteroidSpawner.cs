using System;
using AVALander.Models;

namespace AVALander.Engine;

public class AsteroidSpawner
{
    private static readonly Random Random = new();

    private double _timeSinceLastSpawn;
    private double _spawnInterval;

    private const double BaseSpawnInterval = 8.0; // 8 seconds base
    private const double IntervalDecreasePerLevel = 0.5; // -0.5s per level
    private const double MinSpawnInterval = 3.0; // Minimum 3 seconds

    private const double MinRadius = 10.0;
    private const double MaxRadius = 25.0;

    public AsteroidSpawner()
    {
        Reset(1);
    }

    /// <summary>
    /// Reset the spawner for a new level
    /// </summary>
    public void Reset(int level)
    {
        _timeSinceLastSpawn = 0;
        // Calculate spawn interval based on level
        _spawnInterval = Math.Max(
            MinSpawnInterval,
            BaseSpawnInterval - (level - 1) * IntervalDecreasePerLevel
        );
    }

    /// <summary>
    /// Update the spawner and potentially create a new asteroid
    /// </summary>
    /// <returns>A new Asteroid if one should spawn, null otherwise</returns>
    public Asteroid? Update(double deltaTime, double screenWidth, double screenHeight)
    {
        _timeSinceLastSpawn += deltaTime;

        if (_timeSinceLastSpawn >= _spawnInterval)
        {
            _timeSinceLastSpawn = 0;
            return SpawnAsteroid(screenWidth, screenHeight);
        }

        return null;
    }

    private Asteroid SpawnAsteroid(double screenWidth, double screenHeight)
    {
        // Random size
        double radius = MinRadius + Random.NextDouble() * (MaxRadius - MinRadius);

        // Random height in upper 2/3 of screen
        double minY = screenHeight * 0.1; // Not too close to top
        double maxY = screenHeight * 0.6; // Upper 2/3
        double y = minY + Random.NextDouble() * (maxY - minY);

        // Random direction (left or right)
        bool fromLeft = Random.Next(2) == 0;

        if (fromLeft)
        {
            return Asteroid.CreateFromLeft(y, radius);
        }
        else
        {
            return Asteroid.CreateFromRight(screenWidth, y, radius);
        }
    }

    /// <summary>
    /// Get the current spawn interval (for debugging/display)
    /// </summary>
    public double CurrentInterval => _spawnInterval;

    /// <summary>
    /// Get time until next spawn (for debugging/display)
    /// </summary>
    public double TimeUntilNextSpawn => Math.Max(0, _spawnInterval - _timeSinceLastSpawn);
}
