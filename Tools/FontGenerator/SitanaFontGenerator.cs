﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

using SitanaFont = Sitana.Framework.Graphics.Font;
using SitanaGlyph = Sitana.Framework.Graphics.Glyph;

namespace FontGenerator
{
    public class SitanaFontGenerator
    {
        Graphics _globalGraphics;
        Pen _pen;
        Brush _brush;
        Font _font;
        bool _kerning;

        public SitanaFontGenerator(Font font, Pen pen, Brush brush, bool kerning)
        {
            _globalGraphics = Graphics.FromImage(new Bitmap(1, 1, PixelFormat.Format32bppArgb));

            _font = font;
            _brush = brush;
            _pen = pen;
            _kerning = kerning;
        }

        public void Generate(List<char> list, int width, int cutOpacity, out SitanaFont font, out Bitmap outBitmap)
        {
            int additional = _pen != null ? (int)_pen.Width : 0;

            Bitmap image = Generate("X", _font, _pen, _brush, additional);

            int capLine = 0;
            int baseLine = image.Height - 1;
            int height = image.Height;

            // Remove unused space from the left.
            while ((capLine < baseLine) && (BitmapIsEmpty(image, capLine, true, 1)))
            {
                capLine++;
            }

            // Remove unused space from the right.
            while ((baseLine > capLine) && (BitmapIsEmpty(image, baseLine, false, cutOpacity)))
            {
                baseLine--;
            }

            int top;
            int left;
            int right;

            image = CropCharacter(image, 0, out left, out top, out right);

            int emptyCut = left / 2;

            image.Dispose();

            Dictionary<char, Bitmap> bitmaps = new Dictionary<char, Bitmap>();
            Dictionary<char, SitanaGlyph> glyphs = new Dictionary<char, SitanaGlyph>();

            int maxHeight = 0;

            foreach (var ch in list)
            {
                Bitmap bitmap;
                SitanaGlyph glyph;

                CreateGlyph(ch, emptyCut, list, out glyph, out bitmap);

                bitmaps.Add(ch, bitmap);
                glyphs.Add(ch, glyph);

                maxHeight = Math.Max(maxHeight, bitmap.Height);
            }

            int margin = Math.Min(16,Math.Max(4, height / 8));
            int lineHeight = maxHeight + margin;

            int posX = margin;
            int posY = margin;

            int imageHeight = margin;
            int imageWidth = 0;

            foreach(var gl in glyphs)
            {
                SitanaGlyph glyph = gl.Value;

                if (posX + glyph.Width + margin >= width)
                {
                    posX = margin;
                    posY += lineHeight;
                }

                imageWidth = Math.Max(imageWidth, posX + glyph.Width + margin);
                imageHeight = posY + lineHeight;

                glyph.X = (short)posX;
                glyph.Y = (short)posY;

                posX += glyph.Width + margin;
            }

            outBitmap = new Bitmap(imageWidth, imageHeight, PixelFormat.Format32bppArgb);

            using(Graphics graphics = Graphics.FromImage(outBitmap))
            {
                graphics.Clear(Color.Transparent);

                foreach (var gl in glyphs)
                {
                    SitanaGlyph glyph = gl.Value;
                    Bitmap bmp = bitmaps[gl.Key];

                    graphics.DrawImage(bmp, glyph.X, glyph.Y, new Rectangle(0,0,glyph.Width, glyph.Height), GraphicsUnit.Pixel);
                }
            }

            font = new SitanaFont();

            font.CapLine = (short)capLine;
            font.BaseLine = (short)baseLine;
            font.Height = (short)height;

            foreach(var glyph in glyphs)
            {
                font.AddGlyph(glyph.Value);
            }

            foreach(var bm in bitmaps)
            {
                bm.Value.Dispose();
            }
        }

