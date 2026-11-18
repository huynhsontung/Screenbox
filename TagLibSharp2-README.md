# TagLibSharp2

A modern .NET library for reading and writing metadata in media files.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## When to Use TagLibSharp2

**Choose TagLibSharp2 if you need:**
- MIT license (TagLib# is LGPL)
- Async I/O for high-throughput scenarios
- Modern .NET features (nullable types, `Span<T>`)
- Result-based error handling (no exceptions)

**Choose TagLib# if you need:**
- A battle-tested library used in production for years

See the [Migration Guide](docs/MIGRATION-FROM-TAGLIB.md) for detailed comparison.

## Features

- **Modern .NET**: Built for .NET 8+ with nullable reference types, `Span<T>`, and async support
- **MIT License**: Permissive licensing for all use cases
- **Performance-First**: Zero-allocation parsing with `Span<T>` and `ArrayPool<T>`
- **Multi-Target**: Supports .NET Standard 2.0/2.1, .NET 8.0, and .NET 10.0
- **Format Support**:
  - Audio: MP3 (ID3v1/ID3v2), FLAC, Ogg Vorbis, Ogg Opus, Ogg FLAC, WAV (RIFF INFO/ID3v2), AIFF (ID3v2), MP4/M4A (AAC/ALAC), ASF/WMA, DSF (DSD), DFF (DSDIFF), WavPack, Monkey's Audio (.ape), Musepack

## Format Support Matrix

| Feature | MP3 | FLAC | Ogg Vorbis | Ogg Opus | Ogg FLAC | WAV | AIFF | MP4/M4A | ASF/WMA | DSF | DFF | WavPack | APE | MPC |
|---------|:---:|:----:|:----------:|:--------:|:--------:|:---:|:----:|:-------:|:-------:|:---:|:---:|:-------:|:---:|:---:|
| **Read metadata** | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Write metadata** | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅⁸ |
| **Audio properties** | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Async I/O** | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Album art** | ✅ | ✅ | ✅ | ✅ | ✅² | ✅¹ | ✅¹ | ✅ | ✅⁹ | ✅¹ | ✅¹ | ✅⁸ | ✅⁸ | ✅⁸ |
| **ReplayGain** | ✅ | ✅ | ✅ | ✅⁴ | ✅² | ✅¹ | ✅¹ | ✅⁵ | ✅⁹ | ✅¹ | ✅¹ | ✅⁸ | ✅⁸ | ✅⁸ |
| **MusicBrainz IDs** | ✅ | ✅ | ✅ | ✅ | ✅² | ✅¹ | ✅¹ | ✅ | ✅⁹ | ✅¹ | ✅¹ | ✅⁸ | ✅⁸ | ✅⁸ |
| **Lyrics** | ✅ | ✅² | ✅² | ✅² | ✅² | ✅¹ | ✅¹ | ✅ | ✅⁹ | ✅¹ | ✅¹ | ✅⁸ | ✅⁸ | ✅⁸ |
| **Performer roles** | ✅ | ✅ | ✅ | ✅ | ✅² | ✅¹ | ✅¹ | — | ✅⁹ | ✅¹ | ✅¹ | ✅⁸ | ✅⁸ | ✅⁸ |
| **BWF broadcast metadata** | — | — | — | — | — | ✅ | — | — | — | — | — | — | — | — |
| **Surround sound info** | — | — | — | — | — | ✅³ | — | ✅⁶ | — | ✅⁷ | ✅⁷ | — | — | — |
| **DSD sample rates** | — | — | — | — | — | — | — | — | — | ✅ | ✅ | — | — | — |

¹ Via embedded ID3v2 tag
² Via Vorbis Comment
³ Via WAVEFORMATEXTENSIBLE (channel mask, valid bits per sample)
⁴ Via R128 gain tags (RFC 7845)
⁵ Via iTunes ----:com.apple.iTunes:replaygain_* atoms
⁶ Via channel layout in stsd/esds
⁷ Via channel type in fmt chunk (Mono, Stereo, 5.1 surround)
⁸ Via APE tag
⁹ Via ASF Extended Content Description

### Tag Format by Container

| Container | Native Tag | Alternative Tags | Priority |
|-----------|------------|------------------|----------|
| MP3 | ID3v2 | ID3v1, APE | ID3v2 preferred |
| FLAC | Vorbis Comment | — | Native only |
| Ogg Vorbis | Vorbis Comment | — | Native only |
| Ogg Opus | Vorbis Comment | — | Native only |
| Ogg FLAC | Vorbis Comment | — | Native only |
| WAV | RIFF INFO | ID3v2, bext (BWF) | ID3v2 preferred |
| AIFF | ID3 chunk | — | Native only |
| MP4/M4A | iTunes atoms (ilst) | — | Native only |
| ASF/WMA | ASF Content Description | Extended Content Description | Native only |
| DSF | ID3v2 | — | Native only |
| DFF | ID3v2 | — | Native only |
| WavPack | APE tag | — | Native only |
| Monkey's Audio | APE tag | — | Native only |
| Musepack | APE tag | — | Native only |

## Installation

```bash
dotnet add package TagLibSharp2
```

Or build from source:

```bash
git clone https://github.com/decriptor/TagLibSharp2.git
cd tagsharp
dotnet build
```

## Quick Start

```csharp
using TagLibSharp2.Core;
using TagLibSharp2.Mpeg;
using TagLibSharp2.Xiph;
using TagLibSharp2.Ogg;
using TagLibSharp2.Riff;
using TagLibSharp2.Aiff;
using TagLibSharp2.Mp4;

// Auto-detect format using MediaFile factory
var result = MediaFile.Read("song.m4a");
if (result.IsSuccess)
{
    Console.WriteLine($"Format: {result.Format}");
    Console.WriteLine($"{result.Tag?.Title} by {result.Tag?.Artist}");
}

// Read MP3 tags (prefers ID3v2, falls back to ID3v1)
var mp3Result = Mp3File.ReadFromFile("song.mp3");
if (mp3Result.IsSuccess)
{
    var mp3 = mp3Result.File!;
    Console.WriteLine($"{mp3.Title} by {mp3.Artist}");

    // Modify and save
    mp3.Title = "New Title";
    mp3.SaveToFile("song.mp3", File.ReadAllBytes("song.mp3"));
}

// MP4/M4A files with iTunes metadata
var mp4Result = Mp4File.ReadFromFile("song.m4a");
if (mp4Result.IsSuccess)
{
    var mp4 = mp4Result.File!;
    Console.WriteLine($"{mp4.Title} - {mp4.Duration}");
    Console.WriteLine($"Codec: {mp4.AudioCodec}, {mp4.Properties?.SampleRate}Hz");
}

// FLAC and Ogg Vorbis work the same way
var flac = FlacFile.ReadFromFile("song.flac").File;
var ogg = OggVorbisFile.ReadFromFile("song.ogg").File;

// Ogg Opus (RFC 7845) with R128 gain
var opusResult = OggOpusFile.ReadFromFile("song.opus");
var opus = opusResult.File;
Console.WriteLine($"Opus: {opus?.Properties?.Duration}, R128 gain: {opus?.Properties?.OutputGain}dB");

// WAV files support both RIFF INFO and ID3v2 tags
WavFile.TryRead(new BinaryData(File.ReadAllBytes("song.wav")), out var wav);
Console.WriteLine($"WAV: {wav.Title} - {wav.AudioProperties?.Duration}");

// AIFF files (read and write, includes audio properties)
AiffFile.TryRead(new BinaryData(File.ReadAllBytes("song.aiff")), out var aiff);
Console.WriteLine($"AIFF: {aiff.AudioProperties?.SampleRate}Hz");
aiff.Tag = new Id3v2Tag { Title = "Updated Title" };
aiff.SaveToFile("song.aiff");

// Async support for high-throughput scenarios
var asyncResult = await Mp4File.ReadFromFileAsync("song.m4a");
```

See the [examples](examples/) directory for more comprehensive usage patterns.

## Building

```bash
git clone https://github.com/decriptor/TagLibSharp2.git
cd tagsharp
dotnet build
dotnet test
```

## Project Status

This is a clean-room rewrite of media tagging functionality, designed from specifications rather than existing implementations.

### Phase 1: Core Infrastructure ✅
- [x] BinaryData (immutable binary data with Span<T> support)
- [x] BinaryDataBuilder (mutable builder with ArrayPool integration)
- [x] Multi-framework polyfills (netstandard2.0 through net10.0)
- [x] Tag and Picture abstract base classes
- [x] TagReadResult for error handling

### Phase 2: ID3 Support ✅
- [x] ID3v1/v1.1 reading and writing (id3.org specification)
- [x] ID3v2.3/2.4 reading and writing (id3.org specification)
  - [x] Text frames (TIT2, TPE1, TALB, TYER, TDRC, TCON, TRCK, TPE2, TPOS, TCOM, TBPM, TENC, TSSE, TIT1, TIT3, TPE4, TKEY, TMOO, TMED, TLAN)
  - [x] Involved people frames (TIPL, TMCL, IPLS) for musician credits
  - [x] Picture frames (APIC) with multiple picture types
  - [x] Syncsafe integer handling, multiple text encodings
  - [x] Extended header support

### Phase 3: Xiph Formats ✅
- [x] Vorbis Comments (xiph.org specification)
- [x] FLAC metadata blocks (xiph.org specification)
  - [x] StreamInfo, VorbisComment, Picture block support
- [x] Ogg container support with CRC validation

### Phase 4: I/O Abstraction ✅
- [x] File system abstraction for testability
- [x] Async file I/O support with cancellation
- [x] Extended metadata: Composer, BPM, AlbumArtist, DiscNumber

### Phase 5: File Writing & Media Properties ✅
- [x] FLAC file write operations with atomic saves
- [x] Media properties (duration, bitrate, sample rate, channels)
- [x] ID3v2 Comment (COMM) frame support

### Phase 6: Extended Metadata & High-Level APIs ✅
- [x] ID3v2 TXXX (user-defined text) frames for custom metadata
- [x] ReplayGain tag support (ID3v2 and Vorbis Comments)
- [x] MusicBrainz ID support (ID3v2 and Vorbis Comments)
- [x] Mp3File high-level API for unified ID3v1/ID3v2 access
- [x] Ogg Vorbis file write operations
- [x] Lyrics (USLT frame) with multi-language support
- [x] UFID (Unique File Identifier) for MusicBrainz Recording IDs
- [x] Extended properties: Conductor, Copyright, Compilation, TotalTracks/TotalDiscs, PerformersRole
- [x] Encoding metadata: EncodedBy, EncoderSettings
- [x] Track info: Grouping, Subtitle, Remixer, InitialKey, Mood, Language
- [x] Release data: MediaType, Barcode, CatalogNumber
- [x] Sort fields: ComposerSort
- [x] Tagging metadata: DateTagged, Description, AmazonId
- [x] Extended MusicBrainz: WorkId, DiscId, ReleaseStatus, ReleaseType, ReleaseCountry
- [x] TagTypes flags enum for tag format identification
- [x] Array properties: Performers[], AlbumArtists[], Composers[], Genres[]
- [x] IPicture interface and Pictures[] property on base Tag class

### Phase 7: RIFF/WAV Support ✅
- [x] RIFF container parsing with chunk navigation
- [x] WAV format chunk (fmt) for audio properties
- [x] RIFF INFO tags (INAM, IART, IPRD, etc.)
- [x] ID3v2 chunk support in WAV files
- [x] WAV file write operations

### Phase 8: AIFF Support ✅
- [x] FORM container parsing (IFF-style, big-endian)
- [x] COMM chunk with 80-bit extended float sample rate
- [x] SSND chunk detection
- [x] ID3 chunk support for metadata
- [x] AIFC (compressed) format detection
- [x] ExtendedFloat utility for 80-bit IEEE 754
- [x] AIFF file write operations with ID3v2 support

### Phase 9: Stability & Preservation ✅
- [x] FLAC metadata block preservation (SEEKTABLE, APPLICATION blocks)
- [x] WAV chunk preservation (fact, cue, smpl and other chunks)
- [x] All formats preserve unknown/unrecognized data during round-trip

### Phase 10: Ogg Opus ✅
- [x] OpusHead packet parsing (RFC 7845)
- [x] R128 gain tags (output gain, album gain, track gain)
- [x] Multi-stream support (mapping families 0, 1, 255)
- [x] Stream/coupled count validation per RFC 7845

### Phase 11: MP4/M4A ✅
- [x] ISO 14496-12 box parsing (ftyp, moov, mdat, etc.)
- [x] iTunes-style metadata atoms (ilst with ©nam, ©ART, etc.)
- [x] AAC audio properties via esds parsing
- [x] ALAC audio properties via alac magic cookie
- [x] Album art (covr atom) with JPEG/PNG detection
- [x] MusicBrainz IDs and ReplayGain via freeform atoms
- [x] Atomic file writing with mdat relocation
- [x] MediaFile factory integration for format auto-detection

### Phase 12: DSF & APE Tags ✅
- [x] DSF (DSD Stream File) format for high-resolution DSD audio
  - [x] DSD64/128/256/512 sample rate detection
  - [x] DSD/fmt/data chunk parsing
  - [x] ID3v2 metadata support at end of file
  - [x] Channel type detection (Mono, Stereo, 5.1 surround)
- [x] APE Tag v2 support for tagging flexibility
  - [x] Text, Binary, and External item types
  - [x] Full read/write round-trip support
  - [x] Standard and extended metadata mappings

### Phase 13: Extended Format Support ✅
- [x] DFF (DSDIFF) format for high-resolution DSD audio
  - [x] FRM8/DSD chunk parsing
  - [x] DSD64/128/256/512/1024 sample rate detection
  - [x] ID3v2 metadata at end of file
  - [x] Channel type detection
- [x] Ogg FLAC format
  - [x] Ogg container with FLAC stream parsing
  - [x] STREAMINFO extraction (sample rate, channels, bits per sample)
  - [x] Vorbis Comment support with full read/write
- [x] WavPack (.wv) format
  - [x] Block header parsing with flags
  - [x] Sample rate table lookup (15 standard rates)
  - [x] APE tag support for metadata
- [x] Monkey's Audio (.ape) format
  - [x] Both old (≤3.97) and new (≥3.98) format versions
  - [x] STREAMINFO extraction
  - [x] APE tag support for metadata
- [x] ASF/WMA format
  - [x] Header Object parsing with child objects
  - [x] Content Description (Title, Artist, Copyright)
  - [x] Extended Content Description (Album, Year, Genre, Track, etc.)
  - [x] Stream Properties for audio properties (bitrate, sample rate, channels)
  - [x] File Properties for duration
  - [x] Full read/write support with atomic saves
- [x] Musepack format
  - [x] SV7 and SV8 stream version detection
  - [x] Audio properties (sample rate, channels, duration)
  - [x] APE tag support for metadata

### Future
- [ ] Speex format (Ogg container support exists)
- [ ] TrueAudio format

## Documentation

- [Migration Guide](docs/MIGRATION-FROM-TAGLIB.md) - Migrating from TagLib#
- [Architecture Overview](docs/ARCHITECTURE.md) - Design principles and allocation behavior
- [Core Types Reference](docs/CORE-TYPES.md) - Complete API documentation
- [Building Guide](docs/BUILDING.md) - Build instructions and requirements
- [Examples](examples/) - Working code samples

## Contributing

Contributions are welcome! Please read the [contributing guidelines](CONTRIBUTING.md) before submitting PRs.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
