// This file was obtained under a MIT X11 licence from the mono project - Treer

/*
 * resgen: convert between the resource formats (.txt, .resources, .resx).
 *
 * Copyright (c) 2002 Ximian, Inc
 *
 * Authors:
 *	Paolo Molaro (lupus@ximian.com)
 *	Gonzalo Paniagua Javier (gonzalo@ximian.com)
 */

using System;
using System.Globalization;
using System.Text;
using System.IO;
using System.Collections;
using System.Resources;
using System.Reflection;
using System.Xml;

// We don't need to keep mono compatibility, so I'm referencing Windows.Forms directly
// to save having to do everything through reflection. - Treer
using System.Windows.Forms;

enum CommentOptions
{
    /// <summary>
    /// Export comments to the destination format that existed in the
    /// source file, and feel free to include any comments you can 
    /// automatically generate.
    /// </summary>
    writeFullComments,
    /// <summary>
    /// only export comments to the destination format that existed in the
    /// source file. Do not include automatically created comments.
    /// </summary>
    writeSourceCommentsOnly,
    /// <summary>
    /// don't export the comment from the source file to the destination 
    /// format, and don't include automatically created comments.
    /// </summary>
    writeNoComments
}

class ResGen {
    

	static Assembly swf;
	static Type resxr;
	static Type resxw;

    internal const string cProgramVersion = "v0.11";
    internal const string cProgramNameShort = "resgenEx";
    internal const string cProgramNameFull = "Extended Mono Resource Generator";
    internal const string cOriginalMessageComment_Prefix = "#. Original message text: ";

	/*
	 * We load the ResX format stuff on demand, since the classes are in 
	 * System.Windows.Forms (!!!) and we can't depend on that assembly in mono, yet.
	 */
	static void LoadResX () {
		if (swf != null)
			return;
		try {
			swf = Assembly.Load (Consts.AssemblySystem_Windows_Forms);
			resxr = swf.GetType ("System.Resources.ResXResourceReader");
			resxw = swf.GetType ("System.Resources.ResXResourceWriter");
		} catch (Exception e) {
			throw new Exception ("Cannot load support for ResX format: " + e.Message);
		}
	}

	static void Usage () {

		string Usage = cProgramNameFull + " " + cProgramVersion + //@"Mono Resource Generator version " + Consts.MonoVersion +
            @"
WARNING: this version has been specialized for use converting between '.resx'
and '.po' files, other formats will now fail!

Usage:
		resgen [options] source.ext [dest.ext]";
		Usage += @"

Convert a resource file from one format to another.
The currently supported formats are: '.resx' '.po'.
If the destination file is not specified, source.resources will be used.";

		Usage += @"

Options:
-nocomments, /noComments
    don't export the comment from the source file to the destination 
    format, and don't include automatically created comments.
-sourcecommentsonly, /sourceCommentsOnly
    only export comments to the destination format that existed in the
    source file. Do not include automatically created comments.
-usesourcepath, /useSourcePath
	to resolve relative file paths, use the directory of the resource 
	file as current directory.
    ";
		Usage += @"
";
		Console.WriteLine( Usage );
	}
	
	static IResourceReader GetReader (Stream stream, string name, bool useSourcePath, CommentOptions commentOptions) {
		string format = Path.GetExtension (name);
		switch (format.ToLower (System.Globalization.CultureInfo.InvariantCulture)) {
		case ".po":
                return new PoResourceReader(stream, commentOptions);
        /* this version has been specialized for use converting between '.resx' 
         * and '.po' files, other formats will now fail!
		case ".txt":
		case ".text":
			return new TxtResourceReader (stream);
		case ".resources":
			return new ResourceReader (stream);*/
		case ".resx":
			LoadResX ();
			IResourceReader reader = (IResourceReader) Activator.CreateInstance (
				resxr, new object[] {stream});
			if (useSourcePath) { // only possible on 2.0 profile, or higher
				PropertyInfo p = reader.GetType ().GetProperty ("BasePath",
					BindingFlags.Public | BindingFlags.Instance);
				if (p != null && p.CanWrite) {
					p.SetValue (reader, Path.GetDirectoryName (name), null);
				}
			}
            ((ResXResourceReader)reader).UseResXDataNodes = true;
			return reader;
		default:
			throw new Exception ("Unknown format in file " + name);
		}
	}
	
