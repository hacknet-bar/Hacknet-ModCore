using HacknetModManager;
using Harmony12;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hacknet;
using Hacknet.Gui;
using Hacknet.Localization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.Drawing;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using System.IO;
using Newtonsoft.Json;

namespace ModCore
{

    public class Settings : ModManager.ModSettings
    {
        public int CurFont = -1;
        public List<string> FontList = new List<string>();

        public override void Save(ModManager.ModEntry modEntry) => Save(this, modEntry);
    }

    public static class Main
    {
        //public static float MachineEpsilonFloat = GetMachineEpsilonFloat();
        public static Settings settings;

        public static HarmonyInstance Harmony = null;
        public static ModManager.ModEntry.ModLogger Logger;
        public static Assembly GameAssembly;
        public static MethodInfo PushSprite = typeof(SpriteBatch).GetMethod("PushSprite", BindingFlags.Instance | BindingFlags.NonPublic);
        public static SpriteFontX CurSpriteFont10 = null;
        public static SpriteFontX CurSpriteFont12 = null;
        public static SpriteFontX CurSpriteFont23 = null;
        public static SpriteFontX CurSpriteFont7 = null;

        public static void DrawMenu(ModManager.ModEntry modEntry, object[] param)
        {
            if (param == null || param.Length <= 1)
                return;
            if (param.Length == 2 && !((param[1] is Vector2) || (param[1] is Vector4)))
                return;
            if (!(param[0] is ScreenManager))
                return;
            ScreenManager screenManager = (ScreenManager)param[0];
            Vector4 ranged;
            if (param[1] is Vector2)
                ranged = new Vector4((Vector2)param[1], screenManager.GraphicsDevice.Viewport.Width - ((Vector2)param[1]).X - 55, screenManager.GraphicsDevice.Viewport.Height - ((Vector2)param[1]).Y - 55);
            else if (param[1] is Vector4)
                ranged = (Vector4)param[1];
            else if ((param[1] is int) && param.Length >= 3 && param.Length != 4)
            {
                int x = (int)param[1];
                int y = (int)param[2];

                int w = screenManager.GraphicsDevice.Viewport.Width - x - 55;
                int h = screenManager.GraphicsDevice.Viewport.Height - y - 55;

                if (param.Length == 5)
                {
                    w = (int)param[3];
                    h = (int)param[4];
                }

                ranged = new Vector4(x, y, w, h);
            }
            else
                return;

            TextItem.doMeasuredSmallLabel(new Vector2(ranged.X + 25, ranged.Y + 25), "ModsCore.Settings.FontName".Translate(), Color.White);
            settings.CurFont = SelectableTextList.doFancyList((modEntry.Info.Id + "FontName").GetHashCode(), (int)ranged.X, (int)ranged.Y + 50, (int)ranged.Z - 50, (int)ranged.W - 50, settings.FontList.ToArray(), settings.CurFont, Color.White);
        }

        private static void SaveMethod(ModManager.ModEntry modEntry,object[] param) => settings.Save(modEntry);

