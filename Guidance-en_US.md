# MonoGameLibrary.Core Module Development Specification

This document provides comprehensive guidance for developers building **extension modules** and **game applications** on top of `MonoGameLibrary.Core`. Let's first clarify the key layers:

- **Core**: The base library (`MonoGameLibrary.Core`) — provides platform‑agnostic utilities, timing, logging, and common data structures **related to host infrastructure**. It contains **no** business logic, **no** auxiliary tools and **no** platform‑specific code.
- **Adapters**: The adapter layer (`MonoGameLibrary.Adapters.MonoGame`, `MonoGameLibrary.Adapters.Gum`, etc.) — wraps external platforms and third‑party libraries, converting their native APIs into the interfaces **defined by Extensions**. This is the **only** layer that may tightly couple with MonoGame, Gum, or other external dependencies.
- **Extensions**: Optional feature modules (Audio, Scenes, Input, Networking, UI, etc.) — **must** follow all principles in this specification. Extensions define all business interfaces (contracts) and contain platform‑independent domain logic. They may contain pure C# implementations that do not depend on any external platform or library. If an implementation depends on an external library (e.g., MonoGame.Extended), that implementation must be placed in the Adapters layer, while the interface remains in Extensions.
- **Game Application**: The final game project (e.g., `Game1.cs`) — responsible for the composition root, service registration, and lifecycle driving. It references both Extensions (for programming) and the desired Adapters (for runtime injection).

**Architectural Layer Note**: The `Adapters` layer is the only boundary permitted to tightly couple with external platforms or third-party libraries (including MonoGame runtime, Gum, Flecs.NET, etc.). It is responsible for converting their native APIs (whether instance methods or static global state) into specification-compliant interfaces (e.g., `IContentService`, `IAudioService`, `IECSWorldService`). The Extension and Game layers **must** consume only these interfaces and must never reference external platform APIs directly.

