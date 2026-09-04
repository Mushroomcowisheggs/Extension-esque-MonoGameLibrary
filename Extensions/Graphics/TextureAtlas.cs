using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Core.Primitives;

namespace MonoGameLibrary.Extensions.Graphics {
    /// <summary>
    /// Represents a texture atlas containing named regions and animations.
    /// Loaded from an XML stream using the format:
    /// <TextureAtlas>
    ///   <Texture>assetTexture</Texture>
    ///   <Regions>
    ///     <Region name="..." x="..." y="..." width="..." height="..." />
    ///   </Regions>
    ///   <Animations>
    ///     <Animation name="..." delay="100">
    ///       <Frame region="..." />
    ///     </Animation>
    ///   </Animations>
    /// </TextureAtlas>
    /// </summary>
    public sealed class TextureAtlas : IAsset, IDisposable {
        private readonly Dictionary<string, TextureRegion> _regions;
        private readonly Dictionary<string, Animation> _animations;
        private bool _flagDisposed;
        
        /// <summary>Gets the texture containing all regions. </summary>
        public ITwoDimensionalTexture Texture { get; private set; }
        
        private TextureAtlas() {
            _regions = new Dictionary<string, TextureRegion>();
            _animations = new Dictionary<string, Animation>();
        }
        
        public TextureAtlas(ITwoDimensionalTexture texture) {
            if (texture == null) {
                throw new ArgumentNullException(nameof(texture));
            }
            Texture = texture;
            _regions = new Dictionary<string, TextureRegion>();
            _animations = new Dictionary<string, Animation>();
        }
        
        /// <summary>
        /// Creates a texture atlas from an XML stream.
        /// </summary>
        /// <param name="serviceContent">The content service for loading textures.</param>
        /// <param name="stream">The stream containing the XML definition.</param>
        /// <returns>A new <see cref="TextureAtlas"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="serviceContent"/> or <paramref name="stream"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if XML format is invalid.</exception>
        public static TextureAtlas FromStream(IContentService serviceContent, Stream stream) {
            if (serviceContent == null) { throw new ArgumentNullException(nameof(serviceContent)); }
            if (stream == null) { throw new ArgumentNullException(nameof(stream)); }
            
            XmlDocument document = new XmlDocument();
            document.Load(stream);
            
            XmlNode nodeRoot = document.DocumentElement;
            if (nodeRoot == null) {
                throw new InvalidOperationException("XML document has no root element.");
            }
            if (nodeRoot.Name != "TextureAtlas") {
                throw new InvalidOperationException(
                    $"Invalid atlas definition: root element must be 'TextureAtlas', got '{nodeRoot.Name}'."
                );
            }
            
            // Load texture.
            XmlNode nodeTexture = nodeRoot.SelectSingleNode("Texture");
            if (nodeTexture == null) {
                throw new InvalidOperationException("Missing <Texture> element.");
            }
            string assetTexture = nodeTexture.InnerText;
            if (assetTexture != null) {
                assetTexture = assetTexture.Trim();
            }
            if (string.IsNullOrWhiteSpace(assetTexture)) {
                throw new InvalidOperationException("Texture asset path is empty.");
            }
            
            ITwoDimensionalTexture textureAsset = serviceContent.Load<ITwoDimensionalTexture>(assetTexture);
            TextureAtlas atlas = new TextureAtlas(textureAsset);
            
            // Parse regions.
            XmlNode nodeRegions = nodeRoot.SelectSingleNode("Regions");
            if (nodeRegions != null) {
                foreach (XmlNode nodeRegion in nodeRegions.ChildNodes) {
                    if (nodeRegion.NodeType != XmlNodeType.Element) { continue; }
                    if (nodeRegion.Name != "Region") { continue; }
                    
                    string name = GetAttribute(nodeRegion, "name");
                    if (string.IsNullOrWhiteSpace(name)) {
                        throw new InvalidOperationException("Region missing 'name' attribute.");
                    }
                    int x = GetIntAttribute(nodeRegion, "x");
                    int y = GetIntAttribute(nodeRegion, "y");
                    int width = GetIntAttribute(nodeRegion, "width");
                    int height = GetIntAttribute(nodeRegion, "height");
                    
                    TextureRegion region = new TextureRegion(atlas.Texture, new Rectangle(x, y, width, height));
                    if (!atlas._regions.TryAdd(name, region)) {
                        throw new InvalidOperationException($"Duplicate region name '{name}'.");
                    }
                }
            }
            
            // Parse animations.
            XmlNode nodeAnimations = nodeRoot.SelectSingleNode("Animations");
            if (nodeAnimations != null) {
                foreach (XmlNode nodeAnimation in nodeAnimations.ChildNodes) {
                    if (nodeAnimation.NodeType != XmlNodeType.Element) { continue; }
                    if (nodeAnimation.Name != "Animation") { continue; }
                    
                    string name = GetAttribute(nodeAnimation, "name");
                    if (string.IsNullOrWhiteSpace(name)) {
                        throw new InvalidOperationException("Animation missing 'name' attribute.");
                    }
                    int millisecondsDelay = 100;
                    string attributeDelay = GetAttribute(nodeAnimation, "delay");
                    if (!string.IsNullOrWhiteSpace(attributeDelay)) {
                        if (!int.TryParse(attributeDelay, out millisecondsDelay) || millisecondsDelay <= 0) {
                            millisecondsDelay = 100;
                        }
                    }
                    
                    Animation animation = new Animation();
                    animation.Delay = TimeSpan.FromMilliseconds(millisecondsDelay);
                    
                    foreach (XmlNode nodeFrame in nodeAnimation.ChildNodes) {
                        if (nodeFrame.NodeType != XmlNodeType.Element) { continue; }
                        if (nodeFrame.Name != "Frame") { continue; }
                        
                        string nameRegion = GetAttribute(nodeFrame, "region");
                        if (string.IsNullOrWhiteSpace(nameRegion)) {
                            throw new InvalidOperationException($"Frame in animation '{name}' missing 'region' attribute.");
                        }
                        if (!atlas._regions.TryGetValue(nameRegion, out TextureRegion region)) {
                            throw new InvalidOperationException(
                                $"Animation '{name}' references unknown region '{nameRegion}'."
                            );
                        }
                        animation.Frames.Add(region);
                    }
                    
                    if (animation.Frames.Count == 0) {
                        throw new InvalidOperationException($"Animation '{name}' has no frames.");
                    }
                    if (!atlas._animations.TryAdd(name, animation)) {
                        throw new InvalidOperationException($"Duplicate animation name '{name}'.");
                    }
                }
            }
            
            return atlas;
        }
        