        public static bool Load(ModManager.ModEntry modEntry)
        {
            try
            {
                settings = Settings.Load<Settings>(modEntry);
            }
            catch
            {
                settings = new Settings();
            }
            if(settings.FontList.Count == 0)
            {
                settings.FontList.Add("微软雅黑");
                settings.FontList.Add("宋体");
                settings.FontList.Add("幼圆");
                settings.FontList.Sort();
                settings.CurFont = settings.FontList.IndexOf("微软雅黑");
            }
            if(settings.CurFont < 0 || settings.CurFont > settings.FontList.Count - 1)
                settings.CurFont = settings.FontList.IndexOf("微软雅黑");

            modEntry.SaveMethod = SaveMethod;
            modEntry.DrawSettingUIMethod = DrawMenu;
            Harmony = HarmonyInstance.Create(modEntry.Info.Id);
            Logger = modEntry.Logger;
            GameAssembly = Assembly.GetCallingAssembly();

            //Patch MainMenu.drawMainMenuButtons
            try
            {
                var original = GameAssembly.GetType("Hacknet.MainMenu").GetMethod("drawMainMenuButtons", BindingFlags.NonPublic | BindingFlags.Instance);
                var postfix = typeof(Main).GetMethod("drawMainMenuButtonsPostfix");
                Harmony.Patch(original, null, new HarmonyMethod(postfix));
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }

            //Patch SpriteBatch.DrawString
            try
            {
                var original = typeof(SpriteBatch).GetMethod("DrawString", BindingFlags.Public | BindingFlags.Instance, null, new Type[]{
                    typeof(SpriteFont),
                    typeof(string),
                    typeof(Vector2),
                    typeof(Microsoft.Xna.Framework.Color),
                    typeof(float),
                    typeof(Vector2),
                    typeof(Vector2),
                    typeof(SpriteEffects),
                    typeof(float)
                }, new ParameterModifier[0]);
                var prefixCover = typeof(Main).GetMethod("DrawStringPrefixCover");
                Harmony.Patch(original, new HarmonyMethod(prefixCover), null);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }

            //Patch Game..ctor
            try
            {
                var original = typeof(Game1).GetConstructor(new Type[] { });
                var postfix = typeof(Main).GetMethod("Game1InstantiatePostfix");
                Harmony.Patch(original, null, new HarmonyMethod(postfix));
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }

            Harmony.PatchAll();
            Logger.Log("LoadingComplete".Translate());
            return true;
        }

        public static void drawMainMenuButtonsPostfix(GameScreen __instance)
        {
            if (Button.doButton(999, 10, 10, 220, 30, "Mods", Color.Gray))
                __instance.ScreenManager.AddScreen(new ModsMenu(), __instance.ScreenManager.controllingPlayer);
        }
        public static void Game1InstantiatePostfix(Game __instance)
        {
            Font font;
            if(CurSpriteFont10 == null)
            {
                font = new Font(settings.FontList[settings.CurFont], 10, FontStyle.Bold);
                CurSpriteFont10 = new SpriteFontX(font, (IGraphicsDeviceService)__instance.Content.ServiceProvider.GetService(typeof(IGraphicsDeviceService)), System.Drawing.Text.TextRenderingHint.SystemDefault);
            }
            if (CurSpriteFont12 == null)
            {
                font = new Font(settings.FontList[settings.CurFont], 12, FontStyle.Bold);
                CurSpriteFont12 = new SpriteFontX(font, (IGraphicsDeviceService)__instance.Content.ServiceProvider.GetService(typeof(IGraphicsDeviceService)), System.Drawing.Text.TextRenderingHint.SystemDefault);
            }
            if (CurSpriteFont23 == null)
            {
                font = new Font(settings.FontList[settings.CurFont], 23, FontStyle.Bold);
                CurSpriteFont23 = new SpriteFontX(font, (IGraphicsDeviceService)__instance.Content.ServiceProvider.GetService(typeof(IGraphicsDeviceService)), System.Drawing.Text.TextRenderingHint.SystemDefault);
            }
            if (CurSpriteFont7 == null)
            {
                font = new Font(settings.FontList[settings.CurFont], 7, FontStyle.Bold);
                CurSpriteFont7 = new SpriteFontX(font, (IGraphicsDeviceService)__instance.Content.ServiceProvider.GetService(typeof(IGraphicsDeviceService)), System.Drawing.Text.TextRenderingHint.SystemDefault);
            }
        }

