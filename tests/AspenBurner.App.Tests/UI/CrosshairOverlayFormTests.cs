using System.Drawing;
using AspenBurner.App.Configuration;
using AspenBurner.App.UI;

namespace AspenBurner.App.Tests.UI;

/// <summary>
/// Covers immediate repaint behavior for the crosshair overlay form.
/// </summary>
[TestClass]
public sealed class CrosshairOverlayFormTests
{
    /// <summary>
    /// Ensures a visible form repaints immediately when only style parameters change.
    /// </summary>
    [TestMethod]
    public void ApplyConfig_RepaintsImmediatelyWhenVisibleAndStyleChanges()
    {
        RunInSta(() =>
        {
            using CrosshairOverlayForm form = new();
            int paintCount = 0;
            form.Paint += (_, _) => paintCount++;
            form.Bounds = new Rectangle(0, 0, 80, 80);
            form.ApplyConfig(new CrosshairConfig { Color = "Green", Opacity = 255, Length = 6, Gap = 4, Thickness = 2 });
            form.Show();
            global::System.Windows.Forms.Application.DoEvents();

            paintCount = 0;
            form.ApplyConfig(new CrosshairConfig { Color = "Yellow", Opacity = 180, Length = 6, Gap = 4, Thickness = 2 });

            Assert.IsTrue(paintCount > 0, "Visible crosshair form did not repaint synchronously for a style-only change.");
        });
    }

    private static void RunInSta(Action action)
    {
        Exception? captured = null;
        using ManualResetEventSlim completed = new(false);
        Thread thread = new(() =>
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                captured = exception;
            }
            finally
            {
                completed.Set();
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.IsTrue(completed.Wait(TimeSpan.FromSeconds(15)), "STA test timed out.");
        thread.Join();

        if (captured is not null)
        {
            throw new AssertFailedException($"STA test failed: {captured}", captured);
        }
    }
}
