using UnityEngine;

namespace HatzeLaboratory.Muchio.FontAtlasTool.Editor
{
    /// <summary>
    /// 1バッチぶん焼いた字形テクスチャと、その読み取りに必要な情報
    /// </summary>
    public readonly struct GlyphAtlasSource
    {
        /// <summary>
        /// 字形アトラステクスチャの色情報リスト
        /// </summary>
        public readonly Color32[] PixelColorList;

        /// <summary>
        /// 字形アトラステクスチャの横幅
        /// </summary>
        public readonly int TextureWidth;

        /// <summary>
        /// 字形アトラステクスチャの縦幅
        /// </summary>
        public readonly int TextureHeight;

        /// <summary>
        /// 字形の被膜率が色配列のRGBに保存されているかどうか
        /// </summary>
        public readonly bool IsCoverageRateSavedRGB;

        public GlyphAtlasSource(Texture2D glyphAtlasTexture)
        {
            PixelColorList = glyphAtlasTexture.GetPixels32();
            TextureWidth = glyphAtlasTexture.width;
            TextureHeight = glyphAtlasTexture.height;
            IsCoverageRateSavedRGB = GlyphRasterizer.IsCoverageRateSavedRGB(PixelColorList);
        }
    }

    /// <summary>
    /// フォントから字形を焼き、セル1つぶんの被膜率へ展開する
    /// </summary>
    public sealed class GlyphRasterizer
    {
        private readonly MuchioFontAtlasSettings _settings;