    /// <param name="sourceFile">Optional - allows us to add metadata in the destination header about where the generated file originated from</param>
	static IResourceWriter GetWriter (Stream stream, string name, CommentOptions commentOptions, string sourceFile) {
		string format = Path.GetExtension (name);
        string sourceResource = sourceFile ?? String.Empty;
        sourceResource = "\\" + sourceResource.Substring(Path.GetPathRoot(sourceResource).Length); // remove the drive name from the path, as it's computer specific

		switch (format.ToLower ()) {
		case ".po":
                return new PoResourceWriter(stream, commentOptions, sourceResource);
        /* this version has been specialized for use converting between '.resx' 
         * and '.po' files, other formats will now fail!
		case ".txt":
		case ".text":
			return new TxtResourceWriter (stream);
		case ".resources":
			return new ResourceWriter (stream);*/
		case ".resx":
			LoadResX ();
			return (IResourceWriter)Activator.CreateInstance (resxw, new object[] {stream});
		default:
			throw new Exception ("Unknown format in file " + name);
		}
	}
	
	static int CompileResourceFile (string sname, string dname, bool useSourcePath, CommentOptions commentOptions) {
		FileStream source = null;
		FileStream dest = null;
		IResourceReader reader = null;
		IResourceWriter writer = null;

		try {
			source = new FileStream (sname, FileMode.Open, FileAccess.Read);
            reader = GetReader(source, sname, useSourcePath, commentOptions);

			dest = new FileStream (dname, FileMode.Create, FileAccess.Write);
            writer = GetWriter(dest, dname, commentOptions, sname);

			int rescount = 0;            
			foreach (DictionaryEntry e in reader) {
				rescount++;
				object val = e.Value;
				if (val is string)
					writer.AddResource ((string)e.Key, (string)e.Value);
				else
					writer.AddResource ((string)e.Key, e.Value);
			}
			Console.WriteLine( "Read in {0} resources from '{1}'", rescount, sname );

			reader.Close ();
			writer.Close ();
			Console.WriteLine("Writing resource file...  Done.");
		} catch (Exception e) {
			Console.WriteLine ("Error: {0}", e.Message);
			Exception inner = e.InnerException;

			// under 2.0 ResXResourceReader can wrap an exception into an XmlException
			// and this hides some helpful message from the original exception
			XmlException xex = (inner as XmlException);
			if (xex != null) {
				// message is identical to the inner exception (from MWF ResXResourceReader)
				Console.WriteLine ("Position: Line {0}, Column {1}.", xex.LineNumber, xex.LinePosition);
				inner = inner.InnerException;
			}

			if (inner is TargetInvocationException && inner.InnerException != null)
				inner = inner.InnerException;
			if (inner != null)
				Console.WriteLine ("Inner exception: {0}", inner.Message);

			if (reader != null)
				reader.Dispose ();
			if (source != null)
				source.Close ();
			if (writer != null)
				writer.Dispose ();
			if (dest != null)
				dest.Close ();

			// since we're not first reading all entries in source, we may get a
			// read failure after we're started writing to the destination file
			// and leave behind a broken resources file, so remove it here
			try {
				File.Delete (dname);
			} catch {
			}
			return 1;
		}
		return 0;
	}
	
