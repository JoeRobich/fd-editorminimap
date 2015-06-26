# Editor MiniMap for FlashDevelop 5

Renders a miniature version of the open file in a bar to the side of the editor. The current visible lines are highlighted so you know exactely where in your code you are. Click on the map to scroll quickly through your code. Configure the highlight color, map width, map placement, map font size and more.

![Screenshot](http://dl.dropbox.com/u/3917850/images/editorminimap.png)  

New Code Preview popup on mouse hover.  

![Code Preview](http://dl.dropbox.com/u/3917850/images/editorminimap-codepreview.png)  

## Download
[Releases](https://github.com/JoeRobich/fd-editorminimap/releases/) 

## History
**v1.6** - Fixed scrolling when file is longer than will render in the minimap. Changed how MiniMap works with the split editor. Changed highlighting of visible screen rows to match VS.  
**v1.5.1** - Fixed Auto-scroll by ignoring empty lines. Removed ToolStripButton for toggling MiniMap.  
**v1.5** - Disable Preprocessor lexing, resolves #1. Automatically scroll code preview based on indentation.  
**v1.4** - Fixed bug with non-en_US locales. Better support for high DPI. Updated to work with FD5.  
**v1.3.1** - Fixed issue with single click when not showing code preview (reported by mrpinc).
**v1.3** - Mouse scroll wheel can now be used to scoll the MiniMap for long files.  
**v1.2** - MiniMap scrolls when dragging text over it. When in split view the scrolls the editor the drag was started from.  
**v1.1** - Code preview follows the mouse until click or leave. Fixed bugs reported by mrpinc.  
**v1.0.1** - Fixed bug when clicking the MiniMap in a large file registers two clicks.  
**v1.0** - Right mouse button now scrolls split editor. Code preview popup when hovering.  
**v0.9.5** - Added support for split view which highlights both visible areas.  
**v0.9.4** - Now targets the .Net 2.0 Framework.  
**v0.9.3** - Fixed issue when scrolling with collapsed code. Fixed issue when removing because of too many lines of code.  
**v0.9.2** - Fixed issue with additional maps being added when a file was moved.  
**v0.9.1** - Changed Dispose order to avoid update events after disposed.  
**v0.9** - Added optional toolbar button for toggling the mini map. If you have problems with large files, I added a configurable line limit. Fixed some synchronization and stability issues.  
**v0.8** - Lots of changes to improve scrolling and performance.  
**v0.1** - Initial creation.  

## Thanks go to

- The FlashDevelop team for making an awesome product and being very helpful in the forums (http://flashdevelop.org/)
- Everyone who has submitted a bug report or suggestion (mrpinc)