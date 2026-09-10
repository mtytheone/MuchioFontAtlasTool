using System;
using UnityEngine;

namespace HatzeLaboratory.Muchio.FontAtlasTool.Editor
{
    /// <summary>
    /// セル背景の扱い
    /// </summary>
    public enum CellBackground
    {
        Transparent,
        Opaque,
    }

    /// <summary>
    /// 文字盤アトラスの生成設定
    /// </summary>
    /// <remarks>
    /// EditorWindow が [SerializeField] で保持することを前提にしている。
    /// ScriptableObject にすると Undo.RecordObject(window) の記録対象から外れ、
    /// Undo/Redo が効かなくなるので、あくまで [Serializable] な入れ子クラスにすること
    /// </remarks>
    [Serializable]
    public sealed class MuchioFontAtlasSettings
    {
        public const string DEFAULT_SAMPLE_CHARACTERS = "Aapあ゛";

        private const int DEFAULT_FONT_SIZE = 240;
        private const int DEFAULT_ATLAS_WIDTH = 4096;
        private const int DEFAULT_ATLAS_HEIGHT = 8192;
        private const int DEFAULT_COLUMN_COUNT = 16;
        private const int DEFAULT_ROW_COUNT = 16;
        private const int DEFAULT_OUTLINE_WIDTH = 6;
        private const float DEFAULT_BASELINE_RATIO = 0.67f;

        [SerializeField]
        private Font _font;
        [SerializeField]
        private Material _applyToMaterial;
        [SerializeField]
        private string _buildCharacterList;
        [SerializeField]
        private string _previewSampleCharacters;
        [SerializeField]
        private CellBackground _cellBackground = CellBackground.Transparent;
        [SerializeField]
        private int _fontSize = DEFAULT_FONT_SIZE;
        [SerializeField]
        private int _atlasTextureWidth = DEFAULT_ATLAS_WIDTH;
        [SerializeField]
        private int _atlasTextureHeight = DEFAULT_ATLAS_HEIGHT;
        [SerializeField]
        private int _columnCount = DEFAULT_COLUMN_COUNT;
        [SerializeField]
        private int _rowCount = DEFAULT_ROW_COUNT;
        [SerializeField]
        private int _outlineWidth = DEFAULT_OUTLINE_WIDTH;
        [SerializeField]
        private int _characterOriginOffsetX;
        [SerializeField]
        private float _baselineRatio = DEFAULT_BASELINE_RATIO;
        [SerializeField]
        private bool _compressed;

        public Font Font { get => _font; set => _font = value; }
        public Material ApplyToMaterial { get => _applyToMaterial; set => _applyToMaterial = value; }
        public string BuildCharacterList { get => _buildCharacterList; set => _buildCharacterList = value; }
        public string PreviewSampleCharacters { get => _previewSampleCharacters; set => _previewSampleCharacters = value; }
        public CellBackground CellBackground { get => _cellBackground; set => _cellBackground = value; }
        public int FontSize { get => _fontSize; set => _fontSize = value; }
        public int AtlasTextureWidth { get => _atlasTextureWidth; set => _atlasTextureWidth = value; }
        public int AtlasTextureHeight { get => _atlasTextureHeight; set => _atlasTextureHeight = value; }
        public int ColumnCount { get => _columnCount; set => _columnCount = value; }
        public int RowCount { get => _rowCount; set => _rowCount = value; }
        public int OutlineWidth { get => _outlineWidth; set => _outlineWidth = value; }
        public int CharacterOriginOffsetX { get => _characterOriginOffsetX; set => _characterOriginOffsetX = value; }
        public float BaselineRatio { get => _baselineRatio; set => _baselineRatio = value; }
        public bool Compressed { get => _compressed; set => _compressed = value; }

        /// <summary>
        /// 文字のあるセルの背景アルファ
        /// </summary>
        public float BackgroundAlpha => _cellBackground == CellBackground.Opaque ? 1f : 0f;

        /// <summary>
        /// テクスチャサイズがセル分割数で割り切れるかどうか
        /// </summary>
        public bool IsAtlasSizeDivisible =>
            _atlasTextureWidth % _columnCount == 0 && _atlasTextureHeight % _rowCount == 0;

        /// <summary>
        /// 1セルの横幅。<see cref="IsAtlasSizeDivisible"/> が true のときのみ有効
        /// </summary>
        public int CellWidth => _atlasTextureWidth / _columnCount;

        /// <summary>
        /// 1セルの縦幅。<see cref="IsAtlasSizeDivisible"/> が true のときのみ有効
        /// </summary>
        public int CellHeight => _atlasTextureHeight / _rowCount;

        /// <summary>
        /// アトラスに並ぶタイルの総数
        /// </summary>
        public int CellCount => _columnCount * _rowCount;

        /// <summary>
        /// 字形を焼けるフォントが指定されているかどうか
        /// </summary>
        /// <remarks>
        /// 動的フォントでないと <see cref="Font.RequestCharactersInTexture"/> が使えない
        /// </remarks>
        public bool IsFontUsable => _font && _font.dynamic;

        /// <summary>
        /// 項目が未設定かどうか
        /// </summary>
        /// <remarks>
        /// ドメインリロードをまたいだ値は Unity が復元するので、
        /// 未設定のときだけ埋めてユーザーの編集を消さないようにする
        /// </remarks>
        public bool HasBuildCharacterList => !string.IsNullOrEmpty(_buildCharacterList);

        /// <inheritdoc cref="HasBuildCharacterList"/>
        public bool HasPreviewSampleCharacters => !string.IsNullOrEmpty(_previewSampleCharacters);

        /// <inheritdoc cref="HasBuildCharacterList"/>
        public bool HasFont => _font;
    }
}
