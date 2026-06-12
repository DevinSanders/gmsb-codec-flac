tone.flac
---------
A 0.5-second 440 Hz sine tone, 44.1 kHz, stereo, 16-bit, encoded to FLAC.

Generated with ffmpeg:

    ffmpeg -f lavfi -i "sine=frequency=440:duration=0.5:sample_rate=44100" \
           -ac 2 -sample_fmt s16 tone.flac

It is a synthetic test signal containing no third-party content. Placed in
the public domain (CC0) for use as a decode fixture.
