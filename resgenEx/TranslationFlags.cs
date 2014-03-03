namespace resgenEx
{
    using System;

    [Flags] 
    enum TranslationFlags
    {
        /// <summary>
        /// Indicates the string is indented for use in Strings.Format(), i.e. it contains "{0}" etc
        /// </summary>
        csharpFormatString = 1,

        /// <summary>
        /// Indicates the string is indented for use in Format(), i.e. it contains "%1" etc
        /// </summary>
        innoSetupFormatString = 2
    }
}
