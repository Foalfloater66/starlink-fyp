using System.IO;

namespace Utilities
{
    public class FileWriter
    {
        private readonly StreamWriter _file;

        public FileWriter(string directory, string filename)
        {
            // TODO: remove this and replace usage with a StreamWriter
            var fullFilename = $"{directory}/{filename}.csv";
            if (File.Exists(fullFilename)) File.Delete(fullFilename);
            _file = new StreamWriter(fullFilename);
        }

        public void Write(string text)
        {
            _file.Write(text);
        }

        public void WriteLine(string text)
        {
            _file.WriteLine(text);
        }

        public void Flush()
        {
            _file.Flush();
        }
    }
}