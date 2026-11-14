using MonoGameLibrary.Core.Concurrency;

namespace MonoGameLibrary.Core.Content {
    /// <summary>
    /// Marker interface for all loadable assets. 
    /// </summary>
    [ThreadCondition(ThreadAffinity.Any)]
    public interface IAsset {
    }
}