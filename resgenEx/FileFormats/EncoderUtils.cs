namespace resgenEx.FileFormats
{

    /// <summary>
    /// Provides methods for obtaining code page numbers for languages
    /// </summary>
    public class EncoderUtils
    {
        /// <summary>
        /// For the commonly used languages, the ISO 639-1 standard defines two-letter codes
        /// </summary>
        readonly string[,] c_ISO_639_1 = {
            {"aa", "Afar"},
            {"ab", "Abkhazian"},
            {"ae", "Avestan"},
            {"af", "Afrikaans"},
            {"ak", "Akan"},
            {"am", "Amharic"},
            {"an", "Aragonese"},
            {"ar", "Arabic"},
            {"as", "Assamese"},
            {"av", "Avaric"},
            {"ay", "Aymara"},
            {"az", "Azerbaijani"},
            {"ba", "Bashkir"},
            {"be", "Belarusian"},
            {"bg", "Bulgarian"},
            {"bh", "Bihari"},
            {"bi", "Bislama"},
            {"bm", "Bambara"},
            {"bn", "Bengali"},
            {"bn", "Bangla"}, //*
            {"bo", "Tibetan"},
            {"br", "Breton"},
            {"bs", "Bosnian"},
            {"ca", "Catalan"},
            {"ce", "Chechen"},
            {"ch", "Chamorro"},
            {"co", "Corsican"},
            {"cr", "Cree"},
            {"cs", "Czech"},
            {"cu", "Church Slavic"},
            {"cv", "Chuvash"},
            {"cy", "Welsh"},
            {"da", "Danish"},
            {"de", "German"},
            {"dv", "Divehi"},
            {"dv", "Maldivian"},//*
            {"dz", "Dzongkha"},
            {"dz", "Bhutani"},//*
            {"ee", "Éwé"},
            {"el", "Greek"},
            {"en", "English"},
            {"eo", "Esperanto"},
            {"es", "Spanish"},
            {"et", "Estonian"},
            {"eu", "Basque"},
            {"fa", "Persian"},
            {"ff", "Fulah"},
            {"fi", "Finnish"},
            {"fj", "Fijian"},
            {"fj", "Fiji"}, //*
            {"fo", "Faroese"},
            {"fr", "French"},
            {"fy", "Western Frisian"},
            {"ga", "Irish"},
            {"gd", "Scottish Gaelic"},
            {"gl", "Galician"},
            {"gn", "Guarani"},
            {"gu", "Gujarati"},
            {"gv", "Manx"},
            {"ha", "Hausa"},
            {"he", "Hebrew"}, // (formerly iw)
            {"hi", "Hindi"},
            {"ho", "Hiri Motu"},
            {"hr", "Croatian"},
            {"ht", "Haitian"},
            {"ht", "Haitian Creole"}, //*
            {"hu", "Hungarian"},
            {"hy", "Armenian"},
            {"hz", "Herero"},
            {"ia", "Interlingua"},
            {"id", "Indonesian"}, // (formerly in)
            {"ie", "Interlingue"},
            {"ie", "Occidental"}, //*
            {"ig", "Igbo"},
            {"ii", "Sichuan Yi"},
            {"ii", "Nuosu"}, //*
            {"ik", "Inupiak"},
            {"ik", "Inupiaq"}, //*
            {"io", "Ido"},
            {"is", "Icelandic"},
            {"it", "Italian"},
            {"iu", "Inuktitut"},
            {"ja", "Japanese"},
            {"jv", "Javanese"},
            {"ka", "Georgian"},
            {"kg", "Kongo"},
            {"ki", "Kikuyu"},
            {"ki", "Gikuyu"}, //*
            {"kj", "Kuanyama"},
            {"kj", "Kwanyama"}, //*
            {"kk", "Kazakh"},
            {"kl", "Kalaallisut"},
            {"kl", "Greenlandic"}, //*
            {"km", "Central Khmer"},
            {"km", "Cambodian"}, //*
            {"kn", "Kannada"},
            {"ko", "Korean"},
            {"kr", "Kanuri"},
            {"ks", "Kashmiri"},
            {"ku", "Kurdish"},
            {"kv", "Komi"},
            {"kw", "Cornish"},
            {"ky", "Kirghiz"},
            {"la", "Latin"},
            {"lb", "Letzeburgesch"},
            {"lb", "Luxembourgish"}, //*
            {"lg", "Ganda"},
            {"li", "Limburgish"},
            {"li", "Limburger"}, //*
            {"li", "Limburgan"}, //*
            {"ln", "Lingala"},
            {"lo", "Lao"},
            {"lo", "Laotian"}, //*
            {"lt", "Lithuanian"},
            {"lu", "Luba-Katanga"},
            {"lv", "Latvian"},
            {"lv", "Lettish"}, //*
            {"mg", "Malagasy"},
            {"mh", "Marshallese"},
            {"mi", "Maori"},
            {"mk", "Macedonian"},
            {"ml", "Malayalam"},
            {"mn", "Mongolian"},
            {"mo", "Moldavian"},
            {"mr", "Marathi"},
            {"ms", "Malay"},
            {"mt", "Maltese"},
            {"my", "Burmese"},
            {"na", "Nauru"},
            {"nb", "Norwegian Bokmål"},
            {"nd", "Ndebele, North"},
            {"ne", "Nepali"},
            {"ng", "Ndonga"},
            {"nl", "Dutch"},
            {"nn", "Norwegian Nynorsk"},
            {"no", "Norwegian"},
            {"nr", "Ndebele, South"},
            {"nv", "Navajo"},
            {"nv", "Navaho"}, //*
            {"ny", "Chichewa"},
            {"ny", "Nyanja"}, //*
            {"oc", "Occitan"},
            {"oc", "Provençal"}, //*
            {"oj", "Ojibwa"},
            {"om", "(Afan) Oromo"},
            {"or", "Oriya"},
            {"os", "Ossetian"},
            {"os", "Ossetic"}, //*
            {"pa", "Panjabi"},
            {"pa", "Punjabi"}, //*
            {"pi", "Pali"},
            {"pl", "Polish"},
            {"ps", "Pashto"},
            {"ps", "Pushto"}, //*
            {"pt", "Portuguese"},
            {"qu", "Quechua"},
            {"rm", "Romansh"},
            {"rn", "Rundi"},
            {"rn", "Kirundi"}, //*
            {"ro", "Romanian"},
            {"ru", "Russian"},
            {"rw", "Kinyarwanda"},
            {"sa", "Sanskrit"},
            {"sc", "Sardinian"},
            {"sd", "Sindhi"},
            {"se", "Northern Sami"},
            {"sg", "Sango"},
            {"sg", "Sangro"}, //*
            {"si", "Sinhala"},
            {"si", "Sinhalese"}, //*
            {"sk", "Slovak"},
            {"sl", "Slovenian"},
            {"sm", "Samoan"},
            {"sn", "Shona"},
            {"so", "Somali"},
            {"sq", "Albanian"},
            {"sr", "Serbian"},
            {"ss", "Swati"},
            {"ss", "Siswati"}, //*
            {"st", "Sesotho"},
            {"st", "Sotho, Southern"}, //*
            {"su", "Sundanese"},
            {"sv", "Swedish"},
            {"sw", "Swahili"},
            {"ta", "Tamil"},
            {"te", "Telugu"},
            {"tg", "Tajik"},
            {"th", "Thai"},
            {"ti", "Tigrinya"},
            {"tk", "Turkmen"},
            {"tl", "Tagalog"},
            {"tn", "Tswana"},
            {"tn", "Setswana"}, //*
            {"to", "Tonga"},
            {"tr", "Turkish"},
            {"ts", "Tsonga"},
            {"tt", "Tatar"},
            {"tw", "Twi"},
            {"ty", "Tahitian"},
            {"ug", "Uighur"},
            {"uk", "Ukrainian"},
            {"ur", "Urdu"},
            {"uz", "Uzbek"},
            {"ve", "Venda"},
            {"vi", "Vietnamese"},
            {"vo", "Volapük"},
            {"vo", "Volapuk"}, //*
            {"wa", "Walloon"},
            {"wo", "Wolof"},
            {"xh", "Xhosa"},
            {"yi", "Yiddish (formerly ji)"},
            {"yo", "Yoruba"},
            {"za", "Zhuang"},
            {"zh", "Chinese"},
            {"zu", "Zulu"}
        };



    }
}
