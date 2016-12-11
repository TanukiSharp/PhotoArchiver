using System;
using System.IO;
using System.Text;
using PhotoArchiver.Contracts;
//using System.ComponentModel.Composition;

namespace DateTakenExtractors.Extractors
{
	//[Export(typeof(IDateTakenExtractor))]
	public class QuickTimeDateTakenExtractor : IDateTakenExtractor
	{
		public bool ExtractDateTaken(Stream stream, out DateTime dateTaken)
		{
			dateTaken = DateTime.MinValue;

			try
			{
                using (BinaryReader reader = new BinaryReader(stream, Encoding.ASCII, true))
                {
                    Atom atom = Atom.Read(reader);

                    if (atom == null || atom.Type != "ftyp")
                        return false;

                    // skip 'ftyp' atom
                    atom.Skip(reader);

                    while ((atom = Atom.Read(reader)) != null)
                    {
                        if (atom.Type == "moov" || atom.Type == "trak")
                        {
                            // continue on 'moov' and 'trak' in order to avoid the atom Skip call at the end of the loop and enter these atoms.
                            // 'trak' atom in inside 'moov' atom, and 'tkhd' atom is inside 'trak' atom.
                            // + moov
                            // |
                            // +--+ trak
                            // |  |
                            // |  +-- tkhd
                            // :  :
                            continue;
                        }
                        else if (atom.Type == "tkhd")
                        {
                            // version: 1 byte
                            // falgs: 3 bytes
                            reader.BaseStream.Seek(4, SeekOrigin.Current); // creation date: 4 bytes
                                                                           // modification date: 4 bytes
                                                                           // ...

                            uint creationTime = reader.ReadUInt32BE();

                            // date encoding is the number of seconds elapsed from January 1st, 1904
                            // obtained from QuickTime file format specifications
                            DateTime dt = new DateTime(1904, 1, 1);
                            dateTaken = dt.AddSeconds(creationTime);

                            return true;
                        }

                        atom.Skip(reader);
                    }
                }
			}
			catch (Exception)
			{
			}

			return false;
		}
	}

	internal class Atom
	{
		internal ulong TotalSize { get; private set; }
		internal string Type { get; private set; }
		internal ulong PayloadSize { get; private set; }

		internal static Atom Read(BinaryReader reader)
		{
			if ((reader.BaseStream.Length - reader.BaseStream.Position) < 8)
				return null;

			Atom atom = new Atom();

			uint size = reader.ReadUInt32BE();
			atom.Type = Encoding.ASCII.GetString(reader.ReadBytes(4));

			if (size == 0)
			{
				atom.TotalSize = (ulong)reader.BaseStream.Length;
				atom.PayloadSize = atom.TotalSize - 8;
			}
			else if (size == 1)
			{
				if ((reader.BaseStream.Length - reader.BaseStream.Position) < 16)
					return null;

				atom.TotalSize = reader.ReadUInt64BE();
				atom.PayloadSize = atom.TotalSize - 16;
			}
			else
			{
				atom.TotalSize = size;
				atom.PayloadSize = atom.TotalSize - 8;
			}

			return atom;
		}

		public void Skip(BinaryReader reader)
		{
			reader.BaseStream.Seek((long)PayloadSize, SeekOrigin.Current);
		}
	}
}
