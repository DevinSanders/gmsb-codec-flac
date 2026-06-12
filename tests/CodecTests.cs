using System.IO;
using System.Linq;
using FluentAssertions;
using NAudio.Wave;
using Xunit;

namespace FlacCodecPlugin.Tests;

public class CodecTests
{
    private static readonly string FixturePath =
        Path.Combine(AppContext.BaseDirectory, "fixtures", "tone.flac");

    // 0.5 s @ 44.1 kHz, stereo, 16-bit — see fixtures/README.txt.
    private const int ExpectedSampleRate = 44100;
    private const int ExpectedChannels = 2;
    private const int ExpectedBitsPerSample = 16;
    private const double ExpectedSeconds = 0.5;

    [Fact]
    public void Declarations_AreAsIntended()
    {
        var plugin = new FlacCodecPlugin();

        plugin.SupportedPatterns.Should().Contain(".flac");
        plugin.SupportedContentTypes.Should().BeEquivalentTo("audio/flac", "audio/x-flac");
        plugin.SupportsStreamInput.Should().BeTrue();
    }

    [Fact]
    public void CreateStream_FromPath_DecodesFixture()
    {
        var plugin = new FlacCodecPlugin();
        using var stream = plugin.CreateStream(FixturePath);

        stream.WaveFormat.SampleRate.Should().Be(ExpectedSampleRate);
        stream.WaveFormat.Channels.Should().Be(ExpectedChannels);
        stream.WaveFormat.BitsPerSample.Should().Be(ExpectedBitsPerSample);
        stream.CanSeek.Should().BeTrue("a FLAC backed by a real file is seekable");

        // Length should be plausible for a 0.5 s clip (within 10%).
        var expectedBytes = ExpectedSampleRate * ExpectedSeconds
            * stream.WaveFormat.BlockAlign;
        stream.Length.Should().BeInRange(
            (long)(expectedBytes * 0.9), (long)(expectedBytes * 1.1));

        var buffer = new byte[4096];
        stream.Read(buffer, 0, buffer.Length).Should().BeGreaterThan(0);
    }

    [Fact]
    public void CreateStream_FromStream_DecodesAndTakesOwnership()
    {
        var bytes = File.ReadAllBytes(FixturePath);
        var tracking = new DisposeTrackingStream(bytes);
        var plugin = new FlacCodecPlugin();

        var wave = plugin.CreateStream(tracking, "audio/flac");

        wave.WaveFormat.Channels.Should().Be(ExpectedChannels);
        var buffer = new byte[4096];
        wave.Read(buffer, 0, buffer.Length).Should().BeGreaterThan(0);

        tracking.Disposed.Should().BeFalse("the WaveStream still owns the input");
        wave.Dispose();
        tracking.Disposed.Should().BeTrue(
            "per the SDK ownership-transfer contract, disposing the WaveStream disposes the input");
    }

    [Fact]
    public void CreateStream_FromPath_PositionRoundTrips()
    {
        var plugin = new FlacCodecPlugin();
        using var stream = plugin.CreateStream(FixturePath);

        var target = stream.WaveFormat.BlockAlign * 1000L;
        stream.Position = target;
        stream.Position.Should().Be(target);
    }

    /// <summary>A read-only <see cref="MemoryStream"/> that records whether
    /// it has been disposed, so the ownership-transfer test can observe it.</summary>
    private sealed class DisposeTrackingStream(byte[] data) : MemoryStream(data, writable: false)
    {
        public bool Disposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
