using System;
using System.IO;
using System.Windows.Forms;
using System.ComponentModel;
using WeifenLuo.WinFormsUI.Docking;
using PluginCore.Localization;
using PluginCore.Utilities;
using PluginCore.Managers;
using PluginCore.Helpers;
using PluginCore;
using System.Drawing;
using System.Runtime.InteropServices;

namespace EditorMiniMap
{
	public class PluginMain : IPlugin
	{
        private String pluginName = "EditorMiniMap";
        private String pluginGuid = "1F4B503B-E512-4a2d-B80D-15C87DD6D5FA";
        private String pluginHelp = "www.flashdevelop.org/community/";
        private String pluginDesc = "Adds a minimap to code documents.";
        private String pluginAuth = "Joey Robichaud";
        private String settingFilename = "";
        private Settings settingObject;

	    #region Required Properties

        /// <summary>
        /// Api level of the plugin
        /// </summary>
        public Int32 Api
        {
            get { return 1; }
        }

        /// <summary>
        /// Name of the plugin
        /// </summary> 
        public String Name
		{
			get { return this.pluginName; }
		}

        /// <summary>
        /// GUID of the plugin
        /// </summary>
        public String Guid
		{
			get { return this.pluginGuid; }
		}

        /// <summary>
        /// Author of the plugin
        /// </summary> 
        public String Author
		{
			get { return this.pluginAuth; }
		}

        /// <summary>
        /// Description of the plugin
        /// </summary> 
        public String Description
		{
			get { return this.pluginDesc; }
		}

        /// <summary>
        /// Web address for help
        /// </summary> 
        public String Help
		{
			get { return this.pluginHelp; }
		}

        /// <summary>
        /// Object that contains the settings
        /// </summary>
        [Browsable(false)]
        public Object Settings
        {
            get { return this.settingObject; }
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
            this.AddShortCuts();
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
                DockContent content = PluginBase.MainForm.CurrentDocument as DockContent;
                if (content == null)
                    return;

                ScintillaNet.ScintillaControl sci = PluginBase.MainForm.CurrentDocument.SciControl;
                if (sci == null)
                    return;

                ScintillaMiniMap miniMap = new ScintillaMiniMap(sci, settingObject);
                content.Controls.Add(miniMap);
            }
            else if (e.Type == EventType.FileSwitch)
            {
                DockContent content = PluginBase.MainForm.CurrentDocument as DockContent;
                if (content == null)
                    return;

                ScintillaMiniMap miniMap = GetMiniMap(content);
                if (miniMap == null)
                    return;

                miniMap.RefreshSettings();
            }
		}
		
		#endregion

        #region Plugin Methods

        private ScintillaMiniMap GetMiniMap(DockContent content)
        {
            foreach (Control control in content.Controls)
            {
                if (control is ScintillaMiniMap)
                {
                    return control as ScintillaMiniMap;
                }
            }
            return null;
        }

        private void ShowMiniMap(object sender, EventArgs e)
        {
            this.settingObject.IsVisible = !this.settingObject.IsVisible;
            ((ToolStripMenuItem)sender).Checked = this.settingObject.IsVisible;
        }

        #endregion

        #region Custom Methods

        /// <summary>
        /// Initializes important variables
        /// </summary>
        public void InitBasics()
        {
            String dataPath = Path.Combine(PathHelper.DataDir, pluginName);
            if (!Directory.Exists(dataPath)) Directory.CreateDirectory(dataPath);
            this.settingFilename = Path.Combine(dataPath, "Settings.fdb");
        }

        /// <summary>
        /// Adds the required event handlers
        /// </summary> 
        public void AddEventHandlers()
        {
            // Set events you want to listen (combine as flags)
            EventManager.AddEventHandler(this, EventType.FileOpen | EventType.FileSwitch);
        }

        /// <summary>
        /// Adds shortcuts for manipulating the mini map
        /// </summary>
        public void AddShortCuts()
        {
            ToolStripMenuItem menu = (ToolStripMenuItem)PluginBase.MainForm.FindMenuItem("ViewMenu");
            ToolStripMenuItem menuItem;

            menuItem = new ToolStripMenuItem("Show &MiniMap", null, new EventHandler(ShowMiniMap));
            menuItem.Checked = this.settingObject.IsVisible;
            PluginBase.MainForm.RegisterShortcutItem("EditorMiniMap.ShowMiniMap", menuItem);

            int index = menu.DropDownItems.IndexOfKey("FullScreen") + 1;
            index = index > -1 ? index : 0;

            if (!(menu.DropDownItems[index] is ToolStripSeparator))
                menu.DropDownItems.Insert(index, new ToolStripSeparator());
 
            menu.DropDownItems.Insert(index + 1, menuItem);
        }

        /// <summary>
        /// Loads the plugin settings
        /// </summary>
        public void LoadSettings()
        {
            this.settingObject = new Settings();
            if (!File.Exists(this.settingFilename)) this.SaveSettings();
            else
            {
                Object obj = ObjectSerializer.Deserialize(this.settingFilename, this.settingObject);
                this.settingObject = (Settings)obj;
            }
        }

        /// <summary>
        /// Saves the plugin settings
        /// </summary>
        public void SaveSettings()
        {
            ObjectSerializer.Serialize(this.settingFilename, this.settingObject);
        }

		#endregion

	}
}