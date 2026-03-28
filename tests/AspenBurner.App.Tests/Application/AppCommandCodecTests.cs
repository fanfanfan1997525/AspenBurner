using AspenBurner.App.Application;

namespace AspenBurner.App.Tests.Application;

/// <summary>
/// Contract tests for named-pipe command serialization.
/// </summary>
[TestClass]
public sealed class AppCommandCodecTests
{
    /// <summary>
    /// Ensures show-settings commands round-trip.
    /// </summary>
    [TestMethod]
    public void SerializeAndParse_RoundTripsShowSettings()
    {
        AppCommand command = new(AppCommandKind.ShowSettings);

        string payload = AppCommandCodec.Serialize(command);
        AppCommand parsed = AppCommandCodec.Parse(payload);

        Assert.AreEqual(AppCommandKind.ShowSettings, parsed.Kind);
    }

    /// <summary>
    /// Ensures stop commands round-trip.
    /// </summary>
    [TestMethod]
    public void SerializeAndParse_RoundTripsStop()
    {
        AppCommand command = new(AppCommandKind.Stop);

        string payload = AppCommandCodec.Serialize(command);
        AppCommand parsed = AppCommandCodec.Parse(payload);

        Assert.AreEqual(AppCommandKind.Stop, parsed.Kind);
    }

    /// <summary>
    /// Ensures resume commands round-trip.
    /// </summary>
    [TestMethod]
    public void SerializeAndParse_RoundTripsResume()
    {
        AppCommand command = new(AppCommandKind.Resume);

        string payload = AppCommandCodec.Serialize(command);
        AppCommand parsed = AppCommandCodec.Parse(payload);

        Assert.AreEqual(AppCommandKind.Resume, parsed.Kind);
    }

    /// <summary>
    /// Ensures preview commands can carry a duration payload.
    /// </summary>
    [TestMethod]
    public void SerializeAndParse_RoundTripsPreviewDuration()
    {
        AppCommand command = new(AppCommandKind.Preview, 8);

        string payload = AppCommandCodec.Serialize(command);
        AppCommand parsed = AppCommandCodec.Parse(payload);

        Assert.AreEqual(AppCommandKind.Preview, parsed.Kind);
        Assert.AreEqual(8, parsed.Argument);
    }

    /// <summary>
    /// Ensures malformed payloads are rejected.
    /// </summary>
    [TestMethod]
    public void Parse_RejectsUnknownCommands()
    {
        Assert.ThrowsException<ArgumentException>(() => AppCommandCodec.Parse("explode|99"));
    }
}
