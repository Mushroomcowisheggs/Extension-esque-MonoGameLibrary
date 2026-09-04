using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using MonoGameLibrary.Core.Content;
using MonoGameLibrary.Core.Primitives;

namespace MonoGameLibrary.Extensions.Graphics {
    /// <summary>
    /// Represents a tilemap drawn from a texture atlas.
    /// Loaded from an XML stream using the format:
    /// <Tilemap>
    ///   <Tileset region="x y width height" widthTile="W" heightTile="H">assetTexture</Tileset>
    ///   <Tiles>
    ///     00 01 02 ...
    ///     03 04 05 ...
    ///   </Tiles>
    /// </Tilemap>
    /// Supports multiple layers, but only the first layer is used (ignores extra layers).
    /// </summary>
    public sealed class Tilemap : IAsset, IDisposable {
        private readonly TextureAtlas _atlas;
        private readonly List<int[,]> _layers; // each layer: [rows, columns]
        private readonly int _columns;
        private readonly int _rows;
        private readonly int _widthTile;
        private readonly int _heightTile;
        private bool _flagDisposed;
        
        /// <summary>Gets the number of columns.</summary>
        public int Columns { get { return _columns; } }
        /// <summary>Gets the number of rows.</summary>
        public int Rows { get { return _rows; } }
        /// <summary>Gets the tile width in source pixels.</summary>
        public int TileWidth { get { return _widthTile; } }
        /// <summary>Gets the tile height in source pixels.</summary>
        public int TileHeight { get { return _heightTile; } }
        /// <summary>
        /// Gets the width of a single tile in pixels, after applying the current scale factor.
        /// This is equivalent to <see cref="TileWidth"/> multiplied by <see cref="Scale.X"/>.
        /// </summary>
        public float ScaledTileWidth { get { return TileWidth * Scale.X; } }
        /// <summary>
        /// Gets the height of a single tile in pixels, after applying the current scale factor.
        /// This is equivalent to <see cref="TileHeight"/> multiplied by <see cref="Scale.Y"/>.
        /// </summary>
        public float ScaledTileHeight { get { return TileHeight * Scale.Y; } }
        /// <summary>Gets the number of layers (only first is populated).</summary>
        public int LayerCount { get { return _layers.Count; } }
        
        /// <summary>Gets or sets the draw scale.</summary>
        public TwoDimensionalVector Scale { get; set; } = TwoDimensionalVector.One;
        /// <summary>Gets or sets the color tint.</summary>
        public Color TintColor { get; set; } = Color.White;
        /// <summary>Gets or sets a drawing offset for scrolling.</summary>
        public TwoDimensionalVector Offset { get; set; } = TwoDimensionalVector.Zero;
        
        private Tilemap(TextureAtlas atlas, int columns, int rows, int widthTile, int heightTile) {
            if (atlas == null) { throw new ArgumentNullException(nameof(atlas)); }
            if (columns <= 0) { throw new ArgumentOutOfRangeException(nameof(columns)); }
            if (rows <= 0) { throw new ArgumentOutOfRangeException(nameof(rows)); }
            if (widthTile <= 0) { throw new ArgumentOutOfRangeException(nameof(widthTile)); }
            if (heightTile <= 0) { throw new ArgumentOutOfRangeException(nameof(heightTile)); }
            
            _atlas = atlas;
            _columns = columns;
            _rows = rows;
            _widthTile = widthTile;
            _heightTile = heightTile;
            _layers = new List<int[,]>();
        }
        
