using System;
using System.Collections.Generic;   // Dictionary;
using System.Drawing;
using System.IO;
using System.Linq;  //  ランダムソート
using System.Threading.Tasks;   // 並列処理


namespace MosaicImageMaker
{
    class CoreResult
    {
        public double dDeltaAve { get; set; }
        public double dDeltaMax { get; set; }
    }

    class MosImgCore
    {
        public enum ECode
        {
            Success = 0,
            Er_Unknown = -1,
            Er_ReadTgeImg = -2,
            Er_LackSrcImg = -3,
        }

        //  実処理
        public static async Task<ECode> Do(ImgPath path, CoreResult coreResult, IProgress<int> spProg1, IProgress<int> spProg2)
        {
            Func<ECode> Job = () =>
            {
                //  ターゲット画像の読み込み
                Image imgTg = null;
                try
                {
                    imgTg = Image.FromFile(path.STgtImg);
                }
                catch (Exception)
                {
                    return ECode.Er_ReadTgeImg;
                }
                Bitmap bmTg = new Bitmap(imgTg);


                //  LetImgの配列を構築
                List<ImageLet> aImgLet = MakeImageLets(path.AsSrcImg, spProg1);
                if (aImgLet.Count < DEF.SQR(DEF.LET_IMG_MIN_MIN))
                {
                    return ECode.Er_LackSrcImg;
                }

                //  出力先の構築(取り敢えずオリジナル画像をリサイズして突っ込む)
                int iCelW, iCelH;
                CalcCelSize(imgTg, aImgLet, out iCelW, out iCelH);
                var bmTmp = new Bitmap(bmTg, DEF.LET_D * iCelW, DEF.LET_D * iCelH);
                var imgOut = new UMImage(bmTmp);
                bmTmp.Dispose();

                //  ターゲット画像Celの配列作成
                List<ImageCel> aImgCel = MakeImageCels(imgTg, iCelW, iCelH);

                //  残差計算の処理
                double dDeltaSum = 0;
                double dDeltaMax = 0;

                //  モザイク処理
                int iProg = 0;
                foreach (ImageCel imgCel in aImgCel)
                {
                    int cy = (int)imgCel.pt.Y;
                    int cx = (int)imgCel.pt.X;

                    //  最も近いimgLetを選択
                    double dDelta = 0;
                    int iSel = SelectImageLet(imgCel.aCol, aImgLet, out dDelta);
                    ImageLet imgLet = aImgLet[iSel];

                    dDeltaSum += dDelta;
                    dDeltaMax = Math.Max(dDelta, dDeltaMax);

                    //for (int dy = 0; dy < DEF.LET_D; dy++)
                    Parallel.For(0, DEF.LET_D, dy =>
                    {
                        int y = cy * DEF.LET_D + dy;

                        for (int dx = 0; dx < DEF.LET_D; dx++)
                        {
                            int x = cx * DEF.LET_D + dx;

                            //  値の取得()
                            imgOut.SetPixel(x, y, CommonUtils.ColorBlend(imgLet.bmData.GetPixel(dx, dy), 1, imgOut.GetPixel(x, y), 0));
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
                    spProg2.Report((++iProg) * DEF.PERCENT_MAX / (iCelW * iCelH));
                }

                coreResult.dDeltaAve = dDeltaSum / aImgCel.Count;
                coreResult.dDeltaMax = dDeltaMax;


                //  ファイルを出力して開く
                using (Bitmap bmOut = imgOut.GetBitmap())
                {
                    bmOut.Save(path.SDstImg, System.Drawing.Imaging.ImageFormat.Jpeg);
                    System.Diagnostics.Process.Start(path.SDstImg);
                }

                //  お片付け
                imgTg.Dispose();
                bmTg.Dispose();

                return ECode.Success;
            };

            return await Task.Run(Job);
        }

        private static void CalcCelSize(Image imgTg, List<ImageLet> aImgLet, out int iCelW, out int iCelH)
        {
            int iCelL = (int)Math.Sqrt(aImgLet.Count / 2);
            iCelW = 0;
            iCelH = 0;
            if (imgTg.Width > imgTg.Height)
            {
                iCelW = Math.Max(DEF.LET_IMG_MIN_MIN, iCelL);
                iCelH = Math.Max(DEF.LET_IMG_MIN_MIN, iCelW * imgTg.Height / imgTg.Width);
            }
            else
            {
                iCelH = Math.Max(DEF.LET_IMG_MIN_MIN, iCelL);
                iCelW = Math.Max(DEF.LET_IMG_MIN_MIN, iCelH * imgTg.Width / imgTg.Height);
            }
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
                try
                {
                    // 画像オブジェクトの作成とサムネ取得
                    using (var fs = File.OpenRead(sFile))
                    using (var imgOg = Image.FromStream(fs, false, false))
                    using (var imgTn = imgOg.GetThumbnailImage(DEF.TH_W, DEF.TH_H, delegate { return false; }, IntPtr.Zero))
                    {
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
                    }
                }
                catch (Exception)
                {
                    // ここでの失敗はスルーする
                }
            });

            return aImgLet;
        }

        // ターゲット画像のImageCel
        public static List<ImageCel> MakeImageCels(Image imgOg, int iCelW, int iCelH)
        {
            List<ImageCel> ptTmp = new List<ImageCel>();

            // サムネイルの取得
            var imgTn = imgOg.GetThumbnailImage(iCelW * DEF.FIT_DELTA, iCelH * DEF.FIT_DELTA, delegate { return false; }, IntPtr.Zero);

            //  ビットマップに変換
            using (var bmTn = new Bitmap(imgTn))
            {
                //  セルの作成
                for (int cy = 0; cy < imgTn.Height; cy += DEF.FIT_DELTA)
                {
                    for (int cx = 0; cx < imgTn.Width; cx += DEF.FIT_DELTA)
                    {
                        ImageCel imgCel = new ImageCel();
                        imgCel.pt.X = cx / DEF.FIT_DELTA;
                        imgCel.pt.Y = cy / DEF.FIT_DELTA;
                        imgCel.aCol[0] = bmTn.GetPixel(cx + 0, cy + 0);
                        imgCel.aCol[1] = bmTn.GetPixel(cx + 1, cy + 0);
                        imgCel.aCol[2] = bmTn.GetPixel(cx + 0, cy + 1);
                        imgCel.aCol[3] = bmTn.GetPixel(cx + 1, cy + 1);

                        ptTmp.Add(imgCel);
                    }
                }
            }

            return new List<ImageCel>(ptTmp.OrderBy(i => Guid.NewGuid()).ToArray());
        }


        // 画像ファイル → ImageLet
        public static Boolean MakeImageLet(Image imgTn, ImageLet imgLet)
        {
            //  定数計算(ImageLetの評価値は2x2分割決め打ちで取ってくるので、左上1/4を計算)
            const int iDh = DEF.LET_D / 2;
            int iXs = (DEF.TH_W - DEF.LET_D) / 2;
            int iYs = (DEF.TH_H - DEF.LET_D) / 2;
            int iXe = DEF.TH_W / 2;
            int iYe = DEF.TH_H / 2;

            //  imgLetの生成
            imgLet.bmData = new UMImage(DEF.LET_D, DEF.LET_D);

            //  ビットマップに変換
            Bitmap bmTn = new Bitmap(imgTn);

            double[] aR = new double[DEF.SQR(DEF.FIT_DELTA)];
            double[] aG = new double[DEF.SQR(DEF.FIT_DELTA)];
            double[] aB = new double[DEF.SQR(DEF.FIT_DELTA)];
            for (int y = iYs; y < iYe; y++)
            {
                int yy = y - iYs;

                for (int x = iXs; x < iXe; x++)
                {
                    int xx = x - iXs;
                    //  画素値コピー
                    Color[] aCol = new Color[] {
                        bmTn.GetPixel(x    , y    ),
                        bmTn.GetPixel(x+iDh, y    ),
                        bmTn.GetPixel(x    , y+iDh),
                        bmTn.GetPixel(x+iDh, y+iDh) };

                    imgLet.bmData.SetPixel(xx    , yy    , aCol[0]);
                    imgLet.bmData.SetPixel(xx+iDh, yy    , aCol[1]);
                    imgLet.bmData.SetPixel(xx    , yy+iDh, aCol[2]);
                    imgLet.bmData.SetPixel(xx+iDh, yy+iDh, aCol[3]);

                    //  平均用の積算
                    for (int a = 0; a < DEF.SQR(DEF.FIT_DELTA); a++)
                    {
                        aR[a] += aCol[a].R;
                        aG[a] += aCol[a].G;
                        aB[a] += aCol[a].B;
                    }
                }
            }

            //  平均値算出
            for (int a = 0; a < DEF.SQR(DEF.FIT_DELTA); a++)
            {
                imgLet.adAveR[a] += aR[a] / DEF.SQR(iDh);
                imgLet.adAveG[a] += aG[a] / DEF.SQR(iDh);
                imgLet.adAveB[a] += aB[a] / DEF.SQR(iDh);
            }

            return true;
        }

        public static int SelectImageLet(Color[] aCol, List<ImageLet> aImgLet, out double dDelta)
        {
            int iRet = 0;

            int iSeekMax = Math.Min(aImgLet.Count, DEF.SEEK_MAX);
            double dstMin = 0x0FFFFFFF;

            Object thisLock = new Object();

            //for (int i = 0; i < iSeekMax; i++)
            Parallel.For(0, iSeekMax, i =>
            {
                ImageLet imgLet = aImgLet[i];

                //  3色, 4箇所の二乗誤差を積算
                double dist = 0;
                for (int a = 0; a < DEF.SQR(DEF.FIT_DELTA); a++)
                {
                    //  2点間距離(大小関係だけに着目するのでroot処理は不要)
                    dist += DEF.SQR(aCol[a].R - imgLet.adAveR[a]) + DEF.SQR(aCol[a].G - imgLet.adAveG[a]) + DEF.SQR(aCol[a].B - imgLet.adAveB[a]);
                }

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

            dDelta = dstMin;
            return iRet;
        }
    }
}
