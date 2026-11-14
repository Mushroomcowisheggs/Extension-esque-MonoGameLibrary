using MonoGameLibrary.Core.Concurrency;
using MonoGameLibrary.Core.Primitives;

namespace MonoGameLibrary.Extensions.Graphics {
    /// <summary>
    /// Visitor that performs drawing operations on an <see cref="IRenderContext"/>.
    /// Each asset type creates its own visitor and passes it to <see cref="IRenderContext.Accept"/>.
    /// </summary>
    [ThreadCondition(ThreadAffinity.Frame)]
    public interface IVisitor {
        /// <summary>
        /// Performs the drawing operation on the given render context.
        /// The implementation can safely pattern-match the context to access platform-specific internals.
        /// </summary>
        /// <param name="contextRender">The render context to draw into.</param>
        void Visit(IRenderContext contextRender);
    }
}