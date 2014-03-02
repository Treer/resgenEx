namespace resgenEx.FileFormats
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Resources;
    using System.Text;

    /// <summary>
    /// Not currently being maintained
    /// </summary>
    class TxtResourceWriter : IResourceWriter
    {
        StreamWriter s;

        public TxtResourceWriter(Stream stream)
        {
            s = new StreamWriter(stream);
        }

        public void AddResource(string name, byte[] value)
        {
            throw new Exception("Binary data not valid in a text resource file");
        }

        public void AddResource(string name, object value)
        {
            if (value is string) {
                AddResource(name, (string)value);
                return;
            }
            throw new Exception("Objects not valid in a text resource file");
        }

        public void AddResource(string name, string value)
        {
            s.WriteLine("{0}={1}", name, Escape(value));
        }

        // \n -> \\n ...
        static string Escape(string value)
        {
            StringBuilder b = new StringBuilder();
            for (int i = 0; i < value.Length; i++) {
                switch (value[i]) {
                    case '\n':
                        b.Append("\\n");
                        break;
                    case '\r':
                        b.Append("\\r");
                        break;
                    case '\t':
                        b.Append("\\t");
                        break;
                    case '\\':
                        b.Append("\\\\");
                        break;
                    default:
                        b.Append(value[i]);
                        break;
                }
            }
            return b.ToString();
        }

        public void Close()
        {
            s.Close();
        }

        public void Dispose() { }

        public void Generate() { }
    }
}
