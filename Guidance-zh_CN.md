# MonoGameLibrary.Core 模块开发规范

本文档为基于 `MonoGameLibrary.Core` 构建**扩展模块**和**游戏应用**的开发者提供全面指导。首先明确四个层次：

- **Core**：基础库 (`MonoGameLibrary.Core`) —— 提供平台无关的工具、计时、日志和通用数据结构的**宿主基础设施**。不包含任何业务逻辑，不包含任何辅助工具，也不包含任何平台特定代码。
- **Adapters**：适配层 (`MonoGameLibrary.Adapters.MonoGame`、`MonoGameLibrary.Adapters.Gum` 等) —— 封装外部平台或第三方库，将其原生 API 转换为 **Extensions 定义的接口**。这是**唯一**允许与 MonoGame、Gum 或其他外部依赖紧耦合的层。
- **Extensions**：可选功能模块（Audio、Scenes、Input、Networking、UI 等）—— **必须**遵循本规范的所有原则。Extensions 定义所有业务接口（契约），并包含平台无关的领域逻辑。它们可以包含不依赖任何外部平台或库的纯 C# 实现。若实现依赖于外部库（如 MonoGame.Extended），则该实现必须放置在 Adapters 层，而接口保留在 Extensions 中。
- **Game Application**：最终游戏项目（如 `Game1.cs`）——负责组合根、服务注册和生命周期驱动。它引用 Extensions（编程接口）和所需的 Adapters（运行时注入）。

**架构层级注意**：`Adapters` 层是唯一允许与外部平台或第三方库（包括 MonoGame 运行时、Gum、Flecs.NET 等）紧耦合的边界层。它负责将这些平台的原生 API（无论是实例方法还是静态全局状态）转换为符合规范定义的接口（如 `IContentService`、`IAudioService`、`IECSWorldService`）。Extension 和 Game 层**必须**只消费这些接口，不得直接引用外部平台 API。

**重要区别**：如果某个功能实现**不依赖**任何外部平台或库（即纯 C#），则它应当作为可选模块放在 `Extensions` 层，其接口也定义在该层。`Adapters` 层**并非**用于存放“默认”或“本地”实现——其唯一目的是**适配**外部依赖。例如，自包含的 ECS 实现应放在 `Extensions`，而 MonoGame.Extended 的 ECS 封装应放在 `Adapters/MonoGame/ECS`。

**物理依赖规则（项目级别强制）**：
```
Game → Extensions → Core
Game → Adapters → Extensions → Core   （Adapter 始终指向 Extensions）
Extensions → Core                      （Extensions 绝不指向 Adapters）
```

此规则保证 Extensions（业务层）完全不知晓 Adapters（基础设施），从而实现轻松的后端替换和可测试性。

以下原则按角色标注适用范围。

**所有代码注释必须使用英文**，文档以英文为准，本文档正文为开发指南的中文版翻译。

---

## 1. 显式依赖原则（Explicit Dependencies）

**角色**：[Extension] 必须遵守；[Core] 提供支持；[Game] 在组合根中使用。

### 规则详述

1. **所有外部服务必须通过构造函数传入**。包括但不限于：
   - `IContentService`（内容加载）
   - `ILogger`（日志）
   - `IThreadPool`（后台任务）
   - `ICancellationService`（取消令牌）
   - `ILoadingProgress`（加载进度）
   - `IObjectPoolFactory`（对象池工厂）
   - 任何自定义服务（如 `ISceneService`）
2. **禁止在模块内部使用 `ServiceRegistry.Get<T>()` 或 `GameHost.Services`**。这些是组合根的工具，不是模块的。
3. **禁止在 Extension 或 Game 层使用静态方法或单例**获取依赖（例如 `ContentManager.Load`、`SoundEffect` 的静态工厂）。
   - **对于托管资源资产（纹理、声音、模型等）**：必须通过 `IContentService` 获取。  
   - **对于适配层（`MonoGameLibrary.Adapters.MonoGame`）的特殊豁免**：适配器可以封装 MonoGame 的**运行时全局状态**（如 `MediaPlayer.Volume`、`SoundEffect.MasterVolume` 或 `GraphicsDevice` 静态属性），但**必须**将其隐藏在面向业务的服务接口（如 `IAudioService`）之后。**严禁**将这种静态访问泄露到适配层之外的任何模块（Extension 或 Game）中。且静态构造必须隐藏在 `IContentService` 的实现内部。
