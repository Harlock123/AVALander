using System;
using System.IO;
using NetCoreAudio;

namespace AVALander.Engine;

public class SoundManager : IDisposable
{
    private readonly string _soundsPath;
    private Player? _thrusterPlayer;
    private bool _thrusterPlaying;

    public bool IsMuted { get; set; } = false;

    public SoundManager()
    {
        _soundsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds");
    }

    public void PlayThruster()
    {
        if (IsMuted || _thrusterPlaying) return;

        _thrusterPlaying = true;
        PlayThrusterLoop();
    }

    private async void PlayThrusterLoop()
    {
        var path = Path.Combine(_soundsPath, "Thruster.wav");
        if (!File.Exists(path)) return;

        try
        {
            _thrusterPlayer = new Player();

            while (_thrusterPlaying)
            {
                await _thrusterPlayer.Play(path);

                while (_thrusterPlayer.Playing && _thrusterPlaying)
                {
                    await System.Threading.Tasks.Task.Delay(50);
                }
            }
        }
        catch
        {
            // Silently ignore playback errors
        }
    }

    public void StopThruster()
    {
        _thrusterPlaying = false;
        try
        {
            _thrusterPlayer?.Stop();
        }
        catch
        {
            // Silently ignore stop errors
        }
    }

    public void PlayExplosion()
    {
        if (IsMuted) return;
        PlaySound("Explosion.wav");
    }

    public void PlayLanding()
    {
        if (IsMuted) return;
        PlaySound("Landing.wav");
    }

    public void PlayWarning()
    {
        if (IsMuted) return;
        PlaySound("Warning.wav");
    }

    private async void PlaySound(string filename)
    {
        var path = Path.Combine(_soundsPath, filename);
        if (!File.Exists(path)) return;

        try
        {
            var player = new Player();
            await player.Play(path);
        }
        catch
        {
            // Silently ignore playback errors
        }
    }

    public void Dispose()
    {
        StopThruster();
    }
}
