using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace AshClicker
{
    public class InterceptionControl
    {
        // 临时 DLL 文件路径
        private static string tempDllPath;

        // DLL Handle
        private static IntPtr dllHandle = IntPtr.Zero;

        // Delegate definitions for Interception functions
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr InterceptionCreateContextDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void InterceptionDestroyContextDelegate(IntPtr context);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int InterceptionReceiveDelegate(IntPtr context, int device, IntPtr stroke, uint timeout);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int InterceptionSendDelegate(IntPtr context, int device, IntPtr stroke, uint numStrokes);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int InterceptionIsKeyboardDelegate(int device);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int InterceptionIsMouseDelegate(int device);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int InterceptionWaitDelegate(IntPtr context, uint milliseconds);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void InterceptionSetFilterDelegate(IntPtr context, IntPtr predicate, ushort filter);

        // Interception function pointers
        private static InterceptionCreateContextDelegate interception_create_context;
        private static InterceptionDestroyContextDelegate interception_destroy_context;
        private static InterceptionReceiveDelegate interception_receive;
        private static InterceptionSendDelegate interception_send;
        private static InterceptionIsKeyboardDelegate interception_is_keyboard;
        private static InterceptionIsMouseDelegate interception_is_mouse;
        private static InterceptionWaitDelegate interception_wait_with_timeout;
        private static InterceptionSetFilterDelegate interception_set_filter;
        private static IntPtr interception_is_keyboard_pointer;
        private static IntPtr interception_is_mouse_pointer;

        /// <summary>INTERCEPTION_FILTER_KEY_ALL</summary>
        public const ushort FilterKeyAll = 0xFFFF;

        /// <summary>INTERCEPTION_FILTER_KEY_NONE</summary>
        public const ushort FilterKeyNone = 0x0000;

        /// <summary>INTERCEPTION_FILTER_MOUSE_NONE</summary>
        public const ushort FilterMouseNone = 0x0000;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        // Context for Interception
        private IntPtr _context;

        public InterceptionControl()
        {
            tempDllPath = TempDllManager.ExtractTempDll();
            dllHandle = LoadLibrary(tempDllPath);
            if (dllHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to load DLL. Error: {Marshal.GetLastWin32Error()}");
            }

            interception_create_context =
                GetFunctionDelegate<InterceptionCreateContextDelegate>("interception_create_context");
            interception_destroy_context =
                GetFunctionDelegate<InterceptionDestroyContextDelegate>("interception_destroy_context");
            interception_receive = GetFunctionDelegate<InterceptionReceiveDelegate>("interception_receive");
            interception_send = GetFunctionDelegate<InterceptionSendDelegate>("interception_send");
            interception_is_keyboard = GetFunctionDelegate<InterceptionIsKeyboardDelegate>("interception_is_keyboard");
            interception_is_mouse = GetFunctionDelegate<InterceptionIsMouseDelegate>("interception_is_mouse");
            interception_wait_with_timeout =
                GetFunctionDelegate<InterceptionWaitDelegate>("interception_wait_with_timeout");
            interception_set_filter = GetFunctionDelegate<InterceptionSetFilterDelegate>("interception_set_filter");
            interception_is_keyboard_pointer = GetProcAddress(dllHandle, "interception_is_keyboard");
            interception_is_mouse_pointer = GetProcAddress(dllHandle, "interception_is_mouse");

            _context = interception_create_context();
            if (_context == IntPtr.Zero)
            {
                throw new Exception("Failed to create Interception context.");
            }
            else
            {
                Console.WriteLine("Interception loaded successfully.");
            }
        }

        public static void Cleanup()
        {
            // 释放 DLL
            if (dllHandle != IntPtr.Zero)
            {
                FreeLibrary(dllHandle);
                dllHandle = IntPtr.Zero;
            }

            // 删除临时 DLL 文件
            if (!string.IsNullOrEmpty(tempDllPath))
            {
                TempDllManager.CleanupTempDll(tempDllPath);
                tempDllPath = null;
            }
        }

        private static T GetFunctionDelegate<T>(string functionName) where T : Delegate
        {
            IntPtr procAddress = GetProcAddress(dllHandle, functionName);
            if (procAddress == IntPtr.Zero)
            {
                throw new InvalidOperationException(
                    $"Failed to get address of function '{functionName}'. Error: {Marshal.GetLastWin32Error()}");
            }

            return Marshal.GetDelegateForFunctionPointer<T>(procAddress);
        }

        ~InterceptionControl()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_context != IntPtr.Zero)
            {
                interception_destroy_context(_context);
                _context = IntPtr.Zero;
            }

            Cleanup();
        }

        public bool IsKeyboard(int device)
        {
            return interception_is_keyboard(device) != 0;
        }

        public bool IsMouse(int device)
        {
            return interception_is_mouse(device) != 0;
        }

        /// <summary>
        /// 开启/关闭对键盘的拦截。默认过滤器是 NONE（不拦截），
        /// 必须设为 ALL 才能收到按键事件；设为 NONE 后按键直接透传。
        /// </summary>
        public void SetKeyboardFilter(ushort filter)
        {
            if (_context == IntPtr.Zero || interception_is_keyboard_pointer == IntPtr.Zero) return;

            interception_set_filter(_context, interception_is_keyboard_pointer, filter);
        }

        /// <summary>鼠标始终不拦截，避免影响鼠标移动。</summary>
        public void SetMouseFilter(ushort filter)
        {
            if (_context == IntPtr.Zero || interception_is_mouse_pointer == IntPtr.Zero) return;

            interception_set_filter(_context, interception_is_mouse_pointer, filter);
        }

        /// <summary>
        /// 等待任意一个键盘/鼠标设备产生输入，最多等待 milliseconds 毫秒。
        /// 返回设备号，0 表示超时。
        /// </summary>
        public int WaitWithTimeout(uint milliseconds)
        {
            if (_context == IntPtr.Zero) return 0;

            return interception_wait_with_timeout(_context, milliseconds);
        }

        /// <summary>
        /// 读取一笔输入。注意第 4 个参数是「一次最多读几笔」，不是超时时间。
        /// </summary>
        public int Receive(int device, ref Stroke stroke, uint strokeCount = 1)
        {
            IntPtr strokePtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Stroke)) * (int)strokeCount);
            try
            {
                int result = interception_receive(_context, device, strokePtr, strokeCount);
                if (result > 0)
                {
                    stroke = Marshal.PtrToStructure<Stroke>(strokePtr);
                }

                return result;
            }
            finally
            {
                Marshal.FreeHGlobal(strokePtr);
            }
        }

        public int Send(int device, ref Stroke stroke)
        {
            IntPtr strokePtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Stroke)));
            try
            {
                Marshal.StructureToPtr(stroke, strokePtr, false);
                return interception_send(_context, device, strokePtr, 1);
            }
            finally
            {
                Marshal.FreeHGlobal(strokePtr);
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct Stroke
        {
            [FieldOffset(0)] public KeyStroke Key;

            [FieldOffset(0)] public MouseStroke Mouse;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KeyStroke
        {
            public ushort Code;
            public ushort State;
            public uint Information;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MouseStroke
        {
            public ushort Flags;
            public ushort Rolling;
            public int X;
            public int Y;
            public uint Information;
        }
    }
}