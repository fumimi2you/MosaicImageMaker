using System;
using System.Windows;
using System.Windows.Media.Imaging;

using System.IO;        // FileStream
using System.Drawing;
using System.Drawing.Imaging;

using System.Collections.Generic;   // Dictionary;
using System.Runtime.InteropServices; // Marshal.Copy

using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs; //CommonOpenFileDialog

using System.Linq;  //  ランダムソート

using System.Threading.Tasks;   // 並列処理

namespace WpfAppSample
{
    public class DEF
    {
        public const int TH_W = 160;
        public const int TH_H = 120;
        public const int LET_D = 100;

        public const int SEEK_MAX = 4096;

        public static double SQR(double d) { return d * d; }
    }

    public class ImgPath
    {
        public string[] asSrcImg;
        public string sSrcDir;
        public string sTgtImg;
        public string sDstImg;
    }

    public class UMImage
    {
        const int COL_SIZE = 4;
        enum COL { cB = 0, cG = 1, cR = 2, cAlph = 3 };

        int m_iW;
        int m_iH;
        byte[] m_aData;

        public UMImage(int w, int h)
        {
            m_iW = w;
            m_iH = h;
            m_aData = new byte[m_iW * m_iH * COL_SIZE];
        }

        public UMImage(Bitmap bmOrg)
        {
            m_iW = bmOrg.Size.Width;
            m_iH = bmOrg.Size.Width;
            m_aData = new byte[m_iW * m_iH * COL_SIZE];

            BitmapData data = bmOrg.LockBits(
                new Rectangle(0, 0, m_iW, m_iH),
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb);
            Marshal.Copy(data.Scan0, m_aData, 0, m_aData.Length);
            bmOrg.UnlockBits(data);
        }

        public UMImage(Image imOrg)
        {
            m_iW = imOrg.Size.Width;
            m_iH = imOrg.Size.Width;
            m_aData = new byte[m_iW * m_iH * COL_SIZE];

            Bitmap bm = new Bitmap(imOrg);
            BitmapData data = bm.LockBits(
                new Rectangle(0, 0, m_iW, m_iH),
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb);
            Marshal.Copy(data.Scan0, m_aData, 0, m_aData.Length);
            bm.UnlockBits(data);
        }

        int CalcAdr(int x, int y, COL c)
        {
            return (y * m_iW + x) * COL_SIZE + (int)c;
        }

        public void SetPixel(int x, int y, Color col)
        {
            m_aData[CalcAdr(x, y, COL.cR)] = col.R;
            m_aData[CalcAdr(x, y, COL.cG)] = col.G;
            m_aData[CalcAdr(x, y, COL.cB)] = col.B;
        }
        public Color GetPixel(int x, int y)
        {
            return Color.FromArgb(
                m_aData[CalcAdr(x, y, COL.cR)],
                m_aData[CalcAdr(x, y, COL.cG)],
                m_aData[CalcAdr(x, y, COL.cB)]);
        }

        public Bitmap GetBitmap()
        {
            Bitmap bmRet = new Bitmap(m_iW, m_iH);

            BitmapData data = bmRet.LockBits(
                new Rectangle(0, 0, m_iW, m_iH),
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb);
            Marshal.Copy(m_aData, 0, data.Scan0, m_aData.Length);
            bmRet.UnlockBits(data);

            return bmRet;
        }
    }

    public class ImageLet
    {
        public double dAveR;
        public double dAveG;
        public double dAveB;
        public UMImage bmData;
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

            m_path = new ImgPath();
        }

        private void SetSrcDir_Click(object sender, RoutedEventArgs e)
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
                m_path.asSrcImg = System.IO.Directory.GetFiles(
                    Dialog.FileName, "*.jpg", System.IO.SearchOption.TopDirectoryOnly);
                m_path.sSrcDir = Path.GetDirectoryName(m_path.asSrcImg[0]);

