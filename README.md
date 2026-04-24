# Phonematic

**The Enterprise-Grade Audio Transcription Solution You Didn't Know You Needed™**

---

## Overview

Congratulations on your selection of Phonematic, a revolutionary audio-to-text conversion system that leverages cutting-edge artificial intelligence to transform your spoken word into searchable, indexable text assets.

Unlike our competitors' solutions-which we are contractually obligated not to name but rhyme with "Schmotter" and "Schmamazon"-Phonematic runs entirely on your local hardware. Your audio files never leave your premises. Your IT security team can finally sleep at night.

## Features

- **Audio Transcription** - Converts audio files to text using Whisper AI. Yes, it actually works.
- **AI-Powered Search** - Query your transcriptions using natural language. Ask questions. Get answers. Revolutionary.
- **Local Processing** - All processing occurs on your machine. No cloud. No subscriptions. No "we updated our privacy policy" emails.
- **Cross-Platform** - Runs on Windows, Linux, and macOS. We believe in equality of suffering.
- **SQLite Database** - Your transcriptions stored locally in a format that will outlive most startups.

## System Requirements

| Component | Minimum | Recommended |
|-----------|---------|-------------|
| RAM | 8 GB | 16 GB |
| Storage | 500 MB | 2 GB (models aren't small) |
| OS | Windows 10, Ubuntu 20.04, macOS 12 | Latest stable release |
| CPU | Any x64 processor manufactured this decade | Something with AVX2 support |
| Display | Yes | Also yes |

## Installation

### Windows

Download the MSI installer from the [Releases](https://github.com/chris17453/phonematic/releases) page. Double-click. Follow prompts. We've made it simple enough that even management can do it.

### Linux

```bash
# Download the latest release
wget https://github.com/chris17453/phonematic/releases/latest/download/Phonematic-linux-x64.tar.gz

# Extract
tar -xzf Phonematic-linux-x64.tar.gz

# Run
./Phonematic
```

You're using Linux. We trust you can figure out the rest.

### Building from Source

For those who trust no one (we respect that):

```bash
git clone https://github.com/chris17453/phonematic.git
cd phonematic
dotnet build -c Release
dotnet run --project src/Phonematic/Phonematic.csproj
```

Requires .NET 10 SDK. Yes, we're living in the future.

## Usage

1. **Launch** the application
2. **Import** audio files (MP3, WAV, M4A, and other formats your podcast app generates)
3. **Transcribe** using your choice of Whisper model (larger = more accurate = more waiting)
4. **Search** your transcriptions using the AI Search tab
5. **Export** results as needed

It's not rocket science. It's audio science.

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                    Phonematic                 │
├──────────────┬──────────────┬───────────────────────┤
│   Avalonia   │   Whisper    │      LLamaSharp       │
│     UI       │    .NET      │    (Local LLM)        │
├──────────────┴──────────────┴───────────────────────┤
│                 SQLite Database                     │
├─────────────────────────────────────────────────────┤
│              Your Local File System                 │
│         (Where your data stays. Forever.)           │
└─────────────────────────────────────────────────────┘
```

## FAQ

**Q: Why not just use [Cloud Service X]?**  
A: Because you read the terms of service, didn't you? ...You didn't? We recommend reading them. With a lawyer present.

**Q: How accurate is the transcription?**  
A: Depends on your audio quality and chosen model. Garbage in, garbage out. This is a universal constant.

**Q: Can I transcribe in languages other than English?**  
A: Yes. Whisper supports multiple languages. Your mileage may vary based on your accent's deviation from the training data.

**Q: Why is the first transcription slow?**  
A: The AI models need to be loaded into memory. Subsequent transcriptions are faster. Patience is a virtue.

**Q: My transcription has errors.**  
A: See "Garbage in, garbage out" above. Also, AI is not perfect. Neither are humans. We're all doing our best here.

## Contributing

Pull requests are welcome. Please ensure your code:
- Compiles
- Passes existing tests
- Doesn't introduce security vulnerabilities
- Is not generated entirely by AI without review (ironic, we know)

## License

MIT License. Use it, modify it, sell it, tattoo it on your forearm. We don't care. See [LICENSE](LICENSE) for the legal boilerplate.

---

*Phonematic: Because your audio files aren't going to transcribe themselves.*

*Built with caffeine and mass quantities of reasonable expectations*