        public static GameScreen ShowMessageBox(string message,
            string AcceptedText = "", string CancelText = "",
            Action AcceptedAction = null, Action CancelAction = null)
        {
            var MessageBoxScreen = Main.GameAssembly.GetType("Hacknet.MessageBoxScreen");
            GameScreen MessageBox = (GameScreen)MessageBoxScreen.GetConstructor(new Type[] { typeof(string) }).Invoke(new object[] { message });

            BindingFlags BindingFlagsAll = BindingFlags.Public | BindingFlags.Instance;

            FieldInfo fieldInfo = MessageBoxScreen.GetField("CancelClicked", BindingFlagsAll);
            fieldInfo.SetValue(MessageBox, CancelAction);
            fieldInfo = MessageBoxScreen.GetField("OverrideCancelText", BindingFlagsAll);
            fieldInfo.SetValue(MessageBox,CancelText);

            fieldInfo = MessageBoxScreen.GetField("AcceptedClicked", BindingFlagsAll);
            fieldInfo.SetValue(MessageBox,AcceptedAction);
            fieldInfo = MessageBoxScreen.GetField("OverrideAcceptedText", BindingFlagsAll);
            fieldInfo.SetValue(MessageBox,AcceptedText);

            return MessageBox;
        }

        /// <summary>
        /// 判断一个字符是中文还是英文
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static bool IsChinese(char c)
        {
            //通过字节码进行判断
            return c >= 0x4E00 && c <= 0x29FA5;
        }