        private static string GetAttribute(XmlNode node, string nameAttribute) {
            if (node == null) { return null; }
            XmlAttribute attribute = node.Attributes[nameAttribute];
            if (attribute == null) { return null; }
            return attribute.Value;
        }
        
        private static int GetIntAttribute(XmlNode node, string nameAttribute) {
            string value = GetAttribute(node, nameAttribute);
            if (string.IsNullOrWhiteSpace(value)) {
                throw new InvalidOperationException($"Missing or empty attribute '{nameAttribute}'.");
            }
            if (!int.TryParse(value, out int result)) {
                throw new InvalidOperationException($"Invalid integer value for attribute '{nameAttribute}': '{value}'.");
            }
            return result;
        }
        
        /// <summary>
        /// Gets a texture region by name.
        /// </summary>
        /// <param name="name">The region name.</param>
        /// <returns>The <see cref="TextureRegion"/>.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the region does not exist.</exception>
        public TextureRegion GetRegion(string name) {
            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException("Region name cannot be null or empty.", nameof(name));
            }
            if (_regions.TryGetValue(name, out TextureRegion region)) {
                return region;
            }
            throw new KeyNotFoundException($"Texture region '{name}' not found.");
        }
        
        /// <summary>
        /// Indexer for retrieving a region by name.
        /// </summary>
        public TextureRegion this[string name] {
            get { return GetRegion(name); }
        }
        
        /// <summary>
        /// Gets an animation by name.
        /// </summary>
        /// <param name="name">The animation name.</param>
        /// <returns>The <see cref="Animation"/>.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the animation does not exist.</exception>
        public Animation GetAnimation(string name) {
            if (string.IsNullOrWhiteSpace(name)) {
                throw new ArgumentException("Animation name cannot be null or empty.", nameof(name));
            }
            if (_animations.TryGetValue(name, out Animation animation)) {
                return animation;
            }
            throw new KeyNotFoundException($"Animation '{name}' not found.");
        }
        
        /// <summary>
        /// Creates an animated sprite from a named animation.
        /// </summary>
        /// <param name="nameAnimation">The animation name.</param>
        /// <returns>An <see cref="AnimatedSprite"/> instance.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if the animation does not exist.</exception>
        public AnimatedSprite CreateAnimatedSprite(string nameAnimation) {
            if (string.IsNullOrWhiteSpace(nameAnimation)) {
                throw new ArgumentException("Animation name cannot be null or empty.", nameof(nameAnimation));
            }
            if (_animations.TryGetValue(nameAnimation, out Animation animation)) {
                return new AnimatedSprite(animation);
            }
            throw new KeyNotFoundException($"Animation '{nameAnimation}' not found.");
        }
        
        /// <summary>
        /// Tries to create an animated sprite.
        /// </summary>
        /// <param name="nameAnimation">The animation name.</param>
        /// <param name="spriteAnimated">The created sprite, or null if not found.</param>
        /// <returns>true if successful; otherwise false.</returns>
        public bool TryCreateAnimatedSprite(string nameAnimation, out AnimatedSprite spriteAnimated) {
            spriteAnimated = null;
            if (string.IsNullOrWhiteSpace(nameAnimation)) { return false; }
            if (_animations.TryGetValue(nameAnimation, out Animation animation)) {
                spriteAnimated = new AnimatedSprite(animation);
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Disposes the underlying texture.
        /// </summary>
        public void Dispose() {
            if (_flagDisposed) { return; }
            _regions.Clear();
            _animations.Clear();
            _flagDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
