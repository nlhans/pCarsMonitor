using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace pCarsTelemetry.API
{
    public class SharedMemory<T> : IDisposable
    {
        private bool _disposed = false;

        private int _fileSize;

        private MemoryMappedFile _mmf;
        private MemoryMappedViewAccessor _mmfView;

        public SharedMemory(string map)
        {
            _fileSize = Marshal.SizeOf(typeof(T));

            _mmf = MemoryMappedFile.OpenExisting(map, MemoryMappedFileRights.ReadWrite);
            _mmfView = _mmf.CreateViewAccessor(0, _fileSize);
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                _mmfView.Dispose();
                _mmf.Dispose();

                _mmfView = null;
                _mmf = null;

                _disposed = true;
            }
        }

        public T Read()
        {

            var bf = new byte[_fileSize];
            _mmfView.ReadArray(0, bf, 0, _fileSize);

            var ptr = Marshal.AllocHGlobal(_fileSize);
            Marshal.Copy(bf, 0, ptr, _fileSize);
            T obj = (T) Marshal.PtrToStructure(ptr, typeof (T));
            Marshal.FreeHGlobal(ptr);

            return obj;
        }
    }
}
