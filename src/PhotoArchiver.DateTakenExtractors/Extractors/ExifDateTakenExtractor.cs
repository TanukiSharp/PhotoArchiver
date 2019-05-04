using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using PhotoArchiver.Contracts;

namespace DateTakenExtractors.Extractors
{
    public class ExifDateTakenExtractor : IDateTakenExtractor
    {
        private const ushort StartOfImage = 0xFFD8;
        private const ushort StartOfScan = 0xFFDA;
        private const ushort APP1 = 0xFFE1;
        private const ushort SUBIFD = 0x8769;

        private static readonly char[] SpaceCharacter = new char[] { ' ' };
        private static readonly char[] ColonCharacter = new char[] { ':' };

        public bool ExtractDateTaken(Stream stream, out DateTime dateTaken)
        {
            dateTaken = DateTime.MinValue;

            try
            {
                using (var br = new BinaryReader(stream, Encoding.ASCII, true))
                {
                    if (br.ReadUInt16BE() != StartOfImage)
                        return false;

                    Packet packet;

                    while (true)
                    {
                        packet = Packet.ReadPacket(br);

                        if (packet.Marker == StartOfScan)
                            break;

                        if (packet.Marker == APP1)
                        {
                            packet.ReadContent(br);
                            if (Encoding.ASCII.GetString(packet.Data, 0, 6) == "Exif\0\0")
                                return ProcessApp1Data(packet.Data, out dateTaken);
                        }
                        else
                            packet.SkipContent(br);
                    }

                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool ProcessApp1Data(byte[] data, out DateTime dateTaken)
        {
            dateTaken = DateTime.MinValue;

            bool isLittleEndian;
            int ifdOffset;

            if (CheckTiffHeader(data, out isLittleEndian, out ifdOffset) == false || ifdOffset <= 0)
                return false;

            int workOffset = ifdOffset;

            bool isSubstituteFound = false;
            DateTime substituteDateTaken = DateTime.MinValue;

            while (workOffset > 0)
            {
                workOffset += 6; // 6 is exif header size
                                 // exif spec says all offsets start from tiff header, but current buffer starts from exif header

                int directoryCount;

                if (isLittleEndian)
                    directoryCount = BitConverter.ToInt16(data, workOffset);
                else
                    directoryCount = BitConverterBE.ToInt16(data, workOffset);

                workOffset += 2;

                bool subIFDFound = false;

                for (int i = 0; i < directoryCount; i++)
                {
                    var ifd = ImageFileDirectory.Read(isLittleEndian, data, workOffset);

                    if (ifd.Tag == SUBIFD)
                    {
                        if (isLittleEndian)
                            workOffset = BitConverter.ToInt32(ifd.Value.Array, ifd.Value.Offset);
                        else
                            workOffset = BitConverterBE.ToInt32(ifd.Value.Array, ifd.Value.Offset);

                        subIFDFound = true;
                        break;
                    }

                    if (ifd.Tag == 306)
                    {
                        if (ifd.Type != 2 || ifd.Value.Count != 20)
                            continue;

                        isSubstituteFound = TryParseDateTime(ifd.Value.Array, ifd.Value.Offset, ifd.Value.Count, out substituteDateTaken);
                    }
                    else if (ifd.Tag == 0x9003 || ifd.Tag == 0x9004)
                    {
                        if (ifd.Type != 2 || ifd.Value.Count != 20)
                            return false;

                        if (TryParseDateTime(ifd.Value.Array, ifd.Value.Offset, ifd.Value.Count, out dateTaken))
                            return true;
                    }

                    workOffset += 12;
                }

                if (subIFDFound)
                    continue;

                if (isLittleEndian)
                    workOffset = BitConverter.ToInt32(data, workOffset);
                else
                    workOffset = BitConverterBE.ToInt32(data, workOffset);
            }

            if (isSubstituteFound)
            {
                dateTaken = substituteDateTaken;
                return true;
            }

            return false;
        }

        private bool TryParseDateTime(byte[] data, int offset, int length, out DateTime dateTime)
        {
            dateTime = DateTime.MinValue;

            try
            {
                string value = Encoding.ASCII.GetString(data, offset, length - 1);

                string[] dateTimeParts = value.Split(SpaceCharacter);
                if (dateTimeParts.Length != 2)
                    return false;

                string[] dateParts = dateTimeParts[0].Split(ColonCharacter);
                if (dateParts.Length != 3)
                    return false;

                string[] timeParts = dateTimeParts[1].Split(ColonCharacter);
                if (timeParts.Length != 3)
                    return false;

                dateTime = new DateTime(
                    int.Parse(dateParts[0]), int.Parse(dateParts[1]), int.Parse(dateParts[2]),
                    int.Parse(timeParts[0]), int.Parse(timeParts[1]), int.Parse(timeParts[2]));

                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool CheckTiffHeader(byte[] data, out bool isLittleEndian, out int ifdOffset)
        {
            isLittleEndian = false;
            ifdOffset = 0;

            string byteAlign = Encoding.ASCII.GetString(data, 6, 2);

            if (byteAlign == "II")
            {
                if (data[8] != 0x2A || data[9] != 0x0)
                    return false;

                isLittleEndian = true;
                ifdOffset = BitConverter.ToInt32(data, 10);

                return true;
            }
            else if (byteAlign == "MM")
            {
                if (data[8] != 0x00 || data[9] != 0x2A)
                    return false;

                isLittleEndian = false;
                ifdOffset = BitConverterBE.ToInt32(data, 10);

                return true;
            }
            else
                return false;
        }
    }

    internal class ImageFileDirectory
    {
        internal ushort Tag { get; private set; }
        internal ushort Type { get; private set; }
        public ArraySegment<byte> Value { get; private set; }

        private ImageFileDirectory()
        {
        }

        internal static ImageFileDirectory Read(bool isLittleEndian, byte[] data, int offset)
        {
            var ifd = new ImageFileDirectory();

            if (isLittleEndian)
                ifd.Tag = BitConverter.ToUInt16(data, offset);
            else
                ifd.Tag = BitConverterBE.ToUInt16(data, offset);

            if (isLittleEndian)
                ifd.Type = BitConverter.ToUInt16(data, offset + 2);
            else
                ifd.Type = BitConverterBE.ToUInt16(data, offset + 2);

            uint components;
            uint dataOffset;

            if (isLittleEndian)
                components = BitConverter.ToUInt32(data, offset + 4);
            else
                components = BitConverterBE.ToUInt32(data, offset + 4);

            if (isLittleEndian)
                dataOffset = BitConverter.ToUInt32(data, offset + 8) + 6;
            else
                dataOffset = BitConverterBE.ToUInt32(data, offset + 8) + 6;

            uint componentSize = 1;

            if (ifd.Type == 3 || ifd.Type == 8)
                componentSize = 2;
            else if (ifd.Type == 4 || ifd.Type == 9 || ifd.Type == 11)
                componentSize = 4;
            else if (ifd.Type == 5 || ifd.Type == 10 || ifd.Type == 12)
                componentSize = 8;

            uint totalByteLength = componentSize * components;

            if (totalByteLength <= 4)
                ifd.Value = new ArraySegment<byte>(data, offset + 8, (int)totalByteLength);
            else
                ifd.Value = new ArraySegment<byte>(data, (int)dataOffset, (int)totalByteLength);

            return ifd;
        }
    }

    internal class Packet
    {
        internal ushort Marker { get; private set; }
        internal ushort Length { get; private set; }
        internal byte[] Data { get; private set; }

        private Packet()
        {
        }

        internal static Packet ReadPacket(BinaryReader br)
        {
            return new Packet
            {
                Marker = br.ReadUInt16BE(),
                Length = br.ReadUInt16BE()
            };
        }

        internal void ReadContent(BinaryReader br)
        {
            Data = br.ReadBytes(Length - 2);
        }

        internal void SkipContent(BinaryReader br)
        {
            br.BaseStream.Seek(Length - 2, SeekOrigin.Current);
        }
    }
}
