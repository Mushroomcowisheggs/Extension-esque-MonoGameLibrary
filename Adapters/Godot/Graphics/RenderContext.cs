using System;
using System.Collections.Generic;
using Godot;
using MonoGameLibrary.Core;
using MonoGameLibrary.Core.Primitives;
using MonoGameLibrary.Extensions.Bridge;
using MonoGameLibrary.Extensions.Graphics;
using GodotColor = global::Godot.Color;

namespace MonoGameLibrary.Adapters.Godot.Graphics {
    /// <summary>
    /// Godot-specific implementation of <see cref="IRenderContext"/>. 
    /// Draws through a registered <see cref="CanvasItem"/> anchor and provides the state,
    /// target and drawing operations declared by the extension contract. 
    /// Godot executes immediate-mode draw commands only inside the draw callback of the
    /// node that receives them and has no batch state to bracket, so <see cref="Begin"/>
    /// places the block state on the anchor (texture filter and repeat, material, transform)
    /// and <see cref="End"/> restores it. While a render target is bound, the same
    /// operations are recorded and replayed by the target node (see <see cref="RenderTarget"/>).
    /// </summary>
    internal sealed class RenderContext : IRenderContext {
        private readonly CanvasItem _itemAnchor;
        private readonly Stack<IRenderTarget> _stackTargets = new Stack<IRenderTarget>();
        private IRenderTarget _targetCurrent;
        private CanvasItem _itemBlock;
        private CanvasItem.TextureFilterEnum _filterPrevious;
        private CanvasItem.TextureRepeatEnum _repeatPrevious;
        private Material _materialPrevious;
        private Transform2D _matrixBlock = Transform2D.Identity;
        private bool _flagBatchActive;
        private bool _flagDisposed;
        
        private void ThrowIfDisposed() {
            if (_flagDisposed) { throw new ObjectDisposedException(nameof(RenderContext)); }
        }
        
        /// <summary>
        /// Gets the Godot <see cref="CanvasItem"/> used for drawing. 
        /// Intended for internal use by adapter classes only. 
        /// </summary>
        internal CanvasItem AnchorItem { get { return _itemAnchor; } }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="RenderContext"/> class. 
        /// </summary>
        /// <param name="itemAnchor">The <see cref="CanvasItem"/> that receives the draw commands. </param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="itemAnchor"/> is null. </exception>
        public RenderContext(CanvasItem itemAnchor) {
            if (itemAnchor == null) { throw new ArgumentNullException(nameof(itemAnchor)); }
            _itemAnchor = itemAnchor;
        }
        
        /// <inheritdoc />
        public Optional<IRenderTarget> CurrentTarget {
            get {
                if (_targetCurrent == null) {
                    return default;
                }
                return new Optional<IRenderTarget>(_targetCurrent);
            }
        }
        
        /// <inheritdoc />
        public MonoGameLibrary.Core.Primitives.Rectangle ViewportBoundsRectangle {
            get {
                if (_targetCurrent != null) {
                    return new MonoGameLibrary.Core.Primitives.Rectangle(0, 0, _targetCurrent.Width, _targetCurrent.Height);
                }
                Rect2 bounds = _itemAnchor.GetViewportRect();
                return new MonoGameLibrary.Core.Primitives.Rectangle(
                    (int)bounds.Position.X, (int)bounds.Position.Y, (int)bounds.Size.X, (int)bounds.Size.Y
                );
            }
        }
        
