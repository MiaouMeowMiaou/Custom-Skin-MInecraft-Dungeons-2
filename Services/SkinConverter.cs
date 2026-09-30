using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace MCD2SkinStudioWpf.Services;

public static class SkinConverter
{
    public static bool DetectSlimArm(Bitmap sourceImage)
    {
        using var resized = (sourceImage.Width == 64 && sourceImage.Height == 64)
            ? new Bitmap(sourceImage)
            : new Bitmap(sourceImage, new Size(64, 64));

        for (int y = 20; y < 32; y++)
        {
            for (int x = 54; x < 56; x++)
            {
                if (resized.GetPixel(x, y).A != 0)
                {
                    return false;
                }
            }
        }
        return true;
    }

    public static Bitmap ConvertJavaSkinToMcd(Bitmap sourceImage, bool isSlim)
    {
        var src = (sourceImage.Width == 64 && sourceImage.Height == 64)
            ? new Bitmap(sourceImage)
            : new Bitmap(sourceImage, new Size(64, 64));

        var dst = new Bitmap(64, 64, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(dst);
        g.CompositingMode = CompositingMode.SourceOver;
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;

        void CopyRegion(Rectangle sourceRect, Point targetPoint, Size? resizeTo = null)
        {
            var targetRect = resizeTo.HasValue
                ? new Rectangle(targetPoint, resizeTo.Value)
                : new Rectangle(targetPoint, sourceRect.Size);
            g.DrawImage(src, targetRect, sourceRect, GraphicsUnit.Pixel);
        }

        CopyRegion(new Rectangle(0, 8, 8, 8), new Point(0, 8));
        CopyRegion(new Rectangle(8, 8, 8, 8), new Point(8, 8));
        CopyRegion(new Rectangle(16, 8, 8, 8), new Point(16, 8));
        CopyRegion(new Rectangle(24, 8, 8, 8), new Point(24, 8));
        CopyRegion(new Rectangle(8, 0, 8, 8), new Point(8, 0));
        CopyRegion(new Rectangle(16, 0, 8, 8), new Point(16, 0));

        CopyRegion(new Rectangle(32, 8, 8, 8), new Point(32, 8));
        CopyRegion(new Rectangle(40, 8, 8, 8), new Point(40, 8));
        CopyRegion(new Rectangle(48, 8, 8, 8), new Point(48, 8));
        CopyRegion(new Rectangle(56, 8, 8, 8), new Point(56, 8));
        CopyRegion(new Rectangle(40, 0, 8, 8), new Point(40, 0));
        CopyRegion(new Rectangle(48, 0, 8, 8), new Point(48, 0));

        CopyRegion(new Rectangle(16, 20, 4, 12), new Point(16, 20));
        CopyRegion(new Rectangle(20, 20, 8, 12), new Point(20, 20));
        CopyRegion(new Rectangle(28, 20, 4, 12), new Point(28, 20));
        CopyRegion(new Rectangle(32, 20, 8, 12), new Point(32, 20));
        CopyRegion(new Rectangle(20, 16, 8, 4), new Point(20, 16));
        CopyRegion(new Rectangle(28, 16, 8, 4), new Point(28, 16));

        CopyRegion(new Rectangle(16, 36, 4, 12), new Point(16, 20));
        CopyRegion(new Rectangle(20, 36, 8, 12), new Point(20, 20));
        CopyRegion(new Rectangle(28, 36, 4, 12), new Point(28, 20));
        CopyRegion(new Rectangle(32, 36, 8, 12), new Point(32, 20));
        CopyRegion(new Rectangle(20, 32, 8, 4), new Point(20, 16));
        CopyRegion(new Rectangle(28, 32, 8, 4), new Point(28, 16));

        CopyRegion(new Rectangle(4, 16, 4, 4), new Point(4, 16));
        CopyRegion(new Rectangle(8, 16, 4, 4), new Point(8, 16));
        CopyRegion(new Rectangle(0, 20, 4, 12), new Point(0, 20));
        CopyRegion(new Rectangle(4, 20, 4, 12), new Point(4, 20));
        CopyRegion(new Rectangle(8, 20, 4, 12), new Point(8, 20));
        CopyRegion(new Rectangle(12, 20, 4, 12), new Point(12, 20));

        CopyRegion(new Rectangle(4, 32, 4, 4), new Point(4, 16));
        CopyRegion(new Rectangle(8, 32, 4, 4), new Point(8, 16));
        CopyRegion(new Rectangle(0, 36, 4, 12), new Point(0, 20));
        CopyRegion(new Rectangle(4, 36, 4, 12), new Point(4, 20));
        CopyRegion(new Rectangle(8, 36, 4, 12), new Point(8, 20));
        CopyRegion(new Rectangle(12, 36, 4, 12), new Point(12, 20));

        CopyRegion(new Rectangle(20, 48, 4, 4), new Point(20, 48));
        CopyRegion(new Rectangle(24, 48, 4, 4), new Point(24, 48));
        CopyRegion(new Rectangle(16, 52, 4, 12), new Point(16, 52));
        CopyRegion(new Rectangle(20, 52, 4, 12), new Point(20, 52));
        CopyRegion(new Rectangle(24, 52, 4, 12), new Point(24, 52));
        CopyRegion(new Rectangle(28, 52, 4, 12), new Point(28, 52));

        CopyRegion(new Rectangle(4, 48, 4, 4), new Point(20, 48));
        CopyRegion(new Rectangle(8, 48, 4, 4), new Point(24, 48));
        CopyRegion(new Rectangle(0, 52, 4, 12), new Point(16, 52));
        CopyRegion(new Rectangle(4, 52, 4, 12), new Point(20, 52));
        CopyRegion(new Rectangle(8, 52, 4, 12), new Point(24, 52));
        CopyRegion(new Rectangle(12, 52, 4, 12), new Point(28, 52));

        if (isSlim)
        {
            CopyRegion(new Rectangle(44, 16, 3, 4), new Point(44, 16));
            CopyRegion(new Rectangle(47, 16, 3, 4), new Point(47, 16));
            CopyRegion(new Rectangle(40, 20, 4, 12), new Point(40, 20));
            CopyRegion(new Rectangle(44, 20, 3, 12), new Point(44, 20));
            CopyRegion(new Rectangle(47, 20, 4, 12), new Point(47, 20));
            CopyRegion(new Rectangle(51, 20, 3, 12), new Point(51, 20));

            CopyRegion(new Rectangle(44, 32, 3, 4), new Point(44, 16));
            CopyRegion(new Rectangle(47, 32, 3, 4), new Point(47, 16));
            CopyRegion(new Rectangle(40, 36, 4, 12), new Point(40, 20));
            CopyRegion(new Rectangle(44, 36, 3, 12), new Point(44, 20));
            CopyRegion(new Rectangle(47, 36, 4, 12), new Point(47, 20));
            CopyRegion(new Rectangle(51, 36, 3, 12), new Point(51, 20));
        }
        else
        {
            CopyRegion(new Rectangle(44, 16, 4, 4), new Point(44, 16), new Size(3, 4));
            CopyRegion(new Rectangle(48, 16, 4, 4), new Point(47, 16), new Size(3, 4));
            CopyRegion(new Rectangle(40, 20, 4, 12), new Point(40, 20));
            CopyRegion(new Rectangle(44, 20, 4, 12), new Point(44, 20), new Size(3, 12));
            CopyRegion(new Rectangle(48, 20, 4, 12), new Point(47, 20));
            CopyRegion(new Rectangle(52, 20, 4, 12), new Point(51, 20), new Size(3, 12));

            CopyRegion(new Rectangle(44, 32, 4, 4), new Point(44, 16), new Size(3, 4));
            CopyRegion(new Rectangle(48, 32, 4, 4), new Point(47, 16), new Size(3, 4));
            CopyRegion(new Rectangle(40, 36, 4, 12), new Point(40, 20));
            CopyRegion(new Rectangle(44, 36, 4, 12), new Point(44, 20), new Size(3, 12));
            CopyRegion(new Rectangle(48, 36, 4, 12), new Point(47, 20));
            CopyRegion(new Rectangle(52, 36, 4, 12), new Point(51, 20), new Size(3, 12));
        }

        if (isSlim)
        {
            CopyRegion(new Rectangle(36, 48, 3, 4), new Point(36, 48));
            CopyRegion(new Rectangle(39, 48, 3, 4), new Point(39, 48));
            CopyRegion(new Rectangle(39, 52, 4, 12), new Point(32, 52));
            CopyRegion(new Rectangle(36, 52, 3, 12), new Point(36, 52));
            CopyRegion(new Rectangle(32, 52, 4, 12), new Point(39, 52));
            CopyRegion(new Rectangle(43, 52, 3, 12), new Point(43, 52));

            CopyRegion(new Rectangle(52, 48, 3, 4), new Point(36, 48));
            CopyRegion(new Rectangle(55, 48, 3, 4), new Point(39, 48));
            CopyRegion(new Rectangle(55, 52, 4, 12), new Point(32, 52));
            CopyRegion(new Rectangle(52, 52, 3, 12), new Point(36, 52));
            CopyRegion(new Rectangle(48, 52, 4, 12), new Point(39, 52));
            CopyRegion(new Rectangle(59, 52, 3, 12), new Point(43, 52));
        }
        else
        {
            CopyRegion(new Rectangle(36, 48, 4, 4), new Point(36, 48), new Size(3, 4));
            CopyRegion(new Rectangle(40, 48, 4, 4), new Point(39, 48), new Size(3, 4));
            CopyRegion(new Rectangle(40, 52, 4, 12), new Point(32, 52));
            CopyRegion(new Rectangle(36, 52, 4, 12), new Point(36, 52), new Size(3, 12));
            CopyRegion(new Rectangle(32, 52, 4, 12), new Point(39, 52));
            CopyRegion(new Rectangle(44, 52, 4, 12), new Point(43, 52), new Size(3, 12));

            CopyRegion(new Rectangle(52, 48, 4, 4), new Point(36, 48), new Size(3, 4));
            CopyRegion(new Rectangle(56, 48, 4, 4), new Point(39, 48), new Size(3, 4));
            CopyRegion(new Rectangle(56, 52, 4, 12), new Point(32, 52));
            CopyRegion(new Rectangle(52, 52, 4, 12), new Point(36, 52), new Size(3, 12));
            CopyRegion(new Rectangle(48, 52, 4, 12), new Point(39, 52));
            CopyRegion(new Rectangle(60, 52, 4, 12), new Point(43, 52), new Size(3, 12));
        }

        CopyRegion(new Rectangle(8, 8, 8, 8), new Point(56, 20));
        CopyRegion(new Rectangle(40, 8, 8, 8), new Point(56, 20));
        CopyRegion(new Rectangle(8, 8, 8, 8), new Point(56, 0));
        CopyRegion(new Rectangle(40, 8, 8, 8), new Point(56, 0));

        dst.SetPixel(63, 63, Color.FromArgb(158, 42, 131, 193));

        Color fill = dst.GetPixel(24, 24);
        Color solidColor = (fill.A == 0)
            ? Color.FromArgb(255, 20, 20, 20)
            : Color.FromArgb(255, fill.R, fill.G, fill.B);

        void FillSolidIfTransparent(int xStart, int xEnd, int yStart, int yEnd)
        {
            for (int y = yStart; y < yEnd; y++)
            {
                for (int x = xStart; x < xEnd; x++)
                {
                    if (dst.GetPixel(x, y).A == 0)
                        dst.SetPixel(x, y, solidColor);
                }
            }
        }

        FillSolidIfTransparent(8, 24, 0, 8);
        FillSolidIfTransparent(0, 32, 8, 16);
        FillSolidIfTransparent(4, 12, 16, 20);
        FillSolidIfTransparent(20, 36, 16, 20);
        FillSolidIfTransparent(44, 50, 16, 20);
        FillSolidIfTransparent(0, 54, 20, 32);
        FillSolidIfTransparent(20, 28, 48, 52);
        FillSolidIfTransparent(36, 42, 48, 52);
        FillSolidIfTransparent(16, 46, 52, 64);

        return dst;
    }

    public static Bitmap GeneratePortraitVignette(Bitmap mcdSkin)
    {
        var icon = new Bitmap(256, 256, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(icon))
        {
            for (int y = 0; y < 256; y++)
            {
                int gray = (int)(22 + (y / 256.0) * 20);
                using var pen = new Pen(Color.FromArgb(255, gray, gray + 2, gray + 6));
                g.DrawLine(pen, 0, y, 255, y);
            }

            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            var faceRect = new Rectangle(8, 8, 8, 8);
            var hatRect = new Rectangle(40, 8, 8, 8);
            var targetRect = new Rectangle(58, 48, 140, 140);

            g.DrawImage(mcdSkin, targetRect, faceRect, GraphicsUnit.Pixel);
            g.DrawImage(mcdSkin, targetRect, hatRect, GraphicsUnit.Pixel);
        }
        return icon;
    }

    public static byte[] EncodeDxt5(Bitmap image)
    {
        using var resized = (image.Width == 256 && image.Height == 256)
            ? new Bitmap(image)
            : new Bitmap(image, new Size(256, 256));

        byte[] dxt = new byte[65536];
        int dxtIdx = 0;

        for (int by = 0; by < 256; by += 4)
        {
            for (int bx = 0; bx < 256; bx += 4)
            {
                byte[] alphas = new byte[16];
                Color[] colors = new Color[16];
                int p = 0;

                for (int py = 0; py < 4; py++)
                {
                    for (int px = 0; px < 4; px++)
                    {
                        var c = resized.GetPixel(bx + px, by + py);
                        alphas[p] = c.A;
                        colors[p] = c;
                        p++;
                    }
                }

                byte minA = 255, maxA = 0;
                foreach (var a in alphas)
                {
                    if (a < minA) minA = a;
                    if (a > maxA) maxA = a;
                }

                if (minA == maxA)
                {
                    dxt[dxtIdx + 0] = maxA;
                    dxt[dxtIdx + 1] = minA;
                    for (int i = 2; i < 8; i++) dxt[dxtIdx + i] = 0;
                }
                else
                {
                    byte a0 = maxA, a1 = minA;
                    int[] palette = new int[8];
                    palette[0] = a0;
                    palette[1] = a1;
                    for (int i = 1; i < 7; i++)
                        palette[i + 1] = ((7 - i) * a0 + i * a1) / 7;

                    ulong bits = 0;
                    for (int i = 0; i < 16; i++)
                    {
                        int bestIdx = 0;
                        int bestDist = Math.Abs(alphas[i] - palette[0]);
                        for (int k = 1; k < 8; k++)
                        {
                            int dist = Math.Abs(alphas[i] - palette[k]);
                            if (dist < bestDist) { bestDist = dist; bestIdx = k; }
                        }
                        bits |= ((ulong)bestIdx << (i * 3));
                    }

                    dxt[dxtIdx + 0] = a0;
                    dxt[dxtIdx + 1] = a1;
                    dxt[dxtIdx + 2] = (byte)(bits & 0xFF);
                    dxt[dxtIdx + 3] = (byte)((bits >> 8) & 0xFF);
                    dxt[dxtIdx + 4] = (byte)((bits >> 16) & 0xFF);
                    dxt[dxtIdx + 5] = (byte)((bits >> 24) & 0xFF);
                    dxt[dxtIdx + 6] = (byte)((bits >> 32) & 0xFF);
                    dxt[dxtIdx + 7] = (byte)((bits >> 40) & 0xFF);
                }
                dxtIdx += 8;

                bool visible = false;
                int minR = 255, maxR = 0, minG = 255, maxG = 0, minB = 255, maxB = 0;
                foreach (var c in colors)
                {
                    if (c.A > 8)
                    {
                        visible = true;
                        if (c.R < minR) minR = c.R; if (c.R > maxR) maxR = c.R;
                        if (c.G < minG) minG = c.G; if (c.G > maxG) maxG = c.G;
                        if (c.B < minB) minB = c.B; if (c.B > maxB) maxB = c.B;
                    }
                }

                if (!visible)
                {
                    for (int i = 0; i < 8; i++) dxt[dxtIdx + i] = 0;
                    dxtIdx += 8;
                    continue;
                }

                ushort ColorToRgb565(int r, int g, int b)
                {
                    int r5 = (r * 31 + 127) / 255;
                    int g6 = (g * 63 + 127) / 255;
                    int b5 = (b * 31 + 127) / 255;
                    return (ushort)((r5 << 11) | (g6 << 5) | b5);
                }

                ushort c0 = ColorToRgb565(maxR, maxG, maxB);
                ushort c1 = ColorToRgb565(minR, minG, minB);

                if (c0 == c1)
                {
                    dxt[dxtIdx + 0] = (byte)(c0 & 0xFF);
                    dxt[dxtIdx + 1] = (byte)((c0 >> 8) & 0xFF);
                    dxt[dxtIdx + 2] = (byte)(c1 & 0xFF);
                    dxt[dxtIdx + 3] = (byte)((c1 >> 8) & 0xFF);
                    for (int i = 4; i < 8; i++) dxt[dxtIdx + i] = 0;
                }
                else
                {
                    if (c0 < c1) { (c0, c1) = (c1, c0); }

                    int r0 = ((c0 >> 11) & 0x1F) * 255 / 31;
                    int g0 = ((c0 >> 5) & 0x3F) * 255 / 63;
                    int b0 = (c0 & 0x1F) * 255 / 31;

                    int r1 = ((c1 >> 11) & 0x1F) * 255 / 31;
                    int g1 = ((c1 >> 5) & 0x3F) * 255 / 63;
                    int b1 = (c1 & 0x1F) * 255 / 31;

                    int[][] pal = [
                        [r0, g0, b0],
                        [r1, g1, b1],
                        [(2 * r0 + r1) / 3, (2 * g0 + g1) / 3, (2 * b0 + b1) / 3],
                        [(r0 + 2 * r1) / 3, (g0 + 2 * g1) / 3, (b0 + 2 * b1) / 3]
                    ];

                    uint colorBits = 0;
                    for (int i = 0; i < 16; i++)
                    {
                        var c = colors[i];
                        int bestK = 0;
                        if (c.A > 8)
                        {
                            int bestDist = int.MaxValue;
                            for (int k = 0; k < 4; k++)
                            {
                                int dr = c.R - pal[k][0];
                                int dg = c.G - pal[k][1];
                                int db = c.B - pal[k][2];
                                int dist = dr * dr + dg * dg + db * db;
                                if (dist < bestDist) { bestDist = dist; bestK = k; }
                            }
                        }
                        colorBits |= ((uint)bestK << (i * 2));
                    }

                    dxt[dxtIdx + 0] = (byte)(c0 & 0xFF);
                    dxt[dxtIdx + 1] = (byte)((c0 >> 8) & 0xFF);
                    dxt[dxtIdx + 2] = (byte)(c1 & 0xFF);
                    dxt[dxtIdx + 3] = (byte)((c1 >> 8) & 0xFF);
                    dxt[dxtIdx + 4] = (byte)(colorBits & 0xFF);
                    dxt[dxtIdx + 5] = (byte)((colorBits >> 8) & 0xFF);
                    dxt[dxtIdx + 6] = (byte)((colorBits >> 16) & 0xFF);
                    dxt[dxtIdx + 7] = (byte)((colorBits >> 24) & 0xFF);
                }
                dxtIdx += 8;
            }
        }
        return dxt;
    }

    public static byte[] GenerateBc7Mode6FlatNormal(byte r, byte g, byte b, byte a)
    {
        byte r7 = (byte)(r >> 1);
        byte g7 = (byte)(g >> 1);
        byte b7 = (byte)(b >> 1);
        byte a7 = (byte)(a >> 1);
        byte pbit = (byte)(g & 1);

        ulong low = 0;
        int pos = 0;
        void Put(ulong val, int count)
        {
            low |= ((val & ((1UL << count) - 1UL)) << pos);
            pos += count;
        }

        Put(1UL << 6, 7);
        Put(r7, 7); Put(r7, 7);
        Put(g7, 7); Put(g7, 7);
        Put(b7, 7); Put(b7, 7);
        Put(a7, 7); Put(a7, 7);
        Put(pbit, 1); Put(pbit, 1);

        byte[] block = new byte[16];
        Array.Copy(BitConverter.GetBytes(low), block, 8);
        return block;
    }
}