        public static bool DrawStringPrefixCover( SpriteFont spriteFont, string text, Vector2 position, Microsoft.Xna.Framework.Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects, float layerDepth)
        {
            if (text == null && text.Length == 0)
                return true;

            bool hasChineseWord = false;
            foreach(char i in text)
                if(IsChinese(i))
                {
                    hasChineseWord = true;
                    break;
                }
            if (!hasChineseWord)
                return true;

            if (spriteFont.GetHashCode() == GuiData.tinyfont.GetHashCode())
                CurSpriteFont10.Draw(GuiData.spriteBatch, text, position, color, rotation, origin, scale, effects, layerDepth);
            else if (spriteFont.GetHashCode() == GuiData.smallfont.GetHashCode())
                CurSpriteFont12.Draw(GuiData.spriteBatch, text, position, color, rotation, origin, scale, effects, layerDepth);
            else if (spriteFont.GetHashCode() == GuiData.font.GetHashCode())
                CurSpriteFont23.Draw(GuiData.spriteBatch, text, position, color, rotation, origin, scale, effects, layerDepth);
            else if (spriteFont.GetHashCode() == GuiData.detailfont.GetHashCode())
                CurSpriteFont7.Draw(GuiData.spriteBatch, text, position, color, rotation, origin, scale, effects, layerDepth);
            else
                return true;
            return false;
            //    //effects &= (SpriteEffects.FlipHorizontally | SpriteEffects.FlipVertically);
            //    //Vector2 VecOfText = Vector2.Zero;
            //    //bool newLines = true;

            //    //List<char> characterMap = (List<char>)typeof(SpriteFont).GetField("characterMap", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(spriteFont);
            //    //List<Vector3> kerning = (List<Vector3>)typeof(SpriteFont).GetField("kerning", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(spriteFont);
            //    //List<Rectangle> croppingData = (List<Rectangle>)typeof(SpriteFont).GetField("croppingData", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(spriteFont);
            //    //List<Rectangle> glyphData = (List<Rectangle>)typeof(SpriteFont).GetField("glyphData", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(spriteFont);
            //    //Texture2D textureValue = (Texture2D)typeof(SpriteFont).GetField("textureValue", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(spriteFont);

            //    //if (effects != 0)
            //    //{
            //    //    Vector2 measureString = spriteFont.MeasureString(text);
            //    //    origin.X -= measureString.X * axisIsMirroredX[(int)effects];
            //    //    origin.Y -= measureString.Y * axisIsMirroredY[(int)effects];
            //    //}

            //    //int IndexOfChar;
            //    //foreach (char c in text)
            //    //{
            //    //    switch (c)
            //    //    {
            //    //        case '\n':
            //    //            VecOfText.X = 0f;
            //    //            VecOfText.Y += spriteFont.LineSpacing;
            //    //            newLines = true;
            //    //            continue;
            //    //        case '\r':
            //    //            continue;
            //    //    }

            //    //    IndexOfChar = characterMap.IndexOf(c);
            //    //    if (IndexOfChar == -1)
            //    //    {
            //    //        if (!spriteFont.DefaultCharacter.HasValue)
            //    //            throw new ArgumentException("Text contains characters that cannot be resolved by this SpriteFont.", "text");
            //    //        IndexOfChar = characterMap.IndexOf(spriteFont.DefaultCharacter.Value);
            //    //    }
            //    //    if (newLines)
            //    //    {
            //    //        VecOfText.X += Math.Abs(kerning[IndexOfChar].X);
            //    //        newLines = false;
            //    //    }
            //    //    else
            //    //        VecOfText.X += spriteFont.Spacing + kerning[IndexOfChar].X;

            //    //    float num2 = origin.X + (VecOfText.X + (float)croppingData[IndexOfChar].X) * axisDirectionX[(int)effects];
            //    //    float num3 = origin.Y + (VecOfText.Y + (float)croppingData[IndexOfChar].Y) * axisDirectionY[(int)effects];
            //    //    if (effects != 0)
            //    //    {
            //    //        num2 += (float)glyphData[IndexOfChar].Width * axisIsMirroredX[(int)effects];
            //    //        num3 += (float)glyphData[IndexOfChar].Height * axisIsMirroredY[(int)effects];
            //    //    }
            //    //    float sourceW = Math.Max(glyphData[IndexOfChar].Width, MachineEpsilonFloat) / (float)textureValue.Width;
            //    //    float sourceH = Math.Max(glyphData[IndexOfChar].Height, MachineEpsilonFloat) / (float)textureValue.Height;


            //    //    //PushSprite.Invoke(GuiData.spriteBatch, new object[] {
            //    //    //    textureValue,                                           //texture
            //    //    //    (float)glyphData[IndexOfChar].X / textureValue.Width,   //sourceX
            //    //    //    (float)glyphData[IndexOfChar].Y / textureValue.Height,  //sourceY
            //    //    //    sourceW,                                                //sourceW
            //    //    //    sourceH,                                                //sourceH
            //    //    //    position.X,                                             //destinationX
            //    //    //    position.Y,                                             //destinationY
            //    //    //    (float)glyphData[IndexOfChar].Width * scale.X,          //destinationW
            //    //    //    (float)glyphData[IndexOfChar].Height * scale.Y,         //destinationH
            //    //    //    color,
            //    //    //    num2 / sourceW / textureValue.Width,                    //originX
            //    //    //    num3 / sourceH / textureValue.Height,                   //originY
            //    //    //    (float)Math.Sin(rotation),                              //rotationSin
            //    //    //    (float)Math.Cos(rotation),                              //rotationCos
            //    //    //    layerDepth,                                             //depth
            //    //    //    (byte)effects                                           //effects
            //    //    //});



            //    //    VecOfText.X += kerning[IndexOfChar].Y + kerning[IndexOfChar].Z;
            //    //}
        }
}

    public class ModsMenu : GameScreen
    {
        private static Dictionary<int, ModManager.ModEntry> m_ActDicModEntries = null;
        private static Dictionary<int, ModManager.ModEntry> m_DeactDicModModEntries = null;
        private static FieldInfo ModsConfig_FieldInfo = null;
        private static GameScreen MessageBox_NeedSave = null;
        private static GameScreen MessageBox_NeedClosedGame = null;
        private static bool IsChange = false;
        private static bool Saved = true;
        private static List<string> m_ActivatedMods = null;
        private static List<string> m_DeactivatedMods = null;

        private bool OpenSettingUI = false;
        private int currentActMods = -1;
        private int currentDeactMods = -1;