        /// <summary>
        /// Draws a Godot <see cref="Texture2D"/> onto the active drawing item. 
        /// </summary>
        private static void DrawTextureCore(
            CanvasItem itemTarget,
            Texture2D texture,
            TwoDimensionalVector position,
            OptionalValue<MonoGameLibrary.Core.Primitives.Rectangle> rectangleSource,
            MonoGameLibrary.Core.Primitives.Color color,
            float rotation,
            TwoDimensionalVector origin,
            TwoDimensionalVector scale,
            MonoGameLibrary.Extensions.Graphics.SpriteEffects effectsSprite,
            float depthLayer,
            Transform2D matrixBlock
        ) {
            // depthLayer is accepted and ignored: Godot draws the commands of a canvas item
            // in submission order and has no per-call depth, so submission order is the draw
            // order and the parameter cannot reorder draws.
            Rect2 rectangleSourceNative;
            float width;
            float height;
            if (rectangleSource.HasValue) {
                width = rectangleSource.Value.Width;
                height = rectangleSource.Value.Height;
                rectangleSourceNative = new Rect2(
                    rectangleSource.Value.X, rectangleSource.Value.Y, width, height
                );
            } else {
                width = texture.GetWidth();
                height = texture.GetHeight();
                rectangleSourceNative = new Rect2(0f, 0f, width, height);
            }
            
            // A sprite flip mirrors the content inside its box: the pivot moves to the
            // mirrored origin and the draw transform carries the sign flip, which keeps
            // the drawn box exactly where the caller placed it while mirroring the content.
            float pivotX = origin.X;
            float pivotY = origin.Y;
            float scaleX = scale.X;
            float scaleY = scale.Y;
            if ((effectsSprite & MonoGameLibrary.Extensions.Graphics.SpriteEffects.FlipHorizontally) != 0) {
                pivotX = width - origin.X;
                scaleX = -scaleX;
            }
            if ((effectsSprite & MonoGameLibrary.Extensions.Graphics.SpriteEffects.FlipVertically) != 0) {
                pivotY = height - origin.Y;
                scaleY = -scaleY;
            }
            
            Transform2D matrixCall = new Transform2D(
                rotation,
                new Vector2(scaleX, scaleY),
                0f,
                new Vector2(position.X, position.Y)
            );
            itemTarget.DrawSetTransformMatrix(matrixBlock * matrixCall);
            itemTarget.DrawTextureRectRegion(
                texture,
                new Rect2(-pivotX, -pivotY, width, height),
                rectangleSourceNative,
                ConvertColor(color),
                false,
                false
            );
            itemTarget.DrawSetTransformMatrix(matrixBlock);
        }
        
        /// <summary>
        /// Draws a Godot <see cref="Texture2D"/> stretched onto a destination rectangle. 
        /// </summary>
        private static void DrawTextureRegionCore(
            CanvasItem itemTarget,
            Texture2D texture,
            MonoGameLibrary.Core.Primitives.Rectangle rectangleDestination,
            OptionalValue<MonoGameLibrary.Core.Primitives.Rectangle> rectangleSource,
            MonoGameLibrary.Core.Primitives.Color color,
            Transform2D matrixBlock
        ) {
            Rect2 rectangleSourceNative = rectangleSource.HasValue
            ? new Rect2(
                rectangleSource.Value.X, rectangleSource.Value.Y,
                rectangleSource.Value.Width, rectangleSource.Value.Height
            )
            : new Rect2(0f, 0f, texture.GetWidth(), texture.GetHeight());
            
            itemTarget.DrawSetTransformMatrix(matrixBlock);
            itemTarget.DrawTextureRectRegion(
                texture,
                new Rect2(
                    rectangleDestination.X, rectangleDestination.Y,
                    rectangleDestination.Width, rectangleDestination.Height
                ),
                rectangleSourceNative,
                ConvertColor(color),
                false,
                false
            );
        }
        
