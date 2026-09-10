using System.IO;
using UnityEditor;
using UnityEngine;

namespace HatzeLaboratory.Muchio.FontAtlasTool.Editor
{
    /// <summary>
    /// 焼き上がったアトラスの保存とインポート
    /// </summary>
    public sealed class AtlasAssetIO
    {
        private const string DEFAULT_OUTPUT_DIRECTORY = "Assets";
        private const string DEFAULT_OUTPUT_FILE_NAME = "KAT_CharTiles_Generated";

        /// <summary>
        /// 本家と同じ。実行時は 2048x4096 になる
        /// </summary>
        private const int MAX_TEXTURE_SIZE = 2048;

        private readonly MuchioFontAtlasSettings _settings;
        private string _outputPath;

        public AtlasAssetIO(MuchioFontAtlasSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// 保存ダイアログを開いて出力先を決める
        /// </summary>
        /// <remarks>
        /// AssetDatabase でインポートする必要があるため、
        /// プロジェクト内に保存先を限定する SaveFilePanelInProject を使う
        /// </remarks>
        /// <returns>出力先が決まったかどうか。false ならユーザーがキャンセルした</returns>
        public bool TrySelectOutputPath()
        {
            bool hasPreviousPath = !string.IsNullOrEmpty(_outputPath);
            string directory = hasPreviousPath
                ? Path.GetDirectoryName(_outputPath)?.Replace('\\', '/')
                : DEFAULT_OUTPUT_DIRECTORY;

            string fileName = hasPreviousPath
                ? Path.GetFileNameWithoutExtension(_outputPath)
                : DEFAULT_OUTPUT_FILE_NAME;

            string selectedPath = EditorUtility.SaveFilePanelInProject(
                "文字テクスチャの保存先",
                fileName,
                "png",
                "生成した文字テクスチャの保存先を選んでください",
                directory);

            if (string.IsNullOrEmpty(selectedPath))
            {
                return false;
            }

            _outputPath = selectedPath;
            return true;
        }

        /// <summary>
        /// アトラス1枚分のピクセルをPNGとして書き出す
        /// </summary>
        /// <param name="pixelList">アトラス1枚分のピクセル</param>
        public void SavePNG(Color32[] pixelList)
        {
            byte[] png;
            Texture2D atlasTexture = new(
                _settings.AtlasTextureWidth,
                _settings.AtlasTextureHeight,
                TextureFormat.RGBA32,
                mipChain: false);

            try
            {
                atlasTexture.SetPixels32(pixelList);
                atlasTexture.Apply(updateMipmaps: false, makeNoLongerReadable: false);
                png = atlasTexture.EncodeToPNG();
            }
            finally
            {
                Object.DestroyImmediate(atlasTexture);
            }

            string outputFullPath = Path.GetFullPath(_outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputFullPath));
            File.WriteAllBytes(outputFullPath, png);
        }

        /// <summary>
        /// 書き出したPNGをインポートし、マテリアルへ反映する
        /// </summary>
        public void ImportGeneratedAtlas()
        {
            AssetDatabase.ImportAsset(_outputPath, ImportAssetOptions.ForceUpdate);
            ApplyImportSettings(_outputPath);

            Texture2D generatedAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(_outputPath);
            if (!generatedAtlas)
            {
                Debug.LogError("[MuchioFontAtlas] 生成した文字テクスチャを読み込めませんでした: " + _outputPath);
                return;
            }

            if (_settings.ApplyToMaterial)
            {
                Undo.RecordObject(_settings.ApplyToMaterial, "Apply Char Tiles");
                _settings.ApplyToMaterial.SetTexture("_MainTex", generatedAtlas);
                EditorUtility.SetDirty(_settings.ApplyToMaterial);
            }

            EditorGUIUtility.PingObject(generatedAtlas);
            Debug.Log("[MuchioFontAtlas] 生成しました: " + _outputPath);
        }

        /// <summary>
        /// ImportSettingsを適用する
        /// </summary>
        /// <param name="assetPath">対象アセットのパス</param>
        private void ApplyImportSettings(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (!importer)
            {
                return;
            }

            importer.textureType         = TextureImporterType.Default;
            importer.sRGBTexture         = true;
            importer.alphaSource         = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled       = false;
            importer.wrapMode            = TextureWrapMode.Clamp;
            importer.filterMode          = FilterMode.Bilinear;
            importer.isReadable          = false;
            importer.maxTextureSize      = MAX_TEXTURE_SIZE;
            importer.textureCompression  = _settings.Compressed   // DXT5だと縁取りが滲むので既定は非圧縮化
                ? TextureImporterCompression.CompressedHQ
                : TextureImporterCompression.Uncompressed;

            importer.SaveAndReimport();
        }
    }
}
