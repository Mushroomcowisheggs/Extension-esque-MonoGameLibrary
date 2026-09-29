using System.Threading;

namespace MonoGameLibrary.Core.Concurrency {
    /// <summary>
    /// Provides cancellation tokens for named operations and supports cancellation control. 
    /// Implementations that also implement <see cref="System.IDisposable"/> must document their 
    /// post-disposal behavior; see <see cref="DefaultCancellationService"/> for the reference 
    /// contract (token acquisition throws after disposal, cancellation requests become no-ops). 
    /// </summary>
    public interface ICancellationService {
        /// <summary>
        /// Gets a <see cref="CancellationToken"/> for the specified operation ID. 
        /// If the token does not exist, a new one is created. 
        /// </summary>
        /// <param name="idOperation">The unique identifier of the operation. </param>
        /// <returns>A cancellation token associated with the operation. </returns>
        CancellationToken GetTokenForOperation(string idOperation);
        
        /// <summary>
        /// Renews the cancellation token for the given operation, effectively resetting it. 
        /// The previous token is cancelled and a new one is created. 
        /// </summary>
        /// <param name="idOperation">The operation identifier. </param>
        /// <returns>The newly created cancellation token. </returns>
        CancellationToken RenewToken(string idOperation);
        
        /// <summary>
        /// Cancels the specified operation, triggering its cancellation token. 
        /// </summary>
        /// <param name="idOperation">The operation to cancel. </param>
        void CancelOperation(string idOperation);
        
        /// <summary>
        /// Cancels all active operations managed by this service. 
        /// </summary>
        void CancelAll();
    }
}