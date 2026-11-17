using System;
using Godot;
using MonoGameLibrary.Core.Hosting;

namespace MonoGameLibrary.Adapters.Godot.Lifecycle {
    /// <summary>Bootstraps a fully integrated Godot application.</summary>
    /// <remarks>
    /// Godot owns the frame loop, so these entry points do not block: they install the
    /// integration node into the given scene tree and return.
    /// The application runs until <c>IGameApplicationService.Exit</c> (or
    /// <c>SceneTree.Quit</c>) is requested. This is the documented contract deviation of
    /// the Godot adapter's hosting seam.
    /// </remarks>
    public static class GameApplication {
        /// <summary>Creates an integration node with default options.</summary>
        /// <param name="actionConfigureServices">Composes the host's services and modules.</param>
        /// <returns>The created node, not yet added to any scene tree.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="actionConfigureServices"/> is null.</exception>
        public static IntegrationNode Create(Action<GameBuilder> actionConfigureServices) {
            return Create(new GameApplicationOptions(), actionConfigureServices);
        }
        
        /// <summary>Creates an integration node with the given options.</summary>
        /// <param name="options">The window and timing settings.</param>
        /// <param name="actionConfigureServices">Composes the host's services and modules.</param>
        /// <returns>The created node, not yet added to any scene tree.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="options"/> or <paramref name="actionConfigureServices"/> is null.
        /// </exception>
        public static IntegrationNode Create(
            GameApplicationOptions options,
            Action<GameBuilder> actionConfigureServices
        ) {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }
            if (actionConfigureServices == null) {
                throw new ArgumentNullException(nameof(actionConfigureServices));
            }
            return new IntegrationNode(options, actionConfigureServices);
        }
        
        /// <summary>Installs an integration node into the scene tree and returns immediately.</summary>
        /// <param name="tree">The scene tree to attach to.</param>
        /// <param name="actionConfigureServices">Composes the host's services and modules.</param>
        /// <returns>The installed node.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="tree"/> or <paramref name="actionConfigureServices"/> is null.
        /// </exception>
        public static IntegrationNode Run(SceneTree tree, Action<GameBuilder> actionConfigureServices) {
            return Run(tree, new GameApplicationOptions(), actionConfigureServices);
        }
        
        /// <summary>Installs an integration node into the scene tree and returns immediately.</summary>
        /// <param name="tree">The scene tree to attach to.</param>
        /// <param name="options">The window and timing settings.</param>
        /// <param name="actionConfigureServices">Composes the host's services and modules.</param>
        /// <returns>The installed node.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if <paramref name="tree"/>, <paramref name="options"/> or
        /// <paramref name="actionConfigureServices"/> is null.
        /// </exception>
        public static IntegrationNode Run(
            SceneTree tree,
            GameApplicationOptions options,
            Action<GameBuilder> actionConfigureServices
        ) {
            if (tree == null) {
                throw new ArgumentNullException(nameof(tree));
            }
            IntegrationNode node = Create(options, actionConfigureServices);
            tree.Root.AddChild(node);
            return node;
        }
    }
}