4. **构造函数参数必须进行 null 检查**，并在参数为 null 时抛出 `ArgumentNullException`。
5. **可选依赖**使用 `Optional<T>` 类型（参见第 7 条）。

### 为什么要这样设计？

- **可测试性**：依赖显式化使得单元测试可以传入模拟对象（Mock）。
- **可维护性**：依赖关系一目了然，修改依赖不会影响其他模块。
- **解耦**：模块不关心依赖来自哪里，只关心它是什么。

### 物理项目引用（关键）

为保持以扩展为中心的干净架构，以下项目引用规则**强制**执行：

- `Extensions` 只能引用 `Core`。
- `Adapters` 可以引用 `Extensions`（以及间接的 `Core`）和所需的外部库。
- `Game`（入口项目）可以引用 `Extensions` 以及它需要的任何 `Adapters`。
- **任何情况下**，`Extensions` 都不得引用任何 `Adapters` 项目。如果在 Extension 文件中发现了指向 Adapters 命名空间的 `using` 语句，则该文件应移至 Adapters。

此规则保证业务层（Extensions）完全不知晓基础设施（Adapters），从而实现轻松的后端替换和可测试性。

**层内功能组件隔离（强制）**：

- `Extensions/<Feature>` 下的源文件不得引用任何同级 `MonoGameLibrary.Extensions.<OtherFeature>` 命名空间。每个功能组件拥有自己的词汇与生命周期契约。真正共享的抽象应放入 Core；跨适配器握手契约应放入自包含的 `Extensions/Bridge` 锚点，并通过泛型参数表达，不得引用其他 Extension 所拥有的类型。
- `Adapters/<Backend>/<Capability>` 下的源文件不得引用同级 Adapter 能力或其他 Adapter 项目。每个能力仅通过 Extension/Core 契约独立实现；组件间协作通过注册到 `GameBuilder` 的接口完成，不得引用同级具体类。
- Game Application 是组合根，可以解析并连接多个 Extension 与 Adapter 能力；跨功能编排应位于此处。

这些命名空间级规则比项目引用方向更严格。即使多个组件位于同一程序集，也不代表允许同级功能耦合。

### 示例

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

## 2. 生命周期契约原则（Lifecycle Contract）

**角色**：[Extension] 必须遵守；[Core] 定义接口；[Game] 驱动宿主。

### 规则详述

1. **实现合适的接口**：
   - `ILoadable`：用于一次性加载内容（资源、配置）。在 `GameHost.Initialize` 中被调用。
   - `IUpdateable`：用于每帧更新逻辑（物理、AI、输入处理等）。在 `GameHost.Update` 中被调用。
   - `IDrawable`：用于每帧渲染。在 `GameHost.Draw` 中被调用。

2. **必须提供 `Order` 属性**（int）。数值越小执行越早。默认建议为 `0`，但可根据需要调整（如输入模块应设为负值）。
3. **必须提供 `Enabled` / `Visible` 属性**（bool）。宿主在调用 `Update` / `Draw` 前会检查这些标志。它们应允许在运行时动态更改。
4. **不要在 `Update` 或 `Draw` 中执行耗时操作**（如磁盘 I/O、网络请求）。应使用 `IThreadPool` 异步处理。
5. **保持方法简短**，每个周期内完成最小必要工作。超过 16ms 会导致掉帧。

### 生命周期执行顺序

- `Initialize` 阶段：按 `AddModule` 添加顺序调用所有 `ILoadable.LoadContent`。
- `Update` 阶段：按 `Order` 升序调用所有 `IUpdateable.Update`，但跳过 `Enabled == false` 的模块。
- `Draw` 阶段：按 `Order` 升序调用所有 `IDrawable.Draw`，但跳过 `Visible == false` 的模块。

### 模块放置指引

- **仅做转发**的模块（如 `SceneModule` 调用 `ISceneService.Update`）且不含任何平台特定代码，可以留在 Extensions 中。
- **直接调用平台 API** 的模块（如 `AudioModule` 调用 `AudioService.PlaySoundEffect`，使用了 MonoGame 类型）**必须放在 Adapters 中**，因为它们属于实现层。
- 如果 Extensions 中的模块出现指向 Adapters 命名空间的 `using`，则该模块放错了位置，必须移至 Adapters。