        /// <summary>
        /// Creates a tilemap from an XML stream.
        /// </summary>
        /// <param name="serviceContent">The content service for loading textures.</param>
        /// <param name="stream">The stream containing the XML definition.</param>
        /// <returns>A new <see cref="Tilemap"/> instance.</returns>
        /// <exception cref="ArgumentNullException">Thrown if parameters are null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if XML is invalid.</exception>
        public static Tilemap FromStream(IContentService serviceContent, Stream stream) {
            if (serviceContent == null) { throw new ArgumentNullException(nameof(serviceContent)); }
            if (stream == null) { throw new ArgumentNullException(nameof(stream)); }
            
            XmlDocument document = new XmlDocument();
            document.Load(stream);
            
            XmlNode nodeRoot = document.DocumentElement;
            if (nodeRoot == null) {
                throw new InvalidOperationException("XML document has no root element.");
            }
            if (nodeRoot.Name != "Tilemap") {
                throw new InvalidOperationException(
                    $"Invalid tilemap definition: root element must be 'Tilemap', got '{nodeRoot.Name}'."
                );
            }
            
            // Parse tileset.
            XmlNode nodeTileset = nodeRoot.SelectSingleNode("Tileset");
            if (nodeTileset == null) {
                throw new InvalidOperationException("Missing <Tileset> element.");
            }
            
            string nameRegion = GetAttribute(nodeTileset, "region");
            if (string.IsNullOrWhiteSpace(nameRegion)) {
                throw new InvalidOperationException("Tileset missing 'region' attribute.");
            }
            string[] parts = nameRegion.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4) {
                throw new InvalidOperationException("Tileset 'region' must have 4 integer values (x y width height).");
            }
            if (
                !int.TryParse(parts[0], out int texX) ||
                !int.TryParse(parts[1], out int texY) ||
                !int.TryParse(parts[2], out int texWidth) ||
                !int.TryParse(parts[3], out int texHeight)
            ) {
                throw new InvalidOperationException("Tileset 'region' values must be integers.");
            }
            
            string nameTileWidth = GetAttribute(nodeTileset, "widthTile");
            string nameTileHeight = GetAttribute(nodeTileset, "heightTile");
            if (string.IsNullOrWhiteSpace(nameTileWidth) || string.IsNullOrWhiteSpace(nameTileHeight)) {
                throw new InvalidOperationException("Tileset missing widthTile or heightTile.");
            }
            if (!int.TryParse(nameTileWidth, out int widthTile) || !int.TryParse(nameTileHeight, out int heightTile)) {
                throw new InvalidOperationException("widthTile and heightTile must be integers.");
            }
            
            string assetTexture = nodeTileset.InnerText;
            if (assetTexture != null) {
                assetTexture = assetTexture.Trim();
            }
            if (string.IsNullOrWhiteSpace(assetTexture)) {
                throw new InvalidOperationException("Tileset texture asset path is empty.");
            }
            
            ITwoDimensionalTexture textureAsset = serviceContent.Load<ITwoDimensionalTexture>(assetTexture);
            TextureAtlas atlas = new TextureAtlas(textureAsset);
            // We don't need to add regions yet; we'll compute tile regions on the fly. 
            
            // Determine tile count and precompute tile regions.
            int tilesPerRow = texWidth / widthTile;
            int tilesPerCol = texHeight / heightTile;
            int tilesTotal = tilesPerRow * tilesPerCol;
            Dictionary<int, TextureRegion> regionsTile = new Dictionary<int, TextureRegion>();
            for (int i = 0; i < tilesTotal; i += 1) {
                int tileX = texX + (i % tilesPerRow) * widthTile;
                int tileY = texY + (i / tilesPerRow) * heightTile;
                TextureRegion regionTile = new TextureRegion(textureAsset, new Rectangle(tileX, tileY, widthTile, heightTile));
                regionsTile[i] = regionTile;
            }
            
            // Store regionsTile in a local variable for drawing.
            // Parse tile data.
            XmlNode nodeTiles = nodeRoot.SelectSingleNode("Tiles");
            if (nodeTiles == null) {
                throw new InvalidOperationException("Missing <Tiles> element.");
            }
            
            string data = nodeTiles.InnerText;
            if (data != null) {
                data = data.Trim();
            }
            if (string.IsNullOrWhiteSpace(data)) {
                throw new InvalidOperationException("Tiles data is empty.");
            }
            
            string[] rows = data.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (rows.Length == 0) {
                throw new InvalidOperationException("No rows found in Tiles data.");
            }
            
