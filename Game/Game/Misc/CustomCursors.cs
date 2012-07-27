using System.Windows.Forms;
// For the NativeMethods helper class:
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Reflection;
using System;

namespace IsoGame.Misc
{
    public class CustomCursors
    {
        public static Cursor Attack = LoadCustomCursor(@"Content\Cursors\Roundling\RedSightLink.ani");
        public static Cursor No = LoadCustomCursor(@"Content\Cursors\Roundling\Unavailable.ani");
        public static Cursor Select = LoadCustomCursor(@"Content\Cursors\Roundling\Normal.ani");
        public static Cursor Normal = LoadCustomCursor(@"Content\Cursors\Roundling\Move.ani");
        public static Cursor Busy = LoadCustomCursor(@"Content\Cursors\Roundling\Busy.ani");

        private CustomCursors()
        {
        }

        public static Cursor LoadCustomCursor(string path)
        {
            IntPtr hCurs = LoadCursorFromFile(path);
            if (hCurs == IntPtr.Zero) throw new Win32Exception();
            var curs = new Cursor(hCurs);
            // Note: force the cursor to own the handle so it gets released properly
            var fi = typeof(Cursor).GetField("ownHandle", BindingFlags.NonPublic | BindingFlags.Instance);
            fi.SetValue(curs, true);
            return curs;
        }
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr LoadCursorFromFile(string path);
    }
}
