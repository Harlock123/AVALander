using System;
using System.Collections.Generic;
using Avalonia;
using AVALander.Models;

namespace AVALander.Engine;

public enum GameState
{
    Ready,
    Playing,
    Paused,
    Landed,
    Crashed,
    GameOver
}

public class GameEngine : IDisposable
{
    private static readonly Random Random = new();

    public Lander Lander { get; private set; }
    public Terrain Terrain { get; } = new();
    public List<Explosion> Explosions { get; } = new();

    public int Score { get; private set; }
    public int Level { get; private set; }
    public int Lives { get; private set; }
    public GameState State { get; private set; }

    private double _screenWidth = 1024;
    private double _screenHeight = 768;
    private bool _screenWidthSet;
    private bool _screenHeightSet;
    private bool _initialSetupDone;

    private double _messageTimer;
    private const double MessageDisplayTime = 2.0;
    private string? _customCrashMessage;

    public double ScreenWidth
    {
        get => _screenWidth;
        set
        {
            _screenWidth = value;
            _screenWidthSet = true;
            CheckInitialSetup();
        }
    }

    public double ScreenHeight
    {
        get => _screenHeight;
        set
        {
            _screenHeight = value;
            _screenHeightSet = true;
            CheckInitialSetup();
        }
    }

    private void CheckInitialSetup()
    {
        if (!_initialSetupDone && _screenWidthSet && _screenHeightSet)
        {
            _initialSetupDone = true;
            // Set up the level but stay in Ready state - wait for spacebar to start
            Score = 0;
            Level = 1;
            Lives = 3;
            Explosions.Clear();
            SetupLevel();
            // State remains Ready until player presses space
        }
    }

    public InputHandler Input { get; } = new();
    public SoundManager Sound { get; } = new();

    private bool _wasRestartingLastFrame;
    private bool _wasThrustingLastFrame;

    public GameEngine()
    {
        Lander = new Lander(new Point(ScreenWidth / 2, 50));
        Level = 1;
        Lives = 3;
        State = GameState.Ready;
    }

    public void Dispose()
    {
        Sound.Dispose();
        Input.Dispose();
    }

    public void StartNewGame()
    {
        Score = 0;
        Level = 1;
        Lives = 3;
        Explosions.Clear();

        SetupLevel();
        State = GameState.Playing;
    }

    private void SetupLevel()
    {
        _customCrashMessage = null;
        Terrain.Generate(ScreenWidth, ScreenHeight, Level);

        // Spawn lander at top center with slight random offset
        double startX = ScreenWidth / 2 + (Random.NextDouble() - 0.5) * ScreenWidth * 0.3;
        Lander.Reset(new Point(startX, 50));

        // Always give initial horizontal velocity (90 degrees - purely horizontal)
        // Randomly choose left or right direction
        double baseSpeed = 30 + 10 * Math.Min(Level - 1, 4); // Increases with level
        double direction = Random.Next(2) == 0 ? -1 : 1; // Randomly left (-1) or right (+1)
        Lander.Velocity = new Vector(baseSpeed * direction, 0);

        // Orient ship in direction of movement (tilted ~70 degrees)
        // Player must rotate back to vertical for landing
        double initialRotation = direction * (Math.PI / 2.5); // ~72 degrees in direction of travel
        Lander.Rotation = initialRotation;
    }

    public void NextLevel()
    {
        Level++;
        SetupLevel();
        State = GameState.Playing;
    }

    public void Update(double deltaTime)
    {
        // Handle different game states
        switch (State)
        {
            case GameState.Ready:
                if (Input.IsRestarting && !_wasRestartingLastFrame)
                {
                    State = GameState.Playing;
                }
                break;

            case GameState.Playing:
                UpdatePlaying(deltaTime);
                break;

            case GameState.Paused:
                // Do nothing, wait for resume
                break;

            case GameState.Landed:
                _messageTimer -= deltaTime;
                UpdateExplosions(deltaTime);
                if (_messageTimer <= 0)
                {
                    NextLevel();
                }
                break;

            case GameState.Crashed:
                _messageTimer -= deltaTime;
                UpdateExplosions(deltaTime);
                if (_messageTimer <= 0)
                {
                    if (Lives > 0)
                    {
                        SetupLevel();
                        State = GameState.Playing;
                    }
                    else
                    {
                        State = GameState.GameOver;
                    }
                }
                break;

            case GameState.GameOver:
                UpdateExplosions(deltaTime);
                if (Input.IsRestarting && !_wasRestartingLastFrame)
                {
                    StartNewGame();
                }
                break;
        }

        _wasRestartingLastFrame = Input.IsRestarting;
    }

