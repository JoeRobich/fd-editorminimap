using System;
using System.IO;
using System.Windows.Forms;
using System.ComponentModel;
using PluginCore.Localization;
using PluginCore.Utilities;
using PluginCore.Managers;
using PluginCore;
using System.Drawing;
using System.Runtime.InteropServices;
using EditorMiniMap.Helpers;

namespace EditorMiniMap
{
	public class PluginMain : IPlugin
	{
        private const int API_KEY = 1;
        private const String NAME = "EditorMiniMap";
        private const String GUID = "1F4B503B-E512-4a2d-B80D-15C87DD6D5FA";
        private const String HELP = "www.flashdevelop.org/community/viewtopic.php?f=4&t=9397";
        private const String DESCRIPTION = "Adds a minimap to code documents.";
        private const String AUTHOR = "Joey Robichaud";

        private String _settingsFilename = "";
        private Settings _settings = null;
        private ToolStripSeparator _toggleSeparator = null;
        private ToolStripMenuItem _toggleMenuItem = null;
        private ToolStripButton _toggleButton = null;

	    #region Required Properties

        /// <summary>
        /// Api level of the plugin
        /// </summary>
        public Int32 Api
        {
            get { return API_KEY; }
        }

        /// <summary>
        /// Name of the plugin
        /// </summary> 
        public String Name
		{
			get { return NAME; }
		}

        /// <summary>
        /// GUID of the plugin
        /// </summary>
        public String Guid
		{
			get { return GUID; }
		}

        /// <summary>
        /// Author of the plugin
        /// </summary> 
        public String Author
		{
			get { return AUTHOR; }
		}

        /// <summary>
        /// Description of the plugin
        /// </summary> 
        public String Description
		{
			get { return DESCRIPTION; }
		}

        /// <summary>
        /// Web address for help
        /// </summary> 
        public String Help
		{
			get { return HELP; }
		}

        /// <summary>
        /// Object that contains the settings
        /// </summary>
        [Browsable(false)]
        public Object Settings
        {
            get { return this._settings; }
        }
		
		#endregion

        #region Required Methods

        /// <summary>
		/// Initializes the plugin
		/// </summary>
		public void Initialize()
		{
            this.InitBasics();
            this.LoadSettings();
            this.CreateMenuItems();
            this.AddEventHandlers();
        }
		
		/// <summary>
		/// Disposes the plugin
		/// </summary>
		public void Dispose()
		{
            this.SaveSettings();
		}
		
		/// <summary>
		/// Handles the incoming events
		/// </summary>
		public void HandleEvent(Object sender, NotifyEvent e, HandlingPriority prority)
		{
            if (e.Type == EventType.FileOpen)
            {
                ITabbedDocument document = PluginBase.MainForm.CurrentDocument;
                if (document == null)
                    return;
                // If the main form does not contain a Scintilla control, 
                // then there is no need to add a mini map.
                ScintillaNet.ScintillaControl sci = document.SciControl;
                if (sci == null)
                    return;

                ScintillaMiniMap miniMap = GetMiniMap(document);
                if (miniMap != null)
                    return;

                miniMap = new ScintillaMiniMap(sci, document.SplitSci2, _settings);
                document.Controls.Add(miniMap);
            }
            else if (e.Type == EventType.FileSwitch)
            {
                // Scintilla control Visible property does not seem to respect getting set when
                // the control is not visible. So when switching to a new document, we have to
                // refresh the settings.
                ScintillaMiniMap miniMap = GetMiniMap(PluginBase.MainForm.CurrentDocument);
                if (miniMap == null)
                    return;

                miniMap.RefreshSettings();
            }
		}
		
		#endregion

        #region Plugin Methods

        private ScintillaMiniMap GetMiniMap(ITabbedDocument document)
        {
            if (document != null)
            {
                foreach (Control control in document.Controls)
                {
                    if (control is ScintillaMiniMap)
                    {
                        return control as ScintillaMiniMap;
                    }
                }
            }
            return null;
        }

