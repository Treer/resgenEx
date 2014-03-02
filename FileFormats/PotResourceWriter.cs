namespace resgenEx.FileFormats
{
    using System;
    using System.IO;

    class PotResourceWriter: PoResourceWriter
    {
        /// <summary>
        /// We want to write a .pot file instead of a .po file, so return true
        /// </summary>
        protected override bool WriteValuesAsBlank()
        {
            return true;
        }

        public PotResourceWriter(Stream stream, CommentOptions aCommentOptions, string aSourceFile) : base(stream, aCommentOptions, aSourceFile) { }
    }
}
