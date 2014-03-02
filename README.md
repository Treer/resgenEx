# resgenEx - convert Gnu .PO localization files to/from other formats such as .isl and .resx

For Gnu .po language translation files. See the [GNU gettext documentation](https://www.gnu.org/software/gettext/manual/html_node/index.html) for information about PO files.

resgenEx was made to move and maintain a Windows .Net project to .po files,
so it focuses on preserving/automating comments during file conversions, and
adds support for .isl files - localization files for the Windows 
installation builder "Inno Setup".

To support IDs being used in msgids, instead of the english version of
the msgstr, resgenEx adds an "#. Original message text: " comment when 
converting from other formats into .po. If you wish to add these comments
to an existing .po file you can convert it to .resx then back to .po

**The Windows executable can be [downloaded here](https://mega.co.nz/#!DZ9mGIhI!928EQfsXO8PAZ6PFbF6mkuVg8ZTgtawj7cAFgCURpu0)** (v0.11)

#### 
Usage:
    resgen [options] source.ext [dest.ext]

Convert a resource file from one format to another.
The currently supported formats are: '.resx' '.po' '.pot' '.isl'.
If the destination file is not specified, source.resources will be used.

## Options:
 * -nocomments, /noComments
  -  don't export the rawComments from the source file to the destination
    format, and don't include automatically created comments.

 * -sourcecommentsonly, /sourceCommentsOnly
  -  only export comments to the destination format that existed in the
    source file. Do not include automatically created comments.

 * -addformatflags, /addFormatFlags
  -  when exporting to .po or .pot, flags such as csharp-format will
    be added to strings which contain format specifications, e.g. "{0}"

 * -usesourcepath, /useSourcePath
  -      to resolve relative file paths, use the directory of the resource
        file as current directory.
