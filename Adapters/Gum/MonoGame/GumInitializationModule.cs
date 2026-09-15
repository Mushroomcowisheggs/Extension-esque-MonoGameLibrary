using System;
using Microsoft.Xna.Framework.Content;
using MonoGameLibrary.Core.Lifecycle;
using MonoGameLibrary.Extensions.UserInterface;

namespace MonoGameLibrary.Adapters.Gum.MonoGame {
    /// <summary>
    /// Module that initializes Gum during the content loading phase.
    /// </summary>
    internal sealed class GumInitializationModule : ILoadable {
        private readonly IUserInterfaceService _serviceUserInterface;
        private readonly ContentManager _managerContent;
        
        public GumInitializationModule(IUserInterfaceService serviceUserInterface, ContentManager managerContent) {
            if (serviceUserInterface == null) {
                throw new ArgumentNullException(nameof(serviceUserInterface));
            }
            if (managerContent == null) {
                throw new ArgumentNullException(nameof(managerContent));
            }
            _serviceUserInterface = serviceUserInterface;
            _managerContent = managerContent;
        }
        
        public void LoadContent() {
            _serviceUserInterface.Initialize();
            global::MonoGameGum.GumService.Default.ContentLoader.XnaContentManager = _managerContent;
        }
    }
}