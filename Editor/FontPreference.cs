using UnityEditor;
using UnityEngine;

namespace HatzeLaboratory.Muchio.FontAtlasTool.Editor
{
    /// <summary>
    /// 前回使ったフォントの記憶と、プロジェクト内からの探索
    /// </summary>
    /// <remarks>
    /// EditorPrefs はマシン全体で共有されてしまうため、
    /// プロジェクト単位で保存される EditorUserSettings を使う。
    /// 実体は Library/EditorUserSettings.asset なので、配布物にも混ざらない
    /// </remarks>
    public static class FontPreference
    {
        private const string SETTINGS_KEY_FONT_GUID = "MTTG.FontGuid";

        /// <summary>
        /// 前回使ったフォントを返し、無ければプロジェクト内から探す
        /// </summary>
        /// <returns>見つかったフォント。無ければ null</returns>
        public static Font LoadOrFind()
        {
            return Load() ?? FindProjectDynamicFont();
        }

        /// <summary>
        /// Libraryに保存された前回選択したfontを返す
        /// </summary>
        /// <returns>保存されていたフォント。無ければ null</returns>
        public static Font Load()
        {
            string savedGuid = EditorUserSettings.GetConfigValue(SETTINGS_KEY_FONT_GUID);
            if (string.IsNullOrEmpty(savedGuid))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(savedGuid));
        }

        /// <summary>
        /// 選択しているfontのGUIDをLibraryに保存
        /// </summary>
        /// <param name="font">保存するフォント。null なら記録を消す</param>
        public static void Save(Font font)
        {
            string guid = font
                ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(font))
                : string.Empty;

            EditorUserSettings.SetConfigValue(SETTINGS_KEY_FONT_GUID, guid);
        }

        /// <summary>
        /// プロジェクト内の Dynamic なフォントを1つ探す
        /// </summary>
        /// <returns>見つかったフォント。無ければ null</returns>
        public static Font FindProjectDynamicFont()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Font"))
            {
                Font projectFont = AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(guid));
                if (projectFont && projectFont.dynamic)
                {
                    return projectFont;
                }
            }

            return null;
        }
    }
}