        /// <summary>
        /// Draws a string using a Godot <see cref="Font"/>. 
        /// Called by <see cref="SpriteFont"/> via visitor pattern. 
        /// </summary>
        internal void DrawStringInternal(
            Font font,
            string text,
            TwoDimensionalVector vectorPosition,
            MonoGameLibrary.Core.Primitives.Color color,
            float rotation,
            TwoDimensionalVector vectorOrigin,
            float scale,
            MonoGameLibrary.Extensions.Graphics.SpriteEffects effectsSprite,
            float depthLayer
        ) {
            if (_targetCurrent == null) {
                DrawStringCore(
                    _itemAnchor, font, text, vectorPosition, color,
                    rotation, vectorOrigin, scale, effectsSprite, depthLayer, _matrixBlock
                );
            } else {
                RenderTarget targetRecord = ResolveTarget(_targetCurrent);
                Transform2D matrixBlock = _matrixBlock;
                targetRecord.RecordCommand(delegate(CanvasItem item) {
                    DrawStringCore(
                        item, font, text, vectorPosition, color,
                        rotation, vectorOrigin, scale, effectsSprite, depthLayer, matrixBlock
                    );
                });
            }
        }
        
        private static void DrawStringCore(
            CanvasItem itemTarget,
            Font font,
            string text,
            TwoDimensionalVector vectorPosition,
            MonoGameLibrary.Core.Primitives.Color color,
            float rotation,
            TwoDimensionalVector vectorOrigin,
            float scale,
            MonoGameLibrary.Extensions.Graphics.SpriteEffects effectsSprite,
            float depthLayer,
            Transform2D matrixBlock
        ) {
            // depthLayer is accepted and ignored; see DrawTextureCore.
            int fontSize = SpriteFont.ResolveIntrinsicFontSize(font);
            bool flagFlippedHorz = (effectsSprite & MonoGameLibrary.Extensions.Graphics.SpriteEffects.FlipHorizontally) != 0;
            bool flagFlippedVert = (effectsSprite & MonoGameLibrary.Extensions.Graphics.SpriteEffects.FlipVertically) != 0;
            float scaleX = flagFlippedHorz ? -scale : scale;
            float scaleY = flagFlippedVert ? -scale : scale;
            
            // Flipped text mirrors through the string box instead of through the origin:
            // the offsets below place the mirrored string box where the unmirrored box
            // would have been, so a flipped string stays where it was placed.
            float adjustX = 0f;
            float adjustY = 0f;
            if (flagFlippedHorz || flagFlippedVert) {
                Vector2 sizeText = font.GetStringSize(
                    text, 
                    HorizontalAlignment.Left, 
                    -1f, 
                    fontSize, 
                    TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, 
                    TextServer.Direction.Auto, 
                    TextServer.Orientation.Horizontal
                );
                if (flagFlippedHorz) {
                    adjustX = -sizeText.X;
                }
                if (flagFlippedVert) {
                    adjustY = font.GetHeight(fontSize) - sizeText.Y;
                }
            }
            
            Transform2D matrixCall = new Transform2D(
                rotation,
                new Vector2(scaleX, scaleY),
                0f,
                new Vector2(vectorPosition.X, vectorPosition.Y)
            );
            itemTarget.DrawSetTransformMatrix(matrixBlock * matrixCall);
            itemTarget.DrawString(
                font,
                new Vector2(adjustX - vectorOrigin.X, adjustY - vectorOrigin.Y),
                text,
                HorizontalAlignment.Left,
                -1f,
                fontSize,
                ConvertColor(color),
                TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound,
                TextServer.Direction.Auto,
                TextServer.Orientation.Horizontal
            );
            itemTarget.DrawSetTransformMatrix(matrixBlock);
        }
        
