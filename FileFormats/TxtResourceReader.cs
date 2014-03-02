// This file is GPL
//
// It is modified from files obtained from the mono project under GPL licence.
namespace resgenEx.FileFormats
{
    using System;
    using System.Collections;
    using System.Globalization;
    using System.IO;
    using System.Resources;
    using System.Text;

    /// <summary>
    /// Not currently being maintained
    /// </summary>
    class TxtResourceReader : IResourceReader
    {
        Hashtable data;
        Stream s;

        public TxtResourceReader(Stream stream)
        {
            data = new Hashtable();
            s = stream;
            Load();
        }

        public virtual void Close()
        {
        }

        public IDictionaryEnumerator GetEnumerator()
        {
            return data.GetEnumerator();
        }

        void Load()
        {
            StreamReader reader = new StreamReader(s);
            string line, key, val;
            int epos, line_num = 0;
            while ((line = reader.ReadLine()) != null) {
                line_num++;
                line = line.Trim();
                if (line.Length == 0 || line[0] == '#' ||
                    line[0] == ';')
                    continue;
                epos = line.IndexOf('=');
                if (epos < 0)
                    throw new Exception("Invalid format at line " + line_num);
                key = line.Substring(0, epos);
                val = line.Substring(epos + 1);
                key = key.Trim();
                val = val.Trim();
                if (key.Length == 0)
                    throw new Exception("Key is empty at line " + line_num);

                val = Unescape(val);
                if (val == null)
                    throw new Exception(String.Format("Unsupported escape character in value of key '{0}'.", key));


                data.Add(key, val);
            }
        }

        // \\n -> \n ...
        static string Unescape(string value)
        {
            StringBuilder b = new StringBuilder();

            for (int i = 0; i < value.Length; i++) {
                if (value[i] == '\\') {
                    if (i == value.Length - 1)
                        return null;

                    i++;
                    switch (value[i]) {
                        case 'n':
                            b.Append('\n');
                            break;
                        case 'r':
                            b.Append('\r');
                            break;
                        case 't':
                            b.Append('\t');
                            break;
                        case 'u':
                            int ch = int.Parse(value.Substring(++i, 4), NumberStyles.HexNumber);
                            b.Append(char.ConvertFromUtf32(ch));
                            i += 3;
                            break;
                        case '\\':
                            b.Append('\\');
                            break;
                        default:
                            return null;
                    }

                } else {
                    b.Append(value[i]);
                }
            }

            return b.ToString();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IResourceReader)this).GetEnumerator();
        }

        void IDisposable.Dispose() { }
    }
}
