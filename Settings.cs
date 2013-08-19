using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text;
using System.Drawing;
using EditorMiniMap.Localization;

namespace EditorMiniMap
{
    public delegate void SettingsChangesEvent();

    [Serializable]
    public class Settings
    {
        [field: NonSerialized]
        public event SettingsChangesEvent OnSettingsChanged;

        private const int DEFAULT_FONT_SIZE = 2;
        private const bool DEFAULT_IS_VISIBLE = true;
        private const int DEFAULT_WIDTH = 200;
        private const MiniMapPosition DEFAULT_POSITION = MiniMapPosition.Right;
        private const bool DEFAULT_ONLY_UPDATE_ON_TIMER = false;
        private const bool DEFAULT_SHOW_TOOLBAR_BUTTON = false;
        private const int DEFAULT_MAX_LINE_LIMIT = 5000;
        private const bool DEFAULT_SHOW_CODE_PREVIEW = true;

        private Color highlightColor = Color.LightGray;
        private Color splitHighlightColor = Color.LightPink;
        private int fontSize = DEFAULT_FONT_SIZE;
        private bool isVisible = DEFAULT_IS_VISIBLE;
        private int width = DEFAULT_WIDTH;
        private MiniMapPosition position = DEFAULT_POSITION;
        private bool onlyUpdateOnTimer = DEFAULT_ONLY_UPDATE_ON_TIMER;
        private bool showToolbarButton = DEFAULT_SHOW_TOOLBAR_BUTTON;
        private int _maxLineLimit = DEFAULT_MAX_LINE_LIMIT;
        private bool showCodePreview = DEFAULT_SHOW_CODE_PREVIEW;

        [LocalizedCategory("EditorMiniMap.Category.UI")]
        [LocalizedDisplayName("EditorMiniMap.Label.HighlightColor")]
        [LocalizedDescription("EditorMiniMap.Description.HighlightColor")]
        [DefaultValue(typeof(Color), "LightGray")]
        public Color HighlightColor
        {
            get { return highlightColor; }
            set
            {
                if (highlightColor != value)
                {
                    highlightColor = value;
                    FireChanged();
                }
            }
        }

        [LocalizedCategory("EditorMiniMap.Category.UI")]
        [LocalizedDisplayName("EditorMiniMap.Label.SplitHighlightColor")]
        [LocalizedDescription("EditorMiniMap.Description.SplitHighlightColor")]
        [DefaultValue(typeof(Color), "LightPink")]
        public Color SplitHighlightColor
        {
            get { return splitHighlightColor; }
            set
            {
                if (splitHighlightColor != value)
                {
                    splitHighlightColor = value;
                    FireChanged();
                }
            }
        }

        [LocalizedCategory("EditorMiniMap.Category.UI")]
        [LocalizedDisplayName("EditorMiniMap.Label.FontSize")]
        [LocalizedDescription("EditorMiniMap.Description.FontSize")]
        [DefaultValue(DEFAULT_FONT_SIZE)]
        public int FontSize
        {
            get { return fontSize; }
            set
            {
                if (value < 2)
                    value = DEFAULT_FONT_SIZE;

                if (fontSize != value)
                {
                    fontSize = value;
                    FireChanged();
                }
            }
        }

        [LocalizedCategory("EditorMiniMap.Category.UI")]
        [LocalizedDisplayName("EditorMiniMap.Label.Width")]
        [LocalizedDescription("EditorMiniMap.Description.Width")]
        [DefaultValue(DEFAULT_WIDTH)]
        public int Width
        {
            get { return width; }
            set
            {
                if (value < 1)
                    value = DEFAULT_WIDTH;

                if (width != value)
                {
                    width = value;
                    FireChanged();
                }
            }
        }

        [LocalizedCategory("EditorMiniMap.Category.UI")]
        [LocalizedDisplayName("EditorMiniMap.Label.Position")]
        [LocalizedDescription("EditorMiniMap.Description.Position")]
        [DefaultValue(DEFAULT_POSITION)]
        public MiniMapPosition Position
        {
            get { return position; }
            set
            {
                if (position != value)
                {
                    position = value;
                    FireChanged();
                }
            }
        }

        [LocalizedCategory("EditorMiniMap.Category.Performance")]
        [LocalizedDisplayName("EditorMiniMap.Label.UpdateOnTimer")]
        [LocalizedDescription("EditorMiniMap.Description.UpdateOnTimer")]
        [DefaultValue(DEFAULT_ONLY_UPDATE_ON_TIMER)]
        public bool OnlyUpdateOnTimer
        {
            get { return onlyUpdateOnTimer; }
            set
            {
                if (onlyUpdateOnTimer != value)
                {
                    onlyUpdateOnTimer = value;
                    FireChanged();
                }
            }
        }

        [LocalizedCategory("EditorMiniMap.Category.Performance")]
        [LocalizedDisplayName("EditorMiniMap.Label.MaxLineLimit")]
        [LocalizedDescription("EditorMiniMap.Description.MaxLineLimit")]
        [DefaultValue(DEFAULT_MAX_LINE_LIMIT)]
        public int MaxLineLimit
        {
            get { return _maxLineLimit; }
            set
            {
                if (value < 1)
                    value = DEFAULT_MAX_LINE_LIMIT;

                if (_maxLineLimit != value)
                {
                    _maxLineLimit = value;
                    FireChanged();
                }
            }
        }

        [LocalizedCategory("EditorMiniMap.Category.Visibility")]
        [LocalizedDisplayName("EditorMiniMap.Label.IsVisible")]
        [LocalizedDescription("EditorMiniMap.Description.IsVisible")]
        [DefaultValue(DEFAULT_IS_VISIBLE)]
        public bool IsVisible
        {
            get { return isVisible; }
            set
            {
                if (isVisible != value)
                {
                    isVisible = value;
                    FireChanged();
                }
            }
        }

        [LocalizedCategory("EditorMiniMap.Category.Visibility")]
        [LocalizedDisplayName("EditorMiniMap.Label.ShowToolbarButton")]
        [LocalizedDescription("EditorMiniMap.Description.ShowToolbarButton")]
        [DefaultValue(DEFAULT_SHOW_TOOLBAR_BUTTON)]
        public bool ShowToolbarButton
        {
            get { return showToolbarButton; }
            set
            {
                if (showToolbarButton != value)
                {
                    showToolbarButton = value;
                    FireChanged();
                }
            }
        }

        [LocalizedCategory("EditorMiniMap.Category.Visibility")]
        [LocalizedDisplayName("EditorMiniMap.Label.ShowCodePreview")]
        [LocalizedDescription("EditorMiniMap.Description.ShowCodePreview")]
        [DefaultValue(DEFAULT_SHOW_CODE_PREVIEW)]
        public bool ShowCodePreview
        {
            get { return showCodePreview; }
            set
            {
                if (showCodePreview != value)
                {
                    showCodePreview = value;
                    FireChanged();
                }
            }
        }

        private void FireChanged()
        {
            if (OnSettingsChanged != null) OnSettingsChanged();
        }
    }

    public enum MiniMapPosition
    {
        Right,
        Left
    }
}