        private static Material ConvertBlend(MonoGameLibrary.Extensions.Graphics.BlendState stateBlend) {
            // The blend presets map onto Godot's canvas item blend modes by their
            // factors: AlphaBlend (One/InverseSourceAlpha) is premultiplied source-over,
            // NonPremultiplied (SourceAlpha/InverseSourceAlpha) is straight alpha, and
            // Additive (SourceAlpha/One) is closest to Godot's additive mode.
            if (stateBlend is MonoGameLibrary.Extensions.Graphics.BlendState.AlphaBlendState) {
                return CreateBlendMaterial(CanvasItemMaterial.BlendModeEnum.PremultAlpha);
            }
            if (stateBlend is MonoGameLibrary.Extensions.Graphics.BlendState.AdditiveState) {
                return CreateBlendMaterial(CanvasItemMaterial.BlendModeEnum.Add);
            }
            if (stateBlend is MonoGameLibrary.Extensions.Graphics.BlendState.OpaqueState) {
                return CreateOpaqueMaterial();
            }
            if (stateBlend is MonoGameLibrary.Extensions.Graphics.BlendState.NonPremultipliedState) {
                return CreateBlendMaterial(CanvasItemMaterial.BlendModeEnum.Mix);
            }
            return CreateBlendMaterial(CanvasItemMaterial.BlendModeEnum.PremultAlpha);
        }
        
        private static Material CreateBlendMaterial(CanvasItemMaterial.BlendModeEnum modeBlend) {
            CanvasItemMaterial material = new CanvasItemMaterial();
            material.BlendMode = modeBlend;
            return material;
        }
        
        private static Material CreateOpaqueMaterial() {
            // Godot's canvas item materials expose no blend-disabled mode; the exact
            // One/Zero factors of the Opaque preset are a shader render mode.
            Shader shader = new Shader();
            shader.Code = "shader_type canvas_item;\nrender_mode blend_disabled;";
            ShaderMaterial material = new ShaderMaterial();
            material.Shader = shader;
            return material;
        }
        
        private static (CanvasItem.TextureFilterEnum Filter, CanvasItem.TextureRepeatEnum Repeat) ConvertSampler(
            MonoGameLibrary.Extensions.Graphics.SamplerState stateSampler
        ) {
            // Filtering maps onto the canvas item texture filter and wrap addressing onto
            // the canvas item texture repeat.
            if (stateSampler is MonoGameLibrary.Extensions.Graphics.SamplerState.PointClampState) {
                return (CanvasItem.TextureFilterEnum.Nearest, CanvasItem.TextureRepeatEnum.Disabled);
            }
            if (stateSampler is MonoGameLibrary.Extensions.Graphics.SamplerState.PointWrapState) {
                return (CanvasItem.TextureFilterEnum.Nearest, CanvasItem.TextureRepeatEnum.Enabled);
            }
            if (stateSampler is MonoGameLibrary.Extensions.Graphics.SamplerState.LinearClampState) {
                return (CanvasItem.TextureFilterEnum.Linear, CanvasItem.TextureRepeatEnum.Disabled);
            }
            if (stateSampler is MonoGameLibrary.Extensions.Graphics.SamplerState.LinearWrapState) {
                return (CanvasItem.TextureFilterEnum.Linear, CanvasItem.TextureRepeatEnum.Enabled);
            }
            return (CanvasItem.TextureFilterEnum.Nearest, CanvasItem.TextureRepeatEnum.Disabled);
        }
        
        private static Transform2D ConvertTransform(TwoDimensionalTransform transformTransform) {
            // The contract composes scale, then rotation, then translation, which is the
            // basis of Godot's (rotation, scale, skew, position) transform.
            return new Transform2D(
                transformTransform.Rotation, 
                new Vector2(transformTransform.ScaleX, transformTransform.ScaleY), 
                0f, 
                new Vector2(transformTransform.TranslationX, transformTransform.TranslationY)
            );
        }
        
        private static GodotColor ConvertColor(MonoGameLibrary.Core.Primitives.Color color) {
            return new GodotColor(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f);
        }
        
        private static void ApplyBlockState(
            CanvasItem itemTarget,
            CanvasItem.TextureFilterEnum filter,
            CanvasItem.TextureRepeatEnum repeat,
            Material material,
            Transform2D matrixBlock
        ) {
            itemTarget.SetTextureFilter(filter);
            itemTarget.SetTextureRepeat(repeat);
            itemTarget.Material = material;
            itemTarget.DrawSetTransformMatrix(matrixBlock);
        }
        
