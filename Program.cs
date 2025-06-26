/*
 * program.cs 
 * 
 * replicates the unix LC command
 * 
 *  Date        Author          Description
 *  ====        ======          ===========
 *  06-26-25    Craig           initial implementation
 *
 */

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace DFClone
{
    class Program
    {
        static bool useExact = false;

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            EnableVirtualTerminal();

            foreach (var arg in args)
            {
                if (arg == "-x") useExact = true;
                else if (arg == "-?" || arg == "--help")
                {
                    ShowHelp();
                    return;
                }
                else
                {
                    Console.Error.WriteLine($"Unknown option: {arg}");
                    return;
                }
            }

            var drives = DriveInfo.GetDrives().Where(d => d.IsReady).ToList();
            PrintHeader();
            foreach (var drive in drives)
                PrintDrive(drive);
        }

        static void PrintHeader()
        {
            if (useExact)
            {
                Console.WriteLine("{0,-16}{1,12}{2,12}{3,12} 4,5}  {5}",
                    "Drive", "Total(KB)", "Used(KB)", "Free(KB)", "Use%", "Mount");
            }
            else
            {
                Console.WriteLine("{0,-16}{1,12}{2,12}{3,12} {4,5}  {5}",
                    "Drive", "Total", "Used", "Free", "Use%", "Mount");
            }
        }

        static void PrintDrive(DriveInfo drive)
        {
            long total = drive.TotalSize;
            long free = drive.TotalFreeSpace;
            long used = total - free;
            double pctUsed = total > 0 ? (double)used / total * 100 : 0;

            string driveColor = GetDriveColor(drive.DriveType);
            string usageColor = GetUsageColor(pctUsed);

            string driveLabel = $"{driveColor}{drive.Name,-16}\x1b[0m";

            string totalStr, usedStr, freeStr;
            if (useExact)
            {
                totalStr = $"{total / 1024,12}";
                usedStr = $"{used / 1024,12}";
                freeStr = $"{free / 1024,12}";
            }
            else
            {
                totalStr = $"{FormatSize(total),12}";
                usedStr = $"{FormatSize(used),12}";
                freeStr = $"{FormatSize(free),12}";
            }

            string percent = $"{usageColor}{Math.Floor(pctUsed),3}%\x1b[0m".PadLeft(5);
            string mount = drive.RootDirectory.FullName.TrimEnd('\\');

            Console.WriteLine($"{driveLabel}{totalStr}{usedStr}{freeStr}  {percent}  {mount}");
        }

        static string FormatSize(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = bytes;
            int unit = 0;
            while (size >= 1024 && unit < units.Length - 1)
            {
                size /= 1024;
                unit++;
            }
            return $"{size:0.##} {units[unit]}";
        }

        static string GetDriveColor(DriveType type)
        {
            return type switch
            {
                DriveType.Removable => "\x1b[36m", // Cyan
                DriveType.Network => "\x1b[35m", // Magenta
                DriveType.CDRom => "\x1b[34m", // Blue
                DriveType.Fixed => "\x1b[37m", // White
                _ => "\x1b[2m",  // Dim
            };
        }

        static string GetUsageColor(double pct)
        {
            if (pct >= 80) return "\x1b[31m"; // Red
            if (pct >= 50) return "\x1b[33m"; // Yellow
            return "\x1b[32m";                // Green
        }

        static void ShowHelp()
        {
            Console.WriteLine(
@"Usage: df [-x]
  -x     : show sizes in KB (exact mode)
  -?     : display this help

All mounted and ready volumes are shown, including CD-ROM, removable, and network drives.
Drive name is colored by type; usage percent is colored by fill level.");
        }

        static void EnableVirtualTerminal()
        {
            const int STD_OUTPUT_HANDLE = -11;
            var handle = GetStdHandle(STD_OUTPUT_HANDLE);
            GetConsoleMode(handle, out int mode);
            SetConsoleMode(handle, mode | 0x0004); // ENABLE_VIRTUAL_TERMINAL_PROCESSING
        }

        [DllImport("kernel32.dll")] static extern IntPtr GetStdHandle(int nStdHandle);
        [DllImport("kernel32.dll")] static extern bool GetConsoleMode(IntPtr hConsoleHandle, out int lpMode);
        [DllImport("kernel32.dll")] static extern bool SetConsoleMode(IntPtr hConsoleHandle, int dwMode);
    }
}

