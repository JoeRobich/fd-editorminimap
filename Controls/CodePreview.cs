using PluginCore;
using PluginCore.Helpers;
using ScintillaNet;
using ScintillaNet.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace EditorMiniMap
{
    public class CodePreview : Form
    {
        private ITabbedDocument Document { get; set; }
        private ScintillaControl Editor { get; set; }

        public CodePreview(ITabbedDocument document)
        {
            this.Document = document;
            this.Editor = new ScintillaControl();

            InitializeControls();
            SetupEditor();
        }

        private void InitializeControls()
        {
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.ShowInTaskbar = false;
            this.Width = ScaleHelper.Scale(400);
            this.Height = ScaleHelper.Scale(150);
            this.Controls.Add(this.Editor);
        }

        private void SetupEditor()
        {
            this.Editor.Top = 4;
            this.Editor.Left = 4;
            this.Editor.Height = this.Height - 8;
            this.Editor.Width = this.Width - 8;

            // Make non-editable
            this.Editor.Enabled = false;
            // Hide scrollbars
            this.Editor.IsHScrollBar = false;
            this.Editor.IsVScrollBar = false;

            this.Editor.SetMarginWidthN(1, 0);

            this.Editor.Text = Document.SplitSci1.Text;
            this.Editor.TabWidth = Document.SplitSci1.TabWidth;

            // If language has changed then, update syntax language and zoom level
            this.Editor.ConfigurationLanguage = Document.SplitSci1.ConfigurationLanguage;
            /* Language language = GetLanguage(Document.SplitSci1.ConfigurationLanguage);

            // Get the default style font size
            int fontSize = GetDefaultFontSize(language);
            if (fontSize > _settings.FontSize)
            {
                // zoom level is the delta of the default and target font sizes
                int zoomLevel = -(fontSize - _settings.FontSize);
                if (this.ZoomLevel != zoomLevel)
                    this.ZoomLevel = zoomLevel;
            } */

            this.Editor.SetProperty("lexer.cpp.track.preprocessor", "0");
        }

        private void UpdateFoldedCode()
        {
            // Go line by line updating line visibility. Would prefer a CodeFoldsChanged event.
            for (int index = 0; index < Document.SplitSci1.LineCount; index++)
            {
                bool visible = GetLineVisible(Document.SplitSci1, index);
                bool editorVisible = GetLineVisible(this.Editor, index);

                // if visibility doesn't match the update
                if (editorVisible && !visible)
                {
                    this.Editor.ShowLines(index, index);
                }
                else if (!editorVisible && visible)
                {
                    this.Editor.HideLines(index, index);
                }
            }
        }

        private bool GetLineVisible(ScintillaControl sci, int line)
        {
            // for some reason IsLineVisible was a property, but there was no way to specify which line...
            return sci.SlowPerform(2228, (uint)line, 0) != 0;
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

        public void CenterEditor(int line)
        {
            int linesOnScreen = this.Editor.LinesOnScreen;
            int lineCount = this.Editor.LineCount;
            int firstLine = this.Editor.FirstVisibleLine;

            // Constrain the first visible line to a reasonable number
            int firstVisibleLine = Math.Min(Math.Max(line - (int)Math.Floor(linesOnScreen / 2.0), 0), lineCount - linesOnScreen + 1);

            // Calculate shift then scroll
            int delta = firstVisibleLine - firstLine - 1;
            this.Editor.LineScroll(-5000, delta);

            firstLine = this.Editor.FirstVisibleLine;

            var column = int.MaxValue;
            for (var offset = 0; offset < this.Editor.LinesOnScreen; offset++)
            {
                var lineIndentation = this.Editor.GetLineIndentation(firstLine + offset);
                if (lineIndentation == 0 && IsNullOrWhiteSpace(Editor.GetLine(firstLine + offset)))
                    continue;

                column = Math.Min(column, lineIndentation);
            }

            this.Editor.LineScroll(column - 2, 0);
        }

        static bool IsNullOrWhiteSpace(string value)
        {
            if (value == null) return true;

            for (int i = 0; i < value.Length; i++)
            {
                if (!char.IsWhiteSpace(value[i])) return false;
            }

            return true;
        }
    }
}