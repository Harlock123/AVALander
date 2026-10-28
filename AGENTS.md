# AGENTS.md

Notes for anyone, human or AI agent, changing this code. `README.md` covers playing the game, its controls and the
Linux sound and gamepad requirements; this file covers how the code is put together and what to watch for.

## What this is

A Lunar Lander game with white wireframe vector graphics, in C# on .NET 9 and Avalonia 11. It is one of the older
AVA games and does not use the newer template: there is no test project, no `global.json` and no
`Directory.Build.props`, and the sound goes through NetCoreAudio rather than OpenAL.

## Commands

```bash
dotnet build AVALander.sln                        # needs the .NET 9 SDK (AVALander.csproj targets net9.0)
dotnet run --project AVALander                    # play it; there are no command-line options
```

There are no tests yet.

## Layout

- `AVALander/Engine/`:
  - `GameEngine.cs` holds the states (ready, playing, paused, landed, crashed, game over), the score, level and
    lives, and updates the lander, terrain, asteroids and explosions each frame.
  - `AsteroidSpawner.cs` sends asteroids across, more often as the levels go up.
  - `InputHandler.cs` tracks held keys, the mouse (left button thrusts, the wheel rotates) and the gamepad.
  - `ControllerManager.cs` reads SDL2 gamepads.
  - `SoundManager.cs` plays the WAV files through NetCoreAudio.
- `AVALander/Models/`: the lander and its physics constants (`Lander.cs`), the terrain with its landing pads and
  craters (`Terrain.cs`, `LandingPad.cs`), asteroids, explosions and particles.
- `AVALander/Views/GameCanvas.cs`: the frame drawing, the zooming camera and the game loop, a `DispatcherTimer`
  ticking about every 16 ms. Ctrl+S saves a screenshot to `Pictures/AVALander`.
- `AVALander/Sounds/`: the four WAV files, copied next to the executable.
- `Images/`: the README's screenshots.

## Things to know before changing it

- **Variable time step.** `GameCanvas` measures the real time since the last tick, caps it at 0.1 s and passes it
  to `GameEngine.Update(deltaTime)`. Physics and timers are written per second, so keep new movement scaled by
  `deltaTime`.
- **Randomness is unseeded.** Terrain, start position, asteroids and explosions each use their own `new Random()`,
  so no two games are the same and nothing is reproducible from a seed.
- **SDL under one lock.** `ControllerManager` starts, polls and stops SDL only inside `lock (Sdl)`. Keep it that way
  if you touch it: unlocked SDL calls from two threads corrupt its heap.
- **Sound fails silently.** On Linux NetCoreAudio runs `aplay` for each sound, and `SoundManager` swallows playback
  errors, so a missing `aplay` means no sound and no error. The README's troubleshooting section explains it.
- **No saved files.** There is no high-score table; the game writes nothing except Ctrl+S screenshots.
- **Warnings are not errors** here, and nullable reference types are on.

## Making a change

- Build, then play it to check the change: there are no tests to catch regressions.
- Update `README.md` (features, controls) to match.

## CI and releases

`.github/workflows/build.yml` builds the solution on every push and pull request (there is no test step). It then
publishes self-contained single-file builds for win-x64/x86/arm64, linux-x64/arm64/arm and osx-x64/arm64. Every run
keeps the builds as workflow artifacts. Only pushes to `main` replace the assets of the `latest` pre-release, and a
`v*` tag makes a proper release. After a release it asks the portfolio site to rebuild, if the `SITE_REFRESH_TOKEN`
secret is set.
