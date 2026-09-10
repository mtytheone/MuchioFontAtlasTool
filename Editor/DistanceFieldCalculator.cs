using UnityEngine;

namespace HatzeLaboratory.Muchio.FontAtlasTool.Editor
{
    /// <summary>
    /// 字形の被膜率から距離場を算出する
    /// </summary>
    public static class DistanceFieldCalculator
    {
        // チャンファ距離変換で使う重み定数
        private const float UNREACHABLE_DISTANCE = 1e9f;
        private const float ORTHOGONAL_STEP_DISTANCE = 1f;
        private const float DIAGONAL_STEP_DISTANCE = 1.41421356f;

        /// <summary>
        /// インクとみなす被膜率のしきい値
        /// </summary>
        private const float INK_COVERAGE_RATE = 0.5f;

        /// <summary>
        /// 字形の距離場を算出
        /// </summary>
        /// <param name="coverageRateList">字形の被膜率リスト</param>
        /// <param name="cellWidth">セルの横幅</param>
        /// <param name="cellHeight">セルの縦幅</param>
        /// <remarks>
        /// 2パス チャンファ距離変換（縁取りを丸く出すため）
        /// </remarks>
        /// <returns>
        /// 算出された字形の距離場を格納するリスト
        /// </returns>
        public static float[] Calculate(float[] coverageRateList, int cellWidth, int cellHeight)
        {
            float[] distanceFieldList = new float[coverageRateList.Length];
            for (int i = 0; i < coverageRateList.Length; i++)
            {
                distanceFieldList[i] = coverageRateList[i] >= INK_COVERAGE_RATE ? 0f : UNREACHABLE_DISTANCE;
            }

            for (int y = 0; y < cellHeight; y++)
            {
                for (int x = 0; x < cellWidth; x++)
                {
                    int i = y * cellWidth + x;
                    float distance = distanceFieldList[i];
                    if (distance == 0)
                    {
                        continue;
                    }

                    if (x > 0)
                    {
                        distance = Mathf.Min(distance, distanceFieldList[i - 1] + ORTHOGONAL_STEP_DISTANCE);
                    }
                    if (y > 0)
                    {
                        distance = Mathf.Min(distance, distanceFieldList[i - cellWidth] + ORTHOGONAL_STEP_DISTANCE);
                    }
                    if (x > 0 && y > 0)
                    {
                        distance = Mathf.Min(distance, distanceFieldList[i - cellWidth - 1] + DIAGONAL_STEP_DISTANCE);
                    }
                    if (x < cellWidth - 1 && y > 0)
                    {
                        distance = Mathf.Min(distance, distanceFieldList[i - cellWidth + 1] + DIAGONAL_STEP_DISTANCE);
                    }

                    distanceFieldList[i] = distance;
                }
            }

            for (int y = cellHeight - 1; y >= 0; y--)
            {
                for (int x = cellWidth - 1; x >= 0; x--)
                {
                    int i = y * cellWidth + x;
                    float distance = distanceFieldList[i];
                    if (distance == 0)
                    {
                        continue;
                    }

                    if (x < cellWidth - 1)
                    {
                        distance = Mathf.Min(distance, distanceFieldList[i + 1] + ORTHOGONAL_STEP_DISTANCE);
                    }
                    if (y < cellHeight - 1)
                    {
                        distance = Mathf.Min(distance, distanceFieldList[i + cellWidth] + ORTHOGONAL_STEP_DISTANCE);
                    }
                    if (x < cellWidth - 1 && y < cellHeight - 1)
                    {
                        distance = Mathf.Min(distance, distanceFieldList[i + cellWidth + 1] + DIAGONAL_STEP_DISTANCE);
                    }
                    if (x > 0 && y < cellHeight - 1)
                    {
                        distance = Mathf.Min(distance, distanceFieldList[i + cellWidth - 1] + DIAGONAL_STEP_DISTANCE);
                    }

                    distanceFieldList[i] = distance;
                }
            }

            return distanceFieldList;
        }
    }
}
