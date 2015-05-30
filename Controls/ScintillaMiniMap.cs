using System;
using System.Collections.Generic;
using System.Text;
using ScintillaNet;
using ScintillaNet.Configuration;
using PluginCore;
using ASCompletion.Context;
using ASCompletion.Model;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using PluginCore.Controls;
using EditorMiniMap.Helpers;

namespace EditorMiniMap
{
    public class ScintillaMiniMap : ScintillaControl
    {
        private ITabbedDocument _document = null;
        private ScintillaControl _splitSci1 = null;
        private ScintillaControl _splitSci2 = null;
        private Settings _settings = null;
        private CodePreview _codePopup = null;

        // Track the last visible lines
        private int _lastSci1FirstLine = -1;
        private int _lastSci1LinesOnScreen = -1;
        private int _lastSci1CenterLine = -1;
        private int _lastSci2FirstLine = -1;
        private int _lastSci2LinesOnScreen = -1;
        private int _lastSci2CenterLine = -1;
        private bool _lastSci2Visible = false;
        private int _lastLinesOnScreen = 0;

        private Language _lastLanguage = null;
        private Timer _updateTimer = null;

        // Ignore changes when updating
        private bool _updating = false;
        private bool _disabled = false;

        private bool _mouseDown = false;
        private bool _mouseMoved = false;

        #region Initializing and Disposing

        public ScintillaMiniMap(ITabbedDocument document, Settings settings)
        {
            InitializeMiniMap();

            _settings = settings;
            _document = document;
            _splitSci1 = document.SplitSci1;
            _splitSci2 = document.SplitSci2;

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
            _splitSci1.TextInserted += new TextInsertedHandler(_splitSci1_TextInserted);
            _splitSci1.TextDeleted += new TextDeletedHandler(_splitSci1_TextDeleted);
            _document.SplitContainer.Panel2.VisibleChanged += Sci2Panel_VisibleChanged;
            _updateTimer.Tick += _timer_Update;

            HookUIEvents();
        }

        private void HookUIEvents()
        {
            if (!_settings.OnlyUpdateOnTimer)
            {
                _splitSci1.UpdateUI += new UpdateUIHandler(_splitSci1_UpdateUI);
                _splitSci2.UpdateUI += new UpdateUIHandler(_splitSci1_UpdateUI);
            }
        }

        void _timer_Update(object sender, EventArgs e)
        {
            UpdateMiniMap(false);
        }

        private void UnhookEvents()
        {
            _settings.OnSettingsChanged -= new SettingsChangesEvent(_settings_OnSettingsChanged);
            _splitSci1.TextInserted -= new TextInsertedHandler(_splitSci1_TextInserted);
            _splitSci1.TextDeleted -= new TextDeletedHandler(_splitSci1_TextDeleted);
            _splitSci1.UpdateUI -= new UpdateUIHandler(_splitSci1_UpdateUI);
            _splitSci2.UpdateUI -= new UpdateUIHandler(_splitSci1_UpdateUI);
            _document.SplitContainer.Panel2.VisibleChanged -= Sci2Panel_VisibleChanged;
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

        void Sci2Panel_VisibleChanged(object sender, EventArgs e)
        {
            var timer = new Timer();
            timer.Interval = 10;
            timer.Tick += timer_Tick;
            timer.Start();
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
                    UpdateFoldedCode();
            }

            _splitSci1.UpdateUI -= new UpdateUIHandler(_splitSci1_UpdateUI);
            _splitSci2.UpdateUI -= new UpdateUIHandler(_splitSci1_UpdateUI);
            HookUIEvents();

            // Define the highlight markers
            this.MarkerSetBack(20, PluginCore.Utilities.DataConverter.ColorToInt32(_settings.HighlightColor));
            this.MarkerDefine(20, ScintillaNet.Enums.MarkerSymbol.Background);

            this.MarkerSetBack(21, PluginCore.Utilities.DataConverter.ColorToInt32(_settings.SplitHighlightColor));
            this.MarkerDefine(21, ScintillaNet.Enums.MarkerSymbol.Background);

            Color blendedHighlightColor = BlendColors(_settings.HighlightColor, _settings.SplitHighlightColor);
            this.MarkerSetBack(22, PluginCore.Utilities.DataConverter.ColorToInt32(blendedHighlightColor));
            this.MarkerDefine(22, ScintillaNet.Enums.MarkerSymbol.Background);

            // Force update of the mini map since the settings have changed
            UpdateMiniMap(true);
        }