### 示例

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

## 3. 异常隔离与报告原则（Exception Isolation & Reporting）

**角色**：[Extension] 必须遵守；[Core] 提供隔离；[Game] 配置错误处理。

### 规则详述

1. **模块内抛出的异常将首先被宿主捕获，并通过 `OnError` 回调通知应用层，然后被重新抛出**。这意味着模块不应吞没异常，也不应期待 `OnError` 能“修复”问题。
2. **如果模块需要清理资源（如释放锁、关闭文件），必须使用 `try-finally` 或 `using` 语句**，而不是 `try-catch` 来捕获异常然后什么都不做。
3. **异步操作（通过 `IThreadPool`）中的异常**应该在任务内部被捕获，并通过某种方式（如 `OnError` 或日志）报告，否则它们可能被静默丢失。建议使用 `Task.ContinueWith` 或 `try-catch` 在任务内部处理。
4. **错误处理回调是应用层的责任**，模块不应假设它被设置。如果 `OnError` 未设置，宿主将直接抛出异常，这是预期行为。
5. **模块不应在内部记录异常**（调用 `ILogger` 是可以的，但不应依赖 `OnError` 来记录）。日志与错误报告是不同层面。

### 正确做法

```csharp
public void Update(FrameTime time) {
    // If an exception occurs, we want it to bubble up to the host.
    // We don't catch it unless we need to perform cleanup.
    // For cleanup, use try-finally.
    try {
        // Critical operation
    } finally {
        // Release temporary resources
    }
}
```

### 错误做法

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

### 异步异常处理示例

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
                // Log it; we cannot throw here because it's in a background thread.
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

## 4. 线程安全与状态管理原则（Thread Safety）

**角色**：[Extension] 必须遵守；[Core] 提供线程安全宿主；[Game] 不应绕过宿主直接操作模块。

### 规则详述

1. **假设 `Update` 和 `Draw` 可能在不同线程上被调用**（虽然 MonoGame 通常在主线程，但 Core 不保证）。因此，模块内共享状态必须使用同步原语保护。
2. **使用 `IThreadPool` 执行后台任务**时，注意竞争条件和可见性。使用 `lock`、`Interlocked`、`ConcurrentDictionary` 等。
3. **避免在 `Update`/`Draw` 中阻塞等待**（如 `Thread.Sleep`、`WaitHandle.WaitOne`）。这会拖慢主循环。
4. **如果模块公开了可变的属性（如 `Volume`），确保其实现是线程安全的**（使用 `Interlocked` 或锁）。
5. **设计时考虑重入**：例如，`Update` 中可能触发事件，而事件处理器可能再次调用模块的方法。

### 示例

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

### 避免阻塞

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

## 5. 资源清理原则（Resource Cleanup）

**角色**：[Extension] 必须遵守；[Core] 自动调用清理；[Game] 确保 `Dispose` 被调用。

### 规则详述

1. **如果模块持有任何 `IDisposable` 资源（包括 MonoGame 的 `Texture2D`、`SoundEffect`、`SpriteBatch` 等），模块必须实现 `IDisposable`**。
2. **在 `Dispose` 中释放所有托管和非托管资源**，并将引用设为 `null`。
3. **`Dispose` 必须是幂等的**（多次调用无害）。
4. **调用 `GC.SuppressFinalize(this)`** 以避免不必要的终结。
5. **如果模块实现了 `IDisposable`，它也应该在 `Dispose` 中释放子模块**（如果有）。
6. **宿主会在关闭时调用每个模块的 `Dispose`**，但模块不应该依赖宿主来释放，因为宿主可能崩溃或不调用。
7. **不要在 `Dispose` 中抛出异常**。如果异常发生，宿主会捕获它并通过 `OnError` 报告，但不应影响其他模块的释放。

### 示例

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

### 复合模块（持有其他 IDisposable 模块）

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

## 6. 模块身份与排序原则（Identity and Order）

**角色**：[Extension] 必须遵守；[Core] 使用排序；[Game] 动态调整标志。

### 规则详述

