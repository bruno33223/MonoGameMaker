using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGameMaker.IDE.Core
{
    public static class TextureCache
    {
        private static GraphicsDevice? _graphicsDevice;
        private static ImGuiRenderer? _renderer;

        private static readonly Dictionary<string, (Texture2D texture, IntPtr imguiId)> _loadedPreviews = new();

        public static void Initialize(GraphicsDevice graphicsDevice, ImGuiRenderer renderer)
        {
            _graphicsDevice = graphicsDevice;
            _renderer = renderer;
        }

        public static IntPtr BindRenderTarget(Texture2D renderTarget)
        {
            if (_renderer == null) return IntPtr.Zero;
            return _renderer.BindTexture(renderTarget);
        }

        public static void UnbindRenderTarget(IntPtr imguiId)
        {
            if (_renderer == null || imguiId == IntPtr.Zero) return;
            try
            {
                _renderer.UnbindTexture(imguiId);
            }
            catch {}
        }

        public static Texture2D? GetTexture(string absolutePath)
        {
            if (_graphicsDevice == null || _renderer == null || !File.Exists(absolutePath))
                return null;

            if (_loadedPreviews.TryGetValue(absolutePath, out var preview))
            {
                return preview.texture;
            }

            int w, h;
            GetPreview(absolutePath, out w, out h);
            if (_loadedPreviews.TryGetValue(absolutePath, out var preview2))
            {
                return preview2.texture;
            }

            return null;
        }

        public static IntPtr GetPreview(string absolutePath, out int width, out int height)
        {
            width = 0;
            height = 0;

            if (_graphicsDevice == null || _renderer == null || !File.Exists(absolutePath))
                return IntPtr.Zero;

            if (_loadedPreviews.TryGetValue(absolutePath, out var preview))
            {
                width = preview.texture.Width;
                height = preview.texture.Height;
                return preview.imguiId;
            }

            try
            {
                var texture = Texture2D.FromFile(_graphicsDevice, absolutePath);
                var imguiId = _renderer.BindTexture(texture);
                _loadedPreviews[absolutePath] = (texture, imguiId);
                
                width = texture.Width;
                height = texture.Height;
                return imguiId;
            }
            catch (Exception ex)
            {
                GlobalState.Log($"Error loading preview texture {Path.GetFileName(absolutePath)}: {ex.Message}");
                return IntPtr.Zero;
            }
        }

        public static void Unload(string absolutePath)
        {
            if (_loadedPreviews.TryGetValue(absolutePath, out var preview))
            {
                if (_renderer != null)
                {
                    try
                    {
                        _renderer.UnbindTexture(preview.imguiId);
                    }
                    catch
                    {
                        // Ignore unbinding errors
                    }
                }
                
                try
                {
                    preview.texture.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }
                
                _loadedPreviews.Remove(absolutePath);
            }
        }

        public static void UnloadAll()
        {
            foreach (var preview in _loadedPreviews.Values)
            {
                if (_renderer != null)
                {
                    try
                    {
                        _renderer.UnbindTexture(preview.imguiId);
                    }
                    catch
                    {
                        // Ignore
                    }
                }
                
                try
                {
                    preview.texture.Dispose();
                }
                catch
                {
                    // Ignore
                }
            }
            _loadedPreviews.Clear();
        }
    }
}
