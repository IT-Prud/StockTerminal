using System;
using System.Collections.Generic;
using System.Text;

namespace Utils
{
    public class HexHandler
    {
        public readonly static byte[] HexToByte = new byte[256];
        public readonly static char[] ByteToHex = new char[16] {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'};

        static HexHandler()
        {
            HexToByte[48] = 0;
            HexToByte[49] = 1;
            HexToByte[50] = 2;
            HexToByte[51] = 3;
            HexToByte[52] = 4;
            HexToByte[53] = 5;
            HexToByte[54] = 6;
            HexToByte[55] = 7;
            HexToByte[56] = 8;
            HexToByte[57] = 9;

            HexToByte[65] = 10;
            HexToByte[66] = 11;
            HexToByte[67] = 12;
            HexToByte[68] = 13;
            HexToByte[69] = 14;
            HexToByte[70] = 15;

            HexToByte[97] = 10;
            HexToByte[98] = 11;
            HexToByte[99] = 12;
            HexToByte[100] = 13;
            HexToByte[101] = 14;
            HexToByte[102] = 15;
        }
    }
}
