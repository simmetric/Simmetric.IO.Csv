﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Reflection;

namespace Simmetric.IO.Csv
{
    /// <summary>
    /// Reads a CSV document from the input stream
    /// </summary>
    public class CsvReader : ICsvReader
    {
        //status
        bool isInText = false;
        bool endOfStream = false;

        //internals
        TextReader? reader;

        /// <summary>
        /// Gets the format used to read the CSV document
        /// </summary>
        public CsvFormat Format { get; protected set; }

        /// <summary>
        /// Gets a value that indicates whether the current stream position is at the end of the stream.
        /// </summary>
        public bool EndOfStream
        {
            get { return endOfStream || reader?.Peek() == -1; }
        }

        /// <summary>
        /// Gets the zero-based line number of the current position in the CSV document.
        /// </summary>
        public int LinePosition { get; private set; } = 0;

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
            if (Format.HasHeaders)
            {
                Format.Headers = ReadLine();
            }
        }

        /// <summary>
        /// Instantiates a new CSV reader for the given stream, which must be formatted according to the given CSV format.
        /// </summary>
        /// <param name="stream">The input stream containing a CSV formatted document, will be converted to StreamReader</param>
        /// <param name="format">Describes the format of the CSV document in the stream</param>
        public CsvReader(Stream stream, CsvFormat format) : this(new StreamReader(stream), format)
        {
            //empty constructor for backwards compatibiltiy, inherits this(TextReader, CsvFormat)
        }

        /// <summary>
        /// Reads the next line and returns the field values as a <see cref="T:System.String[]"/>
        /// </summary>
        /// <returns>An enumerable of field values of one line as strings</returns>
        public IEnumerable<string?> ReadLine()
        {
            List<string?> fields = new List<string?>();
            var lineToRead = LinePosition;
            while (!EndOfStream && LinePosition == lineToRead)
            {
                fields.Add(Read());
            }

            return fields;
        }

        /// <summary>
        /// Reads until the end of the CSV file and returns all lines as a nested IEnumerable&lt;string&gt;
        /// </summary>
        /// <returns>An enumerable of field values of all lines as strings</returns>
        public IEnumerable<IEnumerable<string?>> ReadToEnd()
        {
            while (!EndOfStream)
            {
                yield return ReadLine();
            }
        }

        /// <summary>
        /// Reads the next line and uses it to populate a new instance of class T
        /// </summary>
        /// <typeparam name="T">The type to which the line will be converted</typeparam>
        /// <returns>One line as a strong typed object</returns>
        public T ReadLine<T>() where T : new()
        {
            var type = typeof(T);
            var fields = type.GetFields();
            var props = type.GetProperties();
            var result = new T();

            if (Format.HasHeaders && Format.Headers != null)
            {
                foreach (var header in Format.Headers)
                {
                    var headerClean = header?.Replace(" ", string.Empty).Replace("/", string.Empty);

                    FieldInfo? field = fields.SingleOrDefault(f => f.Name.Equals(headerClean, StringComparison.InvariantCultureIgnoreCase));
                    PropertyInfo? property = props.SingleOrDefault(p => p.Name.Equals(headerClean, StringComparison.InvariantCultureIgnoreCase));

                    if (field == null && property == null)
                    {
                        continue;
                    }

                    Type currentType = field != null ? field.FieldType : property!.PropertyType;
                    var value = GetValue(currentType);
                    if (field != null)
                    {
                        field.SetValue(result, value);
                    }
                    else if (property != null)
                    {
                        property.SetValue(result, value);
                    }
                }
            }
            else
            {
                foreach (var field in fields)
                {
                    field.SetValue(result, GetValue(field.FieldType));
                }
                foreach (var property in props)
                {
                    property.SetValue(result, GetValue(property.PropertyType));
                }
            }

            return result;
        }

        private object? GetValue(Type currentType)
        {
            switch (Type.GetTypeCode(currentType))
            {
                case TypeCode.Int32:
                    return ReadAsInt32();
                case TypeCode.Double:
                    return ReadAsDouble();
                case TypeCode.String:
                    return Read();
                case TypeCode.DateTime:
                    return ReadAsDateTime();
                case TypeCode.Decimal:
                    return ReadAsDecimal();
                case TypeCode.Boolean:
                    return ReadAsBoolean();
                default:
                    if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        return GetValue(Nullable.GetUnderlyingType(currentType) ?? throw new InvalidOperationException($"{currentType.Name} is not a closed generic nullable type"));
                    }
                    return null;
            }
        }

        /// <summary>
        /// Reads until the end of the CSV file and returns all lines as an IEnumerable of type T
        /// </summary>
        /// <typeparam name="T">The type to which each line will be converted</typeparam>
        /// <returns>All lines as an enumerable of strong typed object</returns>
        public IEnumerable<T> ReadToEnd<T>() where T : new()
        {
            while (!EndOfStream)
            {
                yield return ReadLine<T>();
            }
        }

        /// <summary>
        /// Reads the next field from the stream, without text qualifiers or separators
        /// </summary>
        /// <returns>The field value as <see cref="T:System.String"/></returns>
        public string? Read()
        {
            var cache = new StringBuilder();
            char currentChar;
            isInText = false;
            string? field = null;
            bool foundLineSeparator = false;

            while (!EndOfStream)
            {
                //crawl the stream
                currentChar = ReadChar();
                //when text qualifier is found, set 'inText'
                if (Format.TextQualifier != null && currentChar == Format.TextQualifier)
                {
                    isInText = !isInText;
                }
                else if (!isInText && currentChar == Format.ColumnSeparator)
                {
                    //when not inText and column separator is found, the end of the field is reached
                    break;
                }
                else if (!isInText && Format.LineSeparator.Contains(currentChar))
                {
                    //when not inText and a line separator char is found, the end of the field and line is reached
                    //skip all following line separators
                    while (!EndOfStream && reader != null && Format.LineSeparator.Contains((char)reader.Peek()))
                    {
                        ReadChar();
                    }

                    //increase line count
                    LinePosition++;
                    foundLineSeparator = true;
                    break;
                }
                else if (!isInText && currentChar == (char)3) //Unicode end of text character
                {
                    //at end of text - this is here primarily for unit testing purposes
                    endOfStream = true;
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
            if (!foundLineSeparator && EndOfStream)
            {
                LinePosition++;
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
            if (bool.TryParse(value, out bool result))
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
            if (DateTime.TryParse(value, Format.Culture, DateTimeStyles.None, out DateTime result))
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
            if (decimal.TryParse(value, NumberStyles.Float, Format.Culture, out decimal result))
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
            if (double.TryParse(value, NumberStyles.Float, Format.Culture, out double result))
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
            if (int.TryParse(value, NumberStyles.Integer, Format.Culture, out int result))
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
            if (reader == null)
            {
                throw new InvalidOperationException($"{nameof(reader)} was null");
            }
            return (char)reader.Read();
        }

        /// <summary>
        /// Releases all resources used by the <see cref="T:Simmetric.IO.Csv.CsvReader"/> object
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all resources used by the <see cref="T:Simmetric.IO.Csv.CsvReader"/> object
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (reader != null)
                {
                    reader.Dispose();
                    reader = null;
                }
            }
        }
    }
}
