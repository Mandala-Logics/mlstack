using System;
using System.Collections.Generic;
using System.Text;

namespace mlStringValidation
{
    public delegate void ValidatedStringEventHandler(ValidatedString vs);
    public delegate void ValidatedStringChangeEventHandler(ValidatedString vs, ValidatedStringBeforeChangeEventArgs e);

    public sealed class ValidatedStringBeforeChangeEventArgs : EventArgs
    {
        public bool Cancel { get; set; } = false;
        public string NewString { get; }

        public ValidatedStringBeforeChangeEventArgs(string newString)
        {
            NewString = newString;
        }
    }
}
