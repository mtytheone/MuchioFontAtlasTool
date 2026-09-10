using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HatzeLaboratory.Muchio.FontAtlasTool.Editor
{
    /// <summary>
    /// ムチォで使用されている文字盤アトラスを、任意のフォントで焼き直すツール
    /// </summary>
    /// <remarks>
    /// KAT (KillFrenzy Avatar Text) 準拠
    /// </remarks>
    public class MuchioFontAtlasGenerator : EditorWindow
    {
        private const string WINDOW_TITLE = "文字盤アトラス生成";
        private const float CHARACTER_TABLE_VIEW_HEIGHT = 34f;
        private const float INDENT_WIDTH = 15f;
        private static readonly Vector2 MIN_WINDOW_SIZE = new(520, 460);
        private static readonly string[] CELL_BACKGROUND_LABEL_LIST = { "透明", "不透明" };

        [SerializeField]
        private MuchioFontAtlasSettings _settings;
        private AtlasBaker _atlasBaker;
        private AtlasAssetIO _atlasAssetIO;
        [SerializeField]
        private bool _isAdvancedSettingsOpen;

        private Texture2D _previewTexture;
        private Vector2 _windowScroll;
        private Vector2 _tableScroll;
        private bool _isPreviewDirty = true;

        [MenuItem("HatzeLaboratory/Muchio/Muchio FontAtlas Generator")]
        private static void Open()
        {
            var window = GetWindow<MuchioFontAtlasGenerator>(WINDOW_TITLE);
            window.minSize = MIN_WINDOW_SIZE;
        }

        private void OnEnable()
        {
            // ドメインリロード後は _settings が作り直されるので、ここで作り直す
            _settings ??= new MuchioFontAtlasSettings();
            _atlasBaker = new AtlasBaker(_settings);
            _atlasAssetIO = new AtlasAssetIO(_settings);

            FillSettingsWithDefault();
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            if (_previewTexture)
            {
                DestroyImmediate(_previewTexture);
            }
        }

        private void OnGUI()
        {
            using (EditorGUILayout.ScrollViewScope windowScope = new(_windowScroll))
            {
                _windowScroll = windowScope.scrollPosition;
                using (EditorGUI.ChangeCheckScope changeCheckScope = new())
                {
                    DrawBasicSettings();

                    EditorGUILayout.Space();
                    DrawAdvancedSettings();

                    if (changeCheckScope.changed)
                    {
                        _isPreviewDirty = true;
                    }
                }
            }

            DrawPreview();
            EditorGUILayout.Space();
            DrawGenerateButton();
        }

        private void Update()
        {
            if (!_isPreviewDirty)
            {
                return;
            }

            _isPreviewDirty = false;
            RebuildPreviewTexture();
            Repaint();
        }

        /// <summary>
        /// 未設定の項目に既定値を入れる
        /// </summary>
        private void FillSettingsWithDefault()
        {
            if (!_settings.HasBuildCharacterList)
            {
                _settings.BuildCharacterList = CharacterTable.BuildMuchio();
            }

            if (!_settings.HasPreviewSampleCharacters)
            {
                _settings.PreviewSampleCharacters = MuchioFontAtlasSettings.DEFAULT_SAMPLE_CHARACTERS;
            }

            if (!_settings.HasFont)
            {
                _settings.Font = FontPreference.LoadOrFind();
            }
        }

        /// <summary>
        /// Undo/Redo でフィールドが書き戻された後の追従処理
        /// </summary>
        /// <remarks>
        /// フィールドの値は Unity が復元してくれるが、
        /// プレビューと EditorUserSettings への保存は自前で追従させる必要がある
        /// </remarks>
        private void OnUndoRedoPerformed()
        {
            _isPreviewDirty = true;
            FontPreference.Save(_settings.Font);
            Repaint();
        }

        /// <summary>
        /// 値が変わったときだけ Undo に記録してから代入する
        /// </summary>
        /// <remarks>
        /// Undo.RecordObject は変更前の状態を記録するため、代入より先に呼ぶ必要がある。
        /// IMGUI のコントロールは描画と同時に新しい値を返すので、
        /// 「ローカルで受ける → 変わっていたら記録して代入」という順序になる
        /// </remarks>
        /// <param name="currentValue">現在の値</param>
        /// <param name="newValue">コントロールが返した新しい値</param>
        /// <param name="setter">代入を行う処理</param>
        /// <param name="undoName">Undo履歴に表示する名前</param>
        /// <returns>値が変わったかどうか</returns>
        private bool SetWithUndo<T>(T currentValue, T newValue, Action<T> setter, string undoName)
        {
            if (EqualityComparer<T>.Default.Equals(currentValue, newValue))
            {
                return false;
            }

            Undo.RecordObject(this, undoName);
            setter(newValue);
            return true;
        }

        /// <summary>
        /// 常に表示する設定を描画
        /// </summary>
        private void DrawBasicSettings()
        {
            EditorGUILayout.LabelField("Basic Settings", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope(1))
            {
                Font newFont = (Font)EditorGUILayout.ObjectField("フォント (ttf/otf)", _settings.Font, typeof(Font), false);
                if (SetWithUndo(_settings.Font, newFont, value => _settings.Font = value, "Change Font"))
                {
                    FontPreference.Save(_settings.Font);
                }

                if (_settings.Font && !_settings.Font.dynamic)
                {
                    EditorGUILayout.HelpBox(
                        "このフォントは Dynamic ではありません。インポート設定の Character を Dynamic にしてください。",
                        MessageType.Error);
                }

                SetWithUndo(
                    _settings.FontSize,
                    EditorGUILayout.IntField("フォントサイズ (px)", _settings.FontSize),
                    value => _settings.FontSize = value,
                    "Change Font Size");

                SetWithUndo(
                    _settings.OutlineWidth,
                    EditorGUILayout.IntField("アウトライン (px)", _settings.OutlineWidth),
                    value => _settings.OutlineWidth = value,
                    "Change Outline Width");

                SetWithUndo(
                    _settings.CellBackground,
                    (CellBackground)EditorGUILayout.Popup("背景", (int)_settings.CellBackground, CELL_BACKGROUND_LABEL_LIST),
                    value => _settings.CellBackground = value,
                    "Change Cell Background");

                if (_settings.CellBackground == CellBackground.Opaque)
                {
                    EditorGUILayout.HelpBox(
                        "文字の後ろに帯が入ります。帯と縁取りは同じ _ShadowColor になるため、縁取りは帯に溶けて見えなくなります。",
                        MessageType.Info);
                }

                SetWithUndo(
                    _settings.ApplyToMaterial,
                    (Material)EditorGUILayout.ObjectField("適用するマテリアル（任意）", _settings.ApplyToMaterial, typeof(Material), false),
                    value => _settings.ApplyToMaterial = value,
                    "Change Apply Target Material");
            }
        }

        /// <summary>
        /// 折りたたみの中に隠す詳細設定を描画
        /// </summary>
        private void DrawAdvancedSettings()
        {
            _isAdvancedSettingsOpen = EditorGUILayout.BeginFoldoutHeaderGroup(_isAdvancedSettingsOpen, "Advanced Settings");
            if (_isAdvancedSettingsOpen)
            {
                using (new EditorGUI.IndentLevelScope(1))
                {
                    DrawAtlasSettings();

                    EditorGUILayout.Space();
                    DrawGlyphSettings();

                    EditorGUILayout.Space();
                    DrawCharacterTable();

                    EditorGUILayout.Space();
                    DrawOutputSettings();
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        /// <summary>
        /// アトラスの分割に関する設定を描画
        /// </summary>
        private void DrawAtlasSettings()
        {
            EditorGUILayout.LabelField("アトラス", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope(1))
            {
                SetWithUndo(
                    _settings.AtlasTextureWidth,
                    Mathf.Max(1, EditorGUILayout.IntField("テクスチャ横サイズ", _settings.AtlasTextureWidth)),
                    value => _settings.AtlasTextureWidth = value,
                    "Change Atlas Width");

                SetWithUndo(
                    _settings.AtlasTextureHeight,
                    Mathf.Max(1, EditorGUILayout.IntField("テクスチャ縦サイズ", _settings.AtlasTextureHeight)),
                    value => _settings.AtlasTextureHeight = value,
                    "Change Atlas Height");

                SetWithUndo(
                    _settings.ColumnCount,
                    Mathf.Max(1, EditorGUILayout.IntField("横に並べる文字数", _settings.ColumnCount)),
                    value => _settings.ColumnCount = value,
                    "Change Column Count");

                SetWithUndo(
                    _settings.RowCount,
                    Mathf.Max(1, EditorGUILayout.IntField("縦に並べる文字数", _settings.RowCount)),
                    value => _settings.RowCount = value,
                    "Change Row Count");

                using (new EditorGUI.IndentLevelScope(1))
                {
                    EditorGUILayout.LabelField("セルサイズ", _settings.CellWidth + " x " + _settings.CellHeight + " px");
                }
            }
        }

        /// <summary>
        /// 字形の配置に関する設定を描画
        /// </summary>
        private void DrawGlyphSettings()
        {
            EditorGUILayout.LabelField("字形", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope(1))
            {
                SetWithUndo(
                    _settings.BaselineRatio,
                    EditorGUILayout.Slider("セル高比", _settings.BaselineRatio, 0f, 1f),
                    value => _settings.BaselineRatio = value,
                    "Change Baseline Ratio");

                SetWithUndo(
                    _settings.CharacterOriginOffsetX,
                    EditorGUILayout.IntField("オフセット X (px)", _settings.CharacterOriginOffsetX),
                    value => _settings.CharacterOriginOffsetX = value,
                    "Change Character Origin Offset X");
            }
        }

        /// <summary>
        /// タイル番号と文字の対応表を描画
        /// </summary>
        private void DrawCharacterTable()
        {
            EditorGUILayout.LabelField($"タイル表（{_settings.CellCount}文字・空きは半角スペース）", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "並びを変えると OSC アプリ側の「文字→タイル番号」対応がズレます。" +
                "フォント差し替えのみが目的なら触らないでください。",
                MessageType.Info);

            using (new IndentedLayoutScope())
            using (EditorGUILayout.ScrollViewScope tableScope = new(
                       _tableScroll,
                       GUI.skin.horizontalScrollbar,
                       GUIStyle.none,
                       GUILayout.Height(CHARACTER_TABLE_VIEW_HEIGHT)))
            {
                _tableScroll = tableScope.scrollPosition;
                SetWithUndo(
                    _settings.BuildCharacterList,
                    EditorGUILayout.TextArea(_settings.BuildCharacterList, GUILayout.ExpandHeight(true)),
                    value => _settings.BuildCharacterList = value,
                    "Edit Character Table");
            }

            using (new IndentedLayoutScope())
            {
                EditorGUILayout.LabelField(
                    "文字数 " + _settings.BuildCharacterList.Length + " / " + _settings.CellCount,
                    GUILayout.Width(120));

                if (GUILayout.Button("ムチォ 互換（既定）"))
                {
                    SetWithUndo(
                        _settings.BuildCharacterList,
                        CharacterTable.BuildMuchio(),
                        value => _settings.BuildCharacterList = value,
                        "Reset Character Table");
                }

                if (GUILayout.Button("KAT 本家準拠"))
                {
                    SetWithUndo(
                        _settings.BuildCharacterList,
                        CharacterTable.BuildKAT(),
                        value => _settings.BuildCharacterList = value,
                        "Reset Character Table");
                }
            }

            SetWithUndo(
                _settings.PreviewSampleCharacters,
                EditorGUILayout.TextField("プレビュー文字", _settings.PreviewSampleCharacters),
                value => _settings.PreviewSampleCharacters = value,
                "Change Preview Sample Characters");
        }

        /// <summary>
        /// 書き出しに関する設定を描画
        /// </summary>
        private void DrawOutputSettings()
        {
            EditorGUILayout.LabelField("出力", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope(1))
            {
                SetWithUndo(
                    _settings.Compressed,
                    EditorGUILayout.Toggle("圧縮化", _settings.Compressed),
                    value => _settings.Compressed = value,
                    "Change Texture Compression");
            }
        }

        /// <summary>
        /// 生成ボタンを描画
        /// </summary>
        private void DrawGenerateButton()
        {
            using (new EditorGUI.DisabledScope(!_settings.IsFontUsable || !_settings.IsAtlasSizeDivisible))
            {
                string buttonText = "生成";
                if (!_settings.IsFontUsable)
                {
                    buttonText = "Dynamic に設定されたフォントを指定してください。";
                }
                else if (!_settings.IsAtlasSizeDivisible)
                {
                    buttonText = "テクスチャサイズが横／縦に並べる文字数で割り切れるようにしてください。";
                }

                if (GUILayout.Button(buttonText, GUILayout.Height(30)))
                {
                    Generate();
                }
            }
        }

        /// <summary>
        /// プレビューを描画
        /// </summary>
        private void DrawPreview()
        {
            if (!_previewTexture)
            {
                return;
            }

            GUILayout.Label("Preview");
            float aspect = (float)_previewTexture.width / _previewTexture.height;
            Rect rect = GUILayoutUtility.GetAspectRect(aspect);
            EditorGUI.DrawTextureTransparent(rect, _previewTexture, ScaleMode.ScaleToFit);
        }

        /// <summary>
        /// アトラスを焼いて書き出し、インポートまで通す
        /// </summary>
        private void Generate()
        {
            if (!_settings.IsAtlasSizeDivisible)
            {
                EditorUtility.DisplayDialog(
                    WINDOW_TITLE,
                    "テクスチャサイズは縦横それぞれの文字数で割り切れる必要があります。",
                    "OK"
                );

                return;
            }

            if (!_atlasAssetIO.TrySelectOutputPath())
            {
                return;
            }

            try
            {
                if (!_atlasBaker.TryBakeAtlas(WINDOW_TITLE, out Color32[] pixelList))
                {
                    return;
                }

                EditorUtility.DisplayProgressBar(WINDOW_TITLE, "Encoding PNG...", 0.95f);
                _atlasAssetIO.SavePNG(pixelList);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            _atlasAssetIO.ImportGeneratedAtlas();
        }

        /// <summary>
        /// 現在の設定でサンプル文字を焼き直し、プレビュー用テクスチャを作る
        /// </summary>
        private void RebuildPreviewTexture()
        {
            if (_previewTexture)
            {
                DestroyImmediate(_previewTexture);
                _previewTexture = null;
            }

            if (!_settings.IsFontUsable || !_settings.IsAtlasSizeDivisible)
            {
                return;
            }

            if (string.IsNullOrEmpty(_settings.PreviewSampleCharacters))
            {
                return;
            }

            _previewTexture = _atlasBaker.BakePreviewTexture();
        }

        /// <summary>
        /// indentLevel を見ない GUILayout 系のコントロールを、インデントに揃えて描くためのスコープ
        /// </summary>
        /// <remarks>
        /// ScrollView や Button は EditorGUI.indentLevel を無視してウィンドウ左端から
        /// 配置されるため、インデント幅ぶんを手で空けたうえで内側の indentLevel を 0 に戻す
        /// </remarks>
        private sealed class IndentedLayoutScope : IDisposable
        {
            private readonly int _previousIndentLevel;

            public IndentedLayoutScope()
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(EditorGUI.indentLevel * INDENT_WIDTH);
                _previousIndentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
            }

            public void Dispose()
            {
                EditorGUI.indentLevel = _previousIndentLevel;
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