        private static void RestoreItemState(
            CanvasItem itemTarget,
            CanvasItem.TextureFilterEnum filter,
            CanvasItem.TextureRepeatEnum repeat,
            Material material
        ) {
            itemTarget.SetTextureFilter(filter);
            itemTarget.SetTextureRepeat(repeat);
            itemTarget.Material = material;
        }
        
        /// <inheritdoc />
        public void Begin(
            Optional<MonoGameLibrary.Extensions.Graphics.SamplerState> stateSampler = default, 
            Optional<MonoGameLibrary.Extensions.Graphics.BlendState> stateBlend = default, 
            Optional<IEffect> effect = default,
            OptionalValue<TwoDimensionalTransform> transform = default
        ) {
            ThrowIfDisposed();
            if (_flagBatchActive) {
                throw new InvalidOperationException("A rendering batch is already active.");
            }
            var stateNativeSampler = stateSampler.HasValue
            ? ConvertSampler(stateSampler.Value) 
            : ConvertSampler(MonoGameLibrary.Extensions.Graphics.SamplerState.PointClamp);
            
            Material materialNative = null;
            if (effect.HasValue) {
                var effectConcrete = effect.Value as Effect;
                if (effectConcrete != null) {
                    materialNative = effectConcrete.NativeMaterial;
                } else {
                    throw new InvalidOperationException(
                        "The provided effect is not a Godot Effect adapter."
                    );
                }
            }
            
            Material materialBlock;
            if (materialNative != null) {
                // One canvas item carries one material, so a shader effect and a blend
                // state cannot be combined independently: when an effect is bound, the
                // shader's render_mode owns the blending.
                materialBlock = materialNative;
            } else {
                materialBlock = stateBlend.HasValue
                ? ConvertBlend(stateBlend.Value)
                : ConvertBlend(MonoGameLibrary.Extensions.Graphics.BlendState.AlphaBlend);
            }
            
            Transform2D matrixTransform = Transform2D.Identity;
            if (transform.HasValue) {
                matrixTransform = ConvertTransform(transform.Value);
            }
            
            RenderTarget targetRecord = null;
            CanvasItem itemBlock = _itemAnchor;
            if (_targetCurrent != null) {
                targetRecord = ResolveTarget(_targetCurrent);
                itemBlock = targetRecord.ReplayItem;
            }
            _itemBlock = itemBlock;
            _filterPrevious = itemBlock.TextureFilter;
            _repeatPrevious = itemBlock.TextureRepeat;
            _materialPrevious = itemBlock.Material;
            _matrixBlock = matrixTransform;
            if (targetRecord == null) {
                ApplyBlockState(
                    itemBlock, stateNativeSampler.Filter, stateNativeSampler.Repeat,
                    materialBlock, matrixTransform
                );
            } else {
                CanvasItem.TextureFilterEnum filterBlock = stateNativeSampler.Filter;
                CanvasItem.TextureRepeatEnum repeatBlock = stateNativeSampler.Repeat;
                targetRecord.RecordCommand(delegate(CanvasItem item) {
                    ApplyBlockState(item, filterBlock, repeatBlock, materialBlock, matrixTransform);
                });
            }
            _flagBatchActive = true;
        }
        
        /// <inheritdoc />
        public void End() {
            ThrowIfDisposed();
            if (!_flagBatchActive) {
                throw new InvalidOperationException("No rendering batch is active.");
            }
            if (_targetCurrent == null) {
                RestoreItemState(_itemBlock, _filterPrevious, _repeatPrevious, _materialPrevious);
                _itemBlock.DrawSetTransformMatrix(Transform2D.Identity);
            } else {
                RenderTarget targetRecord = ResolveTarget(_targetCurrent);
                CanvasItem.TextureFilterEnum filterPrevious = _filterPrevious;
                CanvasItem.TextureRepeatEnum repeatPrevious = _repeatPrevious;
                Material materialPrevious = _materialPrevious;
                targetRecord.RecordCommand(delegate(CanvasItem item) {
                    RestoreItemState(item, filterPrevious, repeatPrevious, materialPrevious);
                    item.DrawSetTransformMatrix(Transform2D.Identity);
                });
                targetRecord.CommitCommands();
            }
            _itemBlock = null;
            _flagBatchActive = false;
        }
        
