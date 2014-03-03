namespace resgenEx.FileFormats
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Implemented by *ResourceReaders that are capable of doing so.
    /// </summary>
    public interface IResourceHeader
    {
        /// <summary>
        /// The ISO 639-1 standard defines two-letter codes for the commonly used languages,
        /// see EncoderUtils.
        /// 
        /// Needed to allow non-unicode formats to use the right code-page
        /// </summary>
        string LanguageID { get; }
    }
}
