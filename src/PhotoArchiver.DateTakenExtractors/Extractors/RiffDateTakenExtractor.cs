using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using PhotoArchiver.Contracts;
//using System.ComponentModel.Composition;

namespace DateTakenExtractors.Extractors
{
	//[Export(typeof(IDateTakenExtractor))]
	public class RiffDateTakenExtractor : IDateTakenExtractor
	{
		public bool ExtractDateTaken(Stream stream, out DateTime dateTaken)
		{
			dateTaken = DateTime.MinValue;

			try
			{
				using (BinaryReader reader = new BinaryReader(stream, Encoding.ASCII, true))
				{
					Chunk chunk = Chunk.Read(reader);
					if (chunk == null || chunk.Id != "RIFF")
						return false;

					ProcessChunk(chunk, reader);

					if (ProcessStandardChunk(chunk, out dateTaken))
						return true;

					if (ProcessNonStandardChunk(chunk, out dateTaken))
						return true;
				}
			}
			catch (Exception)
			{
			}

			return false;
		}

		private bool ProcessStandardChunk(Chunk chunk, out DateTime dateTaken)
		{
			dateTaken = DateTime.MinValue;

			Chunk test;

			test = chunk.FindChunk("LIST:hdrl/IDIT");
			if (test != null)
				return ReadIDITDateTaken(test.Data, out dateTaken);

			test = chunk.FindChunk("LIST:INFO/DTIM");
			if (test != null)
				return false;

			test = chunk.FindChunk("LIST:INFO/ICRD");
			if (test != null)
				return false;

			test = chunk.FindChunk("LIST:EXIF/etim");
			if (test != null)
				return false;

			return false;
		}

		private bool ProcessNonStandardChunk(Chunk chunk, out DateTime dateTaken)
		{
			dateTaken = DateTime.MinValue;

			Chunk test;

			test = chunk.FindChunk("LIST:hdrl/LIST:strl/strd");
			if (test != null)
				return ReadAVIFDateTaken(test.Data, out dateTaken);

			return false;
		}


		private bool CheckChunk(Chunk chunk)
		{
			if (chunk == null)
				return false;

			if (chunk.Data != null && chunk.Data.Length >= 4)
			{
                string str = Encoding.ASCII.GetString(chunk.Data, 0, 4);
				if (str == "AVIF")
					return true;
			}

			if (chunk.Children != null)
			{
				foreach (Chunk ch in chunk.Children)
				{
					if (CheckChunk(ch))
						return true;
				}
			}

			return false;
		}

		private void ProcessChunk(Chunk chunk, BinaryReader reader)
		{
			if (chunk.Id == "RIFF")
			{
				ProcessRiffChunk(chunk, reader);
				chunk.CheckPadding(reader);
			}
			else if (chunk.Id == "LIST")
			{
				ProcessListChunk(chunk, reader);
				chunk.CheckPadding(reader);
			}
			else
			{
				//chunk.Skip(reader);
				chunk.ReadData(reader);
				chunk.CheckPadding(reader);
			}
		}

		private void ProcessRiffChunk(Chunk riffChunk, BinaryReader reader)
		{
			if (riffChunk.ReadType(reader) == false)
				return;

			int size = (int)riffChunk.DataSize - 4;

			Chunk childChunk;
			while ((childChunk = Chunk.Read(reader)) != null)
			{
				ProcessChunk(childChunk, reader);

				riffChunk.AddChild(childChunk);

				size -= (int)childChunk.DataSize + 8;
				if (size <= 0)
					break;
			}
		}

		private void ProcessListChunk(Chunk listChunk, BinaryReader reader)
		{
			if (listChunk.ReadType(reader) == false)
				return;

			if (listChunk.Type == "movi")
			{
				listChunk.Skip(reader, true);
				return;
			}

			int size = (int)listChunk.DataSize - 4;

			Chunk childChunk;
			while ((childChunk = Chunk.Read(reader)) != null)
			{
				ProcessChunk(childChunk, reader);

				listChunk.AddChild(childChunk);

				size -= (int)childChunk.DataSize + 8;
				if (size <= 0)
					break;
			}
		}

        private static readonly char[] StringTrimEndCharacters = new char[] { '\0', '\r', '\n' };
        private static readonly char[] DateTimeSplitCharacters = new char[] { ' ', ':' };
        private static readonly char[] SpaceCharacter = new char[] { ' ' };
        private static readonly char[] ColonCharacter = new char[] { ':' };

        public bool ReadIDITDateTaken(byte[] data, out DateTime dateTaken)
		{
			dateTaken = DateTime.MinValue;

			string str = Encoding.ASCII.GetString(data).TrimEnd(StringTrimEndCharacters);

			string[] datetimeElements = str.Split(DateTimeSplitCharacters);

			if (datetimeElements.Length != 7)
				return false;

			int year = int.Parse(datetimeElements[6]);

			int month = 0;
			switch (datetimeElements[1])
			{
				case "Jan": month = 1; break;
				case "Feb": month = 2; break;
				case "Mar": month = 3; break;
				case "Apr": month = 4; break;
				case "May": month = 5; break;
				case "Jun": month = 6; break;
				case "Jul": month = 7; break;
				case "Aug": month = 8; break;
				case "Sep": month = 9; break;
				case "Oct": month = 10; break;
				case "Nov": month = 11; break;
				case "Dec": month = 12; break;
			}

			if (month == 0)
				return false;

			int day = int.Parse(datetimeElements[2]);

			int hour = int.Parse(datetimeElements[3]);
			int min = int.Parse(datetimeElements[4]);
			int sec = int.Parse(datetimeElements[5]);

			dateTaken = new DateTime(year, month, day, hour, min, sec);

			return true;
		}