        bool UpdateFoldedCode()
        {
            bool changed = false;

            // Go line by line updating line visibility. Would prefer a CodeFoldsChanged event.
            for (int index = 0; index < _splitSci1.LineCount; index++)
            {
                bool visible = GetLineVisible(this, index);
                bool parterVisible = GetLineVisible(_splitSci1, index);

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
            var buttons = _document.SciControl == _document.SplitSci1 ? MouseButtons.Left : MouseButtons.Right;
            ScrollMiniMap(mouse, buttons);
        }

        public void OnMouseWheel(MouseEventArgs e)
        {
            this.LineScroll(0, e.Delta / -40);
        }

        public void OnMouseClick(MouseEventArgs e)
        {
            if (!_mouseMoved)
                ScrollMiniMap(e.Location, e.Button);
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
                ScrollMiniMap(e.Location, e.Button);
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

            _codePopup = new CodePreview(_document);
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

        private void ScrollMiniMap(Point mouse, MouseButtons buttons)
        {
            // get the cursor position under the mouse
            int position = this.PositionFromPoint(mouse.X, mouse.Y);
            int line = this.LineFromPosition(position);
            int centerLine = this.VisibleFromDocLine(line);

            if (buttons == MouseButtons.Left)
            {
                // if it does not match our last position
                if (_lastSci1CenterLine != centerLine)
                {
                    // Center the partner editor on the cursor line
                    CenterSci1Editor(centerLine);
                }
            }
            else if (buttons == MouseButtons.Right)
            {
                // if it does not match our last position
                if (_lastSci2CenterLine != centerLine)
                {
                    // Center the partner editor on the cursor line
                    CenterSci2Editor(centerLine);
                }
            }
        }

        private void CenterSci1Editor(int line)
        {
            // Constrain the first visible line to a reasonable number
            int firstVisibleLine = Math.Min(Math.Max(line - (int)Math.Floor(_splitSci1.LinesOnScreen / 2.0), 0), _splitSci1.LineCount - _lastSci1LinesOnScreen + 1);
            if (firstVisibleLine != _lastSci1FirstLine)
            {
                // Calculate shift then scroll
                int delta = firstVisibleLine - _lastSci1FirstLine - 1;
                _lastSci1FirstLine = Math.Max(firstVisibleLine, 1);
                _splitSci1.LineScroll(0, delta);
            }
            _lastSci1CenterLine = line;
            UpdateMiniMap(false);
        }

        private void CenterSci2Editor(int line)
        {
            // Constrain the first visible line to a reasonable number
            int firstVisibleLine = Math.Min(Math.Max(line - (int)Math.Floor(_splitSci2.LinesOnScreen / 2.0), 0), _splitSci2.LineCount - _lastSci2LinesOnScreen + 1);
            if (firstVisibleLine != _lastSci2FirstLine)
            {
                // Calculate shift then scroll
                int delta = firstVisibleLine - _lastSci2FirstLine - 1;
                _lastSci2FirstLine = Math.Max(firstVisibleLine, 1);
                _splitSci2.LineScroll(0, delta);
            }
            _lastSci2CenterLine = line;
            UpdateMiniMap(false);
        }

        #endregion

        #region Update Methods

        private void UpdateText()
        {
            string text = _splitSci1.Text;
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
            if (_splitSci1.LineCount > _settings.MaxLineLimit)
                Disable();

            if (!this.Visible)
                return;

            _updating = true;

            // If tab width has changed, then update
            if (this.TabWidth != _splitSci1.TabWidth)
                this.TabWidth = _splitSci1.TabWidth;

            // If the editor starts in the middle of the code, then force an update.
            if (_lastLinesOnScreen == 0 && this.LinesOnScreen > 0)
                forceUpdate = true;

            // If folded code has changed then, then perform a full update.
            if (UpdateFoldedCode())
                forceUpdate = true;

            // If language has changed then, update syntax language and zoom level
            Language language = GetLanguage(_splitSci1.ConfigurationLanguage);
            if (_lastLanguage != language || forceUpdate)
            {
                _lastLanguage = language;
                this.ConfigurationLanguage = _splitSci1.ConfigurationLanguage;

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
            }

            int sci1FirstLine = _splitSci1.FirstVisibleLine;
            int sci1LinesOnScreen = _splitSci1.LinesOnScreen;

            int sci2FirstLine = _splitSci2.FirstVisibleLine;
            int sci2LinesOnScreen = _splitSci2.LinesOnScreen;

            // Check to see if we have scrolled or resized or...
            if (sci1FirstLine != _lastSci1FirstLine ||
                sci1LinesOnScreen != _lastSci1LinesOnScreen ||
                sci2FirstLine != _lastSci2FirstLine ||
                sci2LinesOnScreen != _lastSci2LinesOnScreen ||
                forceUpdate)
            {
                RemoveOldLineHighlights();

                // Add the visible lines highlight
                int firstDocumentLine = this.DocLineFromVisible(sci1FirstLine);
                int lastVisibleLine = this.DocLineFromVisible(sci1FirstLine + sci1LinesOnScreen);
                for (int line = firstDocumentLine; line < lastVisibleLine; line++)
                {
                    this.MarkerAdd(line, 20);
                }

                if (_splitSci2.Visible)
                {
                    // Add the visible split highlight
                    int firstSplitDocumentLine = this.DocLineFromVisible(sci2FirstLine);
                    int lastSplitVisibleLine = this.DocLineFromVisible(sci2FirstLine + sci2LinesOnScreen);
                    for (int line = firstSplitDocumentLine; line < lastSplitVisibleLine; line++)
                    {
                        if (line < firstDocumentLine ||
                            line > lastVisibleLine)
                            this.MarkerAdd(line, 21);
                        else
                            this.MarkerAdd(line, 22);
                    }
                }

                // When visible lines < line count, apply a continuous scroll so that all lines can be visible
                ScrollMiniMap();

                _lastLinesOnScreen = this.LinesOnScreen;
                _lastSci1FirstLine = sci1FirstLine;
                _lastSci1LinesOnScreen = sci1LinesOnScreen;
                _lastSci1CenterLine = _lastSci1FirstLine + (int)Math.Floor(_lastSci1LinesOnScreen / 2.0);

                _lastSci2FirstLine = sci2FirstLine;
                _lastSci2LinesOnScreen = sci2LinesOnScreen;
                _lastSci2CenterLine = _lastSci2FirstLine + (int)Math.Floor(_lastSci2LinesOnScreen / 2.0);
                _lastSci2Visible = _splitSci2.Visible;
            }

            _updating = false;
        }

        private void RemoveOldLineHighlights()
        {
            this.MarkerDeleteAll(20);
            this.MarkerDeleteAll(21);
            this.MarkerDeleteAll(22);
        }

        private Color BlendColors(Color c1, Color c2)
        {
            return Color.FromArgb((c1.A + c2.A) / 2,
                                (c1.R + c2.R) / 2,
                                (c1.G + c2.G) / 2,
                                (c1.B + c2.B) / 2);
        }

        private void ScrollMiniMap()
        {
            int displayLineCount = this.VisibleFromDocLine(this.LineCount);
            // If there is more lines that can fit on the screen
            if (displayLineCount > this.LinesOnScreen)
            {
                // Apply a continuous scroll, so that all lines will eventually be visible
                double percent = _lastSci1FirstLine / Convert.ToDouble(displayLineCount - _lastSci1LinesOnScreen);
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