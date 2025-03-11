using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace r.e.p.o_cheat
{
    public static class MonoPatcher
    {
        private static string GameName = "REPO";

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        private static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, uint flNewProtect, out uint lpflOldProtect);

        private const int PROCESS_ALL_ACCESS = 0x1F0FFF;
        private const uint PAGE_EXECUTE_READWRITE = 0x40;

        private static void PatchFunction(IntPtr processHandle, IntPtr functionAddress, byte[] newBytes)
        {
            uint oldProtect;
            VirtualProtectEx(processHandle, functionAddress, newBytes.Length, PAGE_EXECUTE_READWRITE, out oldProtect);
            WriteProcessMemory(processHandle, functionAddress, newBytes, newBytes.Length, out _);
            VirtualProtectEx(processHandle, functionAddress, newBytes.Length, oldProtect, out _);
        }

        private static byte[] ReadOriginalBytes(IntPtr processHandle, IntPtr functionAddress, int length)
        {
            byte[] buffer = new byte[length];
            ReadProcessMemory(processHandle, functionAddress, buffer, length, out _);
            return buffer;
        }

        private static IntPtr GetMonoMethodAddress(IntPtr processHandle, string className, string methodName)
        {
            IntPtr classPointer = GetProcAddress(GetModuleHandle("mono-2.0-bdwgc.dll"), "mono_class_from_name");
            return classPointer;
        }

        public static void DisableMonoMethods(string className, string[] methods, out byte[][] originalBytes)
        {
            DisableMonoMethods(GameName, className, methods, out originalBytes);
        }

        public static void DisableMonoMethods(string processName, string className, string[] methods, out byte[][] originalBytes)
        {
            Process targetProcess = Process.GetProcessesByName(processName).FirstOrDefault();
            if (targetProcess == null)
            {
                Console.WriteLine("Game not found.");
                originalBytes = null;
                return;
            }

            IntPtr processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, targetProcess.Id);
            if (processHandle == IntPtr.Zero)
            {
                Console.WriteLine("Failed to open process.");
                originalBytes = null;
                return;
            }

            originalBytes = new byte[methods.Length][];
            for (int i = 0; i < methods.Length; i++)
            {
                IntPtr functionAddress = GetMonoMethodAddress(processHandle, className, methods[i]);
                if (functionAddress != IntPtr.Zero)
                {
                    originalBytes[i] = ReadOriginalBytes(processHandle, functionAddress, 1); // Store first byte
                    byte[] disableBytes = { 0xC3 }; // `RET` instruction
                    PatchFunction(processHandle, functionAddress, disableBytes);
                    Console.WriteLine($"Disabled {methods[i]} at {functionAddress}");
                }
            }

            Console.WriteLine("Mono methods disabled.");
        }

        public static void EnableMonoMethods(string className, string[] methods, byte[][] originalBytes)
        {
            EnableMonoMethods(GameName, className, methods, originalBytes);
        }

        public static void EnableMonoMethods(string processName, string className, string[] methods, byte[][] originalBytes)
        {
            Process targetProcess = Process.GetProcessesByName(processName).FirstOrDefault();
            if (targetProcess == null)
            {
                Console.WriteLine("Game not found.");
                return;
            }

            IntPtr processHandle = OpenProcess(PROCESS_ALL_ACCESS, false, targetProcess.Id);
            if (processHandle == IntPtr.Zero)
            {
                Console.WriteLine("Failed to open process.");
                return;
            }

            for (int i = 0; i < methods.Length; i++)
            {
                IntPtr functionAddress = GetMonoMethodAddress(processHandle, className, methods[i]);
                if (functionAddress != IntPtr.Zero)
                {
                    PatchFunction(processHandle, functionAddress, originalBytes[i]);
                    Console.WriteLine($"Re-enabled {methods[i]} at {functionAddress}");
                }
            }
        }
    }
}
