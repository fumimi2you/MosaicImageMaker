using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

using System.IO;        // FileStream
using System.Drawing;

using Microsoft.WindowsAPICodePack.Dialogs; //CommonOpenFileDialog
using System.Collections.Generic;   // Dictionary;

using System.Linq;  //  ランダムソート

using System.Threading.Tasks;   // 並列処理

namespace WpfAppSample
{
    public class DEF
    {
        public const int TH_W = 160;
        public const int TH_H = 120;
        public const int LET_D = 100;

        public const int SEEK_MAX = 1024;
        public const double EPSILON_COL = 4;

        public static double SQR(double d ) { return d*d; }
    }

    public class ImgPath
    {
        public string[] asSrcImg;
        public string sSrcDir;
        public string sTgtImg;
        public string sDstImg;
    }

    public class ImageLet
    {
        public double dAveR;
        public double dAveG;
        public double dAveB;
        public Bitmap bmData;
    }

    public class ImageCel
    {
        public System.Windows.Point pt;
        public Color col;
    }


    // 通知内容
    public class ProgressInfo
    {
        public ProgressInfo(int value, string message)
        {
            Value = value;
            Message = message;
        }

        public int Value { get; private set; }
        public string Message { get; private set; }
    }

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        ImgPath m_path;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void SetImgDir_Click(object sender, RoutedEventArgs e)
        {
            var Dialog = new CommonOpenFileDialog();
            // フォルダーを開く設定に
            Dialog.IsFolderPicker = true;
            // 読み取り専用フォルダ/コントロールパネルは開かない
            Dialog.EnsureReadOnly = false;
            Dialog.AllowNonFileSystemItems = false;
            // パス指定
            //            Dialog.DefaultDirectory = Application.StartupPath;
            // 開く
            var Result = Dialog.ShowDialog();
            // もし開かれているなら
            if (Result == CommonFileDialogResult.Ok)
            {
                m_path = new ImgPath();

                m_path.asSrcImg = System.IO.Directory.GetFiles(
                Dialog.FileName, "*.jpg", System.IO.SearchOption.TopDirectoryOnly);
                m_path.sSrcDir = Path.GetDirectoryName(m_path.asSrcImg[0]);
                m_path.sTgtImg = m_path.asSrcImg[0];
                m_path.sDstImg = m_path.sSrcDir + "\\rt\\ret" + Path.GetFileName(m_path.sTgtImg);


                TextBox1.Text = m_path.sSrcDir;
                TextBlock1.Text = m_path.sTgtImg;
                TextBlock2.Text = m_path.sDstImg;

                progressBar1.Minimum = 0;
                progressBar1.Maximum = m_path.asSrcImg.Length;
                progressBar1.Value = 0;
            }

        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            await Do(m_path);
        }



