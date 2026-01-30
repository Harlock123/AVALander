using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using AVALander.Engine;
using AVALander.Models;

namespace AVALander.Views;

public class GameCanvas : Control
{
    private readonly GameEngine _engine;
    private readonly DispatcherTimer _gameTimer;
    private DateTime _lastUpdate;
    private bool _wasPausePressed;
    private bool _wasBackPressed;

    // Zoom settings
    private const double ZoomStartAltitude = 200.0;  // Start zooming when below this altitude
    private const double ZoomMaxAltitude = 50.0;     // Full zoom when at or below this altitude
    private const double MaxZoomLevel = 2.5;         // Maximum zoom multiplier
    private double _currentZoom = 1.0;
    private double _targetZoom = 1.0;
    private const double ZoomSmoothSpeed = 3.0;      // How fast zoom transitions

    private static readonly IPen WhitePen = new Pen(Brushes.White, 2);
    private static readonly IPen ThinWhitePen = new Pen(Brushes.White, 1);
    private static readonly IPen GreenPen = new Pen(Brushes.LimeGreen, 3);
    private static readonly IPen YellowPen = new Pen(Brushes.Yellow, 2);
    private static readonly IPen RedPen = new Pen(Brushes.Red, 2);
    private static readonly IBrush WhiteBrush = Brushes.White;
    private static readonly IBrush GreenBrush = Brushes.LimeGreen;
    private static readonly IBrush YellowBrush = Brushes.Yellow;
    private static readonly IBrush RedBrush = Brushes.Red;

    public GameCanvas()
    {
        _engine = new GameEngine();
        _lastUpdate = DateTime.Now;

        _gameTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _gameTimer.Tick += GameLoop;

        Focusable = true;
        ClipToBounds = true;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _gameTimer.Start();
        Focus();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _gameTimer.Stop();
        _engine.Dispose();
    }

    protected override void OnKeyDown(Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.Escape)
        {
            if (_engine.State == GameState.Playing)
            {
                _engine.Pause();
            }
            else if (_engine.State == GameState.Paused)
            {
                var window = TopLevel.GetTopLevel(this) as Window;
                window?.Close();
            }
            e.Handled = true;
            return;
        }

        if (_engine.State == GameState.Paused)
        {
            _engine.Resume();
            e.Handled = true;
            return;
        }

