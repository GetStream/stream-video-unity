using System;

namespace Libs.DeviceManagers
{
    internal static class AudioDeviceManagerHelper
    {
        public static void ClearBuffer<T>(ref T[] buffer) => Array.Fill(buffer, default);

        public static int FindClosestPowerOfTwo(int value)
        {
            if (value < 1)
            {
                return 1;
            }

            var power = 1;
            while (power < value)
            {
                power *= 2;
            }

            return power;
        }
    }
}