        /// <inheritdoc />
        public void Clear(MonoGameLibrary.Core.Primitives.Color color) {
            ThrowIfDisposed();
            if (_flagBatchActive) {
                throw new InvalidOperationException("End the rendering batch before clearing the target.");
            }
            MonoGameLibrary.Core.Primitives.Rectangle rectangleBounds = ViewportBoundsRectangle;
            GodotColor colorNative = ConvertColor(color);
            if (_targetCurrent == null) {
                // Godot has no clear operation for a canvas item; the target is covered by
                // one full-bounds fill, which is what a clear means for the draws of this
                // frame and what a replayed command list preserves across frames.
                _itemAnchor.DrawRect(new Rect2(rectangleBounds.X, rectangleBounds.Y, rectangleBounds.Width, rectangleBounds.Height), colorNative);
            } else {
                RenderTarget targetRecord = ResolveTarget(_targetCurrent);
                targetRecord.ClearCommands();
                targetRecord.RecordCommand(
                    delegate(CanvasItem item) {
                        item.DrawRect(new Rect2(rectangleBounds.X, rectangleBounds.Y, rectangleBounds.Width, rectangleBounds.Height), colorNative);
                    }
                );
                targetRecord.CommitCommands();
            }
        }
        
        /// <inheritdoc />
        public void SetRenderTarget(IRenderTarget targetRenderTarget) {
            ThrowIfDisposed();
            if (targetRenderTarget == null) {
                throw new ArgumentNullException(nameof(targetRenderTarget));
            }
            if (_flagBatchActive) {
                throw new InvalidOperationException("End the rendering batch before binding a render target.");
            }
            ResolveTarget(targetRenderTarget);
            // Godot has no device viewport or scissor behind a target binding (the target
            // is the viewport and canvas items are not scissored), so only the previously
            // bound target is remembered and restored.
            _stackTargets.Push(_targetCurrent);
            _targetCurrent = targetRenderTarget;
        }
        
        /// <inheritdoc />
        public void ResetRenderTarget() {
            ThrowIfDisposed();
            if (_flagBatchActive) {
                throw new InvalidOperationException("End the rendering batch before restoring a render target.");
            }
            if (_stackTargets.Count == 0) {
                _targetCurrent = null;
                return;
            }
            _targetCurrent = _stackTargets.Pop();
        }
        
        private static RenderTarget ResolveTarget(IRenderTarget targetRenderTarget) {
            RenderTarget targetTyped = targetRenderTarget as RenderTarget;
            if (targetTyped == null) {
                throw new NotSupportedException(
                    "The render target is not a Godot render target adapter."
                );
            }
            SubViewport targetNative = targetTyped.NativeTarget;
            if (!targetNative.IsInsideTree()) {
                throw new InvalidOperationException("The render target viewport is not inside the scene tree.");
            }
            return targetTyped;
        }
        
        /// <inheritdoc />
        public void DrawTexture(
            ITwoDimensionalTexture texture,
            TwoDimensionalVector position,
            OptionalValue<MonoGameLibrary.Core.Primitives.Rectangle> rectangleSource,
            MonoGameLibrary.Core.Primitives.Color color,
            float rotation,
            TwoDimensionalVector origin,
            TwoDimensionalVector scale,
            MonoGameLibrary.Extensions.Graphics.SpriteEffects effectsSprite,
            float depthLayer
        ) {
            ThrowIfDisposed();
            if (texture == null) {
                throw new ArgumentNullException(nameof(texture));
            }
            if (!_flagBatchActive) {
                throw new InvalidOperationException("Begin a rendering batch before drawing.");
            }
            Texture2D textureNative = ResolveTexture(texture);
            if (_targetCurrent == null) {
                DrawTextureCore(
                    _itemAnchor, textureNative, position, rectangleSource, color,
                    rotation, origin, scale, effectsSprite, depthLayer, _matrixBlock
                );
            } else {
                RenderTarget targetRecord = ResolveTarget(_targetCurrent);
                Transform2D matrixBlock = _matrixBlock;
                targetRecord.RecordCommand(delegate(CanvasItem item) {
                    DrawTextureCore(
                        item, textureNative, position, rectangleSource, color,
                        rotation, origin, scale, effectsSprite, depthLayer, matrixBlock
                    );
                });
            }
        }
        
