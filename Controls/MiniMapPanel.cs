using PluginCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace EditorMiniMap
{
    public class MiniMapPanel : Panel
    {
        public ScintillaMiniMap MiniMap { get; private set; }
        private ITabbedDocument Document { get; set; }
        private Settings Settings { get; set; }
        private bool MouseButtonDown { get; set; }
        private Point MouseHoverPosition { get; set; }

        public MiniMapPanel(ITabbedDocument document, Settings settings)
        {
            this.Document = document;
            this.Settings = settings;

            InitializeControl();
            RefreshSettings();
            HookEvents();
        }

        private void InitializeControl()
        {
            // These will get corrected in RefreshSettings
            this.Width = 200;
            this.Dock = DockStyle.Right;

            this.AllowDrop = true;

            // Add the MiniMap
            this.MiniMap = new ScintillaMiniMap(this.Document, this.Settings);
            this.Controls.Add(MiniMap);

            this.ContextMenu = new ContextMenu();
        }

        private void HookEvents()
        {
            this.Settings.OnSettingsChanged += Settings_OnSettingsChanged;
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            var mouse = this.PointToClient(Control.MousePosition);
            this.MiniMap.OnDragOver(mouse);
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
                this.Width = this.Settings.Width;

            this.MiniMap.RefreshSettings();
        }
    }
}