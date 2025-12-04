using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerLib
{
    public class PacketBuilder
    {
        private static ThreadLocal<SendBuffer> _buffer = new ThreadLocal<SendBuffer>(()=> { return new SendBuffer(65535); });
  
        public static ArraySegment<byte> Build(IMessage packet)
        {
            ushort size = (ushort)packet.CalculateSize();
            int totalSize = size + sizeof(ushort);

            if (_buffer.Value.FreeSize < size + sizeof(ushort))
                _buffer.Value = new SendBuffer(65535);

            ArraySegment<byte> segment = _buffer.Value.Open(totalSize);
            Array.Copy(BitConverter.GetBytes((ushort)(size + sizeof(ushort))), 0, segment.Array, segment.Offset, sizeof(ushort));
            Array.Copy(packet.ToByteArray(), 0, segment.Array, segment.Offset + sizeof(ushort), size);

            return _buffer.Value.Close(totalSize);
        }

        public static void ResetBuffer()
        {
            _buffer.Value.Reset();
        }
    }
}
