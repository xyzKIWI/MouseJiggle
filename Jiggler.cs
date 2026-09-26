// Based on Mouse Jiggler 1.8.42 by Arkane Systems, licensed under the Ms-PL.

using System;
using System.Runtime.InteropServices;

namespace ArkaneSystems.MouseJiggle
{
    internal static class Jiggler
    {
        private const int InputMouse = 0;
        private const int MouseEventMove = 0x0001;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint inputCount, ref Input input, int inputSize);

        internal static bool TryJiggle(int deltaX, int deltaY, out int errorCode)
        {
            Input input = new Input
            {
                Type = InputMouse,
                DeltaX = deltaX,
                DeltaY = deltaY,
                MouseData = 0,
                Flags = MouseEventMove,
                Time = 0,
                ExtraInfo = IntPtr.Zero
            };

            uint result = SendInput(1, ref input, Marshal.SizeOf(typeof(Input)));
            if (result == 1)
            {
                errorCode = 0;
                return true;
            }

            errorCode = Marshal.GetLastWin32Error();
            return false;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Input
        {
            internal int Type;
            internal int DeltaX;
            internal int DeltaY;
            internal int MouseData;
            internal int Flags;
            internal int Time;
            internal IntPtr ExtraInfo;
        }
    }
}