                TextBox_SecImgDir.Text = m_path.sSrcDir;
                TextBlock_DrcImgCnt.Text = "有効画像枚数 : " + m_path.asSrcImg.Length.ToString("#,0") ;
            }

        }

        private void SetTgtPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Title = "目標画像ファイルを開く";
            dialog.Filter = "JPEGファイル(*.jpg)|*.jpg";
            if (dialog.ShowDialog() == true)
            {
                m_path.sTgtImg = dialog.FileName;
                TextBox_TgtImgDir.Text = m_path.sTgtImg;
            }
            else
            {
            }
        }

        private void SetDstPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog();
            dialog.Title = "主力画像ファイルを保存";
            dialog.Filter = "JPEGファイル(*.jpg)|*.jpg";
            if (dialog.ShowDialog() == true)
            {
                m_path.sDstImg = dialog.FileName;
                TextBox_DstImgDir.Text = m_path.sDstImg;
            }
            else
            {
            }
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {

            progressBar1.Minimum = 0;
            progressBar1.Maximum = m_path.asSrcImg.Length;
            progressBar1.Value = 0;

            progressBar2.Minimum = 0;
            progressBar2.Maximum = 100;
            progressBar2.Value = 0;

            // Progressクラスのインスタンスを生成
            var spProg1 = new Progress<int>(ShowProgress1);
            var spProg2 = new Progress<int>(ShowProgress2);

            //  実処理実行
            await Do(m_path, spProg1, spProg2);

            TextBlock_Report.Text = "できました！";

            GC.Collect();
        }

        // 進捗を表示するメソッド（これはUIスレッドで呼び出される）
        private void ShowProgress1(int iVal)
        {
            progressBar1.Value = iVal;
            TextBlock_Report.Text = "一通り素材画像を見てみます。";
        }
        private void ShowProgress2(int iVal)
        {
            progressBar2.Value = iVal;
            TextBlock_Report.Text = "タイリングの組み合わせを考えてみます。";
        }

        //  実処理
        public static async Task<int> Do(ImgPath path, IProgress<int> spProg1, IProgress<int> spProg2)
        {
            Func<int> Job = () =>
            {
                //  LetImgの配列を構築
                List<ImageLet> aImgLet = CommonUtils.MakeImageLets(path.asSrcImg, spProg1);


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
                Bitmap bmTmp = new Bitmap(bmTg, DEF.LET_D * iCelW, DEF.LET_D * iCelH);
                UMImage imgOut = new UMImage(bmTmp);
                bmTmp.Dispose();

                //  ターゲット画像Celの配列作成
                List<ImageCel> aImgCel = CommonUtils.MakeImageCels(imgTg, iCelW, iCelH);

                //  モザイク処理
                int iProg = 0;
                foreach (ImageCel imgCel in aImgCel)
                {
                    int cy = (int)imgCel.pt.Y;
                    int cx = (int)imgCel.pt.X;

                    //  最も近いimgLetを選択
                    int iSel = CommonUtils.SelectImageLet(imgCel.col, aImgLet);
                    ImageLet imgLet = aImgLet[iSel];

                    //for (int dy = 0; dy < DEF.LET_D; dy++)
                    Parallel.For(0, DEF.LET_D, dy =>
                   {
                       int y = cy * DEF.LET_D + dy;

                       for (int dx = 0; dx < DEF.LET_D; dx++)
                       {
                           int x = cx * DEF.LET_D + dx;

                            //  値の取得
                            Color colMid = CommonUtils.ColorBlend(imgLet.bmData.GetPixel(dx, dy), 9, imgCel.col, 1);

                           imgOut.SetPixel(x, y, CommonUtils.ColorBlend(colMid, 9, imgOut.GetPixel(x, y), 1));
                       }
                   });

                    //  一度使ったimgLetはもう使わない
                    aImgLet.RemoveAt(iSel);

                    //  どこにも属さないimgLetが溜まらないように、適度にシャッフル
                    if (((iProg + 1) % (DEF.SEEK_MAX / 2)) == 0)
                    {
                        aImgCel = new List<ImageCel>(aImgCel.OrderBy(i => Guid.NewGuid()).ToArray());
                    }

                    //  プログレス処理
                    spProg2.Report((++iProg) * 100 / (iCelW * iCelH));
                }

                //  ファイルを出力して開く
                Bitmap bmOut = imgOut.GetBitmap();
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
        public static Color ColorBlend(Color col1, double d1, Color col2, double d2)
        {
            double dR = (d1 * col1.R + d2 * col2.R) / (d1 + d2);
            double dG = (d1 * col1.G + d2 * col2.G) / (d1 + d2);
            double dB = (d1 * col1.B + d2 * col2.B) / (d1 + d2);

            return Color.FromArgb((int)dR, (int)dG, (int)dB);
        }

        // 素材画像のImageLet
        public static List<ImageLet> MakeImageLets(string[] aFile, IProgress<int> spProg)
        {
            List<ImageLet> aImgLet = new List<ImageLet>();

            //foreach (string sFile in aFile)
            Object thisLock = new Object();
            int iProc = 0;
            Parallel.ForEach(aFile, sFile =>
            {
                // 画像オブジェクトの作成
                Image imgOg = Image.FromFile(sFile);

                // サムネイルの取得
                Image imgTn = imgOg.GetThumbnailImage(DEF.TH_W, DEF.TH_H, delegate { return false; }, IntPtr.Zero);

                //  サムネイルを処理
                ImageLet imgLet = new ImageLet();
                if (MakeImageLet(imgTn, imgLet))
                {
                    lock (thisLock)
                    {
                        //  辞書に保存
                        lock (aImgLet) aImgLet.Add(imgLet);
                        spProg.Report(++iProc);
                    }
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
            imgLet.bmData = new UMImage(DEF.LET_D, DEF.LET_D);

            //  ビットマップに変換
            Bitmap bmTn = new Bitmap(imgTn);

            double vR = 0;
            double vG = 0;
            double vB = 0;
            for (int y = iYs; y < iYe; y++)
            {
                for (int x = iXs; x < iXe; x++)
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

        public static int SelectImageLet(Color col, List<ImageLet> aImgLet)
        {
            int iRet = 0;

            int iSeekMax = Math.Min(aImgLet.Count, DEF.SEEK_MAX);
            double dstMin = 0xFFFFFF;

            Object thisLock = new Object();

            //for (int i = 0; i < iSeekMax; i++)
            Parallel.For(0, iSeekMax, i =>
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
                    //  排他処理
                    lock (thisLock)
                    {
                        iRet = i;
                        dstMin = dist;
                    }
                }
            });

            return iRet;
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