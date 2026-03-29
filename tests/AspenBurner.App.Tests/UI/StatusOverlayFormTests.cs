using System.Drawing;
using AspenBurner.App.UI;

namespace AspenBurner.App.Tests.UI;

/// <summary>
/// Covers immediate repaint behavior for the status overlay form.
/// </summary>
[TestClass]
public sealed class StatusOverlayFormTests
{
    /// <summary>
    /// Ensures a visible status overlay repaints immediately when only text color changes.
    /// </summary>
    [TestMethod]
    public void ApplyStatus_RepaintsImmediatelyWhenVisibleAndStyleChanges()
    {
        RunInSta(() =>
        {
            using StatusOverlayForm form = new();
            int paintCount = 0;
            form.Paint += (_, _) => paintCount++;
            form.ApplyStatus("CPU 4.2GHz | 82C", 11f, Color.FromArgb(220, Color.Yellow));
            form.Show();
            global::System.Windows.Forms.Application.DoEvents();

            paintCount = 0;
            form.ApplyStatus("CPU 4.2GHz | 82C", 11f, Color.FromArgb(180, Color.Lime));

            Assert.IsTrue(paintCount > 0, "Visible status form did not repaint synchronously for a style-only change.");
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