        _engine.Input.OnKeyDown(e.Key);
        e.Handled = true;
    }

    protected override void OnKeyUp(Avalonia.Input.KeyEventArgs e)
    {
        _engine.Input.OnKeyUp(e.Key);
        e.Handled = true;
    }

    private void GameLoop(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        double deltaTime = (now - _lastUpdate).TotalSeconds;
        _lastUpdate = now;

        deltaTime = Math.Min(deltaTime, 0.1);

        _engine.ScreenWidth = Bounds.Width;
        _engine.ScreenHeight = Bounds.Height;

        _engine.Input.Update();

        HandleControllerPause();

        _engine.Update(deltaTime);
        UpdateZoom(deltaTime);
        InvalidateVisual();
    }

    private void UpdateZoom(double deltaTime)
    {
        // Calculate target zoom based on altitude
        if (_engine.State == GameState.Playing && !_engine.Lander.HasCrashed && !_engine.Lander.HasLanded)
        {
            var (leftFoot, _) = _engine.Lander.GetLandingFeetPositions();
            double altitude = _engine.Terrain.GetHeightAt(leftFoot.X) - leftFoot.Y;
            altitude = Math.Max(0, altitude);

            if (altitude < ZoomStartAltitude)
            {
                // Interpolate zoom level based on altitude
                double zoomProgress = 1.0 - (altitude - ZoomMaxAltitude) / (ZoomStartAltitude - ZoomMaxAltitude);
                zoomProgress = Math.Clamp(zoomProgress, 0.0, 1.0);
                _targetZoom = 1.0 + (MaxZoomLevel - 1.0) * zoomProgress;
            }
            else
            {
                _targetZoom = 1.0;
            }
        }
        else if (_engine.State == GameState.Landed || _engine.State == GameState.Crashed)
        {
            // Keep current zoom during landing/crash message
        }
        else
        {
            _targetZoom = 1.0;
        }

        // Smoothly interpolate current zoom towards target
        double zoomDiff = _targetZoom - _currentZoom;
        _currentZoom += zoomDiff * ZoomSmoothSpeed * deltaTime;
    }

    private void HandleControllerPause()
    {
        bool pausePressed = _engine.Input.IsPausePressed;
        if (pausePressed && !_wasPausePressed)
        {
            if (_engine.State == GameState.Playing)
                _engine.Pause();
            else if (_engine.State == GameState.Paused)
                _engine.Resume();
        }
        _wasPausePressed = pausePressed;

        bool backPressed = _engine.Input.IsBackPressed;
        if (backPressed && !_wasBackPressed && _engine.State == GameState.Paused)
        {
            var window = TopLevel.GetTopLevel(this) as Window;
            window?.Close();
        }
        _wasBackPressed = backPressed;
    }

    public override void Render(DrawingContext context)
    {
        // Black background (space)
        context.FillRectangle(Brushes.Black, new Rect(0, 0, Bounds.Width, Bounds.Height));

        // Draw stars (unzoomed - they're far away)
        DrawStars(context);

        // Apply zoom transform for game world elements
        if (_currentZoom > 1.01)
        {
            // Calculate zoom center (focused on lander)
            double centerX = _engine.Lander.Position.X;
            double centerY = _engine.Lander.Position.Y;

            // Create transform matrix: translate to origin, scale, translate back
            // This zooms centered on the lander position
            var transform = Matrix.CreateTranslation(-centerX, -centerY)
                * Matrix.CreateScale(_currentZoom, _currentZoom)
                * Matrix.CreateTranslation(centerX, centerY);

            // Offset to keep lander roughly centered on screen when zoomed
            double offsetX = (Bounds.Width / 2 - centerX) * (_currentZoom - 1) / _currentZoom;
            double offsetY = (Bounds.Height / 2 - centerY) * (_currentZoom - 1) / _currentZoom;
            transform = transform * Matrix.CreateTranslation(offsetX, offsetY);

            using (context.PushTransform(transform))
            {
                DrawGameWorld(context);
            }
        }
        else
        {
            DrawGameWorld(context);
        }

        // Draw HUD (unzoomed - always on screen)
        DrawHUD(context);

        // Draw state-specific overlays (unzoomed)
        DrawOverlays(context);
    }

    private void DrawGameWorld(DrawingContext context)
    {
        // Draw terrain
        DrawTerrain(context);

        // Draw landing pads
        DrawLandingPads(context);

        // Draw explosions
        foreach (var explosion in _engine.Explosions)
        {
            DrawExplosion(context, explosion);
        }

        // Draw lander (if not crashed)
        if (!_engine.Lander.HasCrashed)
        {
            DrawLander(context);
        }
    }

    private void DrawStars(DrawingContext context)
    {
        var random = new Random(42); // Fixed seed for consistent stars
        for (int i = 0; i < 100; i++)
        {
            double x = random.NextDouble() * Bounds.Width;
            double y = random.NextDouble() * Bounds.Height * 0.6;
            double size = 1 + random.NextDouble();
            context.DrawEllipse(WhiteBrush, null, new Point(x, y), size, size);
        }
    }

    private void DrawTerrain(DrawingContext context)
    {
        var points = _engine.Terrain.Points;
        if (points.Count < 2) return;

        // Draw terrain outline
        for (int i = 0; i < points.Count - 1; i++)
        {
            context.DrawLine(WhitePen, points[i], points[i + 1]);
        }
    }

    private void DrawLandingPads(DrawingContext context)
    {
        foreach (var pad in _engine.Terrain.LandingPads)
        {
            var padPoints = pad.GetPolygonPoints();

            // Draw pad surface (bright green)
            context.DrawLine(GreenPen, padPoints[0], padPoints[1]);

            // Draw multiplier text
            var typeface = new Typeface("Consolas", FontStyle.Normal, FontWeight.Bold);
            var multiplierText = new FormattedText(
                $"{pad.Multiplier}X",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                14,
                GreenBrush
            );
            context.DrawText(multiplierText, new Point(
                pad.X + pad.Width / 2 - multiplierText.Width / 2,
                pad.Y + 8
            ));
        }
    }

    private static readonly IPen OrangePen = new Pen(Brushes.Orange, 2);

    private void DrawLander(DrawingContext context)
    {
        // Draw thrust flame FIRST (behind the lander)
        if (_engine.Lander.IsThrusting)
        {
            // Outer flame (yellow/orange)
            var flamePoints = _engine.Lander.GetThrustFlamePoints();
            if (flamePoints.Length > 0)
            {
                var outerPen = (DateTime.Now.Millisecond / 60) % 2 == 0 ? YellowPen : OrangePen;
                DrawFlameShape(context, flamePoints, outerPen);
            }

            // Inner flame (red/yellow) - brighter core
            var innerFlamePoints = _engine.Lander.GetInnerFlamePoints();
            if (innerFlamePoints.Length > 0)
            {
                var innerPen = (DateTime.Now.Millisecond / 40) % 2 == 0 ? RedPen : YellowPen;
                DrawFlameShape(context, innerFlamePoints, innerPen);
            }
        }

        // Draw lander body
        var landerPoints = _engine.Lander.GetPolygonPoints();
        DrawPolygon(context, landerPoints, WhitePen);

        // Draw cabin window
        var windowPoints = _engine.Lander.GetCabinWindowPoints();
        DrawPolygon(context, windowPoints, ThinWhitePen);
        // Close the triangle
        context.DrawLine(ThinWhitePen, windowPoints[2], windowPoints[0]);

        // Draw legs (struts, footpads, inner supports, cross braces)
        var legPoints = _engine.Lander.GetLegPoints();
        // Main struts
        context.DrawLine(ThinWhitePen, legPoints[0], legPoints[1]);
        context.DrawLine(ThinWhitePen, legPoints[4], legPoints[5]);
        // Footpads
        context.DrawLine(WhitePen, legPoints[2], legPoints[3]);
        context.DrawLine(WhitePen, legPoints[6], legPoints[7]);
        // Inner support struts
        context.DrawLine(ThinWhitePen, legPoints[8], legPoints[9]);
        context.DrawLine(ThinWhitePen, legPoints[10], legPoints[11]);
        // Cross braces
        context.DrawLine(ThinWhitePen, legPoints[12], legPoints[13]);
        context.DrawLine(ThinWhitePen, legPoints[14], legPoints[15]);
    }

    private void DrawFlameShape(DrawingContext context, Point[] points, IPen pen)
    {
        if (points.Length < 2) return;
        for (int i = 0; i < points.Length - 1; i++)
        {
            context.DrawLine(pen, points[i], points[i + 1]);
        }
    }

    private void DrawExplosion(DrawingContext context, Explosion explosion)
    {
        double opacity = explosion.GetOpacity();
        var color = Color.FromArgb((byte)(255 * opacity), 255, 200, 100);
        var pen = new Pen(new SolidColorBrush(color), 2);

        // Draw particles
        foreach (var particle in explosion.GetParticles())
        {
            double particleOpacity = particle.GetOpacity();
            var particleColor = Color.FromArgb((byte)(255 * particleOpacity), 255, 150, 50);
            var brush = new SolidColorBrush(particleColor);
            context.DrawEllipse(brush, null, particle.Position, particle.Size, particle.Size);
        }

        // Draw debris lines
        foreach (var (start, end) in explosion.GetDebrisLines())
        {
            context.DrawLine(pen, start, end);
        }
    }

    private void DrawPolygon(DrawingContext context, Point[] points, IPen pen)
    {
        if (points.Length < 2) return;

        for (int i = 0; i < points.Length - 1; i++)
        {
            context.DrawLine(pen, points[i], points[i + 1]);
        }
        // Close the polygon
        context.DrawLine(pen, points[^1], points[0]);
    }

    private void DrawHUD(DrawingContext context)
    {
        var typeface = new Typeface("Consolas", FontStyle.Normal, FontWeight.Bold);

        // Score
        var scoreText = new FormattedText(
            $"SCORE: {_engine.Score}",
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            24,
            WhiteBrush
        );
        context.DrawText(scoreText, new Point(20, 20));

        // Level
        var levelText = new FormattedText(
            $"LEVEL: {_engine.Level}",
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            24,
            WhiteBrush
        );
        context.DrawText(levelText, new Point(20, 50));

        // Lives
        var livesText = new FormattedText(
            $"LIVES: {_engine.Lives}",
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            24,
            WhiteBrush
        );
        context.DrawText(livesText, new Point(20, 80));

        // Fuel gauge (right side)
        DrawFuelGauge(context, typeface);

        // Velocity indicators
        DrawVelocityIndicators(context, typeface);
    }

    private void DrawFuelGauge(DrawingContext context, Typeface typeface)
    {
        double gaugeWidth = 150;
        double gaugeHeight = 20;
        double gaugeX = Bounds.Width - gaugeWidth - 20;
        double gaugeY = 20;

        // Fuel label
        var fuelLabel = new FormattedText(
            "FUEL",
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            16,
            WhiteBrush
        );
        context.DrawText(fuelLabel, new Point(gaugeX, gaugeY - 2));

        // Gauge background
        var bgRect = new Rect(gaugeX + 50, gaugeY, gaugeWidth - 50, gaugeHeight);
        context.DrawRectangle(null, ThinWhitePen, bgRect);

        // Fuel fill
        double fuelPercent = _engine.Lander.Fuel / Lander.MaxFuel;
        var fillRect = new Rect(gaugeX + 51, gaugeY + 1, (gaugeWidth - 52) * fuelPercent, gaugeHeight - 2);

        IBrush fillBrush;
        if (fuelPercent > 0.5) fillBrush = GreenBrush;
        else if (fuelPercent > 0.25) fillBrush = YellowBrush;
        else fillBrush = RedBrush;

        context.FillRectangle(fillBrush, fillRect);
    }

    private void DrawVelocityIndicators(DrawingContext context, Typeface typeface)
    {
        double x = Bounds.Width - 170;
        double y = 60;

        // Horizontal velocity
        double hVel = _engine.Lander.GetHorizontalSpeed();
        var hColor = hVel < 20 ? GreenBrush : (hVel < 40 ? YellowBrush : RedBrush);
        var hText = new FormattedText(
            $"H-VEL: {hVel:F0}",
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            16,
            hColor
        );
        context.DrawText(hText, new Point(x, y));

        // Vertical velocity
        double vVel = _engine.Lander.GetVerticalSpeed();
        var vColor = vVel < 30 ? GreenBrush : (vVel < Lander.MaxSafeLandingSpeed ? YellowBrush : RedBrush);
        var vText = new FormattedText(
            $"V-VEL: {vVel:F0}",
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            16,
            vColor
        );
        context.DrawText(vText, new Point(x, y + 25));

        // Altitude
        var (leftFoot, _) = _engine.Lander.GetLandingFeetPositions();
        double altitude = _engine.Terrain.GetHeightAt(leftFoot.X) - leftFoot.Y;
        altitude = Math.Max(0, altitude);
        var altText = new FormattedText(
            $"ALT: {altitude:F0}",
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            16,
            WhiteBrush
        );
        context.DrawText(altText, new Point(x, y + 50));
    }

    private void DrawOverlays(DrawingContext context)
    {
        var typeface = new Typeface("Consolas", FontStyle.Normal, FontWeight.Bold);

        switch (_engine.State)
        {
            case GameState.Ready:
                DrawCenteredText(context, typeface, "AVALander", 48, Bounds.Height / 2 - 80);
                DrawCenteredText(context, typeface, "Press SPACE to Start", 24, Bounds.Height / 2);
                DrawCenteredText(context, typeface, "W/UP or A to Thrust", 18, Bounds.Height / 2 + 40);
                DrawCenteredText(context, typeface, "A/D or LEFT/RIGHT to Rotate", 18, Bounds.Height / 2 + 70);
                break;

            case GameState.Paused:
                DrawCenteredText(context, typeface, "PAUSED", 48, Bounds.Height / 2 - 50);
                DrawCenteredText(context, typeface, "Press any key to resume", 24, Bounds.Height / 2 + 10);
                DrawCenteredText(context, typeface, "Press ESC to quit", 24, Bounds.Height / 2 + 45);
                break;

            case GameState.Landed:
                DrawCenteredText(context, typeface, "SUCCESSFUL LANDING!", 36, Bounds.Height / 2 - 50, GreenBrush);
                DrawCenteredText(context, typeface, $"Level {_engine.Level} Complete", 24, Bounds.Height / 2);
                break;

            case GameState.Crashed:
                DrawCenteredText(context, typeface, _engine.GetCrashMessage(), 36, Bounds.Height / 2 - 50, RedBrush);
                if (_engine.Lives > 0)
                {
                    DrawCenteredText(context, typeface, $"Lives remaining: {_engine.Lives}", 24, Bounds.Height / 2);
                }
                break;

            case GameState.GameOver:
                DrawCenteredText(context, typeface, "GAME OVER", 48, Bounds.Height / 2 - 80, RedBrush);
                DrawCenteredText(context, typeface, $"Final Score: {_engine.Score}", 32, Bounds.Height / 2 - 20);
                DrawCenteredText(context, typeface, $"Reached Level: {_engine.Level}", 24, Bounds.Height / 2 + 30);
                DrawCenteredText(context, typeface, "Press SPACE to restart", 24, Bounds.Height / 2 + 80);
                break;
        }
    }

    private void DrawCenteredText(DrawingContext context, Typeface typeface, string text, int size, double y, IBrush? brush = null)
    {
        var formattedText = new FormattedText(
            text,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            size,
            brush ?? WhiteBrush
        );
        context.DrawText(formattedText, new Point(
            (Bounds.Width - formattedText.Width) / 2,
            y
        ));
    }
}
