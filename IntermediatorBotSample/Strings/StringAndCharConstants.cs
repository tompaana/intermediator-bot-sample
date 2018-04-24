using System;

namespace IntermediatorBot.Strings
{
    public class StringAndCharConstants
    {
        public static readonly string NoUserNamePlaceholder = "<no user name>";

        public static readonly string LineBreak = "\n\r";
        public static readonly char QuotationMark = '"';

        // For parsing JSON
        public static readonly string EndOfLineInJsonResponse = "\\r\\n";
        public static readonly char BackslashInJsonResponse = '\\';
    }
}