	static int Main (string[] args) {
		bool compileMultiple = false;
		bool useSourcePath = false;
		ArrayList inputFiles = new ArrayList ();
        CommentOptions commentOptions = CommentOptions.writeFullComments;

		for (int i = 0; i < args.Length; i++) {
			switch (args [i].ToLower ()) {
			case "-h":
			case "/h":
			case "-?":
			case "/?":
				Usage ();
				return 1;
            /* this version has been specialized for use converting between '.resx' 
             * and '.po' files, other formats will now fail! /compile is only used
             * for '.resources' files.
			case "/compile":
			case "-compile":
                // takes a list of .resX or .txt files to convert to .resources files
	            // in one bulk operation, replacing .ext with .resources for the 
	            // output file name (if not set).

				if (inputFiles.Count > 0) {
					// the /compile option should be specified before any files
					Usage ();
					return 1;
				}
				compileMultiple = true;
				break;
            */
			case "/usesourcepath":
			case "-usesourcepath":
                // to resolve relative file paths, use the directory of the resource 
	            // file as current directory.

				if (compileMultiple) {
					// the /usesourcepath option should not appear after the
					// /compile switch on the command-line
					Console.WriteLine ("ResGen : error RG0000: Invalid "
						+ "command line syntax.  Switch: \"/compile\"  Bad value: "
						+ args [i] + ".  Use ResGen /? for usage information.");
					return 1;
				}
				useSourcePath = true;
				break;

            case "/nocomments":
            case "-nocomments":
                // don't export the comment from the source file to the destination 
                // format, and don't include automatically created comments.

                if (commentOptions == CommentOptions.writeSourceCommentsOnly) {
                    // the /nocomments option should not appear after the
                    // /sourcecommentsonly switch on the command-line
                    Console.WriteLine("ResGen : error RG0000: Invalid command line syntax.  Switch: \"/nocomments\" cannot be used with \"/sourcecommentsonly\"");
                    return 1;
                }
                commentOptions = CommentOptions.writeNoComments;
                break;

            case "/sourcecommentsonly":
            case "-sourcecommentsonly":
                // only export comments to the destination format that existed in the
                // source file. Do not include automatically created comments.

                if (commentOptions == CommentOptions.writeNoComments) {
                    // the /nocomments option should not appear after the
                    // /sourcecommentsonly switch on the command-line
                    Console.WriteLine("ResGen : error RG0000: Invalid command line syntax.  Switch: \"/nocomments\" cannot be used with \"/sourcecommentsonly\"");
                    return 1;
                }
                commentOptions = CommentOptions.writeSourceCommentsOnly;
                break;

			default:
				if (!IsFileArgument (args [i])) {
					Usage ();
					return 1;
				}

				ResourceInfo resInf = new ResourceInfo ();
				if (compileMultiple) {
					string [] pair = args [i].Split (',');
					switch (pair.Length) {
					case 1:
						resInf.InputFile = Path.GetFullPath (pair [0]);
						resInf.OutputFile = Path.ChangeExtension (resInf.InputFile,
							"resources");
						break;
					case 2:
						if (pair [1].Length == 0) {
							Console.WriteLine (@"error: You must specify an input & outfile file name like this:");
							Console.WriteLine ("inFile.txt,outFile.resources.");
							Console.WriteLine ("You passed in '{0}'.", args [i]);
							return 1;
						}
						resInf.InputFile = Path.GetFullPath (pair [0]);
						resInf.OutputFile = Path.GetFullPath (pair [1]);
						break;
					default:
						Usage ();
						return 1;
					}
				} else {
					if ((i + 1) < args.Length) {
						resInf.InputFile = Path.GetFullPath (args [i]);
						// move to next arg, since we assume that one holds
						// the name of the output file
						i++;
						resInf.OutputFile = Path.GetFullPath (args [i]);
					} else {
						resInf.InputFile = Path.GetFullPath (args [i]);
						resInf.OutputFile = Path.ChangeExtension (resInf.InputFile,
							"resources");
					}
				}
				inputFiles.Add (resInf);
				break;
			}
		}

		if (inputFiles.Count == 0) {
			Usage ();
			return 1;
		}

		foreach (ResourceInfo res in inputFiles) {
            int ret = CompileResourceFile(res.InputFile, res.OutputFile, useSourcePath, commentOptions);
			if (ret != 0 )
				return ret;
		}
		return 0;
	}

	private static bool RunningOnUnix {
		get {
			// check for Unix platforms - see FAQ for more details
			// http://www.mono-project.com/FAQ:_Technical#How_to_detect_the_execution_platform_.3F
			int platform = (int) Environment.OSVersion.Platform;
			return ((platform == 4) || (platform == 128) || (platform == 6));
		}
	}

	private static bool IsFileArgument (string arg)
	{
		if ((arg [0] != '-') && (arg [0] != '/'))
			return true;

		// cope with absolute filenames for resx files on unix, as
		// they also match the option pattern
		//
		// `/home/test.resx' is considered as a resx file, however
		// '/test.resx' is considered as error
		return (RunningOnUnix && arg.Length > 2 && arg.IndexOf ('/', 2) != -1);
	}
}

