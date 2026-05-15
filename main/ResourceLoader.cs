using System;
using System.IO;
using UnityEngine;

namespace Iridium
{
    public static class ResourceLoader
    {
        public static string ResourcesPath
        {
            get
            {
                if (Main.Mod == null) throw new InvalidOperationException(Localization.Get("ModNotInitialized"));
                return Path.Combine(Main.Mod.Path, "Resources");
            }
        }

        public static string LoadTextFile(string fileName)
        {
            string filePath = Path.Combine(ResourcesPath, fileName);
            if (!File.Exists(filePath))
            {
                Main.Logger?.Error(Localization.Get("TextFileNotFound", filePath));
                return string.Empty;
            }

            try
            {
                string content = File.ReadAllText(filePath);
                Main.Logger?.Log(Localization.Get("LoadedTextFile", fileName));
                return content;
            }
            catch (Exception ex)
            {
                Main.Logger?.Error(Localization.Get("FailedToLoadTextFile", fileName, ex.Message));
                return string.Empty;
            }
        }

        public static Texture2D? LoadTexture(string fileName)
        {
            string filePath = Path.Combine(ResourcesPath, fileName);
            if (!File.Exists(filePath))
            {
                Main.Logger?.Error(Localization.Get("ImageFileNotFound", filePath));
                return null;
            }

            try
            {
                byte[] fileData = File.ReadAllBytes(filePath);
                Texture2D texture = new(2, 2);
                if (texture.LoadImage(fileData))
                {
                    Main.Logger?.Log(Localization.Get("LoadedTexture", fileName, texture.width, texture.height));
                    return texture;
                }
                Main.Logger?.Error(Localization.Get("FailedToLoadImageData", fileName));
                return null;
            }
            catch (Exception ex)
            {
                Main.Logger?.Error(Localization.Get("FailedToLoadTexture", fileName, ex.Message));
                return null;
            }
        }

        public static byte[] LoadBinaryFile(string fileName)
        {
            string filePath = Path.Combine(ResourcesPath, fileName);
            if (!File.Exists(filePath))
            {
                Main.Logger?.Error(Localization.Get("BinaryFileNotFound", filePath));
                return [];
            }

            try
            {
                byte[] data = File.ReadAllBytes(filePath);
                Main.Logger?.Log(Localization.Get("LoadedBinaryFile", fileName, data.Length));
                return data;
            }
            catch (Exception ex)
            {
                Main.Logger?.Error(Localization.Get("FailedToLoadBinaryFile", fileName, ex.Message));
                return [];
            }
        }

        public static bool FileExists(string fileName) => File.Exists(Path.Combine(ResourcesPath, fileName));

        public static string[] GetFiles(string searchPattern = "*.*", SearchOption searchOption = SearchOption.AllDirectories)
        {
            try
            {
                if (!Directory.Exists(ResourcesPath))
                {
                    Main.Logger?.Warning(Localization.Get("ResourcesFolderNotFound", ResourcesPath));
                    return [];
                }
                return Directory.GetFiles(ResourcesPath, searchPattern, searchOption);
            }
            catch (Exception ex)
            {
                Main.Logger?.Error(Localization.Get("FailedToGetFiles", ex.Message));
                return [];
            }
        }
    }
}