1. **`Order` 决定执行顺序**，它应该是常量或基于配置，但必须在构造时确定。不要在运行时频繁更改 `Order`，因为排序发生在每次 `Update`/`Draw` 前（会拷贝列表）。
2. **`Enabled` 和 `Visible` 应支持运行时动态更改**。它们通常用于暂停菜单、屏幕过渡等。
3. **模块间不应通过 `Order` 进行通信**。顺序仅用于调度，不是通信机制。
4. **如果模块之间存在依赖关系（如输入必须在物理前更新），应通过 `Order` 建立明确的偏序**。
5. **不要在 `Enabled`/`Visible` 的 getter 中执行复杂逻辑**，因为它们每帧被多次访问。

### 示例

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

### 运行时切换

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

## 7. 可选服务与 Optional<T> 使用原则（Optional Services）

**角色**：[Extension] 推荐遵守；[Core] 提供 `Optional<T>`；[Game] 可选注册。

### 规则详述

1. **对于非必需的服务（如 `IProfiler`、`ILoadingProgress`），模块的构造函数参数应使用 `Optional<T>` 类型**。
2. **在使用前，检查 `HasValue` 属性**，如果为 `false`，则跳过相关操作。
3. **不要将 `Optional<T>` 用于必需的服务**。必需的服务应直接传递，并抛出异常若为 null。
4. **`Optional<T>` 可以隐式从 `T` 或 `null` 转换**，因此调用方可直接传递实例或不传（默认构造为空）。
5. **模块不应将 `Optional<T>` 存储为字段后更改其状态**，因为它是只读结构体。

### 示例

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

### 在 GameBuilder 中使用

```csharp
var logger = new ConsoleLogger();
var profiler = new MyProfiler();

var builder = new GameBuilder();
builder.UseLogger(logger);
builder.UseProfiler(profiler);
builder.AddModule(new MyModule(logger, profiler));
```

---

## 8. 模块命名与组织原则（Naming and Organization）

**角色**：[Extension] 推荐遵守。

### 规则详述

1. **模块类名应体现其功能**，如 `AudioManager`、`SceneStack`、`InputRouter`。
2. **将模块放在与功能对应的命名空间下**，例如 `MonoGameLibrary.Audio`、`MonoGameLibrary.Scenes`。
3. **避免使用 `Manager` 或 `System` 等模糊后缀**，除非确实管理多个子组件。
4. **每个模块应放在单独的文件中**，文件名与类名一致。
5. **公共 API 方法名应动词开头**，如 `PlayClipAudio`、`LoadScene`。
6. **一切使用小驼峰命名的私有字段应以中心词开头**，中心词通常为对应公共API的结尾词，如`_timeFrame`、`_serviceContent`、`_logger`。也就是声明式命名，变量名 = 类别词 + 限定语。它反映的是“这个数据是什么”。
7. **局部字段同样遵循中心词开头的声明式命名**，如`timeFrame`、`serviceContent`、`logger`。
8. **公开字段通常直接使用自然语言顺序命名**，如`FrameTime`、`IContentService`、`Logger`。也就是代入式命名，字段名 = 对象名（或隐含的上下文对象）+ 属性名。它反映的是“这个数据是哪个对象的哪个字段”，而非“我们定义一个字段，然后进行限定”。
9. 用于标识而非计算的布尔变量命名须以flag开头以明确用途。

---

## 9. 显式控制流原则（Explicit Control Flow）

**角色**：[Extension] 必须遵守。

### 规则详述

1. **禁止使用 C# 的空传播运算符 (`?.`) 和空合并运算符 (`??`)**。所有空值检查必须使用显式的 `if (x != null)` 或 `if (x == null)` 分支。
2. **禁止在构造函数中使用 `?? throw new`**。必须使用独立的 null 检查语句。
3. **属性、方法和只读成员的表达式体定义 (`=>`) 不允许使用**。必须使用完整的块体 `{ get { ... } }` 或 `{ return ...; }`。
4. **所有条件逻辑必须使用完整的 `if-else` 块**，在简单的赋值场景中可以用三元运算符 `? :` ，但复杂场景推荐使用 `if-else` 以求清晰。
5. **循环和异常处理必须使用大括号 `{}`**，即使只有一条语句。
6. **应使用显式的匿名委托语法**（delegate { ... }）对于所有委托参数，而不是 lambda 表达式（例如 (x) => x.Foo）。

### 为什么要这样设计？

