using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Lithnet.AccessManager.Interop
{
    public class LpStructArrayMarshaller<T> : IDisposable where T : struct
    {
        public IntPtr Ptr { get; private set; } = IntPtr.Zero;

        public IReadOnlyList<T> Items { get; private set; }

        public int Count => this.Items.Count;

        private bool disposedValue;

        public LpStructArrayMarshaller(IList<T> items)
            : this(items.ToArray())
        {
        }

        public LpStructArrayMarshaller(T[] array)
        {
            if (array == null || array.Length == 0)
            {
                return;
            }

            this.Items = new List<T>(array).AsReadOnly();

            int size = array.Length;

            this.Ptr = Marshal.AllocCoTaskMem(Marshal.SizeOf<T>() * size);

            for (int i = 0; i < size; i++)
            {
                Marshal.StructureToPtr<T>(array[i], new IntPtr(this.Ptr.ToInt64() + (i * Marshal.SizeOf<T>())), false);
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

                disposedValue = true;
            }
        }

        ~LpStructArrayMarshaller()
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