		public bool ReadAVIFDateTaken(byte[] exifTags, out DateTime dateTaken)
		{
			dateTaken = DateTime.MinValue;

			try
			{
				int index = 98; // magical index, not really documented...

				while (exifTags[index] != 0) index++; // skip first string
				while (exifTags[index] == 0) index++; // skip first zero padding

				while (exifTags[index] != 0) index++; // skip second string
				while (exifTags[index] == 0) index++; // skip second zero padding

				int start = index;
				while (exifTags[index] != 0) index++;

				string str = Encoding.ASCII.GetString(exifTags, start, index - start);

				string[] datetimeElements = str.Split(SpaceCharacter);

				if (datetimeElements.Length != 2)
					return false;

				string[] dateElements = datetimeElements[0].Split(ColonCharacter);
				if (dateElements.Length != 3)
					return false;

				string[] timeElements = datetimeElements[1].Split(ColonCharacter);
				if (timeElements.Length != 3)
					return false;

				dateTaken = new DateTime(
					int.Parse(dateElements[0]), int.Parse(dateElements[1]), int.Parse(dateElements[2]),
					int.Parse(timeElements[0]), int.Parse(timeElements[1]), int.Parse(timeElements[2]));

				return true;
			}
			catch (Exception)
			{
			}

			return false;
		}

	}

	internal class Chunk
	{
		internal Chunk Parent { get; private set; }
		internal string Id { get; private set; }
		internal uint DataOffset { get; private set; }
		internal uint DataSize { get; private set; }
		internal byte[] Data { get; private set; }
		internal string Type { get; private set; }
		internal List<Chunk> Children { get; private set; }

		internal static Chunk Read(BinaryReader reader)
		{
			if ((reader.BaseStream.Length - reader.BaseStream.Position) < 8)
				return null;

			var chunk = new Chunk();

			chunk.Children = new List<Chunk>();

			chunk.Id = Encoding.ASCII.GetString(reader.ReadBytes(4));
			chunk.DataSize = reader.ReadUInt32();

			if ((reader.BaseStream.Length - reader.BaseStream.Position) < chunk.DataSize)
				return null;

			return chunk;
		}

		internal void AddChild(Chunk chunk)
		{
			chunk.Parent = this;
			Children.Add(chunk);
		}

		internal bool ReadType(BinaryReader reader)
		{
			if ((reader.BaseStream.Length - reader.BaseStream.Position) < 4 || DataSize < 4)
				return false;

			Type = Encoding.ASCII.GetString(reader.ReadBytes(4)).Trim();

			return true;
		}

		internal void CheckPadding(BinaryReader reader)
		{
			// if payload size is odd, there is one padding byte added
			if ((DataSize & 1) == 1)
			{
				if ((reader.BaseStream.Length - reader.BaseStream.Position) > 0)
					reader.BaseStream.Seek(1, SeekOrigin.Current);
			}
		}

		internal void Skip(BinaryReader reader, bool typeAlreadyRead = false)
		{
			DataOffset = (uint)reader.BaseStream.Position;
			if (DataSize > 0)
			{
				long offset = DataSize;
				if (typeAlreadyRead)
					offset -= 4;
				reader.BaseStream.Seek(offset, SeekOrigin.Current);
			}
		}

		internal void ReadData(BinaryReader reader)
		{
			Data = reader.ReadBytes((int)DataSize);
		}

        private static readonly char[] PathSeparators = new char[] { '/', '\\' };
        private static readonly char[] PathTrimStartCharacters = new char[] { ' ', '\t', '/', '\\' };
        private static readonly char[] ColonCharacter = new char[] { ':' };

        internal Chunk FindChunk(string xpath)
		{
			if (xpath == null)
				throw new ArgumentNullException("xpath");

			xpath = xpath.TrimStart(PathTrimStartCharacters);

			if (string.IsNullOrWhiteSpace(xpath))
				throw new ArgumentException("xpath must be a valid string.");

			string[] xpathHighLow = xpath.Split(PathSeparators, 2);
			string[] chunkDescriptors = xpathHighLow[0].Split(ColonCharacter);

			if (chunkDescriptors.Length > 2)
				throw new FormatException(string.Format("Invalid xpath, a chunk descriptor can contains only one or two fields ({0} contains {1} fields)", xpathHighLow[0], chunkDescriptors.Length.ToString()));

			Chunk foundChunk = null;

			foreach (Chunk chunk in Children)
			{
				if (chunk.Id != chunkDescriptors[0])
					continue;

				if ((chunkDescriptors.Length == 1 && chunk.Type == null) || (chunkDescriptors.Length == 2 && chunk.Type == chunkDescriptors[1]))
				{
					foundChunk = chunk;
					break;
				}
			}

			if (foundChunk == null)
				return null;

            if (xpathHighLow.Length == 1)
				return foundChunk;
			else
				return foundChunk.FindChunk(xpathHighLow[1]);
		}

		public override string ToString()
		{
			if (Type != null)
				return string.Format("{0}:{1}", Id, Type);
			else
				return Id;
		}
	}
}
