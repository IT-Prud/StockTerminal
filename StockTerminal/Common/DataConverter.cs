using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Utils
{
    public static class DataConverter
    {
        private static string[] BinToHex = new string[] { 
            "00", "01", "02", "03", "04", "05", "06", "07", "08", "09", "0A", "0B", "0C", "0D", "0E", "0F", 
            "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "1A", "1B", "1C", "1D", "1E", "1F", 
            "20", "21", "22", "23", "24", "25", "26", "27", "28", "29", "2A", "2B", "2C", "2D", "2E", "2F", 
            "30", "31", "32", "33", "34", "35", "36", "37", "38", "39", "3A", "3B", "3C", "3D", "3E", "3F", 
            "40", "41", "42", "43", "44", "45", "46", "47", "48", "49", "4A", "4B", "4C", "4D", "4E", "4F", 
            "50", "51", "52", "53", "54", "55", "56", "57", "58", "59", "5A", "5B", "5C", "5D", "5E", "5F", 
            "60", "61", "62", "63", "64", "65", "66", "67", "68", "69", "6A", "6B", "6C", "6D", "6E", "6F", 
            "70", "71", "72", "73", "74", "75", "76", "77", "78", "79", "7A", "7B", "7C", "7D", "7E", "7F", 
            "80", "81", "82", "83", "84", "85", "86", "87", "88", "89", "8A", "8B", "8C", "8D", "8E", "8F", 
            "90", "91", "92", "93", "94", "95", "96", "97", "98", "99", "9A", "9B", "9C", "9D", "9E", "9F", 
            "A0", "A1", "A2", "A3", "A4", "A5", "A6", "A7", "A8", "A9", "AA", "AB", "AC", "AD", "AE", "AF", 
            "B0", "B1", "B2", "B3", "B4", "B5", "B6", "B7", "B8", "B9", "BA", "BB", "BC", "BD", "BE", "BF", 
            "C0", "C1", "C2", "C3", "C4", "C5", "C6", "C7", "C8", "C9", "CA", "CB", "CC", "CD", "CE", "CF", 
            "D0", "D1", "D2", "D3", "D4", "D5", "D6", "D7", "D8", "D9", "DA", "DB", "DC", "DD", "DE", "DF", 
            "E0", "E1", "E2", "E3", "E4", "E5", "E6", "E7", "E8", "E9", "EA", "EB", "EC", "ED", "EE", "EF", 
            "F0", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "FA", "FB", "FC", "FD", "FE", "FF" };

        public static byte[] FromBase64(string Value)
        {
            byte[] result = null;

            if (Value != null && Value.Length > 0)
            {
                try
                {
                    result = Convert.FromBase64String(Value);
                }
                catch { }
            }

            return result;
        }

        public static string ToBase64(byte[] Value)
        {
            string result = null;

            if (Value != null && Value.Length > 0)
                result = Convert.ToBase64String(Value);

            return result;
        }

        public static byte[] FromHex(string Value)
        {
            byte[] result = null;

            if (Value != null && Value.Length > 0)
            {
                char c;
                int n = Value.Length;
                int m = n >> 1;
                int i = 0, j = 0;
                int v;

                if ((n & 1) == 1)
                {
                    result = new byte[m + 1];
                    c = Value[0];
                    if (c >= '0' && c <= '9')
                        result[0] = (byte)(c - '0');
                    else if (c >= 'A' && c <= 'F')
                        result[0] = (byte)(c - 'A');
                    else if (c >= 'a' && c <= 'f')
                        result[0] = (byte)(c - 'a');

                    i = 1;
                    j = 1;
                }
                else
                {
                    result = new byte[m];
                }

                for (; i < n; i += 2, j++)
                {
                    c = Value[i];

                    if (c >= '0' && c <= '9')
                        v = (c - 48) << 4;
                    else if (c >= 'A' && c <= 'F')
                        v = (c - 55) << 4;
                    else if (c >= 'a' && c <= 'f')
                        v = (c - 87) << 4;
                    else
                        v = 0;

                    c = Value[i + 1];

                    if (c >= '0' && c <= '9')
                        v |= (c - 48);
                    else if (c >= 'A' && c <= 'F')
                        v |= (c - 55);
                    else if (c >= 'a' && c <= 'f')
                        v |= (c - 87);

                    result[j] = (byte)v;
                }
            }

            return result;
        }

        public static string ToHex(byte[] Value)
        {
            string result = null;

            if (Value != null && Value.Length >= 0)
            {
                StringBuilder sb = new StringBuilder(Value.Length << 1);

                for (int i = 0; i < Value.Length; i++)
                    sb.Append(BinToHex[Value[i]]);

                result = sb.ToString();
            }

            return result;
        }

        public static byte[] FromUTF8(string Value)
        {
            byte[] result = null;

            if (Value != null && Value.Length > 0)
            {
                try
                {
                    result = Encoding.UTF8.GetBytes(Value);
                }
                catch { }
            }

            return result;
        }

        public static string ToUTF8(byte[] Value)
        {
            string result = null;

            if (Value != null && Value.Length > 0)
            {
                try
                {
                    result = Encoding.UTF8.GetString(Value);
                }
                catch { }
            }

            return result;
        }

        public static byte[] SHA1(byte[] Value)
        {
            byte[] result = null;

            if (Value != null && Value.Length > 0)
            {
                using (SHA1 sha = System.Security.Cryptography.SHA1.Create())
                {
                    result = sha.ComputeHash(Value);
                }
            }

            return result;
        }

        public static byte[] SHA256(byte[] Value)
        {
            byte[] result = null;

            if (Value != null && Value.Length > 0)
            {
                using (SHA256 sha = System.Security.Cryptography.SHA256.Create())
                {
                    result = sha.ComputeHash(Value);
                }
            }

            return result;
        }

        public static byte[] SHA512(byte[] Value)
        {
            byte[] result = null;

            if (Value != null && Value.Length > 0)
            {
                using (SHA512 sha = System.Security.Cryptography.SHA512.Create())
                {
                    result = sha.ComputeHash(Value);
                }
            }

            return result;
        }

        public static byte[] GetRandomBytes(int Length)
        {
            byte[] result = null;

            if (Length >= 0)
            {
                result = new byte[Length];
                Random rnd = new Random();
                rnd.NextBytes(result);
            }

            return result;
        }

        public static string GetTimeSalt(string Message, byte[] Secret, int TimeIndex, int Interval)
        {
            if (Message == null || Secret == null)
                return null;

            byte[] msg = Encoding.UTF8.GetBytes(Message);
            byte[] keyPlain = new byte[msg.Length + Secret.Length];

            Buffer.BlockCopy(msg, 0, keyPlain, 0, msg.Length);
            Buffer.BlockCopy(Secret, 0, keyPlain, msg.Length, Secret.Length);

            long unixTime = (long)Math.Floor((DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds / Math.Max(Interval, 1));
            unixTime += TimeIndex;

            byte[] message = new byte[] {
                (byte)(unixTime >> 56), 
                (byte)((unixTime >> 48) & 0xff), 
                (byte)((unixTime >> 40) & 0xff), 
                (byte)((unixTime >> 32) & 0xff), 
                (byte)((unixTime >> 24) & 0xff), 
                (byte)((unixTime >> 16) & 0xff), 
                (byte)((unixTime >> 8) & 0xff), 
                (byte)(unixTime & 0xff)
            };

            using (SHA512 sha = System.Security.Cryptography.SHA512.Create())
            {
                byte[] key = sha.ComputeHash(keyPlain);

                if (key != null)
                {
                    byte[] i_key_pad = new byte[key.Length + message.Length];
                    byte[] o_key_pad;
                    byte[] hash;
                    byte[] truncatedHash = new byte[4];
                    byte[] code = new byte[12];
                    int offset, n;

                    n = key.Length;

                    for (int i = 0; i < key.Length; i++)
                        i_key_pad[i] = (byte)(0x36 ^ key[i]);

                    Buffer.BlockCopy(message, 0, i_key_pad, key.Length, message.Length);

                    hash = sha.ComputeHash(i_key_pad);

                    if (hash != null)
                    {
                        o_key_pad = new byte[key.Length + hash.Length];

                        for (int i = 0; i < n; i++)
                            o_key_pad[i] = (byte)(0x5c ^ key[i]);

                        Buffer.BlockCopy(hash, 0, o_key_pad, key.Length, hash.Length);

                        hash = sha.ComputeHash(o_key_pad);

                        if (hash != null)
                        {
                            offset = hash[hash.Length - 1] & 15;

                            Buffer.BlockCopy(hash, offset, code, 0, Math.Min(code.Length, hash.Length - offset));
                            return Convert.ToBase64String(hash, offset, Math.Min(12, hash.Length)).Replace('+', 'e').Replace('/', 'k');
                        }
                    }
                }
            }

            return null;
        }

        public static void GenerateAESKey(int KeySize, out byte[] Key, out byte[] IV)
        {
            using (RijndaelManaged am = new RijndaelManaged())
            {
                try
                {
                    am.KeySize = KeySize;
                    am.BlockSize = 128;

                    am.GenerateKey();
                    am.GenerateIV();

                    Key = new byte[am.Key.Length];
                    Buffer.BlockCopy(am.Key, 0, Key, 0, am.Key.Length);

                    IV = new byte[am.IV.Length];
                    Buffer.BlockCopy(am.IV, 0, IV, 0, am.IV.Length);
                }
                catch
                {
                    Key = null;
                    IV = null;
                }
            }
        }

        public static void GenerateAESKeyHex(int KeySize, out string Key, out string IV)
        {
            using (RijndaelManaged am = new RijndaelManaged())
            {
                try
                {
                    am.BlockSize = 128;
                    am.KeySize = KeySize;

                    am.GenerateKey();
                    am.GenerateIV();

                    Key = ToHex(am.Key);
                    IV = ToHex(am.IV);
                }
                catch
                {
                    Key = null;
                    IV = null;
                }
            }
        }

        public static byte[] DecryptAES(byte[] Key, byte[] IV, byte[] Cipher, CipherMode Mode, PaddingMode Padding)
        {
            byte[] result = null;

            if (Key != null && Key.Length > 0 && IV != null && IV.Length > 0 && Cipher != null && Cipher.Length > 0)
            {
                using (RijndaelManaged am = new RijndaelManaged())
                {
                    try
                    {
                        am.Key = Key;
                        am.IV = IV;
                        am.Mode = Mode;
                        am.Padding = Padding;

                        ICryptoTransform transForm = am.CreateDecryptor();

                        result = transForm.TransformFinalBlock(Cipher, 0, Cipher.Length);
                    }
                    catch { }
                }
            }

            return result;
        }
    }
}