**Important distinction**: If a feature implementation does **not** depend on any external platform or library (i.e., it is pure C#), it must reside in the `Extensions` layer as an optional module, and its interface is also defined there. The `Adapters` layer is **not** a place for "default" or "native" implementations – its sole purpose is to **adapt** external dependencies. For example, a self‑contained ECS implementation belongs in `Extensions`, while a wrapper for MonoGame.Extended's ECS belongs in `Adapters/MonoGame/ECS`.

**Physical dependency rule (enforced at project level)**:
```
Game → Extensions → Core
Game → Adapters → Extensions → Core   (Adapters always point to Extensions)
Extensions → Core                      (Extensions never point to Adapters)
```

This rule guarantees that Extensions (the business layer) remain completely unaware of Adapters (the infrastructure), enabling easy backend swapping and testability.

The principles below are annotated with the roles to which they apply.

**All code comments must be written in English.**

---

## 1. Explicit Dependencies Principle

**Roles**: [Extension] must follow; [Core] provide support; [Game] uses at composition root.

### Detailed Rules

1. **All external services must be passed via the constructor.** This includes but is not limited to:
   - `IContentService` (content loading)
   - `ILogger` (logging)
   - `IThreadPool` (background tasks)
   - `ICancellationService` (cancellation tokens)
   - `ILoadingProgress` (loading progress)
   - `IObjectPoolFactory` (object pool factory)
   - Any custom service (e.g., `ISceneService`)
2. **Do not use `ServiceRegistry.Get<T>()` or `GameHost.Services` inside a module.** Those are composition-root tools, not module tools.
3. **Do not use static methods or singletons to obtain dependencies in Extension or Game layers** (e.g., `ContentManager.Load`, static factories on `SoundEffect`). 
   - **For managed asset resources (Textures, Sounds, Models, etc.)**: Must be acquired through `IContentService`. 
   - **Special exemption for the Adapter Layer (`MonoGameLibrary.Adapters.MonoGame`)**: Adapters are permitted to wrap MonoGame's **runtime global state** (e.g., `MediaPlayer.Volume`, `SoundEffect.MasterVolume`, or `GraphicsDevice` static properties). However, this access must be strictly encapsulated behind a business-oriented service interface (e.g., `IAudioService`) and **must never** be exposed to or used by layers above the Adapter (Extensions or Game Applications). And static constructions must remain hidden inside the implementation of `IContentService`. 
4. **Constructor parameters must be null-checked**, and an `ArgumentNullException` must be thrown when a parameter is null.
5. **For optional dependencies, use `Optional<T>`** (see Principle 7).

### Why This Design?

- **Testability**: Explicit dependencies enable passing mocks in unit tests.
- **Maintainability**: Dependencies are clear at a glance; changes to one do not ripple unexpectedly.
- **Decoupling**: A module does not care where a dependency comes from, only what contract it fulfills.

### Physical Project References (Critical)

To maintain a clean extension‑centric architecture, the following project‑reference rules are **mandatory**:

- `Extensions` may reference **only** `Core`.
- `Adapters` may reference `Extensions` (and `Core` transitively) **and** any required external libraries.
- `Game` (the entry project) may reference both `Extensions` and any `Adapters` it needs.
- **Under no circumstances** may `Extensions` reference any `Adapters` project. If you find a `using` directive pointing to an Adapter namespace inside an Extension file, that file belongs in Adapters.

This rule guarantees that the business layer (Extensions) remains completely unaware of the infrastructure (Adapters), enabling easy backend swapping and testability.

### Example

```csharp
// Correct
public class AudioModule : ILoadable, IUpdateable, IDisposable {
    private readonly IContentService _serviceContent;
    private readonly ILogger _logger;
    private readonly Optional<IProfiler> _profiler;
    private SoundEffect _effectClickSound;
    private bool _flagDisposed;

    public AudioModule(IContentService serviceContent, ILogger logger, Optional<IProfiler> profiler) {
        if (serviceContent == null) {
            throw new ArgumentNullException(nameof(serviceContent));
        }
        if (logger == null) {
            throw new ArgumentNullException(nameof(logger));
        }
        _serviceContent = serviceContent;
        _logger = logger;
        _profiler = profiler;
    }

    public void LoadContent() {
        if (_profiler.HasValue) {
            using (var measure = _profiler.Value.BeginMeasure("Audio.Load")) {
                _effectClickSound = _serviceContent.Load<SoundEffect>("click");
                _logger.Info("Audio loaded successfully.");
            }
        } else {
            _effectClickSound = _serviceContent.Load<SoundEffect>("click");
            _logger.Info("Audio loaded successfully.");
        }
    }

    public void Update(FrameTime time) {
        // Update audio playback state if needed.
    }

    public void Dispose() {
        if (_flagDisposed) { return; }
        if (_effectClickSound != null) {
            _effectClickSound.Dispose();
            _effectClickSound = null;
        }
        _flagDisposed = true;
        GC.SuppressFinalize(this);
    }
}

// Wrong
public class BadAudioModule : ILoadable {
    public void LoadContent() {
        // Violates explicit dependency: using service locator
        var serviceContent = ServiceRegistry.Get<IContentService>();
        // Violates: using static method
        var effectSound = SoundEffect.FromStream(/* ... */);
    }
}
```

---

## 2. Lifecycle Contract Principle

**Roles**: [Extension] must follow; [Core] defines interfaces; [Game] drives the host.

### Detailed Rules

1. **Implement the appropriate interfaces**:
   - `ILoadable`: for one-time content loading (assets, configuration). Called during `GameHost.Initialize`.
   - `IUpdateable`: for per-frame update logic (physics, AI, input processing, etc.). Called during `GameHost.Update`.
   - `IDrawable`: for per-frame rendering. Called during `GameHost.Draw`.

2. **An `Order` property (int) must be provided.** Smaller values execute earlier. A default of `0` is suggested, but adjust as needed (e.g., an input module should use a negative value).
3. **`Enabled` / `Visible` properties (bool) must be provided.** The host checks these flags before calling `Update` / `Draw`. They must support dynamic changes at runtime.
4. **Do not perform time-consuming operations in `Update` or `Draw`** (e.g., disk I/O, network requests). Use `IThreadPool` for asynchronous processing.
5. **Keep methods short**, performing only the minimal necessary work each cycle. Exceeding 16ms will cause frame drops.

### Lifecycle Execution Order

- `Initialize` phase: Calls `ILoadable.LoadContent` on all modules in the order they were added via `AddModule`.
- `Update` phase: Calls `IUpdateable.Update` on all modules in ascending `Order`, skipping any with `Enabled == false`.
- `Draw` phase: Calls `IDrawable.Draw` on all modules in ascending `Order`, skipping any with `Visible == false`.

### Module Placement Guideline

- **Modules that only forward calls** to a service (e.g., `SceneModule` calls `ISceneService.Update`) and contain no platform‑specific code may stay in Extensions.
- **Modules that directly invoke platform APIs** (e.g., `AudioModule` calls `AudioService.PlaySoundEffect`, which uses MonoGame types) **must reside in Adapters**, because they are part of the implementation layer.
- If a module in Extensions ever requires a `using` directive to an Adapters namespace, it is misplaced and must be moved to Adapters.

### Example

```csharp
// A physics module that must run before rendering
public class PhysicsModule : IUpdateable, IDrawable {
    private int _order;
    private bool _flagEnabled;
    private bool _flagVisible;

    public PhysicsModule() {
        _order = 0;
        _flagEnabled = true;
        _flagVisible = false; // Physics does not draw anything.
    }

    public int Order {
        get { return _order; }
    }

    public bool Enabled {
        get { return _flagEnabled; }
        set { _flagEnabled = value; }
    }

    public bool Visible {
        get { return _flagVisible; }
        set { _flagVisible = value; }
    }

    public void Update(FrameTime time) {
        if (!Enabled) { return; }
        // Physics simulation using time.DeltaTime.
    }

    public void Draw(FrameTime time, IRenderContext context) {
        // Not used, but required by interface.
    }
}

// A debug overlay that draws after everything else
public class DebugOverlay : IDrawable {
    private int _order;
    private bool _flagVisible;

    public DebugOverlay() {
        _order = 100; // Last
        _flagVisible = true;
    }

    public int Order {
        get { return _order; }
    }

    public bool Visible {
        get { return _flagVisible; }
        set { _flagVisible = value; }
    }

    public void Draw(FrameTime time, IRenderContext context) {
        // Draw FPS, etc.
    }
}
```

---

## 3. Exception Isolation & Reporting Principle

**Roles**: [Extension] must follow; [Core] provides isolation; [Game] configures error handling.

### Detailed Rules

1. **Exceptions thrown inside a module will first be caught by the host, reported to the application layer via the `OnError` callback, and then re-thrown.** This means a module must not swallow exceptions, nor should it expect `OnError` to "fix" the problem.
2. **If a module must clean up resources (release locks, close files), it must use `try-finally` or `using` statements**, not a `try-catch` that swallows the exception.
3. **Exceptions in asynchronous operations (via `IThreadPool`)** must be caught inside the task and reported through some channel (e.g., `OnError` or logging); otherwise they may be silently lost. Use `Task.ContinueWith` or a `try-catch` inside the task body.
4. **Error-handling callbacks are the application layer's responsibility**; a module must not assume one has been set. If `OnError` is not set, the host will throw the exception directly — this is expected behavior.
5. **A module should not independently report exceptions** (calling `ILogger` is acceptable, but a module must not rely on `OnError` for logging). Logging and error reporting are separate concerns.

### Correct Approach

```csharp
public void Update(FrameTime time) {
    // If an exception occurs, we want it to bubble up to the host.
    // We do not catch it unless we need to perform cleanup.
    // For cleanup, use try-finally.
    try {
        // Critical operation
    } finally {
        // Release temporary resources
    }
}
```

### Wrong Approach

```csharp
public void Update(FrameTime time) {
    try {
        // Some operation
    } catch (Exception exception) {
        // Swallowing: The host will never know.
        // This is a severe mistake.
    }
}
```

### Asynchronous Exception Handling Example

```csharp
public class AsyncWorker : IUpdateable {
    private readonly IThreadPool _pool;
    private readonly ILogger _logger;
    private int _pending;

    public AsyncWorker(IThreadPool pool, ILogger logger) {
        if (pool == null) {
            throw new ArgumentNullException(nameof(pool));
        }
        if (logger == null) {
            throw new ArgumentNullException(nameof(logger));
        }
        _pool = pool;
        _logger = logger;
        _pending = 0;
    }

    public int Order {
        get { return 0; }
    }

    public bool Enabled {
        get { return true; }
        set { /* not used */ }
    }

    public void Update(FrameTime time) {
        if (_pending > 0) { return; }
        _pending += 1;
        _pool.RunAsync(delegate() {
            try {
                // Potentially throws.
                DoWork();
            } catch (Exception exception) {
                // Log it; we cannot throw here because it is a background thread.
                _logger.Error("Async work failed", exception);
                // Optionally, notify the host through a shared error channel.
            } finally {
                Interlocked.Decrement(ref _pending);
            }
        }, "AsyncWork");
    }

    private void DoWork() { /* ... */ }
}
```

---

## 4. Thread Safety & State Management Principle

**Roles**: [Extension] must follow; [Core] provides thread-safe host; [Game] must not bypass the host to directly manipulate modules.

### Detailed Rules

1. **Assume `Update` and `Draw` may be invoked on different threads** (although MonoGame typically uses the main thread, Core does not guarantee this). Therefore, any shared state within a module must be protected with synchronization primitives.
2. **When using `IThreadPool` for background tasks**, be mindful of race conditions and memory visibility. Use `lock`, `Interlocked`, `ConcurrentDictionary`, etc.
3. **Avoid blocking waits in `Update`/`Draw`** (e.g., `Thread.Sleep`, `WaitHandle.WaitOne`). This will stall the main loop.
4. **If a module exposes mutable properties (e.g., `Volume`), ensure their implementations are thread-safe** (using `Interlocked` or locks).
5. **Design for reentrancy**: for instance, `Update` might fire an event whose handler invokes another method on the same module.

### Example

```csharp
public class SafeCounter : IUpdateable {
    private int _counter;
    private readonly object _lock = new object();

    public void Increment() {
        lock (_lock) {
            _counter += 1;
        }
    }

    public int Value {
        get {
            lock (_lock) {
                return _counter;
            }
        }
    }

    public int Order {
        get { return 0; }
    }

    public bool Enabled {
        get { return true; }
        set { /* not used */ }
    }

    // Called from main thread or thread pool
    public void Update(FrameTime time) {
        // Use Interlocked for simple operations.
        Interlocked.Increment(ref _counter);
    }
}
```

### Avoiding Blocking

```csharp
public void Update(FrameTime time) {
    // WRONG: Blocks the thread.
    // Thread.Sleep(10);

    // Correct: Use async/await with RunAsync.
    async Task DelayedUpdateAsync() {
        await Task.Delay(10);
        // Update result.
    }
    _pool.RunAsync(DelayedUpdateAsync, "DelayedUpdate");
}
```

---

## 5. Resource Cleanup Principle

**Roles**: [Extension] must follow; [Core] automatically invokes cleanup; [Game] ensures `Dispose` is called.

### Detailed Rules

1. **If a module holds any `IDisposable` resources (including MonoGame `Texture2D`, `SoundEffect`, `SpriteBatch`, etc.), the module must implement `IDisposable`.**
2. **Release all managed and unmanaged resources in `Dispose`**, and set references to `null`.
3. **`Dispose` must be idempotent** (multiple calls must do no harm).
4. **Call `GC.SuppressFinalize(this)`** to avoid unnecessary finalization.
5. **If a module implements `IDisposable`, it should also dispose any child modules** in its own `Dispose` method (if it owns any).
6. **The host will call `Dispose` on each module during shutdown**, but a module must not rely on the host to release its resources, as the host may crash or fail to invoke cleanup.
7. **Do not throw exceptions from `Dispose`**. If an exception occurs, the host will catch it and report it via `OnError`, but it must not prevent other modules from being disposed.

### Example

```csharp
public class TextureModule : ILoadable, IDisposable {
    private Texture2D _texture;
    private bool _flagDisposed;

    public void LoadContent() {
        // _texture = load via IContentService
    }

    public void Dispose() {
        if (_flagDisposed) { return; }
        if (_texture != null) {
            _texture.Dispose();
            _texture = null;
        }
        _flagDisposed = true;
        GC.SuppressFinalize(this);
    }
}
```

### Composite Module (holding other IDisposable modules)

```csharp
public class CompositeModule : IDisposable {
    private readonly AudioModule _moduleAudio;
    private readonly TextureModule _moduleTexture;
    private bool _flagDisposed;

    public CompositeModule(AudioModule moduleAudio, TextureModule moduleTexture) {
        _moduleAudio = moduleAudio;
        _moduleTexture = moduleTexture;
    }

    public void Dispose() {
        if (_flagDisposed) { return; }
        if (_moduleAudio != null) {
            _moduleAudio.Dispose();
        }
        if (_moduleTexture != null) {
            _moduleTexture.Dispose();
        }
        _flagDisposed = true;
        GC.SuppressFinalize(this);
    }
}
```

---

## 6. Module Identity & Ordering Principle

**Roles**: [Extension] must follow; [Core] uses ordering; [Game] dynamically adjusts flags.

### Detailed Rules

1. **`Order` determines execution sequence.** It should be a constant or configuration-based, but must be determined at construction time. Do not change `Order` frequently at runtime, because the host re-sorts the module list before each `Update`/`Draw` (which involves copying the list).
2. **`Enabled` and `Visible` must support dynamic runtime changes.** They are typically used for pause menus, screen transitions, etc.
3. **Modules must not communicate through `Order`.** Ordering is purely for scheduling, not a communication mechanism.
4. **If logical dependencies exist between modules (e.g., Input must update before Physics), establish an explicit partial order through the `Order` property.**
5. **Do not execute complex logic in the getters of `Enabled`/`Visible`**, as they are accessed multiple times per frame.

### Example

```csharp
public class InputModule : IUpdateable {
    private int _order;
    private bool _flagEnabled;

    public InputModule() {
        _order = -100; // Earliest
        _flagEnabled = true;
    }

    public int Order {
        get { return _order; }
    }

    public bool Enabled {
        get { return _flagEnabled; }
        set { _flagEnabled = value; }
    }

    // ...
}

public class PhysicsModule : IUpdateable {
    private int _order;
    private bool _flagEnabled;

    public PhysicsModule() {
        _order = 0; // After input
        _flagEnabled = true;
    }

    public int Order {
        get { return _order; }
    }

    public bool Enabled {
        get { return _flagEnabled; }
        set { _flagEnabled = value; }
    }

    // ...
}

public class RenderModule : IDrawable {
    private int _order;
    private bool _flagVisible;

    public RenderModule() {
        _order = 10; // After other drawables
        _flagVisible = true;
    }

    public int Order {
        get { return _order; }
    }

    public bool Visible {
        get { return _flagVisible; }
        set { _flagVisible = value; }
    }

    // ...
}
```

### Runtime Toggling

```csharp
// In Game Application, you might have a pause state:
public class PauseManager {
    private readonly IEnumerable<IUpdateable> _updateables;
    public void Pause() {
        foreach (var updateable in _updateables) { updateable.Enabled = false; }
    }
    public void Resume() {
        foreach (var updateable in _updateables) { updateable.Enabled = true; }
    }
}
```

---

## 7. Optional Services & `Optional<T>` Usage Principle

**Roles**: [Extension] recommended to follow; [Core] provides `Optional<T>`; [Game] optionally registers.

### Detailed Rules

1. **For non-essential services (e.g., `IProfiler`, `ILoadingProgress`), constructor parameters should use the `Optional<T>` type.**
2. **Check the `HasValue` property before use**; if `false`, skip the related operation.
3. **Do not use `Optional<T>` for required services.** Required services must be passed directly, and an exception must be thrown if they are null.
4. **`Optional<T>` supports implicit conversion from `T` or `null`**, so callers may pass an instance directly or omit the argument (default-constructed as empty).
5. **A module must not store `Optional<T>` as a field and then mutate its state**, because it is a read-only struct.

### Example

```csharp
public class MyModule : IUpdateable {
    private readonly Optional<IProfiler> _profiler;
    private readonly ILogger _logger; // required

    public MyModule(ILogger logger, Optional<IProfiler> profiler) {
        if (logger == null) {
            throw new ArgumentNullException(nameof(logger));
        }
        _logger = logger;
        _profiler = profiler;
    }

    public void Update(FrameTime time) {
        if (_profiler.HasValue) {
            using (var measure = _profiler.Value.BeginMeasure("MyUpdate")) {
                DoWork();
            }
        } else {
            DoWork();
        }
    }

    private void DoWork() { /* ... */ }
}
```

### Usage with GameBuilder

```csharp
var logger = new ConsoleLogger();
var profiler = new MyProfiler();

var builder = new GameBuilder();
builder.UseLogger(logger);
builder.UseProfiler(profiler);
builder.AddModule(new MyModule(logger, profiler));
```

---

## 8. Module Naming & Organization Principle

**Roles**: [Extension] recommended to follow.

### Detailed Rules

1. **Module class names must reflect their functionality**, e.g., `AudioManager`, `SceneStack`, `InputRouter`.
2. **Place modules in namespaces that correspond to their function**, e.g., `MonoGameLibrary.Audio`, `MonoGameLibrary.Scenes`.
3. **Avoid vague suffixes such as `Manager` or `System`** unless the class genuinely manages multiple sub-components.
4. **Each module must reside in its own file**, with the file name matching the class name.
5. **Public API method names must begin with a verb**, e.g., `PlayClipAudio`, `LoadScene`.
6. **All camelCase private fields must start with the headword**, which is typically the final word of the corresponding public API, e.g., `_timeFrame`, `_serviceContent`, `_logger`. This is declarative naming: `variable = category word + qualifier`. It reflects "what this data is".
7. **Local variables also follow headword-first declarative naming**, e.g., `timeFrame`, `serviceContent`, `logger`.
8. **Public fields typically use natural language order**, e.g., `FrameTime`, `IContentService`, `Logger`. This is substitutional naming: `field = object name (or implied context object) + property name`. It reflects "which property of which object this data belongs to", rather than "we define a field and then qualify it".
9. Boolean variables used for identification rather than computation must start with `flag` to clarify their purpose.

---

## 9. Explicit Control Flow Principle

**Roles**: [Extension] must follow.

### Detailed Rules

1. **The C# null-conditional operator (`?.`) and null-coalescing operator (`??`) are forbidden.** All null checks must use explicit `if (x != null)` or `if (x == null)` branches.
2. **The `?? throw new` pattern in constructors is forbidden.** A standalone null-check statement must be used.
3. **Expression-bodied definitions (`=>`) for properties, methods, and read-only members are not allowed.** Full block bodies `{ get { ... } }` or `{ return ...; }` must be used.
4. **All conditional logic must use complete `if-else` blocks.** The ternary operator `? :` can be used in simple assignment scenarios, but `if-else` is recommended for clarity in complex situations. 
5. **Loops and exception handling must use braces `{}`**, even when the body contains only a single statement. 
6. **Use explicit anonymous delegate syntax** (delegate { ... }) instead of lambda expressions (e.g., (x) => x.Foo) for all delegate parameters. 

### Why This Design?

- **Readability**: Explicit control flow makes every logic branch obvious, reducing cognitive load, especially for developers unfamiliar with C# syntactic sugar.
- **Debugging-friendly**: Breakpoints and single-stepping are more intuitive on explicit statements; null-conditional operators may hide unexpected null propagation paths.
- **Consistency**: A uniform style avoids mixing multiple null-checking idioms within the same codebase.
- **Safety**: `?.` can silently return null without any effect in some cases; explicit null checks force the developer to reason about and handle the "no value" scenario.

### Example

```csharp
// Correct: explicit null check and full method bodies
public class SafeModule : ILoadable {
    private readonly IContentService _serviceContent;
    private Texture2D _texture;

    public SafeModule(IContentService serviceContent) {
        if (serviceContent == null) {
            throw new ArgumentNullException(nameof(serviceContent));
        }
        _serviceContent = serviceContent;
    }

    public void LoadContent() {
        _texture = _serviceContent.Load<Texture2D>("hero");
        if (_texture == null) {
            throw new InvalidOperationException("Failed to load texture 'hero'.");
        }
    }

    public void Unload() {
        if (_texture != null) {
            _texture.Dispose();
            _texture = null;
        }
    }

    private int _order;
    public int Order {
        get {
            return _order;
        }
    }

    public bool Enabled {
        get {
            return _texture != null;
        } set {
            if (!value) {
                Unload();
            }
        }
    }
}

// Wrong: uses forbidden syntactic sugar
public class BadSafeModule {
    public void Load(IContentService serviceContent) {
        // Violates: null-coalescing throw
        _serviceContent = serviceContent ?? throw new ArgumentNullException(nameof(serviceContent));
        // Violates: null-conditional and expression-bodied member
        _texture?.Dispose();
    }

    public int Order => 0; // Violates: expression-bodied property
}
```

---

## 10. Module Documentation & Comment Requirements

**Roles**: [Extension] must follow.

- **All public methods, properties, and interfaces must include XML documentation comments** (`///`).
- **Comments must be written in English**, describing purpose, parameters, return values, and exceptions.
- **Internal implementation may use comments to explain complex logic**, but this is not mandatory.

### Example

```csharp
/// <summary>
/// Manages audio playback, including sound effects and background music.
/// </summary>
public class AudioModule : ILoadable, IUpdateable, IDisposable {
    /// <summary>
    /// Gets or sets the master volume (0.0 to 1.0).
    /// </summary>
    public float MasterVolume { get; set; }

    /// <summary>
    /// Plays a sound effect by its asset name.
    /// </summary>
    /// <param name="name">The asset name.</param>
    /// <exception cref="InvalidOperationException">Thrown if the sound has not been loaded.</exception>
    public void PlayClipAudio(string name) { /* ... */ }
}
```

---

## 11. Interface Segregation & Internal Abstraction Principle
**Roles**: [Extension] must follow; [Game] consumes interfaces.

### Detailed Rules
1. **Define a Module Interface**: Every functional module (e.g., `AudioModule`) must define a corresponding public business interface (e.g., `IAudioService`). **This interface must be placed in the Extensions project**, because it represents a business capability. The interface should inherit Core lifecycle interfaces (`ILoadable`, `IUpdateable`) as needed.
2. **Program to Interfaces Internally**: Within a module, dependencies between sub-components must reference abstractions (interfaces) rather than concrete implementations. This reduces coupling and facilitates refactoring.
3. **Visibility Strategy**: Concrete implementation classes (which reside in Adapters) should generally be `public` to allow registration with `GameBuilder` across assemblies. However, internal logic should remain hidden behind the interface. If using `internal` implementations, provide a public factory method returning the interface type.
4. **Consumer Flexibility**: The Game Application may consume modules via their interfaces for better decoupling, or use concrete classes if direct access to specific features is required.

> **Note**: This principle extends the classic Interface Segregation Principle (ISP) by requiring modules to define their own contracts and rely on abstractions internally. 
> **Recommended Implementation Pattern**: To achieve clean separation of concerns and facilitate future platform adaptation, it is recommended to separate the service logic from the lifecycle module. The service implements the business interface and contains all platform‑specific logic (placed in Adapters); the module implements `IUpdateable`/`IDrawable` and forwards calls to the service (also placed in Adapters, unless it is pure forwarding with no platform code, in which case it may stay in Extensions). This pattern ensures that the business interface (in Extensions) remains stable while implementations can be swapped.

### Example
```csharp
// Public Contract (in MonoGameLibrary.Extensions.Audio)
public interface IAudioService : IUpdateable {
    void PlayClipAudio(string nameAsset);
}

// Public Implementation (in MonoGameLibrary.Adapters.MonoGame.Audio)
public class AudioModule : IAudioService, ILoadable, IDisposable {
    private readonly IContentService _serviceContent;
    public AudioModule(IContentService serviceContent) { /* ... */ }
    public void PlayClipAudio(string nameAsset) { /* ... */ }
}

// Alternative: Factory Pattern (if implementation must be internal)
public static class AudioFactory {
    public static IAudioService Create(IContentService serviceContent) {
        return new AudioModule(serviceContent);
    }
}
```

## 12. Event-Driven Communication Principle
**Roles**: [Extension] may implement; [Game] orchestrates cross-module events.

### Detailed Rules
1. **Publish State Changes**: Modules may expose events (e.g., `OnSceneLoaded`, `ScoreChanged`) to notify external systems of internal state changes. Use standard .NET `event` keywords.
2. **Minimize Direct Notification References**: Modules **should avoid** holding direct references to other modules specifically for **cross-module notifications**. When a module requires functionality from another module, that dependency must be explicit (via constructor injection). Event-driven communication is the recommended pattern for **broadcasting state changes** to unknown subscribers.
3. **Performance Boundaries**: Events are intended for **low-frequency, logical operations** (state changes, UI updates). **Do not** use events for high-frequency operations within the `Update` or `Draw` loops (e.g., per-frame physics collisions), as delegate invocation overhead can impact performance.
4. **No Global Event Bus**: Core and Extensions must not implement a global "Event Bus" or "Message Broker." If such complexity is needed, it must be implemented at the Game Application level to avoid hidden dependencies.

### Example
```csharp
// In a Scene Module
public class SceneLoadedEventArgs : EventArgs {
    public string SceneName { get; set; }
}
public interface ISceneService {
    event EventHandler<SceneLoadedEventArgs> SceneLoaded;
}

// In Game Application (Composition Root)
public class Game1 {
    public Game1(ISceneService scenes, IUIService ui) {
        // Wire modules together at the highest level
        // Correct: use delegate
        scenes.SceneLoaded += delegate(object scene, SceneLoadedEventArgs argumentsEvent) {
            ui.ShowLoadingScreen(false);
        };
    }
}

// Another correct approach: scenes.SceneLoaded += OnSceneLoaded; private void OnSceneLoaded(object scene, SceneLoadedEventArgs argumentsEvent) { ui.ShowLoadingScreen(false); }
// Wrong: scenes.SceneLoaded += (scene, argumentsEvent) => ui.ShowLoadingScreen(false);
```

## 13. Performance-Oriented Static Dispatch Principle
**Roles**: [Core] and [Extension] must provide dual APIs for performance-critical paths.

### Detailed Rules
1. **Dual-API Strategy**: Provide both a general-purpose interface-based API (for flexibility) and a high-performance API (for speed). Name them distinctly (e.g., `Draw` vs `DrawFast` or use separate static classes) to avoid ambiguity.
2. **High-Performance Constructs**: For hot paths (rendering, particle systems):
   - Use `static` methods or `static` classes for stateless operations.
   - Use `ref struct`, `readonly ref struct`, or `in` parameters to pass **large structs** (e.g., `Matrix`, `TransformData`) by reference, avoiding heap allocation and copying.
   - **Do not use `in` with reference types** (like `SpriteBatch` or `Texture2D`), as it offers no benefit and adds indirection.
   - Use generics with constraints to enable JIT inlining and avoid virtual dispatch.
3. **Avoid Allocation**: Prohibit unnecessary boxing, closure allocations, or delegate creations inside tight loops.
4. **Explicit Choice**: The Game Application developer is responsible for opting into high-performance APIs when profiling indicates a bottleneck.

### Example
```csharp
// General API (Virtual Dispatch - Flexible)
public interface ISprite {
    void Draw(SpriteBatch batch);
}

// High-Performance API (Static Dispatch - Fast)
public readonly ref struct SpriteData {
    public readonly Texture2D Texture;
    public readonly Rectangle SourceRectangle;
    // Ref struct cannot escape to heap
}

public static class SpriteRenderer {
    // Correct: Passing SpriteBatch by value (it's a class).
    // Correct: Passing SpriteData by reference (it's a large struct).
    public static void Draw(SpriteBatch batch, in SpriteData data, Vector2 position) {
        batch.Draw(data.Texture, position, data.SourceRectangle, Color.White);
    }
    
    // Example of using 'in' for a large built-in struct
    public static void DrawWithTransform(SpriteBatch batch, in SpriteData data, in Matrix transform) {
        // Matrix is a struct; 'in' prevents copying
    }
}

// Usage in Game Loop
var data = new SpriteData { Texture = tex, SourceRectangle = rectangle };
SpriteRenderer.Draw(batchSprite, in data, position); // Zero allocation for data
```

---

## 14. Compensation Pattern for Resource Cleanup

**Physical Location**: `MonoGameLibrary.Utilities`

**Design Target**: Game application code only.

### Detailed Rules

1. **Framework modules (IModule implementations) must use explicit cleanup in Dispose**. All resource releases, event unsubscriptions, and state restorations must be explicitly written in the Dispose method.
2. **Game application code may use CompensationStack** to reduce boilerplate cleanup code.
3. **Compensation actions are executed in LIFO order**, matching the reverse order of resource allocation.
4. **Do not use CompensationStack in framework modules**. Framework modules must remain auditable and predictable.
5. **Utilities project does not reference Extensions**. This ensures framework modules cannot accidentally access convenience tools.

### Rationale

This pattern is adapted from the concept of "Compensation" in dynamic composition calculus. It acknowledges that not all operations are mathematically reversible. Instead of requiring exact reversibility, CompensationStack provides a mechanism for registering explicit compensation actions that bring the system to a clean state.

### Example: Framework Module (Explicit)

```csharp
public class AudioModule : IModule, IDisposable {
    private IAudioService _serviceAudio;
    private float _volumeBefore;
    private bool _flagDisposed = false;

    public void Register(GameBuilder builder) {
        _serviceAudio = new AudioService();
        builder.RegisterService<IAudioService>(_serviceAudio);
        _volumeBefore = _serviceAudio.SongVolume;
        _serviceAudio.SongVolume = 1.0f;
    }

    public void Dispose() {
        if (_flagDisposed) {
            return;
        }
        _serviceAudio.SongVolume = _volumeBefore;
        IDisposable disposable = _serviceAudio as IDisposable;
        if (disposable != null) {
            disposable.Dispose();
        }
        _serviceAudio = null;
        _flagDisposed = true;
        GC.SuppressFinalize(this);
    }
}
```

### Example: Game Application (Using CompensationStack)

```csharp
public class GameScene : Scene {
    private readonly CompensationStack _compensation;

    public GameScene() n{
        _compensation = new CompensationStack();
    }

    public override void LoadContent() {
        ITexture texture = ContentService.Load<ITexture>("hero");
        _compensation.CompensateDispose(texture);

        slider.ValueChanged += OnSliderChanged;
        _compensation.Compensate(delegate() {
            slider.ValueChanged -= OnSliderChanged;
        });

        _serviceUI.AddToRoot(panel);
        _compensation.Compensate(delegate() {
            _serviceUI.RemoveFromRoot(panel);
        });
    }

    protected override void Dispose(bool flagDisposing) {
        if (flagDisposing) {
            _compensation.Dispose();
        }
        base.Dispose(flagDisposing);
    }
}
```

### When Not to Use CompensationStack

- In any class that implements `IModule` (framework modules must remain explicit).
- In performance-critical paths where stack overhead is unacceptable.
- When cleanup logic is simple enough that explicit Dispose code is clearer.

### Verification Utilities

`ResourceLeakDetector` and `CleanupAssert` are provided for game application developers during development and testing. Framework module authors rely on explicit, readable Dispose implementations and code review to verify correctness.

---

## Quick Checklist for Writing an Extension Module

Before writing or integrating your extension module, confirm the following:

- [ ] All external dependencies are passed via the constructor; no service locator. 
- [ ] The necessary lifecycle interfaces are implemented (`ILoadable`, `IUpdateable`, `IDrawable`). 
- [ ] `Order`, `Enabled`, `Visible` are provided (where applicable). 
- [ ] Exceptions are not silently swallowed; `try-finally` is used for cleanup. 
- [ ] If `IThreadPool` is used, exceptions in asynchronous operations are properly handled. 
- [ ] Shared state uses synchronization primitives (locks, interlocked, etc.). 
- [ ] `IDisposable` is implemented, all resources are released, and it is idempotent. 
- [ ] Optional dependencies use `Optional<T>`. 
- [ ] Public APIs have XML comments (in English). 
- [ ] Indentation matches Core (4 spaces). 
- [ ] All code comments are in English. 
- [ ] All control flow is explicit. 
- [ ] Module defines a public interface for its business logic; internal interactions favor abstractions. 
- [ ] Cross-module communication uses events for low-frequency state changes; direct references are avoided for notifications. 
- [ ] Performance-critical paths offer static/generic/ref APIs; `in` is used only for structs, not reference types. 
- [ ] If the module belongs to the **Adapters layer**, it references Extensions and implements the interfaces defined there; encapsulating static global state is permitted, but must be hidden behind an interface. If the module belongs to the **Extension or Game layer**, ensure no MonoGame static methods or properties are used directly.
- [ ] **Physical dependency**: The Extension project references only Core (no Adapters references).
- [ ] **Interface ownership**: All business interfaces are defined in the Extension project, not in Adapters.
- [ ] **Module placement**: Platform‑specific modules (those that call external APIs) are placed in Adapters, not in Extensions.

---

## Summary

By following the principles above, your extension module will integrate seamlessly with Core, achieving excellent testability, maintainability, and performance. For game application developers, the composition approach via `GameBuilder` provides clear control and flexible optionality.

Remember: **Core provides the skeleton; extension modules give the game life.** Adhere to the specification to ensure stability and efficiency.