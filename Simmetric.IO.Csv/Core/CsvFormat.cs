﻿using System;
using System.Linq;

namespace Simmetric.IO.Csv
{
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>
    /// Describes formatting rules for a CSV document.
    /// </summary>
    public class CsvFormat
    {
        /// <summary>
        /// The character used to separate columns
        /// </summary>
        public char ColumnSeparator { get; set; }
        /// <summary>
        /// The character(s) used to separate lines
        /// </summary>
        public string LineSeparator { get; set; }
        /// <summary>
        /// The optional character used to mark a field as text
        /// </summary>
        public char? TextQualifier { get; set; }
        /// <summary>
        /// Indicates when the text qualifier is applied when writing CSV
        /// </summary>
        public TextQualificationOption TextQualification { get; set; }
        /// <summary>
        /// Indicates whether the CSV document has a header row
        /// </summary>
        public bool HasHeaders { get; set; }
        /// <summary>
        /// If HasHeaders = true, then this property will contain the headers for the CSV file. Automatically filled by <see cref="Simmetric.IO.Csv.CsvReader"/>
        /// </summary>
        public IEnumerable<string> Headers { get; set; }
        /// <summary>
        /// Culture used in the CSV document
        /// </summary>
        public CultureInfo Culture { get; set; }

        /// <summary>
        /// Returns true if the given input returns a line or column separator character
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public bool ContainsSeparators(string input)
        {
            if(string.IsNullOrEmpty(input))
            {
                return false;
            }
            return input.Contains(ColumnSeparator) || input.IndexOfAny(LineSeparator.ToCharArray()) < 0;
        }

        /// <summary>
        /// Creates a format with ColumnSeparator=; LineSeparator=NewLine TextQualifier=" HasHeaders=true
        /// </summary>
        public static CsvFormat Default => new CsvFormat
        {
            ColumnSeparator = ';',
            Culture = CultureInfo.InvariantCulture,
            LineSeparator = Environment.NewLine,
            TextQualifier = '"',
            TextQualification = TextQualificationOption.OnlyWhenNecessary,
            HasHeaders = true
        };

        /// <summary>
        /// Creates a format with ColumnSeparator=; LineSeparator=NewLine TextQualifier=" HasHeaders=false
        /// </summary>
        public static CsvFormat DefaultNoHeaders => new CsvFormat
        {
            ColumnSeparator = ';',
            Culture = CultureInfo.InvariantCulture,
            LineSeparator = Environment.NewLine,
            TextQualifier = '"',
            TextQualification = TextQualificationOption.OnlyWhenNecessary ,
            HasHeaders = false
        };

        /// <summary>
        /// Returns true if the input string contains one or more alphabetic characters
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static bool ContainsText(string input)
        {
            if(string.IsNullOrEmpty(input))
            {
                return false;
            }

            decimal parsedDecimal;
            return decimal.TryParse(input, out parsedDecimal);
        }

        /// <summary>
        /// Indicates when text qualifiers are applied
        /// </summary>
        public enum TextQualificationOption
        {
            /// <summary>
            /// Text qualifiers are applied when the column contains column or line separators
            /// </summary>
            OnlyWhenNecessary,
            /// <summary>
            /// Text qualifiers are applied when the column contains text, column or line separators
            /// </summary>
            ForTextFields,
            /// <summary>
            /// Text qualifiers are applied to every field
            /// </summary>
            ForAllFields
        }
    }
}