class TxtResourceWriter : IResourceWriter {
	StreamWriter s;
	
	public TxtResourceWriter (Stream stream) {
		s = new StreamWriter (stream);
	}
	
	public void AddResource (string name, byte[] value) {
		throw new Exception ("Binary data not valid in a text resource file");
	}
	
	public void AddResource (string name, object value) {
		if (value is string) {
			AddResource (name, (string)value);
			return;
		}
		throw new Exception ("Objects not valid in a text resource file");
	}
	
	public void AddResource (string name, string value) {
		s.WriteLine ("{0}={1}", name, Escape (value));
	}

	// \n -> \\n ...
	static string Escape (string value)
	{
		StringBuilder b = new StringBuilder ();
		for (int i = 0; i < value.Length; i++) {
			switch (value [i]) {
			case '\n':
				b.Append ("\\n");
				break;
			case '\r':
				b.Append ("\\r");
				break;
			case '\t':
				b.Append ("\\t");
				break;
			case '\\':
				b.Append ("\\\\");
				break;
			default:
				b.Append (value [i]);
				break;
			}
		}
		return b.ToString ();
	}
	
	public void Close () {
		s.Close ();
	}
	
	public void Dispose () {}
	
	public void Generate () {}
}

class TxtResourceReader : IResourceReader {
	Hashtable data;
	Stream s;
	
	public TxtResourceReader (Stream stream) {
		data = new Hashtable ();
		s = stream;
		Load ();
	}
	
	public virtual void Close () {
	}
	
	public IDictionaryEnumerator GetEnumerator() {
		return data.GetEnumerator ();
	}
	
	void Load () {
		StreamReader reader = new StreamReader (s);
		string line, key, val;
		int epos, line_num = 0;
		while ((line = reader.ReadLine ()) != null) {
			line_num++;
			line = line.Trim ();
			if (line.Length == 0 || line [0] == '#' ||
			    line [0] == ';')
				continue;
			epos = line.IndexOf ('=');
			if (epos < 0) 
				throw new Exception ("Invalid format at line " + line_num);
			key = line.Substring (0, epos);
			val = line.Substring (epos + 1);
			key = key.Trim ();
			val = val.Trim ();
			if (key.Length == 0) 
				throw new Exception ("Key is empty at line " + line_num);

			val = Unescape (val);
			if (val == null)
				throw new Exception (String.Format ("Unsupported escape character in value of key '{0}'.", key));


			data.Add (key, val);
		}
	}

	// \\n -> \n ...
	static string Unescape (string value)
	{
		StringBuilder b = new StringBuilder ();

		for (int i = 0; i < value.Length; i++) {
			if (value [i] == '\\') {
				if (i == value.Length - 1)
					return null;

				i++;
				switch (value [i]) {
				case 'n':
					b.Append ('\n');
					break;
				case 'r':
					b.Append ('\r');
					break;
				case 't':
					b.Append ('\t');
					break;
				case 'u':
					int ch = int.Parse (value.Substring (++i, 4), NumberStyles.HexNumber);
					b.Append (char.ConvertFromUtf32 (ch));
					i += 3;
					break;
				case '\\':
					b.Append ('\\');
					break;
				default:
					return null;
				}

			} else {
				b.Append (value [i]);
			}
		}

		return b.ToString ();
	}
	
	IEnumerator IEnumerable.GetEnumerator () {
		return ((IResourceReader) this).GetEnumerator();
	}

	void IDisposable.Dispose () {}
}

class PoResourceReader : IResourceReader {
	Hashtable data;
	Stream s;
    CommentOptions commentOptions;
	int line_num;
	
	public PoResourceReader (Stream stream, CommentOptions aCommentOptions)
	{
		data = new Hashtable ();
		s = stream;
        commentOptions = aCommentOptions;
		Load ();
	}
	
	public virtual void Close ()
	{
		s.Close ();
	}
	
	public IDictionaryEnumerator GetEnumerator()
	{
		return data.GetEnumerator ();
	}
	
