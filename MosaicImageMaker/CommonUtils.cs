using System;
using System.Windows;
using System.Windows.Media.Imaging;

using System.IO;        // FileStream
using System.Drawing;

using System.Collections.Generic;   // Dictionary;


using System.Linq;  //  ランダムソート
using System.Threading.Tasks;   // 並列処理

namespace MosaicImageMaker
{
    public class DEF
    {
        public const int PERCENT_MAX = 100;

        public const int TH_W = 160;
        public const int TH_H = 120;
        public const int LET_D = 100;

        public const int LET_IMG_MIN = 100;
        public const int SEEK_MAX = 4096;

        public static double SQR(double d) { return d * d; }
    }

    /// <summary>
    /// 汎用のメソッド
    /// </summary>
    public static class CommonUtils
    {
        public static Color ColorBlend(Color col1, double d1, Color col2, double d2)
        {
            double dR = (d1 * col1.R + d2 * col2.R) / (d1 + d2);
            double dG = (d1 * col1.G + d2 * col2.G) / (d1 + d2);
            double dB = (d1 * col1.B + d2 * col2.B) / (d1 + d2);

            return Color.FromArgb((int)dR, (int)dG, (int)dB);
        }

    }
}