    private void UpdatePlaying(double deltaTime)
    {
        // Clear thrust state at start of frame (before input processing)
        Lander.ClearThrustState();

        // Handle input
        if (Input.IsRotatingLeft)
            Lander.RotateLeft(deltaTime);
        if (Input.IsRotatingRight)
            Lander.RotateRight(deltaTime);

        // Handle mouse wheel rotation (negative = rotate left, positive = rotate right)
        if (Input.MouseWheelRotation != 0)
        {
            Lander.RotateByAmount(-Input.MouseWheelRotation * 0.1);
        }

        if (Input.IsThrusting && Lander.Fuel > 0)
        {
            Lander.Thrust(deltaTime);
            Sound.PlayThruster();
        }
        else
        {
            if (_wasThrustingLastFrame)
            {
                Sound.StopThruster();
                Lander.ResetThrottle();
            }
        }
        _wasThrustingLastFrame = Input.IsThrusting && Lander.Fuel > 0;

        // Update lander (thrust state persists through this for rendering)
        Lander.Update(deltaTime, ScreenWidth, ScreenHeight);

        // Update explosions
        UpdateExplosions(deltaTime);

        // Check collision with terrain
        CheckCollision();
    }

    private void UpdateExplosions(double deltaTime)
    {
        foreach (var explosion in Explosions)
        {
            explosion.Update(deltaTime);
        }

        Explosions.RemoveAll(e => e.IsFinished);
    }

    private void CheckCollision()
    {
        var (leftFoot, rightFoot) = Lander.GetLandingFeetPositions();

        // Check if ship has entered the mountain zone (instant crash)
        if (Terrain.IsInMountainZone(Lander.Position.X))
        {
            CrashLander("HIT MOUNTAIN!");
            return;
        }

        // Check if either foot is at or below terrain
        if (Terrain.IsCollidingWithTerrain(leftFoot, rightFoot))
        {
            // Check if on landing pad
            var (onPad, pad) = Terrain.CheckLanding(leftFoot, rightFoot);

            if (onPad && pad != null && Lander.IsSafeLandingSpeed() && Lander.IsSafeLandingAngle())
            {
                // Successful landing!
                Lander.Land();
                Sound.StopThruster();
                Sound.PlayLanding();

                // Calculate score
                int fuelBonus = (int)(Lander.Fuel / 10);
                int landingScore = 50 * pad.Multiplier;
                Score += landingScore + fuelBonus;

                State = GameState.Landed;
                _messageTimer = MessageDisplayTime;
            }
            else
            {
                // Crash!
                CrashLander(null);
            }
        }
    }

    private void CrashLander(string? customMessage)
    {
        _customCrashMessage = customMessage;
        Lander.Crash();
        Sound.StopThruster();
        Sound.PlayExplosion();

        Explosions.Add(new Explosion(Lander.Position));

        Lives--;
        State = GameState.Crashed;
        _messageTimer = MessageDisplayTime;
    }

    public void Pause()
    {
        if (State == GameState.Playing)
        {
            State = GameState.Paused;
            Sound.StopThruster();
        }
    }

    public void Resume()
    {
        if (State == GameState.Paused)
        {
            State = GameState.Playing;
        }
    }

    public string GetLandingMessage()
    {
        if (State == GameState.Landed)
        {
            return $"SUCCESSFUL LANDING!\nLevel {Level} Complete";
        }
        return "";
    }

    public string GetCrashMessage()
    {
        if (State == GameState.Crashed)
        {
            if (_customCrashMessage != null)
                return _customCrashMessage;
            if (!Lander.IsSafeLandingSpeed())
                return "TOO FAST!";
            if (!Lander.IsSafeLandingAngle())
                return "BAD ANGLE!";
            return "MISSED THE PAD!";
        }
        return "";
    }
}