- **可读性**：显式的控制流让代码的逻辑分支一目了然，降低认知负担，尤其对于不熟悉 C# 语法糖的开发者。
- **调试友好**：断点和单步执行在显式语句上更直观；空传播运算可能隐藏意外的 null 传播路径。
- **一致性**：强制统一的风格，避免在同一代码库中混用多种空检查方式。
- **安全性**：`?.` 可能在某些情况下悄悄返回 null 而不产生任何效果，显式 null 检查驱动开发者思考并处理“无值”的情况。

### 示例

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

## 10. 模块文档与注释要求

**角色**：[Extension] 必须遵守。

- **所有公开方法、属性、接口必须包含 XML 文档注释**（`///`）。
- **注释必须用英文**，描述目的、参数、返回值、异常。
- **内部实现可使用注释解释复杂逻辑**，但非必须。

### 示例

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

## 11. 接口隔离与内部抽象原则
**角色**：[Extension] 必须遵守；[Game] 消费接口。

### 规则详述
1. **定义模块接口**：每个功能模块（如 `AudioModule`）必须定义相应的公共业务接口（如 `IAudioService`）。**该接口必须放在 Extensions 项目中**，因为它代表业务能力。该接口应根据需要继承 Core 的生命周期接口（`ILoadable`, `IUpdateable`）。
2. **面向接口编程**：在模块内部，子组件之间的依赖必须引用抽象（接口），而非具体实现。这降低了耦合度，便于重构。
3. **可见性策略**：具体实现类（位于 Adapters 中）通常应为 `public`，以便跨程序集注册到 `GameBuilder`。然而，内部逻辑应隐藏在接口之后。若必须使用 `internal` 实现，请提供返回接口类型的公共工厂方法。
4. **消费灵活性**：游戏应用可以通过接口消费模块以获得更好的解耦，如果确实需要访问特定功能，也可以直接使用具体类。

> **注**：本原则扩展了经典的接口隔离原则（ISP），要求模块定义自身的契约，并在内部依赖抽象。
> **推荐实现模式**：为实现职责的清晰分离并便于未来平台适配，建议将服务逻辑与生命周期模块分开。服务实现业务接口，并包含所有平台特定逻辑（放在 Adapters 中）；模块实现 `IUpdateable`/`IDrawable` 接口，并将调用转发给服务（也放在 Adapters 中，除非它仅做转发且不含平台代码，方可留在 Extensions）。此模式确保业务接口（在 Extensions 中）保持稳定，而实现可以自由替换。

### 示例
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

## 12. 事件驱动通信原则
**角色**：[Extension] 可实现；[Game] 编排跨模块事件。

### 规则详述
1. **发布状态变更**：模块可以暴露事件（如 `OnSceneLoaded`, `ScoreChanged`）以通知外部系统内部状态的变化。使用标准的 .NET `event` 关键字。
2. **最小化直接通知引用**：模块**应避免**持有对其他模块的直接引用来进行**跨模块通知**。当模块需要调用另一模块的功能时，该依赖必须显式声明（通过构造函数注入）。事件驱动通信是向未知订阅者**广播状态变更**的推荐模式。
3. **性能边界**：事件旨在用于**低频、逻辑性操作**（状态改变、UI 更新）。**切勿**在 `Update` 或 `Draw` 循环内使用事件处理高频操作（如逐帧的物理碰撞），因为委托调用的开销会影响性能。
4. **无全局事件总线**：Core 和 Extension 不得实现全局的“事件总线”或“消息代理”。如果确实需要此类复杂性，必须由游戏应用在应用层实现，以避免隐藏的依赖关系。

### 示例
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

## 13. 性能导向静态分发原则
**角色**：[Core] 和 [Extension] 必须为性能关键路径提供双重 API。

### 规则详述
1. **双轨 API 策略**：同时提供通用的基于接口的 API（用于灵活性）和高性能的 API（用于速度）。建议通过命名加以区分（例如 `Draw` 与 `DrawFast`，或使用独立的静态类），以避免歧义。
2. **高性能构造**：对于热点路径（渲染、粒子系统）：
   - 对无状态操作使用 `static` 方法或 `static` 类。
   - 使用 `ref struct`、`readonly ref struct` 或 `in` 参数通过引用传递**大型结构体**（如 `Matrix`、`TransformData`），避免堆分配和复制。
   - **请勿对引用类型**（如 `SpriteBatch` 或 `Texture2D`）使用 `in` 修饰符，因为这没有性能收益且会增加间接寻址。
   - 使用带约束的泛型以支持 JIT 内联，避免虚拟调度。
