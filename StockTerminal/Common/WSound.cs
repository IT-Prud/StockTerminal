using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Utils
{
    public class WSound
    {
        private class WSoundItem
        {
            private byte[] data = null;
            private UInt32 dwFlags;

            public WSoundItem(byte[] data, UInt32 dwFlags)
            {
                this.data = data;
                this.dwFlags = dwFlags;
            }

            public void Play()
            {
                if (data != null)
                {
                    PlaySound(data, IntPtr.Zero, (UInt32)dwFlags);
                }
                else
                {
                    // stop all sound
                    PlaySound(null, IntPtr.Zero, (UInt32) SoundFlag.SND_PURGE);
                }
            }
        }

        [DllImport("WinMM.dll")]
        private static extern bool PlaySound(byte[] data, IntPtr hMod, UInt32 dwFlags);

        private static Thread ProcessThread = null;
        private static AutoResetEvent ProcessEvent = new AutoResetEvent(false);
        private static AutoResetEvent ProcessStopEvent = new AutoResetEvent(false);
        private static object ProcessThreadMutex = new object();

        private static Queue<WSoundItem> SoundQueue = new Queue<WSoundItem>(10);
        private static object SoundQueueMutex = new object();

        //  flag values for SoundFlags argument on PlaySound
        public enum SoundFlag
        {
            SND_SYNC = 0x0000,              // play synchronously (default)
            SND_ASYNC = 0x0001,             // play asynchronously
            SND_NODEFAULT = 0x0002,         // silence (!default) if sound not found
            SND_MEMORY = 0x0004,            // pszSound points to a memory file
            SND_LOOP = 0x0008,              // loop the sound until next sndPlaySound
            SND_NOSTOP = 0x0010,            // don't stop any currently playing sound
            SND_NOWAIT = 0x00002000,        // don't wait if the driver is busy
            SND_ALIAS = 0x00010000,         // name is a Registry alias
            SND_ALIAS_ID = 0x00110000,      // alias is a predefined ID
            SND_FILENAME = 0x00020000,      // name is file name
            SND_RESOURCE = 0x00040004,      // name is resource name or atom
            SND_PURGE = 0x0040,             // purge non-static events for task
            SND_APPLICATION = 0x0080        // look for application-specific association
        }

        public static void PlayFile(string FilePath)
        {
            if (FilePath != null && FilePath.Length > 0)
            {
                EnqueueSound(new WSoundItem(Encoding.ASCII.GetBytes(FilePath),
                    (UInt32)(SoundFlag.SND_ASYNC | SoundFlag.SND_NOSTOP | SoundFlag.SND_FILENAME)));
            }
        }

        public static void PlayBuffer(byte[] Buffer)
        {
            if (Buffer != null && Buffer.Length > 0)
            {
                EnqueueSound(new WSoundItem(Buffer, (UInt32)(SoundFlag.SND_ASYNC | SoundFlag.SND_NOSTOP | SoundFlag.SND_MEMORY)));
            }
        }

/*      // Direct play of memory buffer larger than 256 bytes result in jerky sound.
        public static void PlayStream(UnmanagedMemoryStream Stream)
        {
            return;

            if (Stream == null || Stream.Length <= 0 || !Stream.CanRead) return;

            //byte[] buffer = new byte[Stream.Length];
            buffer = new byte[Stream.Length * 10];
            Stream.Read(buffer, 0, (int)Stream.Length);
            soundFlags = SoundFlag.SND_ASYNC | SoundFlag.SND_FILENAME; // | SoundFlag.SND_MEMORY;

            //PlaySound(buffer, IntPtr.Zero, (UInt32)(SoundFlag.SND_ASYNC | SoundFlag.SND_NOSTOP | SoundFlag.SND_MEMORY));

            EnqueueSound();
        }
*/

        public static void Stop()
        {
            ProcessStopEvent.Set();
            
            lock (SoundQueueMutex)
            {
                SoundQueue.Clear();
            }
            
            ProcessEvent.Set();

            (new WSoundItem(null, (UInt32)SoundFlag.SND_PURGE)).Play();
        }

        private static void EnqueueSound(WSoundItem Item)
        {
            if (Item != null)
            {
                lock (SoundQueueMutex)
                {
                    SoundQueue.Enqueue(Item);
                }

                lock (ProcessThreadMutex)
                {
                    if (ProcessThread == null)
                    {
                        ProcessThread = new Thread(new ThreadStart(ProcessSound));
                        ProcessThread.Start();
                    }
                }

                ProcessEvent.Set();
            }
        }

        private static void ProcessSound()
        {
            WSoundItem item = null;

            while(!ProcessStopEvent.WaitOne(0, true))
            {
                lock (SoundQueueMutex)
                {
                    item = SoundQueue.Count > 0 ? SoundQueue.Dequeue() : null;
                }

                if (item != null)
                {
                    item.Play();
                    ProcessEvent.Set();
                }

                ProcessEvent.WaitOne(Timeout.Infinite, true);
            }

            lock (ProcessThreadMutex)
            {
                ProcessThread = null;
            }
        }
    }
}
