using System;
using System.Drawing;

using System.Collections.Generic;   // Dictionary;

using System.Linq;  //  ランダムソート
using System.Threading.Tasks;   // 並列処理

using System.Windows.Forms;

namespace MosaicImageMaker
{
    class MosImgCore
    {
        //  実処理
        public static async Task<int> Do(ImgPath path, IProgress<int> spProg1, IProgress<int> spProg2)
        {
            Func<int> Job = () =>
            {
                //  ターゲット画像の読み込み
                Image imgTg = null;
                try
                {
                    imgTg = Image.FromFile(path.sTgtImg);
                }
                catch (Exception e)
                {
                    MessageBox.Show("コレ、読めないかも↓↓\n" + path.sTgtImg, 
                        "ムリぽ",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return 0;
                }
                Bitmap bmTg = new Bitmap(imgTg);


                //  LetImgの配列を構築
                List<ImageLet> aImgLet = MakeImageLets(path.asSrcImg, spProg1);


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
                List<ImageCel> aImgCel = MakeImageCels(imgTg, iCelW, iCelH);

                //  モザイク処理
                int iProg = 0;
                foreach (ImageCel imgCel in aImgCel)
                {
                    int cy = (int)imgCel.pt.Y;
                    int cx = (int)imgCel.pt.X;

                    //  最も近いimgLetを選択
                    int iSel = SelectImageLet(imgCel.col, aImgLet);
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
                Image imgOg = null; 
                try
                {
                    imgOg = Image.FromFile(sFile);
                }
                catch (Exception e)
                {
                    // ここでの失敗はスルーする
                }

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
    }
}
