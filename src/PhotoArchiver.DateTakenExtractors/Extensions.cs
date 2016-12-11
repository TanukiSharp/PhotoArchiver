using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;

namespace DateTakenExtractors
{
    public static class BitConverterBE
    {
        public static ushort ToUInt16(byte[] data, int offset)
        {
            return (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, offset));
        }

        public static short ToInt16(byte[] data, int offset)
        {
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt16(data, offset));
        }

        public static uint ToUInt32(byte[] data, int offset)
        {
            return (uint)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(data, offset));
        }

        public static int ToInt32(byte[] data, int offset)
        {
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(data, offset));
        }

        public static ulong ToUInt64(byte[] data, int offset)
        {
            return (ulong)IPAddress.NetworkToHostOrder(BitConverter.ToInt64(data, offset));
        }

        public static long ToInt64(byte[] data, int offset)
        {
            return IPAddress.NetworkToHostOrder(BitConverter.ToInt64(data, offset));
        }
    }

    public static class BinaryReaderExtension
    {
        public static ushort ReadUInt16BE(this BinaryReader reader)
        {
            return (ushort)IPAddress.NetworkToHostOrder(reader.ReadInt16());
        }

        public static uint ReadUInt32BE(this BinaryReader reader)
        {
            return (uint)IPAddress.NetworkToHostOrder(reader.ReadInt32());
        }

        public static ulong ReadUInt64BE(this BinaryReader reader)
        {
            return (ulong)IPAddress.NetworkToHostOrder(reader.ReadInt64());
        }
    }
}
