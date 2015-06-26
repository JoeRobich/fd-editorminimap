using PluginCore;
using PluginCore.Helpers;
using ScintillaNet;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace EditorMiniMap
{
    public class MiniMapPanel : Panel
    {
        public ScintillaMiniMap MiniMap { get; private set; }
        private ITabbedDocument Document { get; set; }
        private ScintillaControl ScintillaControl { get; set; }
        private Settings Settings { get; set; }
        private bool MouseButtonDown { get; set; }
        private Point MouseHoverPosition { get; set; }

        public MiniMapPanel(ITabbedDocument document, ScintillaControl sci, Settings settings)
        {
            this.Document = document;
            this.ScintillaControl = sci;
            this.Settings = settings;

            InitializeControl();
            RefreshSettings();
            HookEvents();
        }

        private void InitializeControl()
        {
            // These will get corrected in RefreshSettings
            this.Width = ScaleHelper.Scale(200);
            this.Dock = DockStyle.Right;
            var padding = ScaleHelper.Scale(2);
            this.Padding = new Padding(padding, 0, padding, 0);

            this.AllowDrop = true;

            // Add the MiniMap
            this.MiniMap = new ScintillaMiniMap(this.ScintillaControl, this.Settings);
            this.MiniMap.Margin = new Padding(0);
            this.Controls.Add(MiniMap);

            this.ContextMenu = new ContextMenu();
        }

        private void HookEvents()
        {
            this.Settings.OnSettingsChanged += Settings_OnSettingsChanged;

            if (Document.SplitSci2 == ScintillaControl)
                Document.SplitContainer.Panel2.VisibleChanged += Document_SplitViewVisible;
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            var mouse = this.PointToClient(Control.MousePosition);
            this.MiniMap.OnDragOver(mouse);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            this.MiniMap.OnMouseWheel(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            this.MiniMap.OnMouseClick(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            this.MiniMap.OnMouseLeave();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            MouseButtonDown = true;
            this.MiniMap.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            this.MiniMap.OnMouseMove(e);

            if (!MouseHoverPosition.IsEmpty && !MouseHoverPosition.Equals(e.Location))
            {
                MouseHoverPosition = Point.Empty;
                this.MiniMap.OnMouseHoverEnd();
                this.ResetMouseEventArgs();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            MouseButtonDown = false;
            this.MiniMap.OnMouseUp(e);
        }

        protected override void OnMouseHover(EventArgs e)
        {
            if (!MouseButtonDown)
            {
                var mouse = this.PointToClient(Control.MousePosition);
                this.MiniMap.OnMouseHover(mouse);
                MouseHoverPosition = mouse;
            }
            else
                this.ResetMouseEventArgs();
        }

        private void UnhookEvents()
        {
            this.Settings.OnSettingsChanged -= Settings_OnSettingsChanged;
        }

        private void Document_SplitViewVisible(object sender, EventArgs args)
        {
            RefreshSettings();
        }

        private void Settings_OnSettingsChanged()
        {
            RefreshSettings();
        }

        public void RefreshSettings()
        {
            if (this.Visible != this.Settings.IsVisible)
                this.Visible = this.Settings.IsVisible;

            // Update dock position if necessary
            if (this.Settings.Position == MiniMapPosition.Right && this.Dock == DockStyle.Left)
                this.Dock = DockStyle.Right;
            else if (this.Settings.Position == MiniMapPosition.Left && this.Dock == DockStyle.Right)
                this.Dock = DockStyle.Left;

            if (this.Width != this.Settings.Width)
                this.Width = ScaleHelper.Scale(this.Settings.Width);

            this.BackColor = this.Settings.HighlightColor;

            this.MiniMap.RefreshSettings();
        }
    }
}