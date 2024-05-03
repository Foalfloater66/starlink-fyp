using System.IO;

namespace Utilities
{
    public class FileWriter
    {
        private readonly System.IO.StreamWriter _file;

        public FileWriter(string directory, string filename)
        {
            string fullFilename = $"{directory}/{filename}.txt";
            if (File.Exists(fullFilename))
            {
                File.Delete(fullFilename);
            }
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