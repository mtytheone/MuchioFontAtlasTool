using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace HatzeLaboratory.Muchio.FontAtlasTool.Editor
{
    /// <summary>
    /// 字形を焼いてアトラス1枚分のピクセルを組み立てる
    /// </summary>
    public sealed class AtlasBaker
    {
        /// <summary>
        /// 一度にフォントアトラスへ焼く文字数
        /// </summary>
        /// <remarks>
        /// フォントアトラスに一度に全文字入らないことがあるのでバッチに分ける
        /// </remarks>
        private const int ONCE_BAKE_CHARACTER_COUNT = 48;

        private readonly MuchioFontAtlasSettings _settings;
        private readonly GlyphRasterizer _glyphRasterizer;

        public AtlasBaker(MuchioFontAtlasSettings settings)
        {
            _settings = settings;
            _glyphRasterizer = new GlyphRasterizer(settings);
        }

        /// <summary>
        /// 全タイルの字形を焼いて、アトラス1枚分のピクセルを作る
        /// </summary>
        /// <param name="progressBarTitle">進捗ダイアログのタイトル</param>
        /// <param name="pixelList">焼き上がったアトラス1枚分のピクセル</param>
        /// <returns>最後まで焼けたかどうか。false ならユーザーがキャンセルした</returns>
        public bool TryBakeAtlas(string progressBarTitle, out Color32[] pixelList)
        {
            // 4096x8192 だと約128MB
            pixelList = new Color32[_settings.AtlasTextureWidth * _settings.AtlasTextureHeight];
            FillBackground(pixelList);

            List<int> targetTileIndexList = CharacterTable.CollectTargetTileIndexList(_settings.BuildCharacterList, _settings.CellCount);
            for (int batchIndex = 0; batchIndex < targetTileIndexList.Count; batchIndex += ONCE_BAKE_CHARACTER_COUNT)
            {
                int count = Mathf.Min(ONCE_BAKE_CHARACTER_COUNT, targetTileIndexList.Count - batchIndex);
                if (EditorUtility.DisplayCancelableProgressBar(
                        progressBarTitle,
                        "Rasterizing... " + (batchIndex + count) + " / " + targetTileIndexList.Count,
                        (float)(batchIndex + count) / targetTileIndexList.Count))
                {
                    return false;
                }

                BakeBatch(pixelList, targetTileIndexList, batchIndex, count);
            }

            return true;
        }

        /// <summary>
        /// サンプル文字だけを横に並べて焼き、プレビュー用テクスチャを作る
        /// </summary>
        /// <remarks>
        /// アトラス全体を焼くと時間がかかるため、代表的な数文字だけを焼く。
        /// フォントサイズ・ベースライン・縁取りの当たりはこれで判断できる
        /// </remarks>
        /// <returns>プレビュー用テクスチャ。呼び出し側で DestroyImmediate すること</returns>
        public Texture2D BakePreviewTexture()
        {
            string sampleCharacters = _settings.PreviewSampleCharacters;
            int previewWidth = _settings.CellWidth * sampleCharacters.Length;
            int previewHeight = _settings.CellHeight;

            Color32[] pixelList = new Color32[previewWidth * previewHeight];
            FillBackground(pixelList);

            Texture2D glyphAtlasTexture = _glyphRasterizer.BakeReadableGlyphTexture(sampleCharacters);
            try
            {
                GlyphAtlasSource source = new(glyphAtlasTexture);
                for (int i = 0; i < sampleCharacters.Length; i++)
                {
                    BakeCell(pixelList, previewWidth, previewHeight, source, sampleCharacters[i], cellColumn: i, cellRow: 0);
                }
            }
            finally
            {
                Object.DestroyImmediate(glyphAtlasTexture);
            }

            Texture2D previewTexture = new(previewWidth, previewHeight, TextureFormat.RGBA32, mipChain: false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            previewTexture.SetPixels32(pixelList);
            previewTexture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            return previewTexture;
        }

        /// <summary>
        /// 1バッチぶんの文字をフォントアトラスへ焼き、各タイルへ書き出す
        /// </summary>
        /// <param name="pixelList">書き出し先のアトラス1枚分のピクセル</param>
        /// <param name="targetTileIndexList">対象のタイル番号リスト</param>
        /// <param name="batchIndex">このバッチが始まるリスト上の位置</param>
        /// <param name="count">このバッチで焼く文字数</param>
        private void BakeBatch(Color32[] pixelList, List<int> targetTileIndexList, int batchIndex, int count)
        {
            string buildCharacterList = _settings.BuildCharacterList;
            StringBuilder stringBuilder = new(count);
            for (int i = 0; i < count; i++)
            {
                stringBuilder.Append(buildCharacterList[targetTileIndexList[batchIndex + i]]);
            }

            Texture2D glyphAtlasTexture = _glyphRasterizer.BakeReadableGlyphTexture(stringBuilder.ToString());
            try
            {
                GlyphAtlasSource source = new(glyphAtlasTexture);
                for (int i = 0; i < count; i++)
                {
                    int tileIndex = targetTileIndexList[batchIndex + i];
                    char character = buildCharacterList[tileIndex];
                    bool isBaked = BakeCell(
                        pixelList,
                        _settings.AtlasTextureWidth,
                        _settings.AtlasTextureHeight,
                        source,
                        character,
                        tileIndex % _settings.ColumnCount,
                        tileIndex / _settings.ColumnCount);

                    if (!isBaked)
                    {
                        Debug.LogWarning("[MuchioFontAtlas] 字が焼けませんでした: '" + character + "' (tile " + tileIndex + ")");
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(glyphAtlasTexture);
            }
        }

        /// <summary>
        /// 1文字ぶんの字形を、指定した位置のセルへ書き出す
        /// </summary>
        /// <remarks>
        /// アトラスとプレビューで書き出し先の大きさが違うだけなので、
        /// 対象の大きさとセル位置を受け取って共通化している
        /// </remarks>
        /// <param name="pixelList">書き出し先のピクセル</param>
        /// <param name="targetWidth">書き出し先の横幅</param>
        /// <param name="targetHeight">書き出し先の縦幅</param>
        /// <param name="source">焼き上がった字形テクスチャ</param>
        /// <param name="character">書き出す文字</param>
        /// <param name="cellColumn">書き出すセルの列</param>
        /// <param name="cellRow">書き出すセルの行</param>
        /// <returns>書き出せたかどうか</returns>
        private bool BakeCell(
            Color32[] pixelList,
            int targetWidth,
            int targetHeight,
            in GlyphAtlasSource source,
            char character,
            int cellColumn,
            int cellRow)
        {
            if (!_glyphRasterizer.TryCalculateCoverageRate(character, source, out float[] coverageRateList))
            {
                return false;
            }

            int cellWidth = _settings.CellWidth;
            int cellHeight = _settings.CellHeight;
            float[] distanceFieldList = DistanceFieldCalculator.Calculate(coverageRateList, cellWidth, cellHeight);

            // Texture2D は下原点なので行を反転して詰める
            int cx = cellColumn * cellWidth;
            int cy = targetHeight - (cellRow + 1) * cellHeight;
            for (int y = 0; y < cellHeight; y++)
            {
                int sourceRow = (cellHeight - 1 - y) * cellWidth;
                int destinationRow = (cy + y) * targetWidth + cx;
                for (int x = 0; x < cellWidth; x++)
                {
                    pixelList[destinationRow + x] = ResolvePixel(coverageRateList[sourceRow + x], distanceFieldList[sourceRow + x]);
                }
            }

            return true;
        }

        /// <summary>
        /// アトラス全体を背景アルファで塗りつぶす
        /// </summary>
        /// <remarks>
        /// 字形を持たないタイルには書き出しが行われないため、
        /// ここで塗っておかないと不透明を選んだときに空白文字のタイルだけ穴が空く
        /// </remarks>
        /// <param name="pixelList">塗りつぶす対象のピクセル</param>
        private void FillBackground(Color32[] pixelList)
        {
            byte backgroundAlpha = (byte)Mathf.RoundToInt(_settings.BackgroundAlpha * byte.MaxValue);
            if (backgroundAlpha == 0)
            {
                // 透明なら default(Color32) のままで良い
                return;
            }

            Color32 background = new(0, 0, 0, backgroundAlpha);
            for (int i = 0; i < pixelList.Length; i++)
            {
                pixelList[i] = background;
            }
        }

        /// <summary>
        /// 被膜率と距離場から1ピクセルの色を決める
        /// </summary>
        /// <param name="coverageRate">そのピクセルの被膜率</param>
        /// <param name="distance">そのピクセルから字形までの距離</param>
        /// <returns>書き込む色</returns>
        private Color32 ResolvePixel(float coverageRate, float distance)
        {
            float outline = Mathf.Clamp01(_settings.OutlineWidth + 0.5f - distance);
            byte rgb = (byte)Mathf.RoundToInt(Mathf.Clamp01(coverageRate) * byte.MaxValue);
            byte alpha = (byte)Mathf.RoundToInt(Mathf.Max(_settings.BackgroundAlpha, outline) * byte.MaxValue);
            return new Color32(rgb, rgb, rgb, alpha);
        }
    }
}
