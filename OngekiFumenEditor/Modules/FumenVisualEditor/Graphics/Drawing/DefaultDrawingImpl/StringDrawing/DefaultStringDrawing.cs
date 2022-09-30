﻿using Caliburn.Micro;
using FontStashSharp;
using OngekiFumenEditor.Modules.FumenVisualEditor.Graphics.Drawing.DefaultDrawingImpl.StringDrawing.String.Platform;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OngekiFumenEditor.Modules.FumenVisualEditor.Graphics.Drawing.DefaultDrawingImpl.StringDrawing
{
    [Export(typeof(IStringDrawing))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class DefaultStringDrawing : CommonDrawingBase, IStringDrawing, IDisposable
    {
        private IPerfomenceMonitor performenceMonitor;
        private Renderer renderer;

        private class FontHandle : IStringDrawing.FontHandle
        {
            public string Name { get; set; }
            public string FilePath { get; set; }
        }

        public static IEnumerable<IStringDrawing.FontHandle> DefaultSupportFonts { get; } = GetSupportFonts();
        public static IStringDrawing.FontHandle DefaultFont { get; } = GetSupportFonts().FirstOrDefault(x => x.Name.ToLower() == "consola");

        public IEnumerable<IStringDrawing.FontHandle> SupportFonts { get; } = DefaultSupportFonts;

        private static IEnumerable<IStringDrawing.FontHandle> GetSupportFonts()
        {
            return Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.Fonts)).Select(x => new FontHandle
            {
                Name = Path.GetFileNameWithoutExtension(x),
                FilePath = x
            }).Where(x => Path.GetExtension(x.FilePath).ToLower() == ".ttf").ToArray();
        }

        public DefaultStringDrawing()
        {
            performenceMonitor = IoC.Get<IPerfomenceMonitor>();
            renderer = new Renderer(this);
        }

        Dictionary<IStringDrawing.FontHandle, FontSystem> cacheFonts = new Dictionary<IStringDrawing.FontHandle, FontSystem>();

        public FontSystem GetFontSystem(IStringDrawing.FontHandle fontHandle)
        {
            if (!cacheFonts.TryGetValue(fontHandle, out var fontSystem))
            {
                var handle = fontHandle as FontHandle;
                fontSystem = new FontSystem(new FontSystemSettings
                {
                    FontResolutionFactor = 2,
                    KernelWidth = 2,
                    KernelHeight = 2
                });
                fontSystem.AddFont(File.ReadAllBytes(handle.FilePath));
                cacheFonts[fontHandle] = fontSystem;
            }

            return fontSystem;
        }

        public void Draw(string text, Vector2 pos, Vector2 scale, int fontSize, float rotate, Vector4 color, Vector2 origin, IStringDrawing.StringStyle style, IFumenEditorDrawingContext target, IStringDrawing.FontHandle handle, out Vector2? measureTextSize)
        {
            performenceMonitor.OnBeginDrawing(this);
            {
                handle = handle ?? DefaultFont;

                var fontStyle = TextStyle.None;

                if (style == IStringDrawing.StringStyle.Underline)
                    fontStyle = TextStyle.Underline;
                if (style == IStringDrawing.StringStyle.Strike)
                    fontStyle = TextStyle.Strikethrough;

                renderer.Begin(GetOverrideModelMatrix() * GetOverrideViewProjectMatrixOrDefault(target));
                var font = GetFontSystem(handle).GetFont(fontSize);
                var size = font.MeasureString(text, scale);
                origin.X = origin.X * 2;
                origin = origin * size;
                scale.Y = -scale.Y;

                font.DrawText(renderer, text, pos, new FSColor(color.X, color.Y, color.Z, color.W), scale, rotate, origin, textStyle: fontStyle);
                measureTextSize = size;
                renderer.End();
            }
            performenceMonitor.OnAfterDrawing(this);
        }

        public void Dispose()
        {
            foreach (var fs in cacheFonts)
                fs.Value?.Dispose();
            cacheFonts.Clear();

            renderer?.Dispose();
            renderer = null;
        }
    }
}