3. **避免分配**：严禁在紧密循环内进行不必要的装箱、闭包分配或委托创建。
4. **显式选择**：游戏应用开发者负责在性能分析表明存在瓶颈时，主动选用高性能 API。

### 示例
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

## 14. 资源清理的补偿模式

**物理位置**：`MonoGameLibrary.Utilities`

**设计目标**：仅限游戏应用代码。

### 规则详述

1. **框架模块（实现 IModule 的类）必须使用显式清理**。所有资源释放、事件退订、状态恢复都必须在 Dispose 方法中明确写出。
2. **游戏应用代码可以使用 CompensationStack** 简化样板清理代码。
3. **补偿动作按 LIFO 顺序执行**，天然匹配资源分配的反序释放原则。
4. **不要在框架模块中使用 CompensationStack**。框架模块必须保持可审计和可预测。
5. **Utilities 项目不引用 Extensions**，确保框架模块无法意外访问便利工具。

### 设计理念

此模式借鉴了动态组合演算中“补偿”的概念，承认并非所有操作都是数学上可逆的。CompensationStack 不要求精确可逆，而是提供一种注册显式补偿动作的机制，使系统达到干净状态。

### 示例：框架模块（显式）

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

### 示例：游戏应用（使用 CompensationStack）

```csharp
public class GameScene : Scene {
    private readonly CompensationStack _compensation;

    public GameScene() {
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

### 何时不使用 CompensationStack

- 在任何实现 `IModule` 的类中（框架模块必须保持显式）。
- 在性能关键的路径中，栈的开销不可接受时。
- 当清理逻辑非常简单，显式的 Dispose 代码更清晰时。

### 验证工具

`ResourceLeakDetector` 和 `CleanupAssert` 为游戏应用开发者在开发和测试阶段提供。框架模块作者依靠显式、可读的 Dispose 实现和代码审查来验证正确性。

---

## 编写扩展模块的快速检查清单

在编写或集成您的扩展模块之前，请确认以下事项：

- [ ] 所有外部依赖通过构造函数传入，无服务定位器。
- [ ] 实现了必要的生命周期接口 (`ILoadable`, `IUpdateable`, `IDrawable`)。
- [ ] 提供了 `Order`, `Enabled`, `Visible`（如果适用）。
- [ ] 异常没有静默吞没；使用 `try-finally` 进行清理。
- [ ] 如果使用 `IThreadPool`，异步操作中的异常被妥善处理。
- [ ] 共享状态使用了同步原语（锁、互锁等）。
- [ ] 实现了 `IDisposable`，释放了所有资源，且是幂等的。
- [ ] 可选依赖使用了 `Optional<T>`。
- [ ] 公共 API 有 XML 注释（英文）。
- [ ] 没有使用与 Core 不同的缩进（4 空格）。
- [ ] 代码注释全部使用英文。
- [ ] 所有控制流显式可见。
- [ ] 服务为其业务逻辑定义了公共接口；模块实现生命周期接口（`IUpdateable`/`IDrawable`），并将请求委托给服务。。内部交互优先使用抽象。
- [ ] 跨模块通信对低频状态变更使用事件；避免为通知目的持有直接引用。
- [ ] 性能关键路径提供了静态/泛型/ref 结构的高性能 API；`in` 仅用于结构体，不用于引用类型。
- [ ] 如果模块属于 **Adapters 层**，它引用 Extensions 并实现其中定义的接口，封装静态全局状态是允许的，但必须隐藏在接口之后；如果模块属于 **Extension 或 Game 层**，确保没有直接使用任何 MonoGame 静态方法或属性。
- [ ] **物理依赖**：Extension 项目仅引用 Core（无 Adapters 引用）。
- [ ] **接口所有权**：所有业务接口定义在 Extension 项目中，而非 Adapters。
- [ ] **模块放置**：平台特定模块（调用外部 API 的）放在 Adapters 中，不在 Extensions 中。

---

## 总结

遵循上述原则，您的扩展模块将与 Core 无缝协作，具备良好的可测试性、可维护性和性能。对于游戏应用开发者，通过 `GameBuilder` 的组合方式，您将获得清晰的控制权和灵活的可选性。

记住：**Core 提供的是骨架，扩展模块赋予游戏生命**。遵循规范，确保稳定和高效。
