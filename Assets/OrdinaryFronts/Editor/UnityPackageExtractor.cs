using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace OrdinaryFronts.Editor
{
    /// <summary>
    /// Extracts Unity's own .unitypackage payload without an asynchronous import dialog.
    /// This keeps TMP Essentials reliable in -batchmode, where ImportPackage can return
    /// before the package import has written its assets.
    /// </summary>
    internal static class UnityPackageExtractor
    {
        private sealed class Record
        {
            public string pathname;
            public byte[] asset;
            public byte[] meta;
        }

        internal static void Extract(string packagePath, string allowedAssetPrefix)
        {
            Dictionary<string, Record> records = ReadRecords(packagePath);
            string normalizedPrefix = allowedAssetPrefix.Replace('\\', '/');
            foreach (Record record in records.Values)
            {
                if (record == null || string.IsNullOrWhiteSpace(record.pathname)) continue;
                string assetPath = record.pathname.Trim().Replace('\\', '/');
                if (!assetPath.StartsWith(normalizedPrefix, StringComparison.Ordinal)) continue;
                if (assetPath.Contains("../") || Path.IsPathRooted(assetPath))
                    throw new InvalidDataException("Unitypackage güvenli olmayan yol içeriyor: " + assetPath);

                string destination = assetPath.Replace('/', Path.DirectorySeparatorChar);
                if (record.asset == null)
                {
                    Directory.CreateDirectory(destination);
                }
                else
                {
                    string parent = Path.GetDirectoryName(destination);
                    if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
                    File.WriteAllBytes(destination, record.asset);
                }
                if (record.meta != null) File.WriteAllBytes(destination + ".meta", record.meta);
            }
        }

        private static Dictionary<string, Record> ReadRecords(string packagePath)
        {
            Dictionary<string, Record> records = new Dictionary<string, Record>();
            using (FileStream file = File.OpenRead(packagePath))
            using (GZipStream gzip = new GZipStream(file, CompressionMode.Decompress))
            {
                byte[] header = new byte[512];
                while (ReadExactOrEnd(gzip, header))
                {
                    if (IsZeroBlock(header)) break;
                    string name = ReadTarString(header, 0, 100);
                    string prefix = ReadTarString(header, 345, 155);
                    if (!string.IsNullOrEmpty(prefix)) name = prefix + "/" + name;
                    long size = ReadOctal(header, 124, 12);
                    if (size < 0 || size > int.MaxValue) throw new InvalidDataException("Unitypackage girdisi desteklenmeyen boyutta: " + name);
                    byte[] payload = new byte[(int)size];
                    ReadExact(gzip, payload, payload.Length);
                    long padding = (512 - size % 512) % 512;
                    SkipExact(gzip, padding);

                    string[] parts = name.Replace('\\', '/').Split('/');
                    if (parts.Length < 2) continue;
                    string guid = parts[0];
                    string leaf = parts[parts.Length - 1];
                    Record record;
                    if (!records.TryGetValue(guid, out record))
                    {
                        record = new Record();
                        records.Add(guid, record);
                    }
                    if (leaf == "pathname") record.pathname = Encoding.UTF8.GetString(payload).TrimEnd('\0', '\r', '\n');
                    else if (leaf == "asset") record.asset = payload;
                    else if (leaf == "asset.meta") record.meta = payload;
                }
            }
            return records;
        }

        private static bool ReadExactOrEnd(Stream stream, byte[] buffer)
        {
            int offset = 0;
            while (offset < buffer.Length)
            {
                int count = stream.Read(buffer, offset, buffer.Length - offset);
                if (count == 0)
                {
                    if (offset == 0) return false;
                    throw new EndOfStreamException("Unitypackage tar başlığı yarım kaldı.");
                }
                offset += count;
            }
            return true;
        }

        private static void ReadExact(Stream stream, byte[] buffer, int count)
        {
            int offset = 0;
            while (offset < count)
            {
                int read = stream.Read(buffer, offset, count - offset);
                if (read == 0) throw new EndOfStreamException("Unitypackage girdisi yarım kaldı.");
                offset += read;
            }
        }

        private static void SkipExact(Stream stream, long count)
        {
            byte[] scratch = new byte[512];
            while (count > 0)
            {
                int requested = (int)Math.Min(scratch.Length, count);
                int read = stream.Read(scratch, 0, requested);
                if (read == 0) throw new EndOfStreamException("Unitypackage dolgusu yarım kaldı.");
                count -= read;
            }
        }

        private static bool IsZeroBlock(byte[] block)
        {
            for (int i = 0; i < block.Length; i++) if (block[i] != 0) return false;
            return true;
        }

        private static string ReadTarString(byte[] buffer, int offset, int length)
        {
            int end = offset;
            int limit = offset + length;
            while (end < limit && buffer[end] != 0) end++;
            return Encoding.UTF8.GetString(buffer, offset, end - offset).Trim();
        }

        private static long ReadOctal(byte[] buffer, int offset, int length)
        {
            long value = 0;
            int end = offset + length;
            int index = offset;
            while (index < end && (buffer[index] == 0 || buffer[index] == (byte)' ')) index++;
            for (; index < end; index++)
            {
                byte digit = buffer[index];
                if (digit == 0 || digit == (byte)' ') break;
                if (digit < (byte)'0' || digit > (byte)'7') throw new InvalidDataException("Geçersiz tar boyutu.");
                value = value * 8 + (digit - (byte)'0');
            }
            return value;
        }
    }
}
