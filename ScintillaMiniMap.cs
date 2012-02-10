using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScintillaNet;
using ScintillaNet.Configuration;
using PluginCore;
using ASCompletion.Context;
using ASCompletion.Model;
using System.Windows.Forms;
using System.Drawing;

namespace EditorMiniMap
{
    public class ScintillaMiniMap : ScintillaControl
    {
        private ScintillaControl _partnerEditor = null;
        private Settings _settings = null;
        private Timer _updateTimer;

        // Track the last visible lines
        private int _lastPartnerFirstLine = -1;
        private int _lastPartnerLinesOnScreen = -1;
        private int _lastCenterLine = -1;
        
        // Ignore changes when updating
        private bool _updating = false;

        // Track mouse clicks
        private bool _mouseDownInside = false;
        private bool _mouseDownOutside = false;

        public ScintillaMiniMap(ScintillaControl partnerEditor, Settings settings)
        {
            InitializeMiniMap();

            _settings = settings;
            _partnerEditor = partnerEditor;

            // Update timer for looking a mouse events on MiniMap and keeping the two control in sync
            _updateTimer = new Timer();
            _updateTimer.Interval = 100;

            // Visibilty no matching will force an update in RefreshSettings
            this.Visible = !_settings.IsVisible;

            // Match tab width
            this.TabWidth = _partnerEditor.TabWidth;

            HookEvents();
            UpdateTimer();
            RefreshSettings();
        }

        private void InitializeMiniMap()
        {
            // These will get corrected in RefreshSettings
            this.Width = 200;
            this.Dock = System.Windows.Forms.DockStyle.Right;
            // Make non-editable
            this.Enabled = false;
            // Hide Scrollbars
            this.IsHScrollBar = false;
            this.IsVScrollBar = false;
            // Hide Bookmarks but leave a sliver as a separator
            this.SetMarginWidthN(0, 1);
            // Hide Line Numebrs
            this.SetMarginWidthN(1, 0);
            // Hide Folding
            this.SetMarginWidthN(2, 0);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _updateTimer.Stop();
            UnhookEvents();
        }

        private void HookEvents()
        {
            _updateTimer.Tick += new EventHandler(_updateTimer_Tick);
            _settings.OnSettingsChanged += new SettingsChangesEvent(_settings_OnSettingsChanged);
            // UpdateUI is used to track changes in the scroll position more closely than the UpdateTimer
            if (!_settings.OnlyUpdateOnTimer)
                _partnerEditor.UpdateUI += new UpdateUIHandler(_partnerControl_UpdateUI);
        }

        private void UnhookEvents()
        {
            _updateTimer.Tick -= new EventHandler(_updateTimer_Tick);
            _settings.OnSettingsChanged -= new SettingsChangesEvent(_settings_OnSettingsChanged);
            _partnerEditor.UpdateUI -= new UpdateUIHandler(_partnerControl_UpdateUI);
        }

        public void RefreshSettings()
        {
            // Has the visibility changed?
            if (this.Visible != _settings.IsVisible)
            {
                this.Visible = _settings.IsVisible;

                // If we are newly visible the update folded code in the mini map
                if (this.Visible)
                    UpdateFoldedCode();

                UpdateTimer();
            }

            // Only hook update ui if requested to
            _partnerEditor.UpdateUI -= new UpdateUIHandler(_partnerControl_UpdateUI);
            if (!_settings.OnlyUpdateOnTimer)
                _partnerEditor.UpdateUI += new UpdateUIHandler(_partnerControl_UpdateUI);

            // Update dock position if necessary
            if (_settings.Position == MiniMapPosition.Right &&
                this.Dock == System.Windows.Forms.DockStyle.Left)
                this.Dock = System.Windows.Forms.DockStyle.Right;
            else if (_settings.Position == MiniMapPosition.Left &&
                this.Dock == System.Windows.Forms.DockStyle.Right)
                this.Dock = System.Windows.Forms.DockStyle.Left;

            if (this.Width != _settings.Width)
                this.Width = _settings.Width;

            // Force update of the mini map since the settings have changed
            UpdateMiniMap(true);
        }

        void _settings_OnSettingsChanged()
        {
            RefreshSettings();
        }

        void _partnerControl_UpdateUI(ScintillaControl sender)
        {
            // Since this event occurs so often, only perform a minimal update if we are visible.
            if (this.Visible && !_updating)
                UpdateMiniMap(false);
        }

        private void UpdateTimer()
        {
            // No need to make updates if we are not visible
            if (_settings.IsVisible)
                _updateTimer.Start();
            else
                _updateTimer.Stop();
        }

        void _updateTimer_Tick(object sender, EventArgs e)
        {
            HandleMouseEvents();
            UpdateMiniMap(false);
        }

