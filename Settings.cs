using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text;
using System.Drawing;

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

        private Color highlightColor = Color.LightGray;
        private int fontSize = DEFAULT_FONT_SIZE;
        private bool isVisible = DEFAULT_IS_VISIBLE;
        private int width = DEFAULT_WIDTH;
        private MiniMapPosition position = DEFAULT_POSITION;
        private bool onlyUpdateOnTimer = DEFAULT_ONLY_UPDATE_ON_TIMER;

        [Category("MiniMap")]
        [DisplayName("Highlight color for the visible lines")]
        [Description("The background color to highlight the visible lines with.")]
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

        [Category("MiniMap")]
        [DisplayName("Target font size")]
        [Description("The target font size for default text.")]
        [DefaultValue(DEFAULT_FONT_SIZE)]
        public int FontSize
        {
            get { return fontSize; }
            set
            {
                if (fontSize != value)
                {
                    if (value >= DEFAULT_FONT_SIZE)
                        fontSize = value;
                    else
                        fontSize = DEFAULT_FONT_SIZE;

                    FireChanged();
                }
            }
        }

        [Category("MiniMap")]
        [DisplayName("Is Visible")]
        [Description("Whether or not to display the mini map.")]
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

        [Category("MiniMap")]
        [DisplayName("Width")]
        [Description("How wide the mini map should be.")]
        [DefaultValue(DEFAULT_WIDTH)]
        public int Width
        {
            get { return width; }
            set
            {
                if (width != value)
                {
                    if (value > 0)
                        width = value;
                    else
                        width = DEFAULT_WIDTH;

                    FireChanged();
                }
            }
        }

        [Category("MiniMap")]
        [DisplayName("Position")]
        [Description("Whether the mini map should be on the left or right of the editor.")]
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

        [Category("MiniMap")]
        [DisplayName("Only update on timer")]
        [Description("If you are experiencing performance issues this will reduce updates.")]
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