	string GetValue (string line)
	{
		int begin = line.IndexOf ('"');
		if (begin == -1)
			throw new FormatException (String.Format ("No begin quote at line {0}: {1}", line_num, line));

		int end = line.LastIndexOf ('"');
		if (end == -1)
			throw new FormatException (String.Format ("No closing quote at line {0}: {1}", line_num, line));

		return line.Substring (begin + 1, end - begin - 1);
	}

    void AddData(string msgid, string msgstr, string comment, int sourceLineNumber)
    {
        if (String.IsNullOrEmpty(msgid)) {
            Console.WriteLine("Error: Found empty msgid - will skip it. Line: " + sourceLineNumber);
        } else {
            if (String.IsNullOrEmpty(comment)) {
                data.Add(msgid, msgstr);
            } else {
                ResXDataNode dataNode = new ResXDataNode(msgid, msgstr);
                dataNode.Comment = comment;
                data.Add(msgid, dataNode);
            }
        }
    }

	void Load ()
	{
		StreamReader reader = new StreamReader (s);
		string line;
		string msgid = null;
		string msgstr = null;
        string comment = null;
		bool ignoreNext = false;
        bool ignoreNextExtractedComment = false;

		while ((line = reader.ReadLine ()) != null) {
			line_num++;
			line = line.Trim ();
			if (line.Length == 0) {
                comment = null;
				continue;
            }
				
			if (line [0] == '#') {

                if (line.Length > 1 && line[1] == '.') {

                    if (!ignoreNextExtractedComment) {

                        // It's an extracted comment
                        if (line.StartsWith(ResGen.cOriginalMessageComment_Prefix)) {
                            // It's one of our auto generated comments
                            /* There's no place in .resx files for these, ignore it.
                            if (commentOptions == CommentOptions.writeFullComments) {
                                comment = (comment == null ? String.Empty : comment + "\n");
                                comment += line.Substring(ResGen.cOriginalMessageComment_Prefix.Length);
                            }*/
                            // The presence of an auto generated comment probably means there was a blank line added to the end of the comment.
                            if (comment != null && comment.Length > 0 && (comment[comment.Length - 1] == '\n')) {
                                comment = comment.Substring(0, comment.Length - 1);
                            }
                            ignoreNextExtractedComment = true; // it might be a multiline autogenerated comment, so ignore any #. comments from now on

                        } else {
                            // It's a normal extracted comment
                            if (commentOptions != CommentOptions.writeNoComments) {
                                comment = (comment == null ? String.Empty : comment + "\n");
                                comment += line.Substring(2).TrimStart();
                            }
                        }
                    }
                }


				if (line.Length == 1 || line [1] != ',')
					continue;

				if (line.IndexOf ("fuzzy") != -1) {
					ignoreNext = true;
					if (msgid != null) {
						if (msgstr == null)
							throw new FormatException ("Error. Line: " + line_num);

                        AddData(msgid, msgstr, comment, line_num);
						msgid = null;
						msgstr = null;
                        comment = null;
                        ignoreNextExtractedComment = false;
					}
				}
				continue;
			}
			
			if (line.StartsWith ("msgid ")) {
				if (msgid == null && msgstr != null)
					throw new FormatException ("Found 2 consecutive msgid. Line: " + line_num);

				if (msgstr != null) {
					if (!ignoreNext)
                        AddData(msgid, msgstr, comment, line_num);

					ignoreNext = false;
					msgid = null;
					msgstr = null;
                    comment = null;
                    ignoreNextExtractedComment = false;
				}

				msgid = GetValue (line);
				continue;
			}

			if (line.StartsWith ("msgstr ")) {
				if (msgid == null)
					throw new FormatException ("msgstr with no msgid. Line: " + line_num);

				msgstr = GetValue (line);
				continue;
			}

			if (line [0] == '"') {
				if (msgid == null || msgstr == null)
					throw new FormatException ("Invalid format. Line: " + line_num);

				msgstr += GetValue (line);
				continue;
			}

			throw new FormatException ("Unexpected data. Line: " + line_num);
		}

		if (msgid != null) {
			if (msgstr == null)
				throw new FormatException ("Expecting msgstr. Line: " + line_num);

            if (!ignoreNext) {
                AddData(msgid, msgstr, comment, line_num);
            }
		}
	}
	
