# Extension-esque-MonoGameLibrary
A modular, dependency-injected game framework built on **MonoGame**. Designed for maintainability, testability, and platform abstraction.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)

The MonoGameLibrary provides a clean separation of concerns with Core (platform-agnostic), Adapters (MonoGame bindings), and Extensions (optional modules). It features explicit dependency injection, standardized lifecycle interfaces, and a flexible host system for building maintainable and testable games. 

## Features

### Core (Platform-Agnostic)
- Interfaces for essential services: `IContentService`, `IThreadPool`, `ILogger`
- Base lifecycle contracts for consistent system behavior

### Adapters (Bindings)
- `ContentService`: Streamlined content loading
- `MonoGameAdapter`: Bridge between library systems and MonoGame’s `Game` class
- `GumService`: Integration with the Gum UI framework

### Extensions (Optional Modules)
- **Audio**: Sound effect and music management
- **Input**: Unified input handling across devices
- **Scenes**: Scene graph and state management
- **Graphics**: Sprite rendering, texture atlases, tilemaps
- **...**

### Infrastructure
- **Thread-Safe Host**: `GameHost` with operation counting and graceful shutdown
- **Centralized Error Handling**: Single `OnError` callback for module exceptions
- **Composition Root**: Clear setup flow for wiring dependencies

## Getting Started

### Prerequisites
- [.NET 8.0 SDK or higher](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [MonoGame 3.8+](https://www.monogame.net/)

### Installation

1. Clone the repository (choose one mirror)
- GitHub
   ```bash
   git clone https://github.com/Mushroomcowisheggs/Extension-esque-MonoGameLibrary.git
   ```
- Codeberg
   ```bash
   git clone https://codeberg.org/Mushroomcowisheggs/Extension-esque-MonoGameLibrary.git
   ```
2. Add a reference to `MonoGameLibrary.csproj` in your game project.

### Basic Usage (Composition Root)
```csharp
using MonoGameLibrary.Core.Hosting;
using MonoGameLibrary.Adapters.MonoGame;
using MonoGameLibrary.Extensions;

var builder = new GameBuilder();
builder.UseDefaultServices();
builder.UseAudio();
builder.UseInput();

var serviceContent = new ContentService(Content);
builder.RegisterService<IContentService>(serviceContent);

var host = builder.Build();
host.OnError = delegate(exception, context) { LogError(exception, context); }

// Add modules
host.AddModule(new SceneModule(serviceScene));
// ...
```

For a full example, see the [Samples](https://github.com/mushroomcowisheggs/Extension-esque-MonoGameLibrary.Samples).

## Project Structure

```
MonoGameLibrary/
├── Core/                 # Platform-agnostic interfaces and base classes
├── Adapters/             # MonoGame-specific implementations
│   ├── MonoGame/         # Content, Input, Audio, Render context
│   ├── Gum/              # Gum UI integration
│   └── ...
├── Extensions/           # Optional modules
│   ├── Audio/
│   ├── Input/
│   ├── Scenes/
│   ├── Graphics/         # Sprite, Atlas, Tilemap
│   └── ...
├── Utilities/            # Convenience utilities for game application
│   └── ...
└── Samples/
    ├── DungeonSlime/     # Complete game demonstrating the framework
    └── ...
```

---

## Why choose this library?
It shines when you need structure without bloat. Unlike all-in-one frameworks, it avoids “magic”—every system is visible and replaceable. It’s ideal for developers who want to own their architecture and iterate safely.

### Alternatives & Comparison

| Solution | Best For | Trade-offs |
|----------|----------|------------|
| **Raw MonoGame** | Quick prototypes, maximum control | No built-in architecture; scalability requires custom scaffolding |
| **MonoGame.Extended** | Rich 2D features (physics, particles) | Larger API surface; less emphasis on DI/lifecycle patterns |
| **Stride/A MonoGame Fork** | Cutting-edge graphics, editor workflows | Steeper learning curve; heavier runtime footprint |
| **This Library** | Maintainable, testable 2D games | Requires understanding DI concepts; fewer out-of-the-box features |

## Why might you NOT use this Library?

This library is purpose-built for projects where **long-term maintainability** and **clean architecture** matter. For certain scenarios, especially rapid prototyping or small-scale games, it may introduce unnecessary overhead.

| Scenario | Why You Might Skip This Library |
|----------|----------------------------------|
| **Game Jams & 48-hour prototypes** | You need to ship fast with minimal ceremony. Raw MonoGame or a simple game loop gives you more velocity. |
| **Tiny games (< 1000 lines)** | The DI setup, module system, and lifecycle interfaces add friction for trivial projects. |
| **Learning MonoGame for the first time** | Start with raw MonoGame to understand the basics before layering abstractions. |
| **One-off experimental projects** | If you won't revisit the code later, the architectural benefits aren't worth the upfront cost. |

In short: if your goal is to **make something quickly and move on**, this library is probably overkill. But if you plan to grow, refactor, or collaborate on a game over months or years, the investment pays off.

---

## Documentation

Detailed design principles are documented in [`Guidance-en_US.md`](Guidance-en_US.md) and [`Guidance-zh_CN.md`](Guidance-zh_CN.md).  
The [API Reference](docs/API.md) might be developed in the future.

## Contributing

We welcome the community to build upon this work, but please understand that we cannot provide support or accept contributions at this time. If you are interested in using or extending this codebase, you are strongly encouraged to develop on your own copy. 
We hope that this project serves as a solid foundation for your own extension-esque game development efforts. 

## License

This project is licensed under the MIT License – see the [LICENSE](LICENSE.txt) file for details.

## Acknowledgments

- Mainly built on [MonoGame](https://www.monogame.net/) and inspired by its samples [MonoGame.Samples](https://github.com/MonoGame/MonoGame.Samples)
- Inspired by clean architecture and dependency injection patterns

## Third-Party Licenses

This project uses the following open-source frameworks:

- **MonoGame** (https://github.com/MonoGame/MonoGame) – Licensed under the MIT and Microsoft Public License (MS-PL).
- **Gum UI** (https://github.com/vchelaru/Gum) – Licensed under the MIT License.