        void HandleMouseEvents()
        {
            // Check for mouse events the hard way since the scintilla control was not giving us
            // mouse events even when this.IsMouseDownCaptures is true.
            if (Control.MouseButtons == MouseButtons.Left)
            {
                // Get the mouse position relative to the client.
                Point screenPosition = new Point(Control.MousePosition.X, Control.MousePosition.Y);
                Point clientPosition = this.PointToClient(screenPosition);
                // Check that the mouse is clicking on us
                IntPtr activeHwnd = NativeMethods.WindowFromPoint(screenPosition);

                // if the mouse is clicking on us, is within the mini map bounds, 
                // and the click started within the mini map
                if (activeHwnd == this.Parent.Handle &&
                    this.ClientRectangle.Contains(clientPosition) &&
                    !_mouseDownOutside)
                {
                    // get the cursor position under the mouse
                    int position = this.PositionFromPoint(clientPosition.X, clientPosition.Y);
                    int centerLine = this.LineFromPosition(position);
                    // if it does not match our last position
                    if (_lastCenterLine != centerLine)
                    {
                        // Center the partner editor on the cursor line
                        CenterPartnerEditor(centerLine);

                        _lastCenterLine = centerLine;
                    }
                    _mouseDownInside = true;
                }
                else
                {
                    // The click didn't apply to us
                    if (!_mouseDownInside)
                        _mouseDownOutside = true;
                }
            }
            else if (_mouseDownInside || _mouseDownOutside)
            {
                // The mouse isn't down, so reset flags
                _mouseDownInside = false;
                _mouseDownOutside = false;
            }
        }

        bool UpdateFoldedCode()
        {
            bool changed = false;

            // Go line by line updating line visibility. Would prefer a CodeFoldsChanged event.
            for (int index = 0; index < _partnerEditor.LineCount; index++)
            {
                bool visible = GetLineVisible(this, index);
                bool parterVisible = GetLineVisible(_partnerEditor, index);

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

        private void CenterPartnerEditor(int line)
        {
            // Constrain the first visible line to a reasonable number
            int firstVisibleLine = Math.Min(Math.Max(line - (int)Math.Floor(_partnerEditor.LinesOnScreen / 2.0), 0), _partnerEditor.LineCount - _lastPartnerLinesOnScreen + 1);
            if (firstVisibleLine != _lastPartnerFirstLine)
            {
                // Calculate shift then scroll
                int delta = firstVisibleLine - _lastPartnerFirstLine - 1;
                _lastPartnerFirstLine = Math.Max(firstVisibleLine, 1);
                _partnerEditor.LineScroll(0, delta);
            }
        }

        private void UpdateMiniMap(bool forceUpdate)
        {
            if (!this.Visible)
                return;

            _updating = true;

            // If folded code has changed then perform a full update.
            if (UpdateFoldedCode())
                forceUpdate = true;

            // Update syntax language if changed
            if (this.ConfigurationLanguage != _partnerEditor.ConfigurationLanguage)
                this.ConfigurationLanguage = _partnerEditor.ConfigurationLanguage;

            // Update text if it has changed
            if (this.Text != _partnerEditor.Text)
            {
                this.Text = _partnerEditor.Text;

                forceUpdate = true;
            }

            // Get the default style font size
            int fontSize = GetDefaultFontSize();
            if (fontSize > _settings.FontSize)
            {
                // zoom level is the delta of the default and target font sizes
                int zoomLevel = -(fontSize - _settings.FontSize);
                if (this.ZoomLevel != zoomLevel)
                    this.ZoomLevel = zoomLevel;
            }

            int firstLine = _partnerEditor.FirstVisibleLine;
            int linesOnScreen = _partnerEditor.LinesOnScreen;

            // Check to see if we have scrolled or resized or...
            if (firstLine != _lastPartnerFirstLine ||
                linesOnScreen != _lastPartnerLinesOnScreen || 
                forceUpdate)
            {
                // Remove old line highlight
                this.MarkerDeleteAll(20);

                // Define the highlight marker
                this.MarkerSetBack(20, PluginCore.Utilities.DataConverter.ColorToInt32(_settings.HighlightColor));
                this.MarkerDefine(20, ScintillaNet.Enums.MarkerSymbol.Background);

                // Add the visible lines highlight
                int lastLine = this.DocLineFromVisible(firstLine + linesOnScreen);
                for (int line = firstLine; line < lastLine; line++)
                {
                    this.MarkerAdd(line, 20);
                }
                
                // When visible lines < line count, apply a continuous scroll so that all lines can be visible
                ScrollMiniMap();

                _lastPartnerFirstLine = firstLine;
                _lastPartnerLinesOnScreen = linesOnScreen;
                _lastCenterLine = _lastPartnerFirstLine + (int)Math.Floor(_lastPartnerLinesOnScreen / 2.0);
            }

            _updating = false;
        }

        private void ScrollMiniMap()
        {
            // If there is more lines that can fit on the screen
            if (this.LineCount > this.LinesOnScreen)
            {
                // Apply a continuous scroll, so that all lines will eventually be visible
                double percent = _lastPartnerFirstLine / Convert.ToDouble(_partnerEditor.LineCount - _lastPartnerLinesOnScreen);
                int line = (int)Math.Floor(percent * (this.LineCount - this.LinesOnScreen));
                int delta = line - this.FirstVisibleLine - 1;
                this.LineScroll(0, delta);
            }
        }

        private int GetDefaultFontSize()
        {
            if (PluginBase.MainForm == null || PluginBase.MainForm.SciConfig == null)
                return 0;
            
            Language language = PluginBase.MainForm.SciConfig.GetLanguage(this.ConfigurationLanguage);           
            if (language == null)
                return 0;
           
            // find the default style and return the font size for this syntax configuration
            foreach (UseStyle style in PluginBase.MainForm.SciConfig.GetLanguage(this.ConfigurationLanguage).usestyles)
                if (style.name == "default")
                    return style.FontSize;

            return 0;
        }
    }
}
