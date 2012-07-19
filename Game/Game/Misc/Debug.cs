using System;

namespace IsoGame.Misc
{
    public class Debug
    {
        public static void Write(bool display, string message)
        {
            if (display)
                Console.WriteLine(message);
        }
    }
}