            int countColumns = rows[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
            int countRows = rows.Length;
            
            Tilemap tilemap = new Tilemap(atlas, countColumns, countRows, widthTile, heightTile);
            
            // Create a single layer.
            int[,] layer = new int[countRows, countColumns];
            for (int r = 0; r < countRows; r += 1) {
                string[] columns = rows[r].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (columns.Length != countColumns) {
                    throw new InvalidOperationException($"Row {r} has {columns.Length} columns, expected {countColumns}.");
                }
                for (int c = 0; c < countColumns; c += 1) {
                    if (!int.TryParse(columns[c], out int indexTile)) {
                        throw new InvalidOperationException($"Invalid tile index at ({r},{c}): '{columns[c]}'.");
                    }
                    layer[r, c] = indexTile;
                }
            }
            tilemap._layers.Add(layer);
            
            // Store tile regions for quick lookup.
            tilemap._tileRegions = regionsTile;
            
            return tilemap;
        }
        
        private static string GetAttribute(XmlNode node, string attributeName) {
            if (node == null) { return null; }
            XmlAttribute attribute = node.Attributes[attributeName];
            if (attribute == null) { return null; }
            return attribute.Value;
        }
        
        private Dictionary<int, TextureRegion> _tileRegions; // cached for drawing
        
        /// <summary>
        /// Gets the tile index at the specified position (layer 0).
        /// </summary>
        public int GetTileIndex(int column, int row) {
            return GetTileIndex(0, column, row);
        }
        
        /// <summary>
        /// Gets the tile index at the specified position and layer.
        /// </summary>
        public int GetTileIndex(int indexLayer, int column, int row) {
            if (indexLayer < 0 || indexLayer >= _layers.Count) { return -1; }
            if (column < 0 || column >= _columns || row < 0 || row >= _rows) { return -1; }
            return _layers[indexLayer][row, column];
        }
        
        /// <summary>
        /// Sets the tile index (layer 0).
        /// </summary>
        public void SetTileIndex(int column, int row, int indexTile) {
            SetTileIndex(0, column, row, indexTile);
        }
        
        /// <summary>
        /// Sets the tile index for a specific layer.
        /// </summary>
        public void SetTileIndex(int indexLayer, int column, int row, int indexTile) {
            if (indexLayer < 0 || indexLayer >= _layers.Count) { return; }
            if (column < 0 || column >= _columns || row < 0 || row >= _rows) { return; }
            _layers[indexLayer][row, column] = indexTile;
        }
        
        /// <summary>
        /// Draws all layers.
        /// </summary>
        /// <param name="contextRender">The render context to draw with.</param>
        /// <exception cref="ArgumentNullException">Thrown if batchSprite is null.</exception>
        public void Draw(IRenderContext contextRender) {
            if (contextRender == null) { throw new ArgumentNullException(nameof(contextRender)); }
            if (_atlas.Texture == null) { return; }
            
            foreach (int[,] layer in _layers) {
                DrawLayer(contextRender, layer);
            }
        }
        
        private void DrawLayer(IRenderContext contextRender, int[,] layer) {
            for (int row = 0; row < _rows; row += 1) {
                for (int column = 0; column < _columns; column += 1) {
                    int indexTile = layer[row, column];
                    if (indexTile < 0) { continue; }
                    
                    TextureRegion region = GetTileRegion(indexTile);
                    if (region == null) { continue; }
                    
                    TwoDimensionalVector position = new TwoDimensionalVector(
                        column * _widthTile * Scale.X + Offset.X,
                        row * _heightTile * Scale.Y + Offset.Y
                    );
                    
                    region.Texture.DrawInto(
                        contextRender,
                        position,
                        new OptionalValue<Rectangle>(region.SourceRectangle),
                        TintColor,
                        0f, // rotation
                        TwoDimensionalVector.Zero, // origin
                        Scale,
                        SpriteEffects.None,
                        0f // layer depth
                    );
                }
            }
        }
        
        private TextureRegion GetTileRegion(int indexTile) {
            if (_tileRegions.TryGetValue(indexTile, out TextureRegion region)) {
                return region;
            }
            return null;
        }
        
        /// <summary>
        /// Disposes the tilemap (does not dispose the atlas, as it may be shared).
        /// </summary>
        public void Dispose() {
            if (_flagDisposed) { return; }
            _layers.Clear();
            if (_tileRegions != null) {
                _tileRegions.Clear();
            }
            if (_atlas != null) {
                _atlas.Dispose();
            }
            _flagDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
