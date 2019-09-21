using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace ModCore
{
    public class CharTile
    {
        public readonly Texture2D tex;

        public readonly Rectangle rect;

        public CharTile(Texture2D tex, Rectangle rect)
        {
            this.tex = tex;
            this.rect = rect;
        }
    }

    public class SpriteFontX
    {
        private Font font;

        ///<summary>
        ///间距
        ///</summary>
        public Vector2 Spacing;

        private TextRenderingHint textRenderingHint;

        public Dictionary<char, CharTile> CharTiles;

        protected List<Texture2D> Tex2ds;

        protected Texture2D CurrentTex2d;

        protected int CurrentTop;

        protected int CurrentLeft;

        protected int CurrentMaxHeight;

        protected SizeF sizef;

        protected Bitmap bitmap;

        protected Graphics gr;

        protected IGraphicsDeviceService GDS;

        private static MemoryStream strm;

        private static Bitmap tempBp;

        private static Graphics tempGr;

        private static Brush brush;

        public Font Font => font;

        public TextRenderingHint TextRenderingHint => textRenderingHint;

        ///<summary>
        ///新建SpriteFontX..
        ///</summary>
        ///<param name = "font"> 字体 </param>
        ///<param name = "gds">建纹理时用到的IGraphicsDeviceService</param>
        ///<param name = "trh"> 指定文本呈现的质量 </param>
        public SpriteFontX(Font font, IGraphicsDeviceService gds, TextRenderingHint trh)
        {
            Initialize(font, gds, trh);
        }


        ///<summary>
        ///新建SpriteFontX..
        ///</summary>
        ///<param name = "fontName">字体名字</param>
        ///<param name = "size">字体大小</param>
        ///<param name = "gds">建纹理时用到的IGraphicsDeviceService</param>
        ///<param name = "trh"> 指定文本呈现的质量 </param>
        public SpriteFontX(string fontName, float size, IGraphicsDeviceService gds, TextRenderingHint trh)
        {
            Initialize(new Font(fontName, size), gds, trh);
        }

        private void Initialize(Font font, IGraphicsDeviceService gds, TextRenderingHint trh)
        {
            this.font = font;
            GDS = gds;
            textRenderingHint = trh;
            if (brush == null)
            {
                brush = Brushes.White;
                tempBp = new Bitmap(1, 1);
                tempGr = Graphics.FromImage(tempBp);
                strm = new MemoryStream();
            }
            CharTiles = new Dictionary<char, CharTile>();
            Tex2ds = new List<Texture2D>();
            newTex();
        }

        protected void newTex()
        {
            CurrentTex2d = new Texture2D(GDS.GraphicsDevice, 256, 256);
            Tex2ds.Add(CurrentTex2d);
            CurrentTop = 0;
            CurrentLeft = 0;
            CurrentMaxHeight = 0;
        }

        protected unsafe void addTex(char chr)
        {
            if (CharTiles.ContainsKey(chr))
                return;
            string text = chr.ToString();
            sizef = tempGr.MeasureString(text, Font, PointF.Empty, StringFormat.GenericTypographic);
            if (sizef.Width <= 0f)
                sizef.Width = sizef.Height / 2f;
            if (bitmap == null || (int)Math.Ceiling(sizef.Width) != bitmap.Width || (int)Math.Ceiling(sizef.Height) != bitmap.Height)
            {
                bitmap = new Bitmap((int)Math.Ceiling(sizef.Width), (int)Math.Ceiling(sizef.Height), PixelFormat.Format32bppArgb);
                gr = Graphics.FromImage(bitmap);
            }
            else
                gr.Clear(System.Drawing.Color.Empty);
            gr.TextRenderingHint = textRenderingHint;
            gr.DrawString(text, Font, brush, 0f, 0f, StringFormat.GenericTypographic);
            if (bitmap.Height > CurrentMaxHeight)
                CurrentMaxHeight = bitmap.Height;
            if (CurrentLeft + bitmap.Width + 1 > CurrentTex2d.Width)
            {
                CurrentTop += CurrentMaxHeight + 1;
                CurrentLeft = 0;
            }
            if (CurrentTop + CurrentMaxHeight > CurrentTex2d.Height)
                newTex();
            CharTile charTile = new CharTile(CurrentTex2d, new Microsoft.Xna.Framework.Rectangle(CurrentLeft, CurrentTop, bitmap.Width, bitmap.Height));
            CharTiles.Add(chr, charTile);
            int[] array = new int[bitmap.Width * bitmap.Height];
            BitmapData bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            int* ptr = (int*)(void*)bitmapData.Scan0;
            for (int i = 0; i < array.Length; i++)
            {
                if (*ptr != 0)
                    array[i] = *ptr;
                ptr++;
            }
            bitmap.UnlockBits(bitmapData);
            GDS.GraphicsDevice.Textures[0] = null;
            CurrentTex2d.SetData(0, charTile.rect, array, 0, array.Length);
            CurrentLeft += charTile.rect.Width + 1;
        }

        public void AddText(string str)
        {
            AddText(str.ToCharArray());
        }

        public void AddText(char[] chrs)
        {
            for (int i = 0; i < chrs.Length; i++)
            {
                addTex(chrs[i]);
            }
        }

        public Vector2 Draw(SpriteBatch sb, string str, Vector2 position, Microsoft.Xna.Framework.Color color)
        {
            return Draw(sb, str.ToCharArray(), position, new Vector2(float.MaxValue, float.MaxValue), Vector2.One, color);
        }

        public Vector2 Draw(SpriteBatch sb, char[] str, Vector2 position, Microsoft.Xna.Framework.Color color)
        {
            return Draw(sb, str, position, new Vector2(float.MaxValue, float.MaxValue), Vector2.One, color);
        }

        public Vector2 Draw(SpriteBatch sb, string str, Vector2 position, Vector2 maxBound, Vector2 scale, Microsoft.Xna.Framework.Color color)
        {
            return Draw(sb, str.ToCharArray(), position, maxBound, scale, color);
        }

        public Vector2 Draw(SpriteBatch sb, char[] str, Vector2 position, Vector2 maxBound, Vector2 scale, Microsoft.Xna.Framework.Color color)
        {
            if (maxBound.X == 0f)
                maxBound.X = float.MaxValue;
            else
                maxBound.X += position.X;
            if (maxBound.Y == 0f)
                maxBound.Y = float.MaxValue;
            else
                maxBound.Y += position.Y;
            Vector2 vector = position;
            float num = 0f;
            float num2 = 0f;
            foreach (char c in str)
            {
                addTex(c);
                CharTile charTile = CharTiles[c];
                if (c == '\r' || vector.X + (float)charTile.rect.Width * scale.X > maxBound.X)
                {
                    if (vector.X > num2)
                        num2 = vector.X;
                    vector.X = position.X;
                    vector.Y += num * scale.Y + Spacing.Y * scale.X;
                    num = 0f;
                }
                else
                {
                    if (c == '\n')
                        continue;
                    if ((float)charTile.rect.Height > num)
                    {
                        num = charTile.rect.Height;
                        if (vector.Y + num * scale.Y > maxBound.Y)
                            break;
                    }
                    sb?.Draw(charTile.tex, vector, charTile.rect, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                    vector.X += (float)charTile.rect.Width * scale.X + Spacing.X * scale.X;
                }
            }
            if (vector.X > num2)
                num2 = vector.X;
            vector.X = num2 - Spacing.X * scale.X;
            vector.Y += num * scale.Y;
            return vector - position;
        }

        public Vector2 Draw(SpriteBatch sb, string str, Vector2 position, Microsoft.Xna.Framework.Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth)
        {
            Vector2 maxBound = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 vector = position;
            float num = 0f;
            float num2 = 0f;
            foreach (char c in str)
            {
                addTex(c);
                CharTile charTile = CharTiles[c];
                if (c == '\r' || vector.X + (float)charTile.rect.Width * scale.X > maxBound.X)
                {
                    if (vector.X > num2)
                        num2 = vector.X;
                    vector.X = position.X;
                    vector.Y += num * scale.Y + Spacing.Y * scale.X;
                    num = 0f;
                }
                else
                {
                    if (c == '\n')
                        continue;
                    if ((float)charTile.rect.Height > num)
                    {
                        num = charTile.rect.Height;
                        if (vector.Y + num * scale.Y > maxBound.Y)
                            break;
                    }
                    sb?.Draw(charTile.tex, vector, charTile.rect, color, rotation, origin, scale, effects, layerDepth);
                    vector.X += (float)charTile.rect.Width * scale.X + Spacing.X * scale.X;
                }
            }
            if (vector.X > num2)
                num2 = vector.X;
            vector.X = num2 - Spacing.X * scale.X;
            vector.Y += num * scale.Y;
            return vector - position;
        }


        public Vector2 MeasureString(string str)
        {
            return MeasureString(str.ToCharArray());
        }

        public Vector2 MeasureString(char[] str)
        {
            return Draw(null, str, Vector2.Zero, Microsoft.Xna.Framework.Color.White);
        }

        public Vector2 MeasureString(string str, Vector2 maxBound, Vector2 scale)
        {
            return Draw(null, str, Vector2.Zero, maxBound, scale, Microsoft.Xna.Framework.Color.White);
        }

        public Vector2 MeasureString(char[] str, Vector2 maxBound, Vector2 scale)
        {
            return Draw(null, str, Vector2.Zero, maxBound, scale, Microsoft.Xna.Framework.Color.White);
        }
    }

    public static class _DrawString
    {
        public static Vector2 DrawStringX(this SpriteBatch sb, SpriteFontX sfx, string str, Vector2 position, Color color)
        {
            return sfx.Draw(sb, str, position, color);
        }

        public static Vector2 DrawStringX(this SpriteBatch sb, SpriteFontX sfx, char[] str, Vector2 position, Color color)
        {
            return sfx.Draw(sb, str, position, color);
        }

        public static Vector2 DrawStringX(this SpriteBatch sb, SpriteFontX sfx, string str, Vector2 position, Vector2 maxBound, Vector2 scale, Color color)
        {
            return sfx.Draw(sb, str, position, maxBound, scale, color);
        }

        public static Vector2 DrawStringX(this SpriteBatch sb, SpriteFontX sfx, char[] str, Vector2 position, Vector2 maxBound, Vector2 scale, Color color)
        {
            return sfx.Draw(sb, str, position, maxBound, scale, color);
        }
    }

}
