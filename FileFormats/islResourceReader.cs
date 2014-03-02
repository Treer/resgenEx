// This file is GPL
//
// It is modified from files obtained from the mono project under GPL licence.
namespace resgenEx.FileFormats
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Resources;
    using System.Text;

    class IslResourceReader : IResourceReader
    {
        Dictionary<string, ResourceItem> data;

        Stream s;
        Options options;
        int line_num;

        public IslResourceReader(Stream stream, Options aOptions)
        {
            data = new Dictionary<string, ResourceItem>();
            s = stream;
            options = aOptions;
            Load();
        }

        public virtual void Close()
        {
            s.Close();
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            return (data as IDictionary).GetEnumerator();
        }

        string Unescape(string unescapedCString)
        {
            StringBuilder result = new StringBuilder();

            // System.Text.RegularExpressions.Regex.Unescape(result) would unescape many chars that
            // .po files don't escape (it escapes \, *, +, ?, |, {, [, (,), ^, $,., #, and white space), so I'm
            // doing it manually.

            char lastChar = '\0';
            bool escapeCompleted = false;
            for (int i = 0; i < unescapedCString.Length; i++) {

                char currentChar = unescapedCString[i];

                if (lastChar == '%') {

                    escapeCompleted = true;

                    switch (currentChar) {
                        case '%': result.Append("%"); break;
                        case 'n': result.Append("\n"); break;
                        default:
                            escapeCompleted = false;

                            result.Append(lastChar);
                            result.Append(currentChar);                            
                            break;
                    }
                } else if (currentChar != '%') {
                    result.Append(currentChar);
                }

                if (escapeCompleted) {
                    lastChar = '\0';
                    escapeCompleted = false;
                } else {
                    lastChar = currentChar;
                }
            }

            return result.ToString();
        }


        string GetValue(string line)
        {
            int begin = line.IndexOf('"');
            if (begin == -1)
                throw new FormatException(String.Format("No begin quote at line {0}: {1}", line_num, line));

            int end = line.LastIndexOf('"');
            if (end == -1)
                throw new FormatException(String.Format("No closing quote at line {0}: {1}", line_num, line));

            return Unescape(line.Substring(begin + 1, end - begin - 1));
        }

        void AddData(string msgid, string msgstr, string comment, int sourceLineNumber)
        {
            if (String.IsNullOrEmpty(msgid)) {
                Console.WriteLine("Error: Found empty msgid - will skip it. Line: " + sourceLineNumber);
            } else {
                ResourceItem item = new ResourceItem(msgid, msgstr, comment);
                item.Metadata_OriginalSourceLine = sourceLineNumber;

                if (data.ContainsKey(msgid)) {
                    Console.WriteLine(String.Format("Error: Found duplicate msgid {0} at line {1} - will overwrite the value from earlier instances.", msgid, sourceLineNumber));
                }
                data[msgid] = item;
            }
        }

        void Load()
        {
            StreamReader reader = new StreamReader(s, Encoding.Default);
            string line;

            //string msgid = null;
            //string msgstr = null;
            //string rawComment = null;
            string commentAccumulator = String.Empty;
            //bool fuzzy = false;

            while ((line = reader.ReadLine()) != null) {
                line_num++;
                line = line.Trim();
                

                if (line.Length == 0) {
                    // it's a blank line

                    if (!String.IsNullOrEmpty(commentAccumulator)) {
                        // include blank lines if a comment was started
                        commentAccumulator += line + "\n";
                    }

                } else if (line[0] == ';') {
                    // it's a comment

                    if (line.Length > 1 && line[1] == ' ') {
                        commentAccumulator += line.Substring(2) + "\n";
                    } else {
                        commentAccumulator += line.Substring(1) + "\n";
                    }
                    continue;

                } else if (line[0] == '[') {
                    // It's a new section, any comment lines we've accumulated are unlikely to
                    // be intended for the next item

                    commentAccumulator = String.Empty;

                } else {
                    int assignmentPos = line.IndexOf('=');
                    if (assignmentPos > 0) {
                        // it's a msgid and msgstr
                        AddData(
                            line.Substring(0, assignmentPos).Trim(),     //msgid
                            Unescape(line.Substring(assignmentPos + 1)), // msgstr
                            commentAccumulator,
                            line_num
                        );
                        commentAccumulator = String.Empty;
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void IDisposable.Dispose()
        {
            if (data != null)
                data = null;

            if (s != null) {
                s.Close();
                s = null;
            }
        }
    }
}