        /// <inheritdoc />
        public void DrawTextureRegion(
            ITwoDimensionalTexture texture,
            MonoGameLibrary.Core.Primitives.Rectangle rectangleDestination,
            OptionalValue<MonoGameLibrary.Core.Primitives.Rectangle> rectangleSource,
            MonoGameLibrary.Core.Primitives.Color color
        ) {
            ThrowIfDisposed();
            if (texture == null) {
                throw new ArgumentNullException(nameof(texture));
            }
            if (!_flagBatchActive) {
                throw new InvalidOperationException("Begin a rendering batch before drawing.");
            }
            Texture2D textureNative = ResolveTexture(texture);
            if (_targetCurrent == null) {
                DrawTextureRegionCore(_itemAnchor, textureNative, rectangleDestination, rectangleSource, color, _matrixBlock);
            } else {
                RenderTarget targetRecord = ResolveTarget(_targetCurrent);
                Transform2D matrixBlock = _matrixBlock;
                targetRecord.RecordCommand(
                    delegate(CanvasItem item) {
                        DrawTextureRegionCore(item, textureNative, rectangleDestination, rectangleSource, color, matrixBlock);
                    }
                );
            }
        }
        
        private static Texture2D ResolveTexture(ITwoDimensionalTexture texture) {
            INativeTextureProvider<Texture2D> providerTexture = texture as INativeTextureProvider<Texture2D>;
            if (providerTexture == null) {
                throw new NotSupportedException(
                    $"The texture type '{texture.GetType().FullName}' is not a Godot texture adapter."
                );
            }
            // A released adapter keeps implementing the contract but resolves to null; drawing with that
            // value would fail inside the backend instead of at the call that supplied the texture.
            Texture2D textureNative = providerTexture.GetNativeTexture();
            if (textureNative == null) {
                throw new ObjectDisposedException(
                    texture.GetType().Name, "The texture adapter has been released and no longer holds a texture. "
                );
            }
            return textureNative;
        }
        
        /// <inheritdoc />
        public void Accept(IVisitor visitor) {
            if (visitor == null) {
                throw new ArgumentNullException(nameof(visitor));
            }
            visitor.Visit(this);
        }
        
        /// <summary>
        /// Releases resources held by this context. 
        /// Note: The anchor <see cref="CanvasItem"/> is owned by the hosting integration and shall not be disposed here. 
        /// </summary>
        public void Dispose() {
            if (_flagDisposed) { return; }
            if (_flagBatchActive) {
                try {
                    RestoreItemState(_itemBlock, _filterPrevious, _repeatPrevious, _materialPrevious);
                    _itemBlock.DrawSetTransformMatrix(Transform2D.Identity);
                } catch (Exception exception) {
                    System.Diagnostics.Trace.TraceError("Could not restore the render block state during disposal: {0}", exception);
                }
                _flagBatchActive = false;
                _itemBlock = null;
            }
            if (_targetCurrent != null || _stackTargets.Count > 0) {
                // Never leave the context bound to a target the caller is about to release.
                _targetCurrent = null;
            }
            _stackTargets.Clear();
            _flagDisposed = true;
            // The anchor is managed externally; do not dispose it.
            GC.SuppressFinalize(this);
        }
    }
}
