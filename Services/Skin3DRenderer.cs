using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace MCD2SkinStudioWpf.Services;

public static class Skin3DRenderer
{
    private struct Vector3D
    {
        public double X, Y, Z;
        public Vector3D(double x, double y, double z) { X = x; Y = y; Z = z; }
    }

    private class Quad3D
    {
        public Vector3D V0, V1, V2, V3;
        public Rectangle SrcRect;
        public double Brightness;
        public double Depth;
        public bool IsOuterLayer;
    }

    public static Bitmap RenderIsometricHero(Bitmap skinBitmap, bool isSlim = false, int size = 256)
    {
        var output = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(output);
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using var skin = PrepareSkin(skinBitmap);

        var quads = new List<Quad3D>();
        double armW = isSlim ? 3.0 : 4.0;
        int armTexW = isSlim ? 3 : 4;

        double charYaw = 24.0 * Math.PI / 180.0;
        double charPitch = 10.0 * Math.PI / 180.0;

        Vector3D RotateX(Vector3D v, double angleRad, Vector3D pivot)
        {
            double y = v.Y - pivot.Y;
            double z = v.Z - pivot.Z;
            double cos = Math.Cos(angleRad);
            double sin = Math.Sin(angleRad);
            return new Vector3D(
                v.X,
                pivot.Y + (y * cos - z * sin),
                pivot.Z + (y * sin + z * cos)
            );
        }

        Vector3D RotateY(Vector3D v, double angleRad, Vector3D pivot)
        {
            double x = v.X - pivot.X;
            double z = v.Z - pivot.Z;
            double cos = Math.Cos(angleRad);
            double sin = Math.Sin(angleRad);
            return new Vector3D(
                pivot.X + (x * cos + z * sin),
                v.Y,
                pivot.Z + (-x * sin + z * cos)
            );
        }

        Vector3D RotateZ(Vector3D v, double angleRad, Vector3D pivot)
        {
            double x = v.X - pivot.X;
            double y = v.Y - pivot.Y;
            double cos = Math.Cos(angleRad);
            double sin = Math.Sin(angleRad);
            return new Vector3D(
                pivot.X + (x * cos - y * sin),
                pivot.Y + (x * sin + y * cos),
                v.Z
            );
        }

        Vector3D TransformWorld(Vector3D v)
        {
            double cosY = Math.Cos(charYaw);
            double sinY = Math.Sin(charYaw);
            double x1 = v.X * cosY + v.Z * sinY;
            double z1 = -v.X * sinY + v.Z * cosY;

            double cosP = Math.Cos(charPitch);
            double sinP = Math.Sin(charPitch);
            double y2 = v.Y * cosP - z1 * sinP;
            double z2 = v.Y * sinP + z1 * cosP;

            return new Vector3D(x1, y2, z2);
        }

        bool HasPixels(Rectangle r)
        {
            if (r.X < 0 || r.Y < 0 || r.Right > skin.Width || r.Bottom > skin.Height) return false;
            for (int y = r.Top; y < r.Bottom; y++)
            {
                for (int x = r.Left; x < r.Right; x++)
                {
                    if (skin.GetPixel(x, y).A > 15) return true;
                }
            }
            return false;
        }

        void AddLimbBox(Vector3D origin, Vector3D sizeVec,
                        Rectangle topUV, Rectangle botUV, Rectangle frontUV, Rectangle backUV, Rectangle leftUV, Rectangle rightUV,
                        Func<Vector3D, Vector3D>? localAnim = null, bool isOuter = false)
        {
            double x0 = origin.X, x1 = origin.X + sizeVec.X;
            double y0 = origin.Y, y1 = origin.Y + sizeVec.Y;
            double z0 = origin.Z, z1 = origin.Z + sizeVec.Z;

            var p_000 = new Vector3D(x0, y0, z0);
            var p_100 = new Vector3D(x1, y0, z0);
            var p_010 = new Vector3D(x0, y1, z0);
            var p_110 = new Vector3D(x1, y1, z0);
            var p_001 = new Vector3D(x0, y0, z1);
            var p_101 = new Vector3D(x1, y0, z1);
            var p_011 = new Vector3D(x0, y1, z1);
            var p_111 = new Vector3D(x1, y1, z1);

            Vector3D Prep(Vector3D pt)
            {
                if (localAnim != null) pt = localAnim(pt);
                return TransformWorld(pt);
            }

            void AddFace(Vector3D v0, Vector3D v1, Vector3D v2, Vector3D v3, Rectangle uv, double bright)
            {
                if (isOuter && !HasPixels(uv)) return;

                var w0 = Prep(v0);
                var w1 = Prep(v1);
                var w2 = Prep(v2);
                var w3 = Prep(v3);

                double sx0 = w0.X, sy0 = -w0.Y;
                double sx1 = w1.X, sy1 = -w1.Y;
                double sx3 = w3.X, sy3 = -w3.Y;
                double cross = (sx1 - sx0) * (sy3 - sy0) - (sy1 - sy0) * (sx3 - sx0);
                if (cross <= 0.0) return;

                double avgZ = (w0.Z + w1.Z + w2.Z + w3.Z) / 4.0;
                quads.Add(new Quad3D
                {
                    V0 = w0, V1 = w1, V2 = w2, V3 = w3,
                    SrcRect = uv,
                    Brightness = bright,
                    Depth = avgZ + (isOuter ? 0.05 : 0.0),
                    IsOuterLayer = isOuter
                });
            }

            AddFace(p_011, p_111, p_101, p_001, frontUV, 0.90);
            AddFace(p_110, p_010, p_000, p_100, backUV, 0.60);
            AddFace(p_010, p_110, p_111, p_011, topUV, 1.00);
            AddFace(p_001, p_101, p_100, p_000, botUV, 0.45);
            AddFace(p_010, p_011, p_001, p_000, rightUV, 0.72);
            AddFace(p_111, p_110, p_100, p_101, leftUV, 0.58);
        }

        Vector3D HeadAnim(Vector3D pt) => RotateY(pt, -6.0 * Math.PI / 180.0, new Vector3D(0, 24, 0));

        AddLimbBox(new Vector3D(-4, 24, -4), new Vector3D(8, 8, 8),
            new Rectangle(8, 0, 8, 8), new Rectangle(16, 0, 8, 8),
            new Rectangle(8, 8, 8, 8), new Rectangle(24, 8, 8, 8),
            new Rectangle(16, 8, 8, 8), new Rectangle(0, 8, 8, 8),
            HeadAnim);

        AddLimbBox(new Vector3D(-4.35, 23.65, -4.35), new Vector3D(8.7, 8.7, 8.7),
            new Rectangle(40, 0, 8, 8), new Rectangle(48, 0, 8, 8),
            new Rectangle(40, 8, 8, 8), new Rectangle(56, 8, 8, 8),
            new Rectangle(48, 8, 8, 8), new Rectangle(32, 8, 8, 8),
            HeadAnim, isOuter: true);

        AddLimbBox(new Vector3D(-4, 12, -2), new Vector3D(8, 12, 4),
            new Rectangle(20, 16, 8, 4), new Rectangle(28, 16, 8, 4),
            new Rectangle(20, 20, 8, 12), new Rectangle(32, 20, 8, 12),
            new Rectangle(28, 20, 4, 12), new Rectangle(16, 20, 4, 12));

        AddLimbBox(new Vector3D(-4.25, 11.75, -2.25), new Vector3D(8.5, 12.5, 4.5),
            new Rectangle(20, 32, 8, 4), new Rectangle(28, 32, 8, 4),
            new Rectangle(20, 36, 8, 12), new Rectangle(32, 36, 8, 12),
            new Rectangle(28, 36, 4, 12), new Rectangle(16, 36, 4, 12),
            isOuter: true);

        Vector3D RArmPivot = new Vector3D(-4, 24, 0);
        Vector3D RArmAnim(Vector3D pt)
        {
            var p = RotateX(pt, 18.0 * Math.PI / 180.0, RArmPivot);
            return RotateZ(p, 9.0 * Math.PI / 180.0, RArmPivot);
        }

        AddLimbBox(new Vector3D(-4 - armW, 12, -2), new Vector3D(armW, 12, 4),
            new Rectangle(44, 16, armTexW, 4), new Rectangle(48, 16, armTexW, 4),
            new Rectangle(44, 20, armTexW, 12), new Rectangle(52, 20, armTexW, 12),
            new Rectangle(48, 20, 4, 12), new Rectangle(40, 20, 4, 12),
            RArmAnim);

        AddLimbBox(new Vector3D(-4.25 - armW, 11.75, -2.25), new Vector3D(armW + 0.5, 12.5, 4.5),
            new Rectangle(44, 32, armTexW, 4), new Rectangle(48, 32, armTexW, 4),
            new Rectangle(44, 36, armTexW, 12), new Rectangle(52, 36, armTexW, 12),
            new Rectangle(48, 36, 4, 12), new Rectangle(40, 36, 4, 12),
            RArmAnim, isOuter: true);

        Vector3D LArmPivot = new Vector3D(4, 24, 0);
        Vector3D LArmAnim(Vector3D pt)
        {
            var p = RotateX(pt, -14.0 * Math.PI / 180.0, LArmPivot);
            return RotateZ(p, -7.0 * Math.PI / 180.0, LArmPivot);
        }

        AddLimbBox(new Vector3D(4, 12, -2), new Vector3D(armW, 12, 4),
            new Rectangle(36, 48, armTexW, 4), new Rectangle(40, 48, armTexW, 4),
            new Rectangle(36, 52, armTexW, 12), new Rectangle(44, 52, armTexW, 12),
            new Rectangle(40, 52, 4, 12), new Rectangle(32, 52, 4, 12),
            LArmAnim);

        AddLimbBox(new Vector3D(3.75, 11.75, -2.25), new Vector3D(armW + 0.5, 12.5, 4.5),
            new Rectangle(52, 48, armTexW, 4), new Rectangle(56, 48, armTexW, 4),
            new Rectangle(52, 52, armTexW, 12), new Rectangle(60, 52, armTexW, 12),
            new Rectangle(56, 52, 4, 12), new Rectangle(48, 52, 4, 12),
            LArmAnim, isOuter: true);

        Vector3D RLegPivot = new Vector3D(-2, 12, 0);
        Vector3D RLegAnim(Vector3D pt)
        {
            var p = RotateX(pt, 20.0 * Math.PI / 180.0, RLegPivot);
            return RotateY(p, 10.0 * Math.PI / 180.0, RLegPivot);
        }

        AddLimbBox(new Vector3D(-4, 0, -2), new Vector3D(4, 12, 4),
            new Rectangle(4, 16, 4, 4), new Rectangle(8, 16, 4, 4),
            new Rectangle(4, 20, 4, 12), new Rectangle(12, 20, 4, 12),
            new Rectangle(8, 20, 4, 12), new Rectangle(0, 20, 4, 12),
            RLegAnim);

        AddLimbBox(new Vector3D(-4.25, -0.25, -2.25), new Vector3D(4.5, 12.5, 4.5),
            new Rectangle(4, 32, 4, 4), new Rectangle(8, 32, 4, 4),
            new Rectangle(4, 36, 4, 12), new Rectangle(12, 36, 4, 12),
            new Rectangle(8, 36, 4, 12), new Rectangle(0, 36, 4, 12),
            RLegAnim, isOuter: true);

        Vector3D LLegPivot = new Vector3D(2, 12, 0);
        Vector3D LLegAnim(Vector3D pt) => RotateX(pt, -14.0 * Math.PI / 180.0, LLegPivot);

        AddLimbBox(new Vector3D(0, 0, -2), new Vector3D(4, 12, 4),
            new Rectangle(20, 48, 4, 4), new Rectangle(24, 48, 4, 4),
            new Rectangle(20, 52, 4, 12), new Rectangle(28, 52, 4, 12),
            new Rectangle(24, 52, 4, 12), new Rectangle(16, 52, 4, 12),
            LLegAnim);

        AddLimbBox(new Vector3D(-0.25, -0.25, -2.25), new Vector3D(4.5, 12.5, 4.5),
            new Rectangle(4, 48, 4, 4), new Rectangle(8, 48, 4, 4),
            new Rectangle(4, 52, 4, 12), new Rectangle(12, 52, 4, 12),
            new Rectangle(8, 52, 4, 12), new Rectangle(0, 52, 4, 12),
            LLegAnim, isOuter: true);

        quads.Sort((a, b) => a.Depth.CompareTo(b.Depth));

        double scale = 6.2;
        double offsetX = size / 2.0 + 2.0;
        double offsetY = size - 18.0;

        PointF Project(Vector3D v)
        {
            return new PointF(
                (float)(offsetX + v.X * scale),
                (float)(offsetY - v.Y * scale)
            );
        }

        foreach (var quad in quads)
        {
            var p0 = Project(quad.V0);
            var p1 = Project(quad.V1);
            var p2 = Project(quad.V2);
            var p3 = Project(quad.V3);

            float b = (float)Math.Clamp(quad.Brightness, 0.0, 1.0);
            var colorMatrix = new ColorMatrix(new float[][]
            {
                new float[] { b, 0, 0, 0, 0 },
                new float[] { 0, b, 0, 0, 0 },
                new float[] { 0, 0, b, 0, 0 },
                new float[] { 0, 0, 0, 1, 0 },
                new float[] { 0, 0, 0, 0, 1 }
            });

            using var attr = new ImageAttributes();
            attr.SetColorMatrix(colorMatrix);

            g.DrawImage(skin, new PointF[] { p0, p1, p3 },
                new RectangleF(quad.SrcRect.X, quad.SrcRect.Y, quad.SrcRect.Width, quad.SrcRect.Height),
                GraphicsUnit.Pixel, attr);
        }

        return output;
    }

    private static Bitmap PrepareSkin(Bitmap source)
    {
        if (source.Width == 64 && source.Height == 64)
        {
            return new Bitmap(source);
        }

        var converted = new Bitmap(64, 64, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(converted);
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;

        if (source.Width == 64 && source.Height == 32)
        {
            g.DrawImage(source, new Rectangle(0, 0, 64, 32), new Rectangle(0, 0, 64, 32), GraphicsUnit.Pixel);
            g.DrawImage(source, new Rectangle(32, 48, 16, 16), new Rectangle(40, 16, 16, 16), GraphicsUnit.Pixel);
            g.DrawImage(source, new Rectangle(16, 48, 16, 16), new Rectangle(0, 16, 16, 16), GraphicsUnit.Pixel);
        }
        else
        {
            g.DrawImage(source, new Rectangle(0, 0, 64, 64), new Rectangle(0, 0, source.Width, source.Height), GraphicsUnit.Pixel);
        }

        return converted;
    }
}
