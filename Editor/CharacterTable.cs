using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HatzeLaboratory.Muchio.FontAtlasTool.Editor
{
    /// <summary>
    /// タイル番号と文字の対応表を扱う
    /// </summary>
    public static class CharacterTable
    {
        /// <summary>
        /// KillFrenzyAvatarTextで使用する文字の一覧を構築
        /// </summary>
        /// <remarks>
        /// index 0..94 = ASCII 32..126、95 = '€'、127以降は日本語（JISかな配列順）
        /// これは本家 KAT のアトラスを実測して起こした並び
        /// 空きセルは半角スペース（描画されないのでそのまま空タイルになる）
        /// </remarks>
        /// <returns>
        /// KillFrenzyAvatarTextで使用する文字の一覧
        /// </returns>
        public static string BuildKAT()
        {
            StringBuilder stringBuilder = new(256);
            for (int i = 0; i < 95; i++)
            {
                stringBuilder.Append((char)(32 + i));  // 0..94
            }

            stringBuilder.Append('€');                                       // 95
            stringBuilder.Append(' ', 31);                                   // 96..126 (本家は空き)
            stringBuilder.Append('ぬ');                                      // 127
            stringBuilder.Append(' ');                                       // 128 (本家も空き)
            stringBuilder.Append("ふあうえおやゆよわをほへたてい");              // 129..143
            stringBuilder.Append("すかんなにらせちとしはきくまのり");            // 144..159
            stringBuilder.Append("れけむつさそひこみもねるめろ。ぶ");            // 160..175
            stringBuilder.Append("ぷぼぽべぺだでずがぜぢどじばぱぎ");            // 176..191
            stringBuilder.Append("ぐげづざぞびぴごぁぃぅぇぉゃゅょ");            // 192..207
            stringBuilder.Append("ヌフアウエオヤユヨワヲホヘタテイ");            // 208..223
            stringBuilder.Append("スカンナニラセチトシハキクマノリ");            // 224..239
            stringBuilder.Append("レケムツサソヒコミモネルメロ゛゜");            // 240..255
            return stringBuilder.ToString();
        }

        /// <summary>
        /// ムチォで使用する文字の一覧を構築
        /// </summary>
        /// <remarks>
        /// 96..101 に小書き文字と約物を置いた版
        /// これらのタイル番号は OSC 側アプリとの互換のための「対応表」であって、
        /// 描画する文字そのものは標準的な日本語の文字。
        /// 想定ユーザーは VRCPet 利用者なので、96..101 入りを既定にする。
        /// これが無いと「っ ッ 、 ！ …」のタイルが空になり文字が欠ける。
        /// </remarks>
        /// <returns>
        /// ムチォで使用する文字の一覧
        /// </returns>
        public static string BuildMuchio()
        {
            const string EXTRA_CHARACTER_FOR_MUCHIO = "っッ　、！…";  // 96..101
            StringBuilder stringBuilder = new(BuildKAT());
            for (int i = 0; i < EXTRA_CHARACTER_FOR_MUCHIO.Length; i++)
            {
                stringBuilder[96 + i] = EXTRA_CHARACTER_FOR_MUCHIO[i];
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// 字形を焼く対象のタイル番号を集める
        /// </summary>
        /// <remarks>
        /// 空白文字のタイルは焼く対象が無いので除外する
        /// </remarks>
        /// <param name="buildCharacterList">タイル番号順に並んだ文字の一覧</param>
        /// <param name="cellCount">アトラスに並ぶタイルの総数</param>
        /// <returns>対象のタイル番号リスト</returns>
        public static List<int> CollectTargetTileIndexList(string buildCharacterList, int cellCount)
        {
            return Enumerable
                .Range(0, Math.Min(cellCount, buildCharacterList.Length))
                .Where(i => !char.IsWhiteSpace(buildCharacterList[i]))
                .ToList();
        }
    }
}
