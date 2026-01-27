using System;
using static SDL2.SDL;

namespace AVALander.Engine;

public class ControllerManager : IDisposable
{
    private IntPtr _controller;
    private bool _initialized;
    private bool _disposed;

    private const short StickDeadzone = 8000;

    public bool IsConnected => _controller != IntPtr.Zero;
    public bool ButtonA { get; private set; }
    public bool ButtonB { get; private set; }
    public bool ButtonX { get; private set; }
    public bool ButtonY { get; private set; }
    public bool ButtonStart { get; private set; }
    public bool ButtonBack { get; private set; }
    public bool DPadLeft { get; private set; }
    public bool DPadRight { get; private set; }
    public bool DPadUp { get; private set; }
    public bool DPadDown { get; private set; }
    public bool LeftBumper { get; private set; }
    public bool RightBumper { get; private set; }
    public float LeftTrigger { get; private set; }
    public float RightTrigger { get; private set; }
    public float LeftStickX { get; private set; }
    public float LeftStickY { get; private set; }

    public ControllerManager()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (_initialized) return;

        try
        {
            if (SDL_Init(SDL_INIT_GAMECONTROLLER) < 0)
            {
                Console.WriteLine($"SDL_Init failed: {SDL_GetError()}");
                return;
            }

            _initialized = true;
            OpenFirstController();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Controller initialization failed: {ex.Message}");
        }
    }

    private void OpenFirstController()
    {
        int numJoysticks = SDL_NumJoysticks();

        for (int i = 0; i < numJoysticks; i++)
        {
            if (SDL_IsGameController(i) == SDL_bool.SDL_TRUE)
            {
                _controller = SDL_GameControllerOpen(i);
                if (_controller != IntPtr.Zero)
                {
                    string? name = SDL_GameControllerName(_controller);
                    Console.WriteLine($"Controller connected: {name ?? "Unknown"}");
                    return;
                }
            }
        }
    }

    public void Update()
    {
        if (!_initialized) return;

        while (SDL_PollEvent(out SDL_Event e) != 0)
        {
            switch (e.type)
            {
                case SDL_EventType.SDL_CONTROLLERDEVICEADDED:
                    if (_controller == IntPtr.Zero)
                    {
                        OpenFirstController();
                    }
                    break;

                case SDL_EventType.SDL_CONTROLLERDEVICEREMOVED:
                    if (_controller != IntPtr.Zero)
                    {
                        SDL_GameControllerClose(_controller);
                        _controller = IntPtr.Zero;
                        Console.WriteLine("Controller disconnected");
                        OpenFirstController();
                    }
                    break;
            }
        }

        if (_controller == IntPtr.Zero) return;

        ButtonA = SDL_GameControllerGetButton(_controller, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A) == 1;
        ButtonB = SDL_GameControllerGetButton(_controller, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B) == 1;
        ButtonX = SDL_GameControllerGetButton(_controller, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X) == 1;
        ButtonY = SDL_GameControllerGetButton(_controller, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y) == 1;
        ButtonStart = SDL_GameControllerGetButton(_controller, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_START) == 1;
        ButtonBack = SDL_GameControllerGetButton(_controller, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK) == 1;
        DPadLeft = SDL_GameControllerGetButton(_controller, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT) == 1;
        DPadRight = SDL_GameControllerGetButton(_controller, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT) == 1;
        DPadUp = SDL_GameControllerGetButton(_controller, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP) == 1;
        DPadDown = SDL_GameControllerGetButton(_controller, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN) == 1;
        LeftBumper = SDL_GameControllerGetButton(_controller, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER) == 1;
        RightBumper = SDL_GameControllerGetButton(_controller, SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER) == 1;

        short leftTriggerRaw = SDL_GameControllerGetAxis(_controller, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT);
        short rightTriggerRaw = SDL_GameControllerGetAxis(_controller, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT);
        LeftTrigger = leftTriggerRaw / 32767f;
        RightTrigger = rightTriggerRaw / 32767f;

        short leftXRaw = SDL_GameControllerGetAxis(_controller, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX);
        short leftYRaw = SDL_GameControllerGetAxis(_controller, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY);

        LeftStickX = ApplyDeadzone(leftXRaw);
        LeftStickY = ApplyDeadzone(leftYRaw);
    }

    private float ApplyDeadzone(short value)
    {
        if (Math.Abs(value) < StickDeadzone)
            return 0f;

        float normalized;
        if (value > 0)
            normalized = (value - StickDeadzone) / (32767f - StickDeadzone);
        else
            normalized = (value + StickDeadzone) / (32768f - StickDeadzone);

        return Math.Clamp(normalized, -1f, 1f);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_controller != IntPtr.Zero)
        {
            SDL_GameControllerClose(_controller);
            _controller = IntPtr.Zero;
        }

        if (_initialized)
        {
            SDL_QuitSubSystem(SDL_INIT_GAMECONTROLLER);
        }
    }
}
