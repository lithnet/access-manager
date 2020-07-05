using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Lithnet.AccessManager.Interop
{
    public class LpArrayOfByteArrayConverter : IDisposable
    {
        public IntPtr Ptr { get; private set; } = IntPtr.Zero;

        private List<IntPtr> allocatedItems = new List<IntPtr>();

        public IReadOnlyList<byte[]> Items { get; private set; }

        public int Count => this.allocatedItems.Count;

        private bool disposedValue;

        public LpArrayOfByteArrayConverter(List<byte[]> array)
        {
            if (array == null || array.Count == 0)
            {
                return;
            }

            this.Items = array.AsReadOnly();

            int size = array.Count;

            this.Ptr = Marshal.AllocHGlobal(size * IntPtr.Size);

            for (int i = 0; i < size; i++)
            {
                IntPtr s = Marshal.AllocHGlobal(array[i].Length);
                Marshal.Copy(array[i], 0, s, array[i].Length);

                Marshal.WriteIntPtr(this.Ptr, i * IntPtr.Size, s);
                this.allocatedItems.Add(s);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                if (this.Ptr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(this.Ptr);
                }

                foreach (IntPtr p in this.allocatedItems)
                {
                    Marshal.FreeHGlobal(p);
                }

                disposedValue = true;
            }
        }

        ~LpArrayOfByteArrayConverter()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}