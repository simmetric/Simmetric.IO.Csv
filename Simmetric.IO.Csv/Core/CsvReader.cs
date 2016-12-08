using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;

namespace Simmetric.IO.Csv
{
    /// <summary>
    /// Reads a CSV document from the input stream
    /// </summary>
    public class CsvReader : ICsvReader
    {
        //status
        bool isInText = false;
        int currentLine = 0;
        //internals
        TextReader reader;

        /// <summary>
        /// Gets the format used to read the CSV document
        /// </summary>
        public CsvFormat Format { get; protected set; }

        /// <summary>
        /// Gets a value that indicates whether the current stream position is at the end of the stream.
        /// </summary>
        public bool EndOfStream
        {
            get { return reader?.Peek() == -1; }
        }

        /// <summary>
        /// Gets the zero-based line number of the current position in the CSV document.
        /// </summary>
        public int LinePosition
        {
            get { return this.currentLine; }
        }

        /// <summary>
        /// Instantiates a new CSV reader for the given stream, which must be formatted according to the given CSV format.
        /// </summary>
        /// <param name="reader">The input reader containing a CSV formatted document</param>
        /// <param name="format">Describes the format of the CSV document in the stream</param>
        public CsvReader(TextReader reader, CsvFormat format)
        {
            this.Format = format;
            this.reader = reader;

            //read headers
            if (this.Format.HasHeaders)
            {
                this.Format.Headers = ReadLine();
            }
        }

        public CsvReader(Stream stream, CsvFormat format):this(new StreamReader(stream), format)
        {
            //empty constructor for backwards compatibiltiy, inherits this(TextWriter, CsvFormat)
        }

        /// <summary>
        /// Reads the next line and returns the field values as a <see cref="T:System.String[]"/>
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> ReadLine()
        {
            List<string> fields = new List<string>();
            var lineToRead = currentLine;
            while (!this.EndOfStream && currentLine == lineToRead)
            {
                fields.Add(Read());
            }

            return fields;
        }

        /// <summary>
        /// Reads the next field from the stream, without text qualifiers or separators
        /// </summary>
        /// <returns>The field value as <see cref="T:System.String"/></returns>
        public string Read()
        {
            var cache = new StringBuilder();
            char currentChar;
            isInText = false;
            string field = null;
            bool foundLineSeparator = false;

            while (!this.EndOfStream)
            {
                //crawl the stream
                currentChar = this.ReadChar();
                //when text qualifier is found, set 'inText'
                if (this.Format.TextQualifier != null && currentChar == this.Format.TextQualifier)
                {
                    isInText = !isInText;
                }
                else if (!isInText && currentChar == this.Format.ColumnSeparator)
                {
                    //when not inText and column separator is found, the end of the field is reached
                    break;
                }
                else if (!isInText && this.Format.LineSeparator.Contains(currentChar))
                {
                    //when not inText and a line separator char is found, the end of the field and line is reached
                    //skip all following line separators
                    while (!this.EndOfStream && this.Format.LineSeparator.Contains((char)reader.Peek()))
                    {
                        this.ReadChar();
                    }

                    //increase line count
                    currentLine++;
                    foundLineSeparator = true;
                    break;
                }
                else
                {
                    //add character to the cache
                    cache.Append(currentChar);
                }
            }

            isInText = false;

            //if the line ended because of EndOfStream, we still need to increase the line number
            if (!foundLineSeparator && this.EndOfStream)
            {
                currentLine++;
            }

            //write cache to field and return
            if (cache.Length > 0)
            {
                field = cache.ToString();
                cache.Clear();
            }
            return field;
        }

        /// <summary>
        /// Reads the next field and returns the value as <see cref="T:System.Nullable`1"/>
        /// </summary>
        /// <returns>The converted value if the field is a valid bool, otherwise <c>null</c></returns>
        public bool? ReadAsBoolean()
        {
            var value = Read();
            bool result;
            if (bool.TryParse(value, out result))
            {
                return result;
            }

            return null;
        }

        /// <summary>
        /// Reads the next field and returns the value as <see cref="T:System.Nullable`1"/>
        /// </summary>
        /// <returns>The converted value if the field is a valid DateTime, otherwise <c>null</c></returns>
        public DateTime? ReadAsDateTime()
        {
            var value = Read();
            DateTime result;
            if (DateTime.TryParse(value, Format.Culture, DateTimeStyles.None, out result))
            {
                return result;
            }

            return null;
        }

        /// <summary>
        /// Reads the next field and returns the value as <see cref="T:System.Nullable`1"/>
        /// </summary>
        /// <returns>The converted value if the field is a valid decimal, otherwise <c>null</c></returns>
        public decimal? ReadAsDecimal()
        {
            var value = Read();
            decimal result;
            if (decimal.TryParse(value, NumberStyles.Float, Format.Culture, out result))
            {
                return result;
            }

            return null;
        }

        /// <summary>
        /// Reads the next field and returns the value as <see cref="T:System.Nullable`1"/>
        /// </summary>
        /// <returns>The converted value if the field is a valid double, otherwise <c>null</c></returns>
        public double? ReadAsDouble()
        {
            var value = Read();
            double result;
            if (double.TryParse(value, NumberStyles.Float, Format.Culture, out result))
            {
                return result;
            }

            return null;
        }

        /// <summary>
        /// Reads the next field and returns the value as <see cref="T:System.Nullable`1"/>
        /// </summary>
        /// <returns>The converted value if the field is a valid integer, otherwise <c>null</c></returns>
        public int? ReadAsInt32()
        {
            var value = Read();
            int result;
            if (int.TryParse(value, NumberStyles.Integer, Format.Culture, out result))
            {
                return result;
            }

            return null;
        }

        /// <summary>
        /// Returns the next character from the stream
        /// </summary>
        /// <returns>The next <see cref="T:System.Char"/> in the stream</returns>
        private char ReadChar()
        {
            return (char)reader.Read();
        }

        /// <summary>
        /// Releases all resources used by the <see cref="T:Simmetric.IO.Csv.CsvReader"/> object
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all resources used by the <see cref="T:Simmetric.IO.Csv.CsvReader"/> object
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.reader != null)
                {
                    this.reader.Dispose();
                    this.reader = null;
                }
            }
        }
    }
}
