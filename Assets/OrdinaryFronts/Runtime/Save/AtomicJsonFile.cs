using System;
using System.IO;
using System.Text;

namespace OrdinaryFronts
{
    internal static class AtomicJsonFile
    {
        public static void Write(string path, string json)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            string tempPath = path + ".tmp";
            string backupPath = path + ".bak";

            byte[] bytes = new UTF8Encoding(false).GetBytes(json);
            using (FileStream stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush(true);
            }

            if (!File.Exists(path))
            {
                File.Move(tempPath, path);
                return;
            }

            try
            {
                File.Replace(tempPath, path, backupPath);
            }
            catch (Exception exception) when (exception is PlatformNotSupportedException || exception is IOException || exception is UnauthorizedAccessException)
            {
                File.Copy(path, backupPath, true);
                File.Copy(tempPath, path, true);
                File.Delete(tempPath);
            }
        }
    }
}