        public ModsMenu()
        {
            //初始化 m_ActivatedMods 与 dicModEntries
            if (m_ActivatedMods == null || m_ActDicModEntries == null)
            {
                m_ActivatedMods = new List<string>();
                m_ActDicModEntries = new Dictionary<int, ModManager.ModEntry>();
                foreach (var i in ModManager.EnabledModEntries)
                {
                    m_ActivatedMods.Add($"{i.Info.DisplayName} (Id:{i.Info.Id})");
                    m_ActDicModEntries.Add($"{i.Info.DisplayName} (Id:{i.Info.Id})".GetHashCode(), i);
                }
            }

            //初始化 MessageBox_NeedSave
            if (MessageBox_NeedSave == null)
            {
                MessageBox_NeedSave = Main.ShowMessageBox("ModsCore.Mods.EscMes".Translate(),
                    "ModsCore.Mods.Save".Translate(), "ModsCore.Mods.Cancel".Translate(),
                    Save);
            }

            //初始化 MessageBox_ClosedGame
            if (MessageBox_NeedClosedGame == null)
            {
                MessageBox_NeedClosedGame = Main.ShowMessageBox("ModsCore.Mods.ClosedGame".Translate(),
                    "ModsCore.Mods.Closed".Translate(), "ModsCore.Mods.Cancel".Translate(), delegate {
                        MusicManager.stop();
                        Game1.threadsExiting = true;
                        Game1.getSingleton().Exit();
                    });
            }

            //初始化 ModsConfig_FieldInfo
            if (ModsConfig_FieldInfo == null)
                ModsConfig_FieldInfo = typeof(ModManager).GetField("m_ModsConfig", BindingFlags.NonPublic | BindingFlags.Static);
            
            //初始化 m_DeactivatedMods
            try
            {
                if (m_DeactivatedMods == null || m_DeactDicModModEntries == null)
                {
                    ModManager.ModsConfig ModsConfig = (ModManager.ModsConfig)ModsConfig_FieldInfo.GetValue(new ModManager());
                    m_DeactivatedMods = new List<string>();
                    m_DeactDicModModEntries = new Dictionary<int, ModManager.ModEntry>();

                    DirectoryInfo directoryInfo = new DirectoryInfo(ModManager.ModsPath);
                    ModManager.ModInfo modInfo;
                    foreach (var i in directoryInfo.GetDirectories())
                    {
                        foreach (var path in ModsConfig.EnabledMod)
                            if (path == i.Name)
                                goto Continue_End;

                        modInfo = JsonConvert.DeserializeObject<ModManager.ModInfo>(File.ReadAllText(Path.Combine(i.FullName, "Info.json")));
                        if (string.IsNullOrEmpty(modInfo.Id))
                            goto Continue_End;

                        m_DeactivatedMods.Add($"{modInfo.DisplayName} (Id:{modInfo.Id})");
                        m_DeactDicModModEntries.Add($"{modInfo.DisplayName} (Id:{modInfo.Id})".GetHashCode(), new ModManager.ModEntry(modInfo, i.FullName));

                    Continue_End:;
                    }
                }
            }
            catch(Exception ex)
            {
                Main.Logger.Error(ex.ToString());
            }
        }

        public void Save()
        {
            IsChange = false;
            Saved = true;

            ModManager.ModsConfig ModsConfig = (ModManager.ModsConfig)ModsConfig_FieldInfo.GetValue(new ModManager());
            ModsConfig.Save();
            foreach(var i in ModManager.EnabledModEntries)
                if (i.SaveMethod != null)
                    i.SaveMethod(i,null);
            ScreenManager.AddScreen(MessageBox_NeedClosedGame, ScreenManager.controllingPlayer);
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }

        public override void HandleInput(InputState input)
        {
            base.HandleInput(input);
            GuiData.doInput(input);
        }

