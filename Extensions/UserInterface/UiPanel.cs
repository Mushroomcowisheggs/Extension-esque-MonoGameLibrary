using System;
using System.Collections.Generic;
using MonoGameLibrary.Core.Primitives;
using MonoGameLibrary.Core.Time;

namespace MonoGameLibrary.Extensions.UserInterface {
    /// <summary>
    /// A rectangular panel with a background color and child elements drawn on top.
    /// </summary>
    public sealed class UiPanel : UiElement {
        private readonly List<UiElement> _listChildren = new List<UiElement>();
        
        /// <summary>Gets or sets the background color.</summary>
        public Color BackgroundColor { get; set; }
        
        /// <summary>Gets the child elements in drawing order.</summary>
        public IReadOnlyList<UiElement> Children {
            get { return _listChildren; }
        }
        
        /// <summary>Initializes a transparent panel.</summary>
        public UiPanel() {
            BackgroundColor = Color.Transparent;
        }
        
        /// <summary>Adds a child element.</summary>
        /// <param name="element">The child to add.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="element"/> is null.</exception>
        public void AddChild(UiElement element) {
            if (element == null) {
                throw new ArgumentNullException(nameof(element));
            }
            _listChildren.Add(element);
        }
        
        /// <summary>Removes a child element.</summary>
        /// <param name="element">The child to remove.</param>
        /// <returns>True when the child was found and removed.</returns>
        public bool RemoveChild(UiElement element) {
            return _listChildren.Remove(element);
        }
        
        /// <summary>Removes every child element.</summary>
        public void ClearChildren() {
            _listChildren.Clear();
        }
        
        /// <inheritdoc />
        public override void Update(FrameTime timeFrame) {
            foreach (UiElement child in _listChildren) {
                if (child.Visible) {
                    child.Update(timeFrame);
                }
            }
        }
        
        /// <inheritdoc />
        public override void Draw(IUiRenderer renderer, float zoom) {
            if (BackgroundColor.A > 0) {
                renderer.DrawRectangle(GetScaledBounds(zoom), BackgroundColor);
            }
            foreach (UiElement child in _listChildren) {
                if (child.Visible) {
                    child.Draw(renderer, zoom);
                }
            }
        }
    }
}