        private void ToggleMiniMap(object sender, EventArgs e)
        {
            _settings.IsVisible = !_settings.IsVisible;
        }

        void settingObject_OnSettingsChanged()
        {
            if (_settings.ShowToolbarButton != _toggleButton.Visible)
            {
                _toggleButton.Visible = _settings.ShowToolbarButton;
                _toggleSeparator.Visible = _settings.ShowToolbarButton;
            }

            if (_settings.IsVisible != _toggleMenuItem.Checked)
            {
                _toggleMenuItem.Checked = _settings.IsVisible;
                _toggleButton.Checked = _settings.IsVisible;
            }
        }

        #endregion

        #region Custom Methods

        /// <summary>
        /// Initializes important variables
        /// </summary>
        public void InitBasics()
        {
            String dataPath = Path.Combine(PluginCore.Helpers.PathHelper.DataDir, NAME);
            if (!Directory.Exists(dataPath)) Directory.CreateDirectory(dataPath);
            _settingsFilename = Path.Combine(dataPath, "Settings.fdb");
        }

        /// <summary>
        /// Adds the required event handlers
        /// </summary> 
        public void AddEventHandlers()
        {
            // Set events you want to listen (combine as flags)
            EventManager.AddEventHandler(this, EventType.FileOpen | EventType.FileSwitch);
            _settings.OnSettingsChanged += new SettingsChangesEvent(settingObject_OnSettingsChanged);
        }

        /// <summary>
        /// Adds shortcuts for manipulating the mini map
        /// </summary>
        public void CreateMenuItems()
        {
            ToolStripMenuItem menu = (ToolStripMenuItem)PluginBase.MainForm.FindMenuItem("ViewMenu");

            _toggleMenuItem = new ToolStripMenuItem(ResourceHelper.GetString("EditorMiniMap.Label.ToggleMiniMap"), ResourceHelper.GetImage("MiniMap"), new EventHandler(ToggleMiniMap));
            _toggleMenuItem.Checked = _settings.IsVisible;
            PluginBase.MainForm.RegisterShortcutItem("EditorMiniMap.ShowMiniMap", _toggleMenuItem);

            int index = menu.DropDownItems.IndexOfKey("FullScreen") + 1;
            index = index > -1 ? index : 0;

            if (!(menu.DropDownItems[index] is ToolStripSeparator))
                menu.DropDownItems.Insert(index, new ToolStripSeparator());

            menu.DropDownItems.Insert(index + 1, _toggleMenuItem);

            _toggleButton = new ToolStripButton(ResourceHelper.GetString("EditorMiniMap.Description.ToggleMiniMap"), ResourceHelper.GetImage("MiniMap"), new EventHandler(ToggleMiniMap));
            _toggleButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            _toggleButton.ToolTipText = ResourceHelper.GetString("EditorMiniMap.Description.ToggleMiniMap");
            _toggleButton.Checked = _settings.IsVisible;
            _toggleButton.Visible = _settings.ShowToolbarButton;

            _toggleSeparator = new ToolStripSeparator();
            _toggleSeparator.Visible = _settings.ShowToolbarButton;

            PluginBase.MainForm.ToolStrip.Items.Add(_toggleSeparator);
            PluginBase.MainForm.ToolStrip.Items.Add(_toggleButton);
        }

        /// <summary>
        /// Loads the plugin settings
        /// </summary>
        public void LoadSettings()
        {
            _settings = new Settings();
            if (!File.Exists(_settingsFilename)) this.SaveSettings();
            else
            {
                Object obj = ObjectSerializer.Deserialize(_settingsFilename, _settings);
                _settings = (Settings)obj;
                _settings.MaxLineLimit = _settings.MaxLineLimit;
            }
        }

        /// <summary>
        /// Saves the plugin settings
        /// </summary>
        public void SaveSettings()
        {
            ObjectSerializer.Serialize(_settingsFilename, _settings);
        }

		#endregion
	}
}