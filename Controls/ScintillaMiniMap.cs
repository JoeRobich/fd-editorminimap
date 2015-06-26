using EditorMiniMap.Helpers;
using PluginCore;
using ScintillaNet;
using ScintillaNet.Configuration;
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace EditorMiniMap
{
    public class ScintillaMiniMap : ScintillaControl
    {
        private ScintillaControl _sci = null;
        private Settings _settings = null;
        private CodePreview _codePopup = null;

        // Track the last visible lines
        private int _lastSciFirstLine = -1;
        private int _lastSciLinesOnScreen = -1;
        private int _lastSciCenterLine = -1;
        private int _lastLinesOnScreen = 0;

        private Language _lastLanguage = null;
        private Timer _updateTimer = null;

        // Ignore changes when updating
        private bool _updating = false;
        private bool _disabled = false;

        private bool _mouseDown = false;
        private bool _mouseMoved = false;

        #region Initializing and Disposing

        public ScintillaMiniMap(ScintillaControl sci, Settings settings)
        {
            InitializeMiniMap();

            _settings = settings;
            _sci = sci;

            // Visibilty no matching will force an update in RefreshSettings
            this.Visible = !_settings.IsVisible;

            HookEvents();
            UpdateText();
            RefreshSettings();

            _updateTimer.Start();
        }

        private void InitializeMiniMap()
        {
            this.Dock = DockStyle.Fill;
            // Make non-editable
            this.Enabled = false;
            // Hide scrollbars
            this.IsHScrollBar = false;
            this.IsVScrollBar = false;
            // Hide the margin
            this.SetMarginWidthN(0, 0);
            this.SetMarginWidthN(1, 0);
            this.AllowDrop = true;
            // Setup timer
            _updateTimer = new Timer();
            _updateTimer.Interval = 100;
        }

        private void Disable()
        {
            // Unhook events then remove and dispose of ourselves
            Dispose(true);
            _disabled = true;
            //this.Parent.Controls.Remove(this);
        }

        protected override void Dispose(bool disposing)
        {
            _updateTimer.Stop();
            CloseCodePopup();
            UnhookEvents();
            base.Dispose(disposing);
        }

        #endregion

        #region Events and Handlers

        private void HookEvents()
        {
            _settings.OnSettingsChanged += new SettingsChangesEvent(_settings_OnSettingsChanged);
            _sci.TextInserted += new TextInsertedHandler(_splitSci1_TextInserted);
            _sci.TextDeleted += new TextDeletedHandler(_splitSci1_TextDeleted);
            _updateTimer.Tick += _timer_Update;

            HookUIEvents();
        }

        private void HookUIEvents()
        {
            if (!_settings.OnlyUpdateOnTimer)
                _sci.UpdateUI += new UpdateUIHandler(_splitSci1_UpdateUI);
        }

        void _timer_Update(object sender, EventArgs e)
        {
            UpdateMiniMap(false);
        }

        private void UnhookEvents()
        {
            _settings.OnSettingsChanged -= new SettingsChangesEvent(_settings_OnSettingsChanged);
            _sci.TextInserted -= new TextInsertedHandler(_splitSci1_TextInserted);
            _sci.TextDeleted -= new TextDeletedHandler(_splitSci1_TextDeleted);
            _sci.UpdateUI -= new UpdateUIHandler(_splitSci1_UpdateUI);
        }

        void _settings_OnSettingsChanged()
        {
            RefreshSettings();
        }

        void _splitSci1_UpdateUI(ScintillaControl sender)
        {
            // Since this event occurs so often, only perform a minimal update if we are visible.
            if (this.Visible && !_updating)
                UpdateMiniMap(false);
        }

        void _splitSci1_TextInserted(ScintillaControl sender, int position, int length, int linesAdded)
        {
            UpdateText();
        }

        void _splitSci1_TextDeleted(ScintillaControl sender, int position, int length, int linesAdded)
        {
            UpdateText();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            Timer timer = (Timer)sender;
            timer.Stop();
            timer.Dispose();

            UpdateMiniMap(true);
        }

        #endregion

        #region Refresh Settings Methods

        public void RefreshSettings()
        {
            if (_disabled)
                return;

            // Has the visibility changed?
            if (this.Visible != _settings.IsVisible)
            {
                this.Visible = _settings.IsVisible;

                // If we are newly visible the update folded code in the mini map
                if (this.Visible)
                {
                    _sci.IsVScrollBar = _settings.ShowVerticalScrollbar;
                    UpdateFoldedCode();
                }
                else
                {
                    _sci.IsVScrollBar = true;
                }
            }

            if (_sci.IsVScrollBar != _settings.ShowVerticalScrollbar)
            {
                _sci.IsVScrollBar = _settings.ShowVerticalScrollbar;
            }

            _sci.UpdateUI -= new UpdateUIHandler(_splitSci1_UpdateUI);
            HookUIEvents();

            // Define the highlight markers
            this.MarkerSetBack(20, PluginCore.Utilities.DataConverter.ColorToInt32(_settings.HighlightColor));
            this.MarkerDefine(20, ScintillaNet.Enums.MarkerSymbol.Background);

            BackColor = _settings.HighlightColor;

            // Force update of the mini map since the settings have changed
            UpdateMiniMap(true);
        }

        bool UpdateFoldedCode()
        {
            bool changed = false;

            // Go line by line updating line visibility. Would prefer a CodeFoldsChanged event.
            for (int index = 0; index < _sci.LineCount; index++)
            {
                bool visible = GetLineVisible(this, index);
                bool parterVisible = GetLineVisible(_sci, index);

                // if visibility doesn't match the update
                if (parterVisible && !visible)
                {
                    this.ShowLines(index, index);
                    changed = true;
                }
                else if (!parterVisible && visible)
                {
                    this.HideLines(index, index);
                    changed = true;
                }
            }

            return changed;
        }

        private bool GetLineVisible(ScintillaControl sci, int line)
        {
            // for some reason IsLineVisible was a property, but there was no way to specify which line...
            return sci.SlowPerform(2228, (uint)line, 0) != 0;
        }

        #endregion

        #region Handle Mouse Click Methods

        public void OnDragOver(Point mouse)
        {
            ScrollMiniMap(mouse);
        }

        public void OnMouseWheel(MouseEventArgs e)
        {
            this.LineScroll(0, e.Delta / -40);
        }

        public void OnMouseClick(MouseEventArgs e)
        {
            if (!_mouseMoved)
                ScrollMiniMap(e.Location);
        }

        public void OnMouseLeave()
        {
            CloseCodePopup();
        }

        public void OnMouseDown(MouseEventArgs e)
        {
            _mouseDown = true;
            _mouseMoved = false;
            CloseCodePopup();
        }

        public void OnMouseMove(MouseEventArgs e)
        {
            if (_mouseDown)
            {
                _mouseMoved = true;
                ScrollMiniMap(e.Location);
            }

            if (_codePopup != null)
                UpdateCodePopup(e.Location);
        }

        public void OnMouseUp(MouseEventArgs e)
        {
            _mouseDown = false;
        }

        public void OnMouseHover(Point mouse)
        {
            if (_codePopup != null)
                return;

            if (_settings.ShowCodePreview)
                ShowCodePopup(mouse);
        }

        public void OnMouseHoverEnd()
        {
        }

        private void ShowCodePopup(Point point)
        {
			if (!WindowHelper.ApplicationIsActivated())
				return;

            var position = this.PositionFromPoint(point.X, point.Y);
            var line = this.LineFromPosition(position);

            var controlPosition = this.PointToScreen(new Point(this.Left, this.Top));
            point = this.PointToScreen(point);

            _codePopup = new CodePreview(this);
            _codePopup.CenterEditor(line);

            if (this.Parent.Dock == DockStyle.Right)
                _codePopup.Left = controlPosition.X - _codePopup.Width;
            else
                _codePopup.Left = controlPosition.X + this.Width;

            _codePopup.Top = point.Y - (_codePopup.Height / 2);
            _codePopup.TopMost = true;
            _codePopup.Show(PluginCore.PluginBase.MainForm);
        }

        private void UpdateCodePopup(Point point)
        {
            var position = this.PositionFromPoint(point.X, point.Y);
            var line = this.LineFromPosition(position);
            _codePopup.CenterEditor(line);

            point = this.PointToScreen(point);
            _codePopup.Top = point.Y - (_codePopup.Height / 2);
        }

        private void CloseCodePopup()
        {
            if (_codePopup != null)
            {
                _codePopup.Close();
                _codePopup.Dispose();
                _codePopup = null;
            }
        }

        private void ScrollMiniMap(Point mouse)
        {
            // get the cursor position under the mouse
            var percent = (double)mouse.Y / this.Height;
            var centerLine = (int)(this.LinesOnScreen * percent) + this.FirstVisibleLine;

            // if it does not match our last position
            if (_lastSciCenterLine != centerLine)
            {
                // Center the partner editor on the cursor line
                CenterSci1Editor(centerLine);
            }
        }

        private void CenterSci1Editor(int line)
        {
            // Constrain the first visible line to a reasonable number
            int firstVisibleLine = Math.Min(Math.Max(line - (int)Math.Floor(_sci.LinesOnScreen / 2.0), 0), _sci.LineCount + _lastSciLinesOnScreen - 1);
            if (firstVisibleLine != _lastSciFirstLine)
            {
                // Calculate shift then scroll
                int delta = firstVisibleLine - _lastSciFirstLine - 1;
                _lastSciFirstLine = Math.Max(firstVisibleLine, 1);
                _sci.LineScroll(0, delta);
            }
            _lastSciCenterLine = line;
            UpdateMiniMap(false);
        }

        #endregion

        #region Update Methods

        private void UpdateText()
        {
            string text = _sci.Text.Replace("�", "?") + string.Join(Environment.NewLine, new string[_sci.LinesOnScreen > 0 ? _sci.LinesOnScreen - 1 : 0]);

            if (text == null)
                return;

            // If text has changed, then update and perform a full update.
            if (this.Text != text)
            {
                this.Text = text;
                UpdateMiniMap(true);
            }
        }

        private void UpdateMiniMap(bool forceUpdate)
        {
            if (_disabled)
                return;

            // Check to see if we've gone over our line limit
            if (_sci.LineCount > _settings.MaxLineLimit)
                Disable();

            if (!this.Visible)
                return;

            _updating = true;

            // If tab width has changed, then update
            if (this.TabWidth != _sci.TabWidth)
                this.TabWidth = _sci.TabWidth;

            // If the editor starts in the middle of the code, then force an update.
            if (_lastLinesOnScreen == 0 && this.LinesOnScreen > 0)
                forceUpdate = true;

            // If folded code has changed then, then perform a full update.
            if (UpdateFoldedCode())
                forceUpdate = true;

            // If language has changed then, update syntax language and zoom level
            Language language = GetLanguage(_sci.ConfigurationLanguage);
            if (_lastLanguage != language || forceUpdate)
            {
                _lastLanguage = language;
                this.ConfigurationLanguage = _sci.ConfigurationLanguage;

                // Get the default style font size
                int fontSize = GetDefaultFontSize(language);
                if (fontSize > _settings.FontSize)
                {
                    // zoom level is the delta of the default and target font sizes
                    int zoomLevel = -(fontSize - _settings.FontSize);
                    if (this.ZoomLevel != zoomLevel)
                        this.ZoomLevel = zoomLevel;
                }

                this.SetProperty("lexer.cpp.track.preprocessor", "0");

                var defaultStyle = language.usestyles.FirstOrDefault(s => s.name == "default");
                var defaultBackColor = defaultStyle.BackgroundColor;

                this.MarkerDeleteAll(20);
                this.MarkerDeleteHandle(20);
                this.MarkerSetBack(20, defaultBackColor);
                this.MarkerDefine(20, ScintillaNet.Enums.MarkerSymbol.Background);

                foreach (var style in language.usestyles)
                    this.StyleSetBack(style.key, PluginCore.Utilities.DataConverter.ColorToInt32(_settings.HighlightColor));
            }

            int sci1FirstLine = _sci.FirstVisibleLine;
            int sci1LinesOnScreen = _sci.LinesOnScreen;

            // Check to see if we have scrolled or resized or...
            if (sci1FirstLine != _lastSciFirstLine ||
                sci1LinesOnScreen != _lastSciLinesOnScreen ||
                forceUpdate)
            {
                UpdateText();

                RemoveOldLineHighlights();

                // Add the visible lines highlight
                int firstDocumentLine = this.DocLineFromVisible(sci1FirstLine);
                int lastVisibleLine = this.DocLineFromVisible(sci1FirstLine + sci1LinesOnScreen);
                int linesWithOverflow = this.LineCount + this.LinesOnScreen;
                for (int line = 0; line < linesWithOverflow; line++)
                {
                    if (line >= firstDocumentLine && line < lastVisibleLine)
                        this.MarkerAdd(line, 20);
                }

                _lastLinesOnScreen = this.LinesOnScreen;
                _lastSciFirstLine = sci1FirstLine;
                _lastSciLinesOnScreen = sci1LinesOnScreen;
                _lastSciCenterLine = _lastSciFirstLine + (int)Math.Floor(_lastSciLinesOnScreen / 2.0);

                // When visible lines < line count, apply a continuous scroll so that all lines can be visible
                ScrollMiniMap();
            }

            _updating = false;
        }

        private void RemoveOldLineHighlights()
        {
            this.MarkerDeleteAll(20);
        }

        private void ScrollMiniMap()
        {
            int displayLineCount = this.VisibleFromDocLine(this.LineCount);
            // If there is more lines that can fit on the screen
            if (displayLineCount > this.LinesOnScreen)
            {
                // Apply a continuous scroll, so that all lines will eventually be visible
                double percent = _lastSciFirstLine / Convert.ToDouble(displayLineCount - _lastSciLinesOnScreen);
                int line = (int)Math.Floor(percent * (displayLineCount - this.LinesOnScreen));
                int delta = line - this.FirstVisibleLine - 1;
                this.LineScroll(0, delta);
            }
        }

        private Language GetLanguage(string name)
        {
            if (PluginBase.MainForm == null || PluginBase.MainForm.SciConfig == null)
                return null;

            Language language = PluginBase.MainForm.SciConfig.GetLanguage(name);
            return language;
        }

        private int GetDefaultFontSize(Language language)
        {
            // find the default style and return the font size for this syntax configuration
            if (language != null)
                foreach (UseStyle style in language.usestyles)
                    if (style.name == "default")
                        return style.FontSize;

            return 10;
        }

        #endregion
    }
}