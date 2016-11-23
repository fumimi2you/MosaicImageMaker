using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

using Microsoft.Win32;  // OpenFileDialog, SaveFileDialog
using System.IO;        // FileStream
using System.Drawing;

using Microsoft.WindowsAPICodePack.Dialogs; //CommonOpenFileDialog
using System.Collections.Generic;   // Dictionary;

using System.Linq;  //  ランダムソート

namespace WpfAppSample
{
    public class DEF
    {
        public const int TH_W   = 160;
        public const int TH_H   = 120;
        public const int LET_D  = 100;
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


    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
        }

        // メニュー - 終了
        private void CloseCommand_Executed(
            object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }

        // メニュー - 開く
        private void OpenCommand_Executed(
            object sender, ExecutedRoutedEventArgs e)
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
                //"C:\test"以下のファイルをすべて取得する
                //ワイルドカード"*"は、すべてのファイルを意味する
                string[] aFile = System.IO.Directory.GetFiles(
                    Dialog.FileName, "*.jpg", System.IO.SearchOption.TopDirectoryOnly);

                //  LetImgの配列を構築
                List<ImageLet> aImgLet = CommonUtils.MakeImageLets(aFile);


                //  ターゲット画像の読み込み
                Image imgTg = Image.FromFile(aFile[0]);
                Bitmap bmTg = new Bitmap(imgTg);

                //  出力先の構築(取り敢えずオリジナル画像をリサイズして突っ込む)
                int iCelW = (int)Math.Sqrt(aImgLet.Count);
                int iCelH = iCelW * imgTg.Height / imgTg.Width;
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
                    {
                        int y = cy * DEF.LET_D + dy;

                        for (int dx = 0; dx < DEF.LET_D; dx++)
                        {
                            int x = cx * DEF.LET_D + dx;

                            //  値の取得
                            Color colMid = CommonUtils.ColorBlend(imgLet.bmData.GetPixel(dx, dy), 9, imgCel.col, 1);

                            bmOut.SetPixel(x, y, CommonUtils.ColorBlend(colMid, 9, bmOut.GetPixel(x,y), 1));
                        }
                    }
                }

                string stOutput = Path.GetDirectoryName(aFile[0]) + "\\rt\\ret" + Path.GetFileName(aFile[0]);
                bmOut.Save(stOutput, System.Drawing.Imaging.ImageFormat.Jpeg);
            }
        }

        // メニュー - 保存
        private void SaveAsCommand_Executed(
            object sender, ExecutedRoutedEventArgs e)
        {
/*
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "BMPファイル(*.bmp)|*.bmp"
                + "|GIFファイル(*.gif)|*.gif"
                + "|JPEGファイル(*.jpg)|*.jpg"
                + "|PNGファイル(*.png)|*.png"
                + "|TIFFファイル(*.tif)|*.tif";
            Nullable<bool> result = dialog.ShowDialog();
            if (result == true)
            {
                CommonUtils.BitmapSourceToFile(
                    dialog.FileName, image.Source as BitmapSource);
            }
*/
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

            foreach (string sFile in aFile)
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
                    aImgLet.Add(imgLet);

                    // サムネイルの保存(デバッグ用)
                    string fileTh = Path.GetDirectoryName(sFile) + "\\tn\\" + Path.GetFileName(sFile);
                    imgLet.bmData.Save(fileTh, System.Drawing.Imaging.ImageFormat.Jpeg);
                }

                //  お片付け
                imgOg.Dispose();
                imgTn.Dispose();
            }

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
            int iSel = 0;
            double dstMin = Math.Pow( 255, 3 );

            for( int i = 0; i < aImgLet.Count; i++)
            {
                ImageLet imgLet = aImgLet[i];

                double dist = Math.Pow(
                    Math.Pow(col.R - imgLet.dAveR, 2) +
                    Math.Pow(col.G - imgLet.dAveG, 2) +
                    Math.Pow(col.B - imgLet.dAveB, 2), 0.5);

                if (dist < dstMin)
                {
                    iSel = i;
                    dstMin = dist;
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