// This file is MIT X11 licensed
//
// It is modified from files obtained from the mono project under MIT X11 licence.

/*
 * resgen: convert between the resource formats (.txt, .resources, .resx).
 *
 * Copyright (c) 2002 Ximian, Inc
 *
 * Authors:
 *	Paolo Molaro (lupus@ximian.com)
 *	Gonzalo Paniagua Javier (gonzalo@ximian.com)
 */
namespace resgenEx
{

    using System;
    using System.Globalization;
    using System.Text;
    using System.IO;
    using System.Collections;
    using System.Resources;
    using System.Reflection;
    using System.Xml;

    using resgenEx.FileFormats;

    // We don't need to keep mono compatibility, so I'm referencing Windows.Forms directly
    // to save having to do everything through reflection. - Treer
    using System.Windows.Forms;

    class ResGen
    {


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
        static void LoadResX()
        {
            if (swf != null)
                return;
            try {
                swf = Assembly.Load(Consts.AssemblySystem_Windows_Forms);
                resxr = swf.GetType("System.Resources.ResXResourceReader");
                resxw = swf.GetType("System.Resources.ResXResourceWriter");
            } catch (Exception e) {
                throw new Exception("Cannot load support for ResX format: " + e.Message);
            }
        }

        static void Usage()
        {

            string Usage = cProgramNameFull + " " + cProgramVersion + //@"Mono Resource Generator version " + Consts.MonoVersion +
                @"
WARNING: this version has been specialized for use converting between '.resx'
and '.po' files, other formats will now fail!

Usage:
		resgen [options] source.ext [dest.ext]";
            Usage += @"

Convert a resource file from one format to another.
The currently supported formats are: '.resx' '.po' '.pot'.
If the destination file is not specified, source.resources will be used.";

            Usage += @"

Options:
-nocomments, /noComments
    don't export the rawComments from the source file to the destination 
    format, and don't include automatically created comments.

-sourcecommentsonly, /sourceCommentsOnly
    only export comments to the destination format that existed in the
    source file. Do not include automatically created comments.

-addformatflags, /addFormatFlags
    when exporting to .po or .pot, flags such as csharp-format will 
    be added to strings which contain format specifications, e.g. ""{0}""

-usesourcepath, /useSourcePath
	to resolve relative file paths, use the directory of the resource 
	file as current directory.
    ";
            Usage += @"
";
            Console.WriteLine(Usage);
        }

        static IResourceReader GetReader(Stream stream, string name, bool useSourcePath, Options options)
        {
            string format = Path.GetExtension(name);
            switch (format.ToLower(System.Globalization.CultureInfo.InvariantCulture)) {
                case ".po":
                case ".pot":
                    return new PoResourceReader(stream, options);
                /* this version has been specialized for use converting between '.resx' 
                 * and '.po' files, other formats will now fail!
                case ".txt":
                case ".text":
                    return new TxtResourceReader (stream);
                case ".resources":
                    return new ResourceReader (stream);*/
                case ".resx":
                    LoadResX();
                    IResourceReader reader = (IResourceReader)Activator.CreateInstance(
                        resxr, new object[] { stream });
                    if (useSourcePath) { // only possible on 2.0 profile, or higher
                        PropertyInfo p = reader.GetType().GetProperty("BasePath",
                            BindingFlags.Public | BindingFlags.Instance);
                        if (p != null && p.CanWrite) {
                            p.SetValue(reader, Path.GetDirectoryName(name), null);
                        }
                    }
                    ((ResXResourceReader)reader).UseResXDataNodes = true;
                    return reader;
                default:
                    throw new Exception("Unknown format in file " + name);
            }
        }

        /// <param name="sourceFile">Optional - allows us to add metadata in the destination header about where the generated file originated from</param>
        static IResourceWriter GetWriter(Stream stream, string name, Options options, string sourceFile)
        {
            string format = Path.GetExtension(name);
            string sourceResource = sourceFile ?? String.Empty;
            sourceResource = "\\" + sourceResource.Substring(Path.GetPathRoot(sourceResource).Length); // remove the drive name from the path, as it's computer specific

            switch (format.ToLower()) {
                case ".po":
                    return new PoResourceWriter(stream, options, sourceResource);
                case ".pot":
                    return new PotResourceWriter(stream, options, sourceResource);
                /* this version has been specialized for use converting between '.resx' 
                 * and '.po' files, other formats will now fail!
                case ".txt":
                case ".text":
                    return new TxtResourceWriter (stream);
                case ".resources":
                    return new ResourceWriter (stream);*/
                case ".resx":
                    LoadResX();
                    return (IResourceWriter)Activator.CreateInstance(resxw, new object[] { stream });
                default:
                    throw new Exception("Unknown format in file " + name);
            }
        }

        static int CompileResourceFile(string sname, string dname, bool useSourcePath, Options options)
        {
            FileStream source = null;
            FileStream dest = null;
            IResourceReader reader = null;
            IResourceWriter writer = null;

            try {
                source = new FileStream(sname, FileMode.Open, FileAccess.Read);
                reader = GetReader(source, sname, useSourcePath, options);

                dest = new FileStream(dname, FileMode.Create, FileAccess.Write);
                writer = GetWriter(dest, dname, options, sname);

                int rescount = 0;
                foreach (DictionaryEntry e in reader) {
                    rescount++;
                    object val = e.Value;
                    if (val is string) {
                        writer.AddResource((string)e.Key, (string)e.Value);
                    } else {
                        // refactoring to do: We should probably wrap the ResXResourceWriter, and replace our use of IResourceWriter with a ResourceItem based interface
                        if (writer is ResXResourceWriter && val is ResourceItem) {
                            // only write if the ResourceItem can be cast to ResXDataNode
                            ResXDataNode dataNode = ((ResourceItem)val).ToResXDataNode();
                            if (dataNode != null) writer.AddResource((string)e.Key, dataNode);
                        } else {
                            writer.AddResource((string)e.Key, e.Value);
                        }
                    }
                }
                Console.WriteLine("Read in {0} resources from '{1}'", rescount, sname);

                reader.Close();
                writer.Close();
                Console.WriteLine("Writing resource file...  Done.");
            } catch (Exception e) {
                Console.WriteLine("Error: {0}", e.Message);
                Exception inner = e.InnerException;

                // under 2.0 ResXResourceReader can wrap an exception into an XmlException
                // and this hides some helpful message from the original exception
                XmlException xex = (inner as XmlException);
                if (xex != null) {
                    // message is identical to the inner exception (from MWF ResXResourceReader)
                    Console.WriteLine("Position: Line {0}, Column {1}.", xex.LineNumber, xex.LinePosition);
                    inner = inner.InnerException;
                }

                if (inner is TargetInvocationException && inner.InnerException != null)
                    inner = inner.InnerException;
                if (inner != null)
                    Console.WriteLine("Inner exception: {0}", inner.Message);

                if (reader != null)
                    reader.Dispose();
                if (source != null)
                    source.Close();
                if (writer != null)
                    writer.Dispose();
                if (dest != null)
                    dest.Close();

                // since we're not first reading all entries in source, we may get a
                // read failure after we're started writing to the destination file
                // and leave behind a broken resources file, so remove it here
                try {
                    File.Delete(dname);
                } catch {
                }
                return 1;
            }
            return 0;
        }

        static int Main(string[] args)
        {
            bool compileMultiple = false;
            bool useSourcePath = false;
            ArrayList inputFiles = new ArrayList();
            Options options = new Options(CommentOptions.writeFullComments, false);

            for (int i = 0; i < args.Length; i++) {
                switch (args[i].ToLower()) {
                    case "-h":
                    case "/h":
                    case "-?":
                    case "/?":
                        Usage();
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
                            Console.WriteLine("ResGen : error RG0000: Invalid "
                                + "command line syntax.  Switch: \"/compile\"  Bad value: "
                                + args[i] + ".  Use ResGen /? for usage information.");
                            return 1;
                        }
                        useSourcePath = true;
                        break;

                    case "/nocomments":
                    case "-nocomments":
                        // don't export the rawComments from the source file to the destination 
                        // format, and don't include automatically created comments.

                        if (options.Comments == CommentOptions.writeSourceCommentsOnly) {
                            // the /nocomments option should not appear after the
                            // /sourcecommentsonly switch on the command-line
                            Console.WriteLine("ResGen : error RG0000: Invalid command line syntax.  Switch: \"/nocomments\" cannot be used with \"/sourcecommentsonly\"");
                            return 1;
                        }
                        options.Comments = CommentOptions.writeNoComments;
                        break;

                    case "/sourcecommentsonly":
                    case "-sourcecommentsonly":
                        // only export comments to the destination format that existed in the
                        // source file. Do not include automatically created comments.

                        if (options.Comments == CommentOptions.writeNoComments) {
                            // the /nocomments option should not appear after the
                            // /sourcecommentsonly switch on the command-line
                            Console.WriteLine("ResGen : error RG0000: Invalid command line syntax.  Switch: \"/nocomments\" cannot be used with \"/sourcecommentsonly\"");
                            return 1;
                        }
                        options.Comments = CommentOptions.writeSourceCommentsOnly;
                        break;

                    case "/addformatflags":
                    case "-addformatflags":
                        // Format flags in a .po like csharp-format tells the tools to check that the msgid and msgstr 
                        // contain the same number of format specifications, but if your not using the english 
                        // strings as msgids then this just creates a bunch of erroneous warnings.

                        options.FormatFlags = true;
                        break;

                    default:
                        if (!IsFileArgument(args[i])) {
                            Usage();
                            return 1;
                        }

                        ResourceInfo resInf = new ResourceInfo();
                        if (compileMultiple) {
                            string[] pair = args[i].Split(',');
                            switch (pair.Length) {
                                case 1:
                                    resInf.InputFile = Path.GetFullPath(pair[0]);
                                    resInf.OutputFile = Path.ChangeExtension(resInf.InputFile,
                                        "resources");
                                    break;
                                case 2:
                                    if (pair[1].Length == 0) {
                                        Console.WriteLine(@"error: You must specify an input & outfile file name like this:");
                                        Console.WriteLine("inFile.txt,outFile.resources.");
                                        Console.WriteLine("You passed in '{0}'.", args[i]);
                                        return 1;
                                    }
                                    resInf.InputFile = Path.GetFullPath(pair[0]);
                                    resInf.OutputFile = Path.GetFullPath(pair[1]);
                                    break;
                                default:
                                    Usage();
                                    return 1;
                            }
                        } else {
                            if ((i + 1) < args.Length) {
                                resInf.InputFile = Path.GetFullPath(args[i]);
                                // move to next arg, since we assume that one holds
                                // the name of the output file
                                i++;
                                resInf.OutputFile = Path.GetFullPath(args[i]);
                            } else {
                                resInf.InputFile = Path.GetFullPath(args[i]);
                                resInf.OutputFile = Path.ChangeExtension(resInf.InputFile,
                                    "resources");
                            }
                        }
                        inputFiles.Add(resInf);
                        break;
                }
            }

            if (inputFiles.Count == 0) {
                Usage();
                return 1;
            }

            foreach (ResourceInfo res in inputFiles) {
                int ret = CompileResourceFile(res.InputFile, res.OutputFile, useSourcePath, options);
                if (ret != 0)
                    return ret;
            }
            return 0;
        }

        private static bool RunningOnUnix
        {
            get
            {
                // check for Unix platforms - see FAQ for more details
                // http://www.mono-project.com/FAQ:_Technical#How_to_detect_the_execution_platform_.3F
                int platform = (int)Environment.OSVersion.Platform;
                return ((platform == 4) || (platform == 128) || (platform == 6));
            }
        }

        private static bool IsFileArgument(string arg)
        {
            if ((arg[0] != '-') && (arg[0] != '/'))
                return true;

            // cope with absolute filenames for resx files on unix, as
            // they also match the option pattern
            //
            // `/home/test.resx' is considered as a resx file, however
            // '/test.resx' is considered as error
            return (RunningOnUnix && arg.Length > 2 && arg.IndexOf('/', 2) != -1);
        }
    }
}