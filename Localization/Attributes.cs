using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Text;
using EditorMiniMap.Helpers;

namespace EditorMiniMap.Localization
{
    [AttributeUsage(AttributeTargets.All)]
    public class LocalizedCategoryAttribute : CategoryAttribute
    {
        private bool initialized = false;
        private string _categoryValue = null;

        public LocalizedCategoryAttribute(string key)
            : base(key)
        {
        }
        
        protected override string GetLocalizedString(string key)
        {
            if (!initialized)
            {
                _categoryValue = ResourceHelper.GetString(key);
                if (_categoryValue == null)
                    _categoryValue = key;
                initialized = true;
            }
            return _categoryValue;
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    public class LocalizedDescriptionAttribute : DescriptionAttribute
    {
        private bool initialized = false;

        public LocalizedDescriptionAttribute(string key)
            : base(key)
        {
        }

        public override string Description
        {
            get
            {
                if (!initialized)
                {
                    string key = base.Description;
                    DescriptionValue = ResourceHelper.GetString(key);
                    if (DescriptionValue == null)
                        DescriptionValue = key;
                    initialized = true;
                }
                return DescriptionValue;
            }
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    public class LocalizedDisplayNameAttribute : DisplayNameAttribute
    {
        private bool initialized = false;

        public LocalizedDisplayNameAttribute(string key)
            : base(key)
        {
        }

        public override string DisplayName
        {
            get
            {
                if (!initialized)
                {
                    string key = base.DisplayName;
                    DisplayNameValue = ResourceHelper.GetString(key);
                    if (DisplayNameValue == null) 
                        DisplayNameValue = key;
                    initialized = true;
                }
                return DisplayNameValue;
            }
        }
    }
}