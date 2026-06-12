using System.Collections.Generic;
using System.IO;
using NAudio.Flac;
using NAudio.Wave;
using SoundBoard.PluginApi;

namespace FlacCodecPlugin;

/// <summary>
/// <see cref="IAudioCodecPlugin"/> that adds FLAC (<c>.flac</c>) playback
/// to Game Master Sound Board via
/// <a href="https://www.nuget.org/packages/BunLabs.NAudio.Flac">BunLabs.NAudio.Flac</a>.
/// Fully managed (no native libFLAC binaries to ship).
///
/// <para><see cref="NAudio.Flac.FlacReader"/> already derives from
/// <see cref="WaveStream"/> and reports <c>CanSeek = true</c> for local
/// files, so <see cref="CreateStream(string)"/> is a one-liner.</para>
///
/// <para><b>Inter-plugin dispatch.</b> Implements the
/// <see cref="IAudioCodecPlugin.CreateStream(System.IO.Stream, string)"/>
/// overload so transport plugins (e.g. <c>codec.webstream</c>) can hand
/// a pre-opened HTTP stream here for decode. Declared MIME types:
/// <c>"audio/flac"</c> and <c>"audio/x-flac"</c>.</para>
/// </summary>
public sealed class FlacCodecPlugin : IAudioCodecPlugin
{
    public string Id => "codec.flac";
    public string Name => "FLAC Codec";
    public string Description => "Adds .flac playback support via the BunLabs.NAudio.Flac managed decoder.";
    public string Version => PluginVersion.OfAssembly(typeof(FlacCodecPlugin));
    public string Author => "Devin Sanders";

    public IEnumerable<string> SupportedPatterns => new[] { ".flac" };

    // "audio/flac" is the modern RFC 9639 type; "audio/x-flac" is the
    // legacy form some servers still emit.
    public IEnumerable<string> SupportedContentTypes => new[] { "audio/flac", "audio/x-flac" };

    public bool SupportsStreamInput => true;

    public WaveStream CreateStream(string source) => new FlacReader(source);

    /// <summary>Decode an already-open <see cref="Stream"/> of FLAC bytes.
    /// The SDK contract requires the returned <see cref="WaveStream"/> to
    /// take ownership of <paramref name="source"/> and dispose it on its
    /// own <see cref="WaveStream.Dispose"/>. <c>FlacReader(Stream)</c> does
    /// <i>not</i> do this — it only closes streams it opened itself (e.g.
    /// the file handle behind <c>FlacReader(string)</c>) — so we wrap it in
    /// <see cref="OwnedFlacStream"/> to honor the contract.
    /// <paramref name="formatHint"/> is advisory; FlacReader validates
    /// STREAMINFO headers itself.</summary>
    public WaveStream CreateStream(Stream source, string formatHint)
        => new OwnedFlacStream(source);

    public void Initialize(IPluginContext context) { }
    public void Shutdown() { }

    /// <summary>A <see cref="WaveStream"/> that wraps a <see cref="FlacReader"/>
    /// over a caller-supplied <see cref="Stream"/> and disposes <i>both</i> on
    /// <see cref="Dispose"/>, satisfying the SDK's ownership-transfer contract
    /// for the <see cref="CreateStream(Stream, string)"/> overload.
    ///
    /// <para>Composition rather than subclassing: <see cref="FlacReader"/>
    /// (a) only closes streams it opened itself, never a caller's, and
    /// (b) hides <see cref="Stream.Dispose()"/> with a non-virtual method, so
    /// a subclass can't reliably extend its teardown. A plain wrapper sidesteps
    /// both: its own <see cref="Dispose(bool)"/> runs through the normal
    /// <see cref="Stream"/> path.</para></summary>
    private sealed class OwnedFlacStream : WaveStream
    {
        private readonly FlacReader _reader;
        private readonly Stream _source;

        public OwnedFlacStream(Stream source)
        {
            _source = source;
            _reader = new FlacReader(source);
        }

        public override WaveFormat WaveFormat => _reader.WaveFormat;
        public override long Length => _reader.Length;
        public override bool CanSeek => _reader.CanSeek;
        public override long Position
        {
            get => _reader.Position;
            set => _reader.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
            => _reader.Read(buffer, offset, count);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _reader.Dispose();
                _source.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
