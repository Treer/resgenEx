// This file is GPL
//
// It is modified from files obtained from the mono project under GPL licence.
namespace resgenEx
{
    public enum CommentOptions
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
        /// don't export the rawComments from the source file to the destination 
        /// format, and don't include automatically created comments.
        /// </summary>
        writeNoComments
    }
}