        public async Task<int> Do( ImgPath path )
        {
            Func<int> Job = () =>
            {
                //  LetImgの配列を構築
                List<ImageLet> aImgLet = CommonUtils.MakeImageLets(path.asSrcImg);


                //  ターゲット画像の読み込み
                Image imgTg = Image.FromFile(path.sTgtImg);
                Bitmap bmTg = new Bitmap(imgTg);

                //  出力先の構築(取り敢えずオリジナル画像をリサイズして突っ込む)
                int iCelL = (int)Math.Sqrt(aImgLet.Count / 2);
                int iCelW = 0;
                int iCelH = 0;
                if (imgTg.Width > imgTg.Height)
                {
                    iCelW = iCelL;
                    iCelH = iCelW * imgTg.Height / imgTg.Width;
                }
                else
                {
                    iCelH = iCelL;
                    iCelW = iCelH * imgTg.Width / imgTg.Height;
                }
                Bitmap bmOut = new Bitmap(bmTg, DEF.LET_D * iCelW, DEF.LET_D * iCelH);

                //  ターゲット画像Celの配列作成
                List<ImageCel> aImgCel = CommonUtils.MakeImageCels(imgTg, iCelW, iCelH);

                //  モザイク処理
                foreach (ImageCel imgCel in aImgCel)
                {
                    int cy = (int)imgCel.pt.Y;
                    int cx = (int)imgCel.pt.X;

                    ImageLet imgLet = CommonUtils.SelectImageLet(imgCel.col, aImgLet);

                    for (int dy = 0; dy < DEF.LET_D; dy++)
                    //                    Parallel.For( 0, DEF.LET_D, dy =>
                    {
                        int y = cy * DEF.LET_D + dy;

                        for (int dx = 0; dx < DEF.LET_D; dx++)
                        {
                            int x = cx * DEF.LET_D + dx;

                            //  値の取得
                            Color colMid = CommonUtils.ColorBlend(imgLet.bmData.GetPixel(dx, dy), 9, imgCel.col, 1);

                            bmOut.SetPixel(x, y, CommonUtils.ColorBlend(colMid, 9, bmOut.GetPixel(x, y), 1));
                        }
                    }
                }

                //  ファイルを出力して開く
                bmOut.Save(path.sDstImg, System.Drawing.Imaging.ImageFormat.Jpeg);
                System.Diagnostics.Process.Start(path.sDstImg);

                //  お片付け
                imgTg.Dispose();
                bmTg.Dispose();
                bmOut.Dispose();

                return 0;
            };

            return await Task.Run(Job);
        }
    }

        /// <summary>
        /// 汎用のメソッド
        /// </summary>
        public static class CommonUtils
    {
        public static Color ColorBlend( Color col1, double d1, Color col2, double d2 )
        {
            double dR = (d1 * col1.R + d2 * col2.R) / (d1 + d2);
            double dG = (d1 * col1.G + d2 * col2.G) / (d1 + d2);
            double dB = (d1 * col1.B + d2 * col2.B) / (d1 + d2);

            return Color.FromArgb( (int)dR, (int)dG, (int)dB);
        }

        // 素材画像のImageLet
        public static List<ImageLet> MakeImageLets(string[] aFile)
        {
            List<ImageLet> aImgLet = new List<ImageLet>();

            //foreach (string sFile in aFile)
            Parallel.ForEach(aFile, sFile =>
            {
                Console.WriteLine(sFile);

                // 画像オブジェクトの作成
                Image imgOg = Image.FromFile(sFile);

                // サムネイルの取得
                Image imgTn = imgOg.GetThumbnailImage(DEF.TH_W, DEF.TH_H, delegate { return false; }, IntPtr.Zero);

                //  サムネイルを処理
                ImageLet imgLet = new ImageLet();
                if (MakeImageLet(imgTn, imgLet))
                {
                    //  辞書に保存
                    lock (aImgLet) aImgLet.Add(imgLet);

                    // サムネイルの保存(デバッグ用)
                    //string fileTh = Path.GetDirectoryName(sFile) + "\\tn\\" + Path.GetFileName(sFile);
                    //imgLet.bmData.Save(fileTh, System.Drawing.Imaging.ImageFormat.Jpeg);
                }

                //  お片付け
                imgOg.Dispose();
                imgTn.Dispose();
            });

            return aImgLet;
        }


        // ターゲット画像のImageCel
        public static List<ImageCel> MakeImageCels(Image imgOg, int iCelW, int iCelH)
        {
            // サムネイルの取得
            Image imgTn = imgOg.GetThumbnailImage(iCelW, iCelH, delegate { return false; }, IntPtr.Zero);

            //  ビットマップに変換
            Bitmap bmTn = new Bitmap(imgTn);

            //  セルの作成
            List<ImageCel> ptTmp = new List<ImageCel>();
            for (int cy = 0; cy < iCelH; cy++)
            {
                for (int cx = 0; cx < iCelW; cx++)
                {
                    ImageCel imgCel = new ImageCel();
                    imgCel.pt.X = cx;
                    imgCel.pt.Y = cy;
                    imgCel.col = bmTn.GetPixel(cx, cy);

                    ptTmp.Add(imgCel);
                }
            }

            //  ランダマイズ
            List<ImageCel> aImgCels = new List<ImageCel>(ptTmp.OrderBy(i => Guid.NewGuid()).ToArray());

            return aImgCels;
        }


        // 画像ファイル → ImageLet
        public static Boolean MakeImageLet(Image imgTn, ImageLet imgLet)
        {
            //  定数計算
            int iXs = (DEF.TH_W - DEF.LET_D) / 2;
            int iXe = (DEF.TH_W + DEF.LET_D) / 2;
            int iYs = (DEF.TH_H - DEF.LET_D) / 2;
            int iYe = (DEF.TH_H + DEF.LET_D) / 2;

            //  imgLetの生成
            imgLet.bmData = new Bitmap(DEF.LET_D, DEF.LET_D);

            //  ビットマップに変換
            Bitmap bmTn = new Bitmap(imgTn);

            double vR = 0;
            double vG = 0;
            double vB = 0;
            for (int y= iYs; y< iYe; y++)
            {
                for(int x= iXs; x< iXe; x++)
                {
                    //  画素値コピー
                    Color colVal = bmTn.GetPixel(x, y);
                    imgLet.bmData.SetPixel(x - iXs, y - iYs, colVal);

                    //  平均用の積算
                    vR += colVal.R;
                    vG += colVal.G;
                    vB += colVal.B;
                }
            }

            //  平均値産出
            imgLet.dAveR = vR / (DEF.LET_D * DEF.LET_D);
            imgLet.dAveG = vG / (DEF.LET_D * DEF.LET_D);
            imgLet.dAveB = vB / (DEF.LET_D * DEF.LET_D);

            return true;
        }

        public static ImageLet SelectImageLet(Color col, List<ImageLet> aImgLet)
        {
            int iSeekMax = Math.Min( aImgLet.Count, DEF.SEEK_MAX);
            int iSel = 0;
            double dstMin = 0xFFFFFF;

            for ( int i = 0; i < iSeekMax; i++)
            {
                ImageLet imgLet = aImgLet[i];

                //  2点間距離(ルートはかけない)
                double dist = 
                    DEF.SQR(col.R - imgLet.dAveR) +
                    DEF.SQR(col.G - imgLet.dAveG) +
                    DEF.SQR(col.B - imgLet.dAveB);

                //  最近距離の更新
                if (dist < dstMin)
                {
                    iSel = i;
                    dstMin = dist;

                    //  十分に小さい場合は抜ける
                    if (dstMin <= DEF.SQR( DEF.EPSILON_COL ) )
                    {
                        break;
                    }
                }
            }

            ImageLet imgLetRet = aImgLet[iSel];
            aImgLet.RemoveAt(iSel);

            return imgLetRet;
        }

        // BitmapSource → 画像ファイル
        public static void BitmapSourceToFile(
            string filePath, BitmapSource bmpSrc)
        {
            if (bmpSrc == null)
            {
                return;
            }

            string ext = System.IO.Path.GetExtension(filePath).ToLower();
            BitmapEncoder encoder = null;
            switch (ext)
            {
                case ".bmp":
                    encoder = new BmpBitmapEncoder();
                    break;
                case ".gif":
                    encoder = new GifBitmapEncoder();
                    break;
                case ".jpg":
                    encoder = new JpegBitmapEncoder();
                    break;
                case ".png":
                    encoder = new PngBitmapEncoder();
                    break;
                case ".tif":
                    encoder = new TiffBitmapEncoder();
                    break;
            }

            if (encoder != null)
            {
                encoder.Frames.Add(BitmapFrame.Create(bmpSrc));
                using (FileStream fs =
                    new FileStream(filePath, System.IO.FileMode.Create))
                {
                    encoder.Save(fs);
                }
            }
        }
    }
}