// This file is GPL
//
// It is modified from files obtained from the mono project under GPL licence.
namespace resgenEx.FileFormats
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Resources;
    using System.Text;
    using System.Security.Principal;

    class IslResourceWriter : IResourceWriter
    {
        TextWriter s;
        Options options;
        bool headerWritten;
        string sourceFile = null;

        /// <summary>
        /// Override this in subclass if you want to write a .pot file instead of a .po file
        /// </summary>
        protected virtual bool WriteValuesAsBlank()
        {
            return false;
        }

        public IslResourceWriter(Stream stream, Options aOptions, string aSourceFile)
        {
            // The unicode version of InnoSetup still requires that its .isl files be ANSI.
            //
            // To do: The resourceReader needs to be queried to determine (or guess) the
            // appropriate codepage to use, encode the .isl in that format and include 
            // a header to specify the code page:
            // 
            //   [LangOptions]
            //   LanguageCodePage=<codepage>
            //
            // Command-line override of the output codepage should also be provided.
            //
            // For now we will use the default code page, as that is what our current .isl
            // files were in.
            s = new StreamWriter(stream, Encoding.Default);

            options = aOptions;
            sourceFile = aSourceFile;
        }

        public string SourceFile
        {
            get { return sourceFile; }
            set { sourceFile = value; }
        }

        StringBuilder ebuilder = new StringBuilder();

        public string Escape(string ns)
        {
            ebuilder.Length = 0;
            char lastChar = '\0';

            foreach (char c in ns) {
                switch (c) {
                    case '0':
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                    case '8':
                    case '9':
                        if (lastChar == '%') ebuilder.Append("%");
                        ebuilder.Append(c);
                        break;
                    case '%':
                        // wait until the next char to decide what to do with this one
                        break;
                    case '\n':
                        ebuilder.Append("%n");
                        break;
                    default:
                        if (lastChar == '%') ebuilder.Append("%%");
                        ebuilder.Append(c);
                        break;
                }
                lastChar = c;
            }
            if (lastChar == '%') ebuilder.Append("%%");

            return ebuilder.ToString();
        }

        /// <param name="commentType">
        /// Inserts comment markers after newlines
        /// </param>
        /// <param name="indent">
        /// If the rawComments contains a new-line, this paramter will determine how many
        /// spaces of indent will precede the rawComments when it continues after the newline. 
        /// </param>
        public string EscapeComment(string ns, int indent)
        {
            string newlineReplacement = "\n;";
            if (indent > 0) newlineReplacement = newlineReplacement.PadRight(newlineReplacement.Length + indent, ' ');

            return ns.Replace("\n", newlineReplacement);
        }

        /// <param name="commentType">
        /// If the rawComments contains a new-line, this paramter will determine
        /// which type of rawComments it will be continued as after the newline. 
        /// '\0' for a translator-rawComments, '.' for an extracted rawComments, ':' for a reference etc
        /// </param>
        public string EscapeComment(string ns)
        {
            return EscapeComment(ns, 1);
        }

        public void AddResource(string name, byte[] value)
        {
            AddResource(ResourceItem.Get(name, value));
        }

        public void AddResource(string name, object value)
        {
            AddResource(ResourceItem.Get(name, value));
        }

        public void AddResource(string name, string value)
        {
            AddResource(ResourceItem.Get(name, value));
        }

        public virtual void AddResource(ResourceItem item) 
        {        
            if (!headerWritten) {
                headerWritten = true;
                WriteHeader();
            }


            if (options.Comments != CommentOptions.writeNoComments) {
                if (!String.IsNullOrEmpty(item.Metadata_Comment)) {
                    // "#." in a .po file indicates an extracted rawComments
                    s.WriteLine("; {0}", EscapeComment(item.Metadata_Comment));                        
                }
            }

            string value = WriteValuesAsBlank() ? String.Empty : Escape(item.Value);

            s.WriteLine("{0}={1}", item.Name, value);
        }

        void WriteHeader()
        {
            s.WriteLine("; This file was generated by " + ResGen.cProgramNameShort + " " + ResGen.cProgramVersion);
            if (!String.IsNullOrEmpty(SourceFile)) {
                s.WriteLine(";");
                s.WriteLine("; Converted to .isl from:");
                s.WriteLine(";   " + sourceFile);
            }
            s.WriteLine(";");

            string usersIdentity = WindowsIdentity.GetCurrent().Name;
            if (String.IsNullOrEmpty(usersIdentity)) usersIdentity = "NAME";
            int slashPos = usersIdentity.LastIndexOf('\\');
            if (slashPos >= 0 && slashPos < usersIdentity.Length) usersIdentity = usersIdentity.Substring(slashPos + 1); // Drop the domain name from the user name, if it's present
            s.WriteLine("; By:\n;    " + usersIdentity + " <EMAIL@ADDRESS>\\n\"");
            s.WriteLine("; =======");

            s.WriteLine();
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