        void CreateGlyph(char character, int emptyCut, List<char> list, out SitanaGlyph glyph, out Bitmap bitmap)
        {
            string text = character.ToString();

            int additional = (int)(_pen != null ? _pen.Width : 0);

            bitmap = Generate(text, _font, _pen, _brush, additional);

            int left = 0;
            int top = 0;

            int bmWidth = bitmap.Width;
            int right = bitmap.Width;

            bitmap = CropCharacter(bitmap, emptyCut, out left, out top, out right);

            glyph = new SitanaGlyph();
            glyph.Character = character;
            glyph.Width = (short)bitmap.Width;
            glyph.Height = (short)bitmap.Height;
            glyph.Top = (short)top;

            StringFormat format = StringFormat.GenericDefault;

            //float firstWidth = _globalGraphics.MeasureString(text, _font, 10000, format).Width;

            GraphicsPath path = new GraphicsPath(FillMode.Winding);
            path.AddString(text, _font.FontFamily, (Int32)_font.Style, _font.Size, Point.Empty, StringFormat.GenericDefault);

            float firstWidth = path.GetBounds().Width;

            if (_kerning)
            {
                foreach (var ch in list)
                {
                    path.Reset();
                    path.AddString(ch.ToString(), _font.FontFamily, (Int32)_font.Style, _font.Size, Point.Empty, StringFormat.GenericDefault);

                    float secondWidth = path.GetBounds().Width;

                    path.Reset();
                    path.AddString(String.Format("{0}{1}", ch, character), _font.FontFamily, (Int32)_font.Style, _font.Size, Point.Empty, StringFormat.GenericDefault);

                    float bothWidth = path.GetBounds().Width;

                    //float secondWidth = _globalGraphics.MeasureString(ch.ToString(), _font, 10000, format).Width;
                    //float bothWidth = _globalGraphics.MeasureString(String.Format("{0}{1}", ch, character), _font, 10000, format).Width;

                    float diff = (float)Math.Ceiling(bothWidth - (secondWidth + firstWidth)) - _font.Height / 20;

                    if (ch != 32)
                    {
                        glyph.AddKerning(ch, (short)(diff * 10));
                    }
                }
            }
        }

        Bitmap Generate(string text, Font font, Pen pen, Brush brush, int additional)
        {
            SizeF size = _globalGraphics.MeasureString(text, font);

            int width = (int)Math.Ceiling(size.Width + additional * 2);
            int height = (int)Math.Ceiling(size.Height + additional * 2);

            Bitmap image = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            using (Graphics graphics = Graphics.FromImage(image))
            {
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;

                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                graphics.Clear(Color.Transparent);

                using (StringFormat format = new StringFormat())
                using (GraphicsPath path = new GraphicsPath(FillMode.Winding))
                {
                    path.AddString(text, font.FontFamily, (Int32)font.Style, font.Size, new PointF(additional, additional), StringFormat.GenericDefault);

                    if (pen != null)
                    {
                        graphics.DrawPath(pen, path);
                    }

                    if (brush != null)
                    {
                        graphics.FillPath(brush, path);
                    }
                }
            }

            return image;
        }

        Bitmap CropCharacter(Bitmap bitmap, int emptyCut, out int cropLeft, out int cropTop, out int cropRight)
        {
            cropLeft = 0;
            cropRight = bitmap.Width - 1;

            // Remove unused space from the left.
            while ((cropLeft < cropRight) && (BitmapIsEmpty(bitmap, cropLeft, true)))
            {
                cropLeft++;
            }

            // Remove unused space from the right.
            while ((cropRight > cropLeft) && (BitmapIsEmpty(bitmap, cropRight, true)))
            {
                cropRight--;
            }

            cropTop = 0;
            int cropBottom = bitmap.Height - 1;

            while (cropTop < cropBottom && BitmapIsEmpty(bitmap, cropTop, false))
            {
                cropTop++;
            }

            while (cropBottom > cropTop && BitmapIsEmpty(bitmap, cropBottom, false))
            {
                cropBottom--;
            }

            if (cropLeft == cropRight)
            {
                cropLeft = emptyCut;
                cropRight = bitmap.Width - 1 - emptyCut;
            }

            // Add some padding back in.
            cropLeft = Math.Max(cropLeft, 0);
            cropRight = Math.Min(cropRight, bitmap.Width - 1);

            Int32 width = cropRight - cropLeft + 1;
            Int32 height = cropBottom - cropTop + 2;

            // Crop the glyph.
            Bitmap croppedBitmap = new Bitmap(width, height, bitmap.PixelFormat);

            using (Graphics graphics = Graphics.FromImage(croppedBitmap))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;

                graphics.DrawImage(bitmap, 0, 0,
                                   new Rectangle(cropLeft, cropTop, width, height),
                                   GraphicsUnit.Pixel);

                graphics.Flush();
            }

            bitmap.Dispose();

            return croppedBitmap;
        }

        bool BitmapIsEmpty(Bitmap bitmap, Int32 coord, bool vertical, int cutOpacity = 1)
        {
            int compare = cutOpacity;
            int length = vertical ? bitmap.Height : bitmap.Width;

            for (Int32 pos = 0; pos < length; pos++)
            {
                if (vertical)
                {
                    if (bitmap.GetPixel(coord, pos).A >= compare)
                    {
                        return false;
                    }
                }
                else
                {
                    if (bitmap.GetPixel(pos, coord).A >= compare)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
