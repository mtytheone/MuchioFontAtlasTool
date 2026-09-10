# Muchio FontAtlasTool

> [日本語版 README はこちら](README.ja.md)

A Unity editor extension that swaps the font of Muchio's character board for any font you like.<br>
It follows the tile layout of KAT (KillFrenzy Avatar Text).
Just drop the generated texture into the material's `_MainTex` — the font changes, and you can remove the black band behind the text if you want to.

> The editor UI is in Japanese. Japanese labels are shown in parentheses below.

## Requirements

> Unity 2022.3 or later

## Installation

### Via VCC (VRChat Creator Companion)

1. Open the link below and press **Add to VCC** to register the listing

   https://mtytheone.github.io/HatzeLaboratory-VPM-Page/

2. Open **Manage Project** in VCC and press **+** on `Muchio FontAtlasTool`

### Via git URL (without VCC)

Add to `dependencies` in `Packages/manifest.json`:

```json
"com.hatzelaboratory.muchio.fontatlastool": "https://github.com/mtytheone/MuchioFontAtlasTool.git"
```

Or in Unity: **Window > Package Manager > + > Add package from git URL...**

---

## Usage

1. Import your `.ttf` / `.otf` into Unity and set **Character** to **Dynamic** in the import settings
2. Open **HatzeLaboratory > Muchio > Muchio FontAtlas Generator**
3. Assign a font, then press **Generate (生成)**
4. Choose where to save the generated PNG

The defaults are tuned for Muchio, so normally you only need to point at a font.

### Basic Settings

| Setting | Default | Description |
|---|---|---|
| Font (フォント, ttf/otf) | auto | Dynamic fonts only |
| Font size (フォントサイズ, px) | 240 | Matches the 168px cap height of the original atlas |
| Outline (アウトライン, px) | 6 | Outline thickness. Baked into alpha, tinted by `_ShadowColor` |
| **Background (背景)** | **Transparent (透明)** | **Transparent = punch out the background. Opaque (不透明) = the same band as the original** |
| Apply to material (適用するマテリアル) | none | When set, the result is pushed into `_MainTex` after generating |

### Advanced Settings

| Setting | Default | Description |
|---|---|---|
| Texture width / height (テクスチャ横／縦サイズ) | 4096 × 8192 | **Must be divisible** by the tile counts |
| Tile columns / rows (横／縦に並べる文字数) | 16 × 16 | 256 tiles |
| Baseline ratio (セル高比) | 0.67 | Baseline position relative to the cell height |
| Offset X (オフセット X, px) | 0 | Horizontal fine tuning of the glyph |
| Character table (タイル表) | Muchio compatible (ムチォ 互換) | Includes `っ ッ 　 、 ！ …` at 96–101 |
| Preview characters (プレビュー文字) | `Aapあ゛` | Sample characters baked into the preview |
| Compressed (圧縮化) | OFF | Import as DXT5 when ON |

### Tips

- Glyph proportions differ per font. Watch the preview and adjust the **font size within 220–260**.
  Lower it when full-width kana overflow the 256px cell width.
- `maxTextureSize` is 2048, so a 4096×8192 bake is downscaled to 2048×4096 at runtime.
  To cut generation time and memory, use 2048×4096 with an outline of 3 — the result is identical
  at a quarter of the cost (a 4096×8192 bake uses roughly 128MB while running).
- `Compressed` defaults to OFF. The original uses DXT5, but a 6px outline has a steep alpha edge
  that shows block artifacts easily. Turn it on only when size matters more.
- Fonts with a small glyph set may be missing `€` (tile 95), `゛` `゜` (tiles 254/255) or the
  full-width `！` `…`. **Put those into the preview characters to check without generating**
  (missing glyphs are also reported as warnings in the Console during generation).

## Atlas specification (measured)

| Item | Value |
|---|---|
| Image size | 4096 × 8192 (2048×4096 at runtime due to maxTextureSize 2048) |
| Grid | 16 × 16 = 256 tiles, one cell 256 × 512 px |
| Baseline | 343px from the top of the cell |
| Cap height | 168px (`A` `H` `1`) / full-width kana about 200px |
| Horizontal | Centered in the cell against the advance width |
| RGB | White glyph only (black elsewhere) |
| Alpha | Glyph dilated by about 6px = 255, otherwise 0 (or the band value) |
| Import settings | No mipmaps / sRGB / Clamp / Bilinear / maxTextureSize 2048 |

The outline is produced with a chamfer distance transform, so it comes out round rather than square or diamond shaped.

### Tile layout

- **0–94** … ASCII 32–126 (`index = char code - 32`)
- **95** … `€`
- **96–101** … `っ ッ 　 、 ！ …` (Muchio compatible; empty in stock KAT)
- **127** … `ぬ` (128 is empty too)
- **129–255** … JIS kana keyboard order (`ふあうえおやゆよわをほへたてい` → `すかんなにらせちとしはきくまのり` → …), 254/255 are `゛` `゜`

The hiragana section follows the JIS kana keyboard order because the OSC-side application looks up tile numbers by key position.

> **Changing this order garbles the text.** Leave it alone as long as you are only swapping fonts.

The "Stock KAT (KAT 本家準拠)" button switches to a layout with 96–101 left empty, but VRCPet needs
those tiles, so keep the default "Muchio compatible (ムチォ 互換)".

---

## License

MIT License — Copyright (c) 2026 Hatze Laboratory

See [LICENSE.md](LICENSE.md) for the full text.

## Third-Party Notices

The tile layout is a compatibility specification derived by analyzing the character board texture of
**KillFrenzy Avatar Text**, published under the MIT License.

- **Author:** KillFrenzy / Evan Tran
- **Repository:** https://github.com/killfrenzy96/KillFrenzyAvatarText
- **License:** MIT License — Copyright 2023 KillFrenzy / Evan Tran
