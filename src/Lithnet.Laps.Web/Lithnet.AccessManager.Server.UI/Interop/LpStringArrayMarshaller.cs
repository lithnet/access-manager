using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Lithnet.AccessManager.Server.UI.Interop
{
    public class LpStringArrayConverter : IDisposable
    {
        public IntPtr Ptr { get; private set; } = IntPtr.Zero;

        private List<IntPtr> allocatedStrings = new List<IntPtr>();

        public IReadOnlyList<string> Items { get; private set; }

        public int Count => this.allocatedStrings.Count;

        private bool disposedValue;

        public LpStringArrayConverter(IList<string> items)
            :this(items.ToArray())
        {
        }

        public LpStringArrayConverter(string[] array)
        {
            if (array == null || array.Length == 0)
            {
                return;
            }

            this.Items = new List<string>(array).AsReadOnly();

            int size = array.Length;

            this.Ptr = Marshal.AllocCoTaskMem(size * IntPtr.Size);

            for (int i = 0; i < size; i++)
            {
                IntPtr s = Marshal.StringToCoTaskMemUni(array[i]);
                Marshal.WriteIntPtr(this.Ptr, i * IntPtr.Size, s);
                this.allocatedStrings.Add(s);
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
                    Marshal.FreeCoTaskMem(this.Ptr);
                }

                foreach (IntPtr p in this.allocatedStrings)
                {
                    Marshal.FreeCoTaskMem(p);
                }

                disposedValue = true;
            }
        }

        ~LpStringArrayConverter()
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