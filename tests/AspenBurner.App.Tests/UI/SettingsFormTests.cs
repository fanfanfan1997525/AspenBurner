using AspenBurner.App.UI;

namespace AspenBurner.App.Tests.UI;

/// <summary>
/// Regression coverage for settings window initialization order.
/// </summary>
[TestClass]
public sealed class SettingsFormTests
{
    /// <summary>
    /// Ensures the form constructor remains safe after preset controls are wired up.
    /// </summary>
    [TestMethod]
    public void Constructor_DoesNotThrowWhenPresetSelectionInitializes()
    {
        RunInSta(() =>
        {
            using SettingsForm form = new();
            Assert.IsFalse(form.IsDisposed);
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
            throw new AssertFailedException("STA test failed.", captured);
        }
    }
}
