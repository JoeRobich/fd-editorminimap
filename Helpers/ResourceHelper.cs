using System;
using System.Collections.Generic;
using System.Text;
using PluginCore.Localization;
using PluginCore;
using System.Resources;
using System.Reflection;
using System.Drawing;

namespace EditorMiniMap.Helpers
{
    public class ResourceHelper
    {
        private const LocaleVersion defaultLocale = LocaleVersion.en_US;

        private static ResourceManager resourceManager = null;
        private static LocaleVersion storedLocale = LocaleVersion.en_US;
        private static Dictionary<string, Image> _imageCache = new Dictionary<string, Image>();

        /// <summary>
        /// Gets the specified localized string
        /// </summary>
        public static string GetString(string key)
        {
            string result;
            LocaleVersion localeSetting = PluginBase.MainForm.Settings.LocaleVersion;
            if (resourceManager == null || localeSetting != storedLocale)
            {
                storedLocale = localeSetting;

                if (!TryGetResourceManager(storedLocale, key, out resourceManager))
                    TryGetResourceManager(defaultLocale, key, out resourceManager);
            }
            result = resourceManager.GetString(key);
            if (result == null)
                result = key;
            return result;
        }

        private static bool TryGetResourceManager(LocaleVersion locale, string testKey, out ResourceManager manager)
        {
            manager = null;

            try
            {
                Assembly callingAssembly = Assembly.GetCallingAssembly();
                string prefix = callingAssembly.GetName().Name;
                string path = prefix + ".Resources." + locale.ToString();

                var testManager = new ResourceManager(path, callingAssembly);
                var testResult = testManager.GetString(testKey);
                manager = testManager;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static Image GetImage(string name)
        {
            return GetImage(name, "png");
        }

        public static Image GetImage(string name, string extension)
        {
            Assembly callingAssembly = Assembly.GetCallingAssembly();
            string prefix = callingAssembly.GetName().Name;
            string resourceName = string.Format("{0}.Resources.{1}.{2}", prefix, name, extension);
            if (!_imageCache.ContainsKey(resourceName))
            {
                _imageCache.Add(resourceName, Image.FromStream(callingAssembly.GetManifestResourceStream(resourceName)));
            }
            return _imageCache[resourceName];
        }
    }
}
