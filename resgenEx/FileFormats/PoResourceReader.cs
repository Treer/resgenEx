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

    class PoResourceReader : IResourceReader
    {
        Dictionary<string, PoItem> data;
        //Directory<DictionaryEntry> data;

        Stream s;
        Options options;
        int line_num;

        public PoResourceReader(Stream stream, Options aOptions)
        {
            data = new Dictionary<string, PoItem>();
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

                if (lastChar == '\\') {

                    escapeCompleted = true;

                    switch (currentChar) {
                        case '\\': result.Append("\\"); break;
                        case '"': result.Append("\""); break;
                        case 'r': result.Append("\r"); break;
                        case 'n': result.Append("\n"); break;
                        case 't': result.Append("\t"); break;
                        default:
                            escapeCompleted = false;

                            result.Append(lastChar);
                            result.Append(currentChar);                            
                            break;
                    }
                } else if (currentChar != '\\') {
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

        void AddData(string msgid, string msgstr, string rawComments, bool fuzzy, int sourceLineNumber)
        {
            if (String.IsNullOrEmpty(msgid)) {
                Console.WriteLine("Error: Found empty msgid - will skip it. Line: " + sourceLineNumber);
            } else {
                PoItem item = new PoItem(msgid, msgstr, rawComments, fuzzy);
                item.Metadata_OriginalSourceLine = sourceLineNumber;

                if (data.ContainsKey(msgid)) {
                    Console.WriteLine(String.Format("Error: Found duplicate msgid {0} at line {1} - will overwrite the value from earlier instances.", msgid, sourceLineNumber));
                }
                data[msgid] = item;
            }
        }

        void Load()
        {
            StreamReader reader = new StreamReader(s);
            string line;

            string msgid = null;
            string msgstr = null;
            string rawComment = null;
            string rawCommentAccumulator = String.Empty;
            bool fuzzy = false;

            while ((line = reader.ReadLine()) != null) {
                line_num++;
                line = line.Trim();
                

                if (line.Length == 0) {
                    if (!String.IsNullOrEmpty(rawCommentAccumulator)) {
                        // include blank lines if a comment was started
                        rawCommentAccumulator += line + "\n";
                    }
                    continue;
                }

                if (line[0] == '#') {
                    rawCommentAccumulator += line + "\n";

                    if (line.Length > 1 && line[1] == ',') {
                        // it's a flag rawComments
                        if (line.IndexOf("fuzzy") != -1) fuzzy = true;
                    }
                    continue;
                }

                if (line.StartsWith("msgid ")) {
                    if (msgid == null && msgstr != null)
                        throw new FormatException("Found 2 consecutive msgid. Line: " + line_num);

                    // A new msgid has been encountered, so commit the last one
                    if (msgid != null && msgstr != null) {
                        AddData(msgid, msgstr, rawComment, fuzzy, line_num);
                    }

                    msgid = GetValue(line);
                    msgstr = null;
                    rawComment = rawCommentAccumulator;
                    rawCommentAccumulator = String.Empty;
                    fuzzy = false;

                    continue;
                }

                if (line.StartsWith("msgstr ")) {
                    if (msgid == null)
                        throw new FormatException("msgstr with no msgid. Line: " + line_num);

                    msgstr = GetValue(line);
                    continue;
                }

                if (line[0] == '"') {
                    if (msgid == null && msgstr == null)
                        throw new FormatException("Invalid format. Line: " + line_num);

                    if (msgstr == null) {
                        msgid += GetValue(line).Replace("\\r", "\r").Replace("\\n", "\n");
                    } else {
                        msgstr += GetValue(line);
                    }
                    continue;
                }

                throw new FormatException("Unexpected data. Line: " + line_num);
            }

            if (msgid != null) {
                if (msgstr == null)
                    throw new FormatException("Expecting msgstr. Line: " + line_num);

                AddData(msgid, msgstr, rawComment, fuzzy, line_num);
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
