using System;
using System.Collections.Generic;
using Avalonia.Input;

namespace AVALander.Engine;

public class InputHandler : IDisposable
{
    private readonly HashSet<Key> _pressedKeys = new();
    private readonly ControllerManager _controller = new();

    private const float StickThreshold = 0.3f;
    private const float TriggerThreshold = 0.3f;

    public void OnKeyDown(Key key)
    {
        _pressedKeys.Add(key);
    }

    public void OnKeyUp(Key key)
    {
        _pressedKeys.Remove(key);
    }

    public bool IsKeyPressed(Key key)
    {
        return _pressedKeys.Contains(key);
    }

    public void Update()
    {
        _controller.Update();
    }

    public bool IsControllerConnected => _controller.IsConnected;

    // Rotate left (for lander rotation)
    public bool IsRotatingLeft =>
        IsKeyPressed(Key.A) ||
        IsKeyPressed(Key.Left) ||
        _controller.DPadLeft ||
        _controller.LeftStickX < -StickThreshold;

    // Rotate right (for lander rotation)
    public bool IsRotatingRight =>
        IsKeyPressed(Key.D) ||
        IsKeyPressed(Key.Right) ||
        _controller.DPadRight ||
        _controller.LeftStickX > StickThreshold;

    // Main thruster (up/against gravity)
    public bool IsThrusting =>
        IsKeyPressed(Key.W) ||
        IsKeyPressed(Key.Up) ||
        IsKeyPressed(Key.Space) ||
        _controller.ButtonA ||
        _controller.RightTrigger > TriggerThreshold;

    // Restart game
    public bool IsRestarting =>
        IsKeyPressed(Key.Space) ||
        IsKeyPressed(Key.Enter) ||
        _controller.ButtonA ||
        _controller.ButtonStart;

    public bool IsPausePressed =>
        _controller.ButtonStart;

    public bool IsBackPressed =>
        _controller.ButtonBack;

    public void Dispose()
    {
        _controller.Dispose();
    }
}
