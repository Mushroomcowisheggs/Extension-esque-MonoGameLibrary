using System;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Core.Hosting {
    /// <summary>
    /// Represents a game host that manages the lifecycle of game modules. 
    /// The host exposes no service registry on purpose: capabilities are resolved once, at the
    /// composition root, and a participant receives them through constructor injection. 
    /// </summary>
    public interface IGameHost : IDisposable {
        /// <summary>
        /// Gets the failure policy selected when the host was built.
        /// </summary>
        FailurePolicy FailurePolicy { get; }
        
        /// <summary>
        /// Gets the diagnostic record of the last failed initialization or admission, or null.
        /// </summary>
        LifecycleFailure Failure { get; }
        
        /// <summary>
        /// Gets or sets a callback that is invoked when an unhandled exception occurs in any module. 
        /// If not set, exceptions are rethrown. 
        /// </summary>
        Action<Exception, string> OnError { get; set; }
        
        /// <summary>
        /// Adds a module to the host. Modules may be added at any time, before or after 
        /// <see cref="Initialize"/>. A module added before initialization is loaded by 
        /// <see cref="Initialize"/>. A module added afterwards is loaded immediately: its
        /// <c>LoadContent</c> runs before this method returns, and the admission is a tracked host
        /// operation, so a disposal in progress waits for that load to finish. If the late load fails,
        /// the module is removed again and the exception is rethrown; the host itself stays initialized.
        /// <see cref="FailurePolicy.Release"/> disposes the withdrawn module; 
        /// <see cref="FailurePolicy.Retain"/> leaves it in <see cref="Failure"/> for the caller.
        /// Under Retain the failed module is caller-owned and is not reusable.
        /// Modules are picked up by <see cref="Update"/>/<see cref="Draw"/> on the next frame. 
        /// A participant that implements <see cref="Lifecycle.IUpdateable"/> or <see cref="Lifecycle.IDrawable"/>
        /// takes part in a schedule and must use <see cref="AddModule(object, ScheduleOptions)"/>; this overload
        /// rejects it, including an intentional order of zero, which is written as <c>new ScheduleOptions(0, 0)</c>.
        /// </summary>
        /// <param name="module">The module to add. </param>
        /// <exception cref="NotSupportedException">Thrown if the participant implements <see cref="Lifecycle.IUpdateable"/>
        /// or <see cref="Lifecycle.IDrawable"/> and therefore requires an explicit schedule. </exception>
        void AddModule(object module);
        
        /// <summary>
        /// Adds a module with the schedule keys the host uses for it. 
        /// </summary>
        /// <param name="module">The module to add. </param>
        /// <param name="options">The fixed update and draw order for this admission. </param>
        /// <remarks>
        /// The host copies <paramref name="options"/> into its schedule entry at admission and never reads
        /// an order from the participant afterwards, so the scheduling key cannot be changed by mutating a
        /// participant's own member. Use <see cref="AddModule(object)"/> only for participants that do not
        /// participate in a schedule; that overload rejects a scheduling participant instead of admitting it at
        /// order zero. Pass <c>new ScheduleOptions(0, 0)</c> when zero is the intended order.
        /// </remarks>
        void AddModule(object module, ScheduleOptions options);
        
        /// <summary>
        /// Removes an admitted module from the schedule without releasing it. 
        /// </summary>
        /// <param name="module">The admitted module to withdraw. </param>
        /// <remarks>
        /// <para>
        /// Withdrawal ends participation, not ownership. The module stops being called by
        /// <see cref="Update"/> and <see cref="Draw"/> from the next publication onwards and its
        /// admission index is consumed, but the host still owns it and releases it exactly once when
        /// the host is disposed. A frame that captured a schedule snapshot before the withdrawal may
        /// still call the module once, because a published snapshot is immutable by design.
        /// </para>
        /// <para>
        /// A withdrawn module may be added again, which mints a new admission index; that is the
        /// documented way to reprioritise a participant, and it is why withdrawal is not release. The
        /// host records one ownership per object, so a module that is withdrawn, re-added and finally
        /// released by the host is still released once.
        /// </para>
        /// <para>
        /// Withdrawal is a tracked operation like admission, so a disposal in progress waits for it,
        /// and it is refused while <see cref="Initialize"/> is running because the loading loop
        /// iterates a snapshot and would still load a module the call removed.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown if the module is not admitted or was
        /// withdrawn already, the host is initializing, or the host is faulted. </exception>
        void WithdrawModule(object module);
        
        /// <summary>
        /// Initializes the host and loads every admitted <see cref="Lifecycle.ILoadable"/> module.
        /// </summary>
        /// <param name="serviceContent">The content service used by the host; ownership transfers to the host. </param>
        /// <remarks>
        /// After this call the host owns the content service and the admitted modules. If loading
        /// fails, <see cref="FailurePolicy.Release"/> releases every admitted module and the content
        /// service, then the host becomes disposed. <see cref="FailurePolicy.Retain"/> leaves the
        /// host faulted and retains the modules and content in <see cref="Failure"/>; the caller must
        /// then call <see cref="Dispose"/>. In both cases the original exception is rethrown and a
        /// disposed or faulted host cannot be reused.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="serviceContent"/> is null. </exception>
        void Initialize(IContentService serviceContent);
        
        void Update(FrameTime timeFrame);
        
        void Draw(FrameTime timeFrame);
    }
}