        private void MoveUp()
        {
            if(currentActMods > 0)
            {
                m_ActivatedMods.Insert(currentActMods - 1, m_ActivatedMods[currentActMods]);
                m_ActivatedMods.RemoveAt(currentActMods + 1);

                ModManager.ModsConfig ModsConfig = (ModManager.ModsConfig)ModsConfig_FieldInfo.GetValue(new ModManager());
                ModsConfig.EnabledMod.Insert(currentActMods - 1, ModsConfig.EnabledMod[currentActMods]);
                ModsConfig.EnabledMod.RemoveAt(currentActMods + 1);
                ModsConfig_FieldInfo.SetValue(null, ModsConfig);
                currentActMods--;
            }
        }

        private void MoveDown()
        {
            if (currentActMods != -1 && currentActMods != m_ActivatedMods.Count - 1)
            {
                m_ActivatedMods.Insert(currentActMods + 1, m_ActivatedMods[currentActMods]);
                m_ActivatedMods.RemoveAt(currentActMods);

                ModManager.ModsConfig ModsConfig = (ModManager.ModsConfig)ModsConfig_FieldInfo.GetValue(new ModManager());
                ModsConfig.EnabledMod.Insert(currentActMods + 1, ModsConfig.EnabledMod[currentActMods]);
                ModsConfig.EnabledMod.RemoveAt(currentActMods);
                ModsConfig_FieldInfo.SetValue(null, ModsConfig);
                currentActMods++;
            }
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            PostProcessor.begin();
            ScreenManager.FadeBackBufferToBlack(255);
            GuiData.startDraw();
            PatternDrawer.draw(new Rectangle(0, 0, ScreenManager.GraphicsDevice.Viewport.Width, ScreenManager.GraphicsDevice.Viewport.Height), 0.5f, Color.Black, new Color(2, 2, 2), GuiData.spriteBatch);
            if (Button.doButton("Back".GetHashCode(), 10, 10, 220, 30, "<- " + LocaleTerms.Loc("Back"), Color.Gray))
            {
                if(IsChange && !Saved)
                    ScreenManager.AddScreen(MessageBox_NeedSave, ScreenManager.controllingPlayer);
                SettingsLoader.writeStatusFile();
                ExitScreen();
            }
            if (Button.doButton("Save".GetHashCode(), 10, 45, 220, 30, "ModsCore.Mods.Save".Translate(), Color.Blue))
            {
                Save();
            }

            TextItem.doLabel(new Vector2(75, 100), $"Mods ({"ModsCore.Mods.Activate".Translate()})", null, 300);

            //打开了Setting界面不允许修改选定的Mod
            if (OpenSettingUI)
                SelectableTextList.doFancyList(25, 50, 140, 300, ScreenManager.GraphicsDevice.Viewport.Height - 190, m_ActivatedMods.ToArray(), currentActMods, Color.White);
            else
                currentActMods = SelectableTextList.doFancyList(25, 50, 140, 300, ScreenManager.GraphicsDevice.Viewport.Height - 50, m_ActivatedMods.ToArray(), currentActMods, Color.White);

            if (OpenSettingUI && Button.doButton("CloseSettingUI".GetHashCode(), 400, 210, 100, 30, "ModsCore.Mods.CloseSettingUI".Translate(), Color.BlueViolet))
                OpenSettingUI = false;
            if (currentActMods > 0 && Button.doButton("Up".GetHashCode(), 400, 250, 100, 30, "ModsCore.Mods.Up".Translate(), Color.BlueViolet))
            {
                MoveUp();
                IsChange = true;
                Saved = false;
            }
            if (currentActMods >= 0 && currentActMods != m_ActivatedMods.Count - 1 && Button.doButton("Down".GetHashCode(), 400, 290, 100, 30, "ModsCore.Mods.Down".Translate(), Color.BlueViolet))
            {
                MoveDown();
                IsChange = true;
                Saved = false;
            }
            if (!OpenSettingUI && currentActMods != -1 && Button.doButton("OpenSettingUI".GetHashCode(), 400, 210, 100, 30, "ModsCore.Mods.OpenSettingUI".Translate(), Color.BlueViolet))
                OpenSettingUI = true;

            if (currentActMods >= 0 && Button.doButton("DeactivateMod".GetHashCode(), 400, 330, 100, 30, "ModsCore.Mods.DeactivateMod".Translate(), Color.BlueViolet))
            {
                IsChange = true;
                Saved = false;
                m_DeactivatedMods.Add(m_ActivatedMods[currentActMods]);
                m_DeactDicModModEntries.Add(m_ActivatedMods[currentActMods].GetHashCode(), m_ActDicModEntries[m_ActivatedMods[currentActMods].GetHashCode()]);
                m_ActDicModEntries.Remove(m_ActivatedMods[currentActMods].GetHashCode());
                m_ActivatedMods.RemoveAt(currentActMods);

                ModManager.ModsConfig ModsConfig = (ModManager.ModsConfig)ModsConfig_FieldInfo.GetValue(new ModManager());
                ModsConfig.EnabledMod.RemoveAt(currentActMods);
                ModsConfig_FieldInfo.SetValue(null, ModsConfig);
                currentDeactMods = m_DeactivatedMods.Count -1;
                currentActMods = -1;
            }

            if (!OpenSettingUI && currentDeactMods >= 0 && Button.doButton("ActivateMod".GetHashCode(), 400, 370, 100, 30, "ModsCore.Mods.ActivateMod".Translate(), Color.BlueViolet))
            {
                IsChange = true;
                Saved = false;
                m_ActivatedMods.Add(m_DeactivatedMods[currentDeactMods]);
                m_ActDicModEntries.Add(m_DeactivatedMods[currentDeactMods].GetHashCode(), m_DeactDicModModEntries[m_DeactivatedMods[currentDeactMods].GetHashCode()]);
                

                ModManager.ModsConfig ModsConfig = (ModManager.ModsConfig)ModsConfig_FieldInfo.GetValue(new ModManager());
                ModsConfig.EnabledMod.Add(Path.GetFileName(m_DeactDicModModEntries[m_DeactivatedMods[currentDeactMods].GetHashCode()].Path));
                ModsConfig_FieldInfo.SetValue(null, ModsConfig);

                m_DeactDicModModEntries.Remove(m_DeactivatedMods[currentDeactMods].GetHashCode());
                m_DeactivatedMods.RemoveAt(currentDeactMods);
                currentActMods = m_ActivatedMods.Count - 1;
                currentDeactMods = -1;
            }

            if (OpenSettingUI && m_ActDicModEntries[m_ActivatedMods[currentActMods].GetHashCode()].Started && m_ActDicModEntries[m_ActivatedMods[currentActMods].GetHashCode()].DrawSettingUIMethod != null)
            {
                RenderedRectangle.doRectangleOutline(570, 100, ScreenManager.GraphicsDevice.Viewport.Width - 50 - 570, ScreenManager.GraphicsDevice.Viewport.Height - 100 - 50, 2 , Color.White);
                m_ActDicModEntries[m_ActivatedMods[currentActMods].GetHashCode()].DrawSettingUIMethod(m_ActDicModEntries[m_ActivatedMods[currentActMods].GetHashCode()],new object[] { ScreenManager, 575 , 100, ScreenManager.GraphicsDevice.Viewport.Width - 50 - 570, ScreenManager.GraphicsDevice.Viewport.Height - 100 - 50 });
            }
            else
            {
                TextItem.doLabel(new Vector2(600, 100), $"Mods ({"ModsCore.Mods.Deactivate".Translate()})", null, 300);
                currentDeactMods = SelectableTextList.doFancyList(25, 575, 140, 300, ScreenManager.GraphicsDevice.Viewport.Height - 50, m_DeactivatedMods.ToArray(), currentDeactMods, null);
            }
            GuiData.endDraw();
            PostProcessor.end();
        }
    }

}