        public GlyphRasterizer(MuchioFontAtlasSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// 指定した文字をフォントアトラスへ焼き、その内容をCPUから読める字形テクスチャとして複製
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="Font.RequestCharactersInTexture"/> はフォント内部のアトラスを書き換える副作用を持つ。
        /// </para>
        ///
        /// <para>
        /// 呼び出し後は <see cref="Font.GetCharacterInfo"/> が同じ文字集合の字形情報を返すようになるため、
        /// 字形の展開は必ずこの関数を通してから行うこと。
        /// </para>
        ///
        /// <para>
        /// 動的フォントのアトラスはGPU上にしか無く isReadable が false なので、
        /// RenderTexture 経由で読み戻さないと <see cref="Texture2D.GetPixels32"/> が例外を投げる。
        /// 併せて Alpha8 から ARGB32 へ変換される。
        /// </para>
        /// </remarks>
        /// <param name="bakeCharacterList">アトラスへ焼く文字のリスト</param>
        /// <returns>
        /// CPUから読める字形テクスチャ。呼び出し側で DestroyImmediate すること
        /// </returns>
        public Texture2D BakeReadableGlyphTexture(string bakeCharacterList)
        {
            Font font = _settings.Font;
            font.RequestCharactersInTexture(bakeCharacterList, _settings.FontSize, FontStyle.Normal);

            Texture sourceFontTexture = font.material.mainTexture;
            RenderTexture renderTexture = RenderTexture.GetTemporary(
                sourceFontTexture.width,
                sourceFontTexture.height,
                depthBuffer: 0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear
            );

            RenderTexture prev = RenderTexture.active;
            Graphics.Blit(sourceFontTexture, renderTexture);
            RenderTexture.active = renderTexture;

            Texture2D outputTexture = new(
                sourceFontTexture.width,
                sourceFontTexture.height,
                TextureFormat.RGBA32,
                mipChain: false,
                linear: true
            );

            Rect rect = new(x: 0, y: 0, sourceFontTexture.width, sourceFontTexture.height);
            outputTexture.ReadPixels(rect, destX: 0, destY: 0);
            outputTexture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(renderTexture);
            return outputTexture;
        }

        /// <summary>
        /// 各ピクセルが字形のインクにどれだけ覆われているかという字形の被膜率が
        /// 色配列のRGBに保存されているかどうか
        /// </summary>
        /// <param name="pixelColorList">ピクセルごとの色配列</param>
        /// <remarks>
        /// 動的フォントのアトラスは Alpha8（1チャンネルのみ）
        /// これを Graphics.Blit で ARGB32 に変換した時、
        /// その1チャンネルがどこへ展開されるかが
        /// グラフィックスAPIやプラットフォームによって違うため
        /// この関数を用意して判定している
        /// </remarks>
        /// <returns>
        /// <para>true: アルファが全ピクセル255＝情報を持たない。字形被膜率はRGB側にある</para>
        /// <para>false: アルファに濃淡がある。字形被膜率はアルファ側にある</para>
        /// </returns>
        public static bool IsCoverageRateSavedRGB(Color32[] pixelColorList)
        {
            // テクスチャ幅は通常2のべき乗であることが多いことを利用し、
            // 公約数を持たない素数97を利用して走査を間引いている
            for (int i = 0; i < pixelColorList.Length; i += 97)
            {
                if (pixelColorList[i].a != 255)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 1文字ぶんの字形の被膜率を算出
        /// </summary>
        /// <remarks>
        /// 各ピクセルが字形のインクにどれだけ覆われているかを示す被膜率を、
        /// セル1つぶんのバッファへ展開する。
        /// 字形が見つからない文字や、字形を持たない文字（フォントによっては ゛ ゜ など）は
        /// 焼く対象が無いので false を返す
        /// </remarks>
        /// <param name="character">対象の文字</param>
        /// <param name="source">焼き上がった字形テクスチャ</param>
        /// <param name="coverageRateList">算出された字形の被膜率を格納するリスト</param>
        /// <returns>算出できたかどうか</returns>
        public bool TryCalculateCoverageRate(char character, in GlyphAtlasSource source, out float[] coverageRateList)
        {
            coverageRateList = null;
            if (!_settings.Font.GetCharacterInfo(character, out CharacterInfo characterInfo, _settings.FontSize, FontStyle.Normal))
            {
                return false;
            }

            if (characterInfo.maxX <= characterInfo.minX || characterInfo.maxY <= characterInfo.minY)
            {
                return false;
            }

            int cellWidth = _settings.CellWidth;
            int cellHeight = _settings.CellHeight;
            int glyphWidth = characterInfo.maxX - characterInfo.minX;
            int glyphHeight = characterInfo.maxY - characterInfo.minY;

            // 字送り幅基準で水平センタリング、ベースラインは上端からの比率で決める
            int originX = Mathf.RoundToInt((cellWidth - characterInfo.advance) * 0.5f) + _settings.CharacterOriginOffsetX;
            int baseY = Mathf.RoundToInt(cellHeight * _settings.BaselineRatio);
            int left = originX + characterInfo.minX;
            int top = baseY - characterInfo.maxY;

            // UV四隅から張るアフィン写像
            // フォントアトラス内で90度回転して、パックされている字形もこれで正しく読める
            coverageRateList = new float[cellWidth * cellHeight];
            Vector2 uv00Vector = characterInfo.uvBottomLeft;
            Vector2 uv00to10Vector = characterInfo.uvBottomRight - characterInfo.uvBottomLeft;
            Vector2 uv00to01Vector = characterInfo.uvTopLeft - characterInfo.uvBottomLeft;
            for (int heightIndex = 0; heightIndex < glyphHeight; heightIndex++)
            {
                int destinationY = top + heightIndex;
                if (destinationY < 0 || destinationY >= cellHeight)
                {
                    continue;
                }

                // UVは下原点
                float normalizeY = (glyphHeight - 1 - heightIndex + 0.5f) / glyphHeight;
                for (int widthIndex = 0; widthIndex < glyphWidth; widthIndex++)
                {
                    int destinationX = left + widthIndex;
                    if (destinationX < 0 || destinationX >= cellWidth)
                    {
                        continue;
                    }

                    float normalizeX = (widthIndex + 0.5f) / glyphWidth;
                    Vector2 uv = uv00Vector + uv00to10Vector * normalizeX + uv00to01Vector * normalizeY;
                    int sourceX = Mathf.Clamp((int)(uv.x * source.TextureWidth), 0, source.TextureWidth - 1);
                    int sourceY = Mathf.Clamp((int)(uv.y * source.TextureHeight), 0, source.TextureHeight - 1);
                    Color32 color = source.PixelColorList[sourceY * source.TextureWidth + sourceX];

                    float alpha = source.IsCoverageRateSavedRGB
                        ? (Mathf.Max(color.r, Mathf.Max(color.g, color.b)) / 255f)
                        : (color.a / 255f);

                    int destinationIndex = destinationY * cellWidth + destinationX;
                    if (alpha > coverageRateList[destinationIndex])
                    {
                        coverageRateList[destinationIndex] = alpha;
                    }
                }
            }

            return true;
        }
    }
}
