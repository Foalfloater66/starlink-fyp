// (c) Francois GUIBERT, Frozax Games
//
// Free to use for personal and commercial uses.
// Tweet @Frozax if you like it.
//

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Utilities
{
    public class fgCSVReader
    {
        public delegate void ReadLineDelegate(int line_index, List<string> line);

        public static void LoadFromFile(string file_name, ReadLineDelegate line_reader)
        {
            LoadFromString(File.ReadAllText(file_name), line_reader);
        }

        public static void LoadFromString(string file_contents, ReadLineDelegate line_reader)
        {
            var file_length = file_contents.Length;
            //Debug.Log (file_length);
            // read char by char and when a , or \n, perform appropriate action
            var cur_file_index = 0; // index in the file
            var cur_line = new List<string>(); // current line of data
            var cur_line_number = 0;
            var cur_item = new StringBuilder("");
            var inside_quotes = false; // managing quotes
            while (cur_file_index < file_length)
            {
                var c = file_contents[cur_file_index++];

                switch (c)
                {
                    case '"':
                        if (!inside_quotes)
                        {
                            inside_quotes = true;
                        }
                        else
                        {
                            if (cur_file_index == file_length)
                            {
                                // end of file
                                inside_quotes = false;
                                goto case '\n';
                            }
                            else if (file_contents[cur_file_index] == '"')
                            {
                                // double quote, save one
                                cur_item.Append("\"");
                                cur_file_index++;
                            }
                            else
                            {
                                // leaving quotes section
                                inside_quotes = false;
                            }
                        }

                        break;
                    case '\r':
                        // ignore it completely
                        break;
                    case ',':
                        goto case '\n';
                    case '\n':
                        if (inside_quotes)
                        {
                            // inside quotes, this characters must be included
                            cur_item.Append(c);
                        }
                        else
                        {
                            // end of current item
                            cur_line.Add(cur_item.ToString());
                            cur_item.Length = 0;
                            if (c == '\n' || cur_file_index == file_length)
                            {
                                // also end of line, call line reader
                                line_reader(cur_line_number++, cur_line);
                                cur_line.Clear();
                            }
                        }

                        break;
                    default:
                        // other cases, add char
                        cur_item.Append(c);
                        break;
                }
            }
        }
    }
}