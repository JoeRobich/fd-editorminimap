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
        private ScintillaControl _partnerControl = null;
        private int _lastFirstVisibleLine = -1;
        private int _lastLinesOnScreen = -1;
        private int _lastPosition = -1;
        private int _lastAnchor = -1;
        private bool _updating = false;
        private Settings _settings = null;
        private Timer _updateTimer;

        public ScintillaMiniMap(ScintillaControl partnerControl, Settings settings)
        {
            this.Visible = settings.IsVisible;
            this.Width = settings.Width;
            this.Dock = settings.Position == MiniMapPosition.Right ? System.Windows.Forms.DockStyle.Right :
                                                                     System.Windows.Forms.DockStyle.Left;

            // Make non-editable
            this.IsReadOnly = true;
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

            this.CaretStyle = (int)ScintillaNet.Enums.CaretStyle.Invisible;
            this.IsMouseDownCaptures = true;
            this.IsBraceMatching = false;
            this.UsePopUp(false);

            _updateTimer = new Timer();
            _updateTimer.Interval = 100;

            _settings = settings;
            _partnerControl = partnerControl;

            HookEvents();
            UpdateTimer();
        }

        private void HookEvents()
        {
            _updateTimer.Tick += new EventHandler(_updateTimer_Tick);
            _settings.OnSettingsChanged += new SettingsChangesEvent(_settings_OnSettingsChanged);
            _partnerControl.UpdateUI += new UpdateUIHandler(_partnerControl_UpdateUI);
        }

        private void UnhookEvents()
        {
            _updateTimer.Tick -= new EventHandler(_updateTimer_Tick);
            _settings.OnSettingsChanged -= new SettingsChangesEvent(_settings_OnSettingsChanged);

            if (!_settings.OnlyUpdateOnTimer)
                _partnerControl.UpdateUI -= new UpdateUIHandler(_partnerControl_UpdateUI);
        }

        void _partnerControl_UpdateUI(ScintillaControl sender)
        {
            if (this.Visible && !_updating)
                UpdateMiniMap(false);
        }

        private void UpdateTimer()
        {
            if (_partnerControl != null)
            {
                if (this.Visible)
                    _updateTimer.Start();
                else
                    _updateTimer.Stop();
            }
        }

        void  _updateTimer_Tick(object sender, EventArgs e)
        {
            bool forceUpdate = false;

            if (UpdateVisibleLines())
                forceUpdate = true;

            if (this.Text != _partnerControl.Text)
                forceUpdate = true;

            if (Control.MouseButtons == MouseButtons.Left)
            {
                Point mousePosition = new Point(Control.MousePosition.X, Control.MousePosition.Y);
                Point cursorPosition = this.PointToClient(mousePosition);
                if (this.ClientRectangle.Contains(cursorPosition))
                {
                    int position = this.PositionFromPoint(cursorPosition.X, cursorPosition.Y);

                    if (_lastPosition != position)
                    {
                        _lastPosition = position;

                        int scrollLine = this.LineFromPosition(position);
                        ScrollHighlight(scrollLine);

                        if (this.SelectionStart != this.SelectionEnd)
                            this.SetSel(position, position);
                    }
                }
            }

            UpdateMiniMap(forceUpdate);
        }

        bool UpdateVisibleLines()
        {
            bool changed = false;

            for (int index = 0; index < _partnerControl.LineCount; index++)
            {
                bool visible = GetLineVisible(this, index);
                bool parentVisible = GetLineVisible(_partnerControl, index);
                if (parentVisible && !visible)
                {
                    this.ShowLines(index, index);
                    changed = true;
                }
                else if (!parentVisible && visible)
                {
                    this.HideLines(index, index);
                    changed = true;
                }
            }

            return changed;
        }

        void _settings_OnSettingsChanged()
        {
            if (this.Visible != _settings.IsVisible)
            { 
                this.Visible = _settings.IsVisible;

                if (this.Visible)
                    UpdateVisibleLines();

                UpdateTimer();
            }

            _partnerControl.UpdateUI -= new UpdateUIHandler(_partnerControl_UpdateUI);
            if (!_settings.OnlyUpdateOnTimer)
                _partnerControl.UpdateUI += new UpdateUIHandler(_partnerControl_UpdateUI);

            if (_settings.Position == MiniMapPosition.Right &&
                this.Dock == System.Windows.Forms.DockStyle.Left)
                this.Dock = System.Windows.Forms.DockStyle.Right;
            else if (_settings.Position == MiniMapPosition.Left &&
                this.Dock == System.Windows.Forms.DockStyle.Right)
                this.Dock = System.Windows.Forms.DockStyle.Left;

            if (this.Width != _settings.Width)
                this.Width = _settings.Width;
        
            UpdateMiniMap(true);
        }

        private void ScrollHighlight(int line)
        {
            int firstVisibleLine = Math.Min(Math.Max(line - GetHalfVisibleLines(_partnerControl), 0), _partnerControl.LineCount - _lastLinesOnScreen + 1);
            if (firstVisibleLine != _lastFirstVisibleLine)
            {
                int delta = firstVisibleLine - _lastFirstVisibleLine - 1;
                _lastFirstVisibleLine = Math.Max(firstVisibleLine, 1);
                _partnerControl.LineScroll(0, delta);
            }
        }

        private void UpdateMiniMap(bool forceUpdate)
        {
            if (!this.Visible)
                return;

            _updating = true;

            // Update Language
            if (this.ConfigurationLanguage != _partnerControl.ConfigurationLanguage)
                this.ConfigurationLanguage = _partnerControl.ConfigurationLanguage;

            if (this.Text != _partnerControl.Text)
            {
                this.IsReadOnly = false;
                this.Text = _partnerControl.Text;
                this.IsReadOnly = true;

                int centerLine = _partnerControl.FirstVisibleLine + GetHalfVisibleLines(_partnerControl);
                this.GotoLine(centerLine);
                _lastPosition = this.CurrentPos;
                _lastAnchor = this.CurrentPos;
                forceUpdate = true;
            }

            // Update Zoom Level
            int fontSize = GetDefaultFontSize();
            if (fontSize > _settings.FontSize)
            {
                int zoomLevel = -(fontSize - _settings.FontSize);
                if (this.ZoomLevel != zoomLevel)
                    this.ZoomLevel = zoomLevel;
            }

            // Check to see if we have scrolled or resized or...
            int firstLine = _partnerControl.FirstVisibleLine;
            int numberLines = _partnerControl.LinesOnScreen;

            if (firstLine != _lastFirstVisibleLine ||
                numberLines != _lastLinesOnScreen || 
                forceUpdate)
            {
                // Update Visible Line Highlights
                this.MarkerDeleteAll(20);
                this.MarkerSetBack(20, PluginCore.Utilities.DataConverter.ColorToInt32(_settings.HighlightColor));
                this.MarkerDefine(20, ScintillaNet.Enums.MarkerSymbol.Background);
                int lastLine = this.DocLineFromVisible(firstLine + numberLines);
                for (int line = firstLine; line < lastLine; line++)
                {
                    this.MarkerAdd(line, 20);
                }

                ScrollMiniMap();

                _lastFirstVisibleLine = firstLine;
                _lastLinesOnScreen = numberLines;
            }

            _updating = false;
        }

        private void ScrollMiniMap()
        {
            if (this.LineCount > this.LinesOnScreen)
            {
                double percent = _lastFirstVisibleLine / Convert.ToDouble(_partnerControl.LineCount - _lastLinesOnScreen);
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
           
            foreach (UseStyle style in PluginBase.MainForm.SciConfig.GetLanguage(this.ConfigurationLanguage).usestyles)
                if (style.name == "default")
                    return style.FontSize;

            return 0;
        }

        private int GetHalfVisibleLines(ScintillaControl sci)
        {
            return (int)Math.Floor(Convert.ToDouble(sci.LinesOnScreen) / 2);
        }

        private bool GetLineVisible(ScintillaControl sci, int line)
        {
            return sci.SlowPerform(2228, (uint)line, 0) != 0;
        }
    }
}
