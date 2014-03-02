namespace resgenEx.FileFormats
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Resources;
    using System.Text;

    class PoResourceWriter : IResourceWriter
    {
        TextWriter s;
        CommentOptions commentOptions;
        bool headerWritten;
        string sourceFile = null;

        public PoResourceWriter(Stream stream, CommentOptions aCommentOptions) : this(stream, aCommentOptions, null) { }

        public PoResourceWriter(Stream stream, CommentOptions aCommentOptions, string aSourceFile)
        {
            s = new StreamWriter(stream);
            commentOptions = aCommentOptions;
            sourceFile = aSourceFile;
        }

        public string SourceFile
        {
            get { return sourceFile; }
            set { sourceFile = value; }
        }

        public void AddResource(string name, byte[] value)
        {
            throw new InvalidOperationException("Binary data not valid in a po resource file");
        }

        public void AddResource(string name, object value)
        {
            string comment = null;

            ResXDataNode dataNode = value as ResXDataNode;
            if (dataNode != null) {
                comment = dataNode.Comment;
                value = dataNode.GetValue(new AssemblyName[0]);
            }

            if (value is string) {
                AddResource(name, (string)value, comment, null);
                return;
            }
            throw new InvalidOperationException("Objects not valid in a po resource file: " + (value == null ? "null" : value.ToString()));
        }

        StringBuilder ebuilder = new StringBuilder();

        public string Escape(string ns)
        {
            ebuilder.Length = 0;

            // the empty string is used on the first line, to allow better alignment of the multi-line string to follow
            if (ns.Contains("\n")) ebuilder.Append("\"\r\n\"");

            foreach (char c in ns) {
                switch (c) {
                    case '"':
                    case '\\':
                        ebuilder.Append('\\');
                        ebuilder.Append(c);
                        break;
                    case '\a':
                        ebuilder.Append("\\a");
                        break;
                    case '\n':
                        ebuilder.Append("\\n\"\r\n\"");
                        break;
                    case '\r':
                        ebuilder.Append("\\r");
                        break;
                    default:
                        ebuilder.Append(c);
                        break;
                }
            }
            return ebuilder.ToString();
        }

        /// <param name="commentType">
        /// If the comment contains a new-line, this paramter will determine
        /// which type of comment it will be continued as after the newline. 
        /// '\0' for a translator-comment, '.' for an extracted comment, ':' for a reference etc
        /// </param>
        /// <param name="indent">
        /// If the comment contains a new-line, this paramter will determine how many
        /// spaces of indent will precede the comment when it continues after the newline. 
        /// </param>
        public string EscapeComment(string ns, char commentType, int indent)
        {
            string newlineReplacement = "\n#";
            if (commentType != '\0') newlineReplacement += commentType;
            if (indent > 0) newlineReplacement = newlineReplacement.PadRight(newlineReplacement.Length + indent, ' ');

            return ns.Replace("\n", newlineReplacement);
        }

        /// <param name="commentType">
        /// If the comment contains a new-line, this paramter will determine
        /// which type of comment it will be continued as after the newline. 
        /// '\0' for a translator-comment, '.' for an extracted comment, ':' for a reference etc
        /// </param>
        public string EscapeComment(string ns, char commentType)
        {
            return EscapeComment(ns, commentType, 0);
        }

        public void AddResource(string name, string value)
        {
            AddResource(name, value, null, null);
        }

        public void AddResource(string name, string value, string comment, string sourceReference)
        {
            if (!headerWritten) {
                headerWritten = true;
                WriteHeader();
            }

            if (commentOptions != CommentOptions.writeNoComments) {

                // if FullComments is set, then store the original message in a comment
                // so the file could be converted into a .pot file (.po template file)
                // without losing information.
                string originalMessage = null;
                if (commentOptions == CommentOptions.writeFullComments) {
                    originalMessage = value;
                    if (String.IsNullOrEmpty(sourceReference)) sourceReference = SourceFile;
                } else {
                    // Don't include automatically generated comments such as file reference
                    sourceReference = null;
                }

                if (!String.IsNullOrEmpty(comment)) {
                    // "#." in a .po file indicates an extracted comment
                    s.WriteLine("#. {0}", EscapeComment(comment, '.'));
                    if (!String.IsNullOrEmpty(originalMessage)) s.WriteLine("#. "); // leave an empty line between this comment and when we list the originalMessage
                }

                if (!String.IsNullOrEmpty(originalMessage)) {
                    // "#." in a .po file indicates an extracted comment
                    if (originalMessage.Contains("\n")) {
                        // Start multi-line messages indented on a new line, and have each new line in the message indented
                        s.WriteLine(ResGen.cOriginalMessageComment_Prefix + "\n#.    " + EscapeComment(originalMessage, '.', 4));
                    } else {
                        s.WriteLine(ResGen.cOriginalMessageComment_Prefix + EscapeComment(originalMessage, '.', 4));
                    }
                }

                if (!String.IsNullOrEmpty(sourceReference)) {
                    // "#:" in a .po file indicates a code reference comment, such as the line of source code the 
                    // string is used in, currently PoResourceWriter just inserts the source file name though.
                    s.WriteLine("#: {0}", EscapeComment(sourceReference, '.'));
                }
            }

            s.WriteLine("msgid \"{0}\"", Escape(name));
            s.WriteLine("msgstr \"{0}\"", Escape(value));
            s.WriteLine("");
        }

        void WriteHeader()
        {
            s.WriteLine("# This file was generated by " + ResGen.cProgramNameShort + " " + ResGen.cProgramVersion);
            if (!String.IsNullOrEmpty(SourceFile)) {
                s.WriteLine("#");
                s.WriteLine("# Converted to PO from:");
                s.WriteLine("#   " + sourceFile);
            }
            s.WriteLine("#");
            s.WriteLine("#, fuzzy"); // this flag will cause this header item to be ignored as a msgid when converted to .resx
            s.WriteLine("msgid \"\"");
            s.WriteLine("msgstr \"\"");
            s.WriteLine("\"MIME-Version: 1.0\\n\"");
            s.WriteLine("\"Content-Type: text/plain; charset=UTF-8\\n\"");
            s.WriteLine("\"Content-Transfer-Encoding: 8bit\\n\"");
            s.WriteLine("\"X-Generator: AdvaTel resgenEx 0.11\\n\"");
            /* Use msginit (a gettext tool) to fill in the header, missing header values detailed below
            s.WriteLine ("#\"Project-Id-Version: FILLME\\n\"");
            s.WriteLine ("#\"POT-Creation-Date: yyyy-MM-dd HH:MM+zzzz\\n\"");
            s.WriteLine ("#\"PO-Revision-Date: yyyy-MM-dd HH:MM+zzzz\\n\"");
            s.WriteLine ("#\"Last-Translator: FILLME\\n\"");
            s.WriteLine ("#\"Language-Team: FILLME\\n\"");
            s.WriteLine ("#\"Report-Msgid-Bugs-To: \\n\"");
             */
            s.WriteLine();
        }

        public void Close()
        {
            s.Close();
        }

        public void Dispose() { }

        public void Generate() { }
    }
}
