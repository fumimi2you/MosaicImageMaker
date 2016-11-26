﻿
using System.Drawing;
using System.Drawing.Imaging;


using System.Runtime.InteropServices; // Marshal.Copy

namespace MosaicImageMaker
{

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
            m_iH = bmOrg.Size.Height;
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
            m_iH = imOrg.Size.Height;
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
}