	IEnumerator IEnumerable.GetEnumerator ()
	{
		return GetEnumerator();
	}

	void IDisposable.Dispose ()
	{
		if (data != null)
			data = null;

		if (s != null) {
			s.Close ();
			s = null;
		}
	}
}

class PoResourceWriter : IResourceWriter
{
	TextWriter s;
    CommentOptions commentOptions;
	bool headerWritten;
    string sourceFile = null;

    public PoResourceWriter(Stream stream, CommentOptions aCommentOptions) : this(stream, aCommentOptions, null) { }

	public PoResourceWriter (Stream stream, CommentOptions aCommentOptions, string aSourceFile)
	{
		s = new StreamWriter (stream);
        commentOptions = aCommentOptions;
        sourceFile = aSourceFile;
	}

    public string SourceFile
    {
        get { return sourceFile; }
        set { sourceFile = value; }
    }

	public void AddResource (string name, byte [] value)
	{
		throw new InvalidOperationException ("Binary data not valid in a po resource file");
	}
	
	public void AddResource (string name, object value)
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
		throw new InvalidOperationException ("Objects not valid in a po resource file: " + (value == null ? "null" : value.ToString()));
	}

	StringBuilder ebuilder = new StringBuilder ();
	
	public string Escape (string ns)
	{
		ebuilder.Length = 0;

        // the empty string is used on the first line, to allow better alignment of the multi-line string to follow
        if (ns.Contains("\n")) ebuilder.Append ("\"\r\n\"");

		foreach (char c in ns){
			switch (c){
			case '"':
			case '\\':
				ebuilder.Append ('\\');
				ebuilder.Append (c);
				break;
			case '\a':
				ebuilder.Append ("\\a");
				break;
			case '\n':
				ebuilder.Append ("\\n\"\r\n\"");
				break;
			case '\r':
				ebuilder.Append ("\\r");
				break;
			default:
				ebuilder.Append (c);
				break;
			}
		}
		return ebuilder.ToString ();
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

	public void AddResource (string name, string value)
    {
        AddResource(name, value, null, null);
    }
	
	public void AddResource (string name, string value, string comment, string sourceReference)
	{
		if (!headerWritten) {
			headerWritten = true;
			WriteHeader ();
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
		s.WriteLine ("msgstr \"{0}\"", Escape (value));
		s.WriteLine ("");
	}
	
	void WriteHeader ()
	{
        s.WriteLine("# This file was generated by " + ResGen.cProgramNameShort + " " + ResGen.cProgramVersion);
        if (!String.IsNullOrEmpty(SourceFile)) {
            s.WriteLine("#");
            s.WriteLine("# Converted to PO from:");
            s.WriteLine("#   " + sourceFile);
        }
        s.WriteLine("#");
        s.WriteLine ("#, fuzzy"); // this flag will cause this header item to be ignored as a msgid when converted to .resx
		s.WriteLine ("msgid \"\"");
		s.WriteLine ("msgstr \"\"");
		s.WriteLine ("\"MIME-Version: 1.0\\n\"");
		s.WriteLine ("\"Content-Type: text/plain; charset=UTF-8\\n\"");
		s.WriteLine ("\"Content-Transfer-Encoding: 8bit\\n\"");
		s.WriteLine ("\"X-Generator: AdvaTel resgenEx 0.11\\n\"");
        /* Use msginit (a gettext tool) to fill in the header, missing header values detailed below
		s.WriteLine ("#\"Project-Id-Version: FILLME\\n\"");
		s.WriteLine ("#\"POT-Creation-Date: yyyy-MM-dd HH:MM+zzzz\\n\"");
		s.WriteLine ("#\"PO-Revision-Date: yyyy-MM-dd HH:MM+zzzz\\n\"");
		s.WriteLine ("#\"Last-Translator: FILLME\\n\"");
		s.WriteLine ("#\"Language-Team: FILLME\\n\"");
		s.WriteLine ("#\"Report-Msgid-Bugs-To: \\n\"");
         */ 
		s.WriteLine ();
	}

	public void Close ()
	{
		s.Close ();
	}
	
	public void Dispose () { }
	
	public void Generate () {}
}

class ResourceInfo
{
	public string InputFile;
	public string OutputFile;
}
