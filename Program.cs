using System;

namespace plist_extract
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0) {
                TexturePacker.Extract(args[0]);
            }
            else {
                Console.WriteLine("please enter the folder or file you want to extract.");
            }
        }
    }
}
