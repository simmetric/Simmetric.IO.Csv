using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Simmetric.IO.Csv.Core;

namespace Simmetric.IO.Csv
{
    /// <summary>
    /// Reads a CSV document from the input stream
    /// </summary>
    public class AsyncCsvReader : IAsyncCsvReader
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
        public AsyncCsvReader(TextReader reader, CsvFormat format)
        {
            this.Format = format;
            this.reader = reader;

            //read headers
            if (Format.HasHeaders)
            {
                Format.Headers = ReadLineAsync().Result;
            }
        }

        /// <summary>
        /// Instantiates a new CSV reader for the given stream, which must be formatted according to the given CSV format.
        /// </summary>
        /// <param name="stream">The input stream containing a CSV formatted document, will be converted to StreamReader</param>
        /// <param name="format">Describes the format of the CSV document in the stream</param>
        public AsyncCsvReader(Stream stream, CsvFormat format) : this(new StreamReader(stream), format)
        {
            //empty constructor for backwards compatibiltiy, inherits this(TextReader, CsvFormat)
        }

        /// <summary>
        /// Reads the next line and returns the field values as a <see cref="T:System.String[]"/>
        /// </summary>
        /// <returns>An enumerable of field values of one line as strings</returns>
        public async Task<IEnumerable<string?>> ReadLineAsync()
        {
            List<string?> fields = new List<string?>();
            var lineToRead = LinePosition;
            while (!EndOfStream && LinePosition == lineToRead)
            {
                fields.Add(await ReadAsync());
            }

            return fields;
        }

        /// <summary>
        /// Reads until the end of the CSV file and returns all lines as a nested IEnumerable&lt;string&gt;
        /// </summary>
        /// <returns>An enumerable of field values of all lines as strings</returns>
        public async IAsyncEnumerable<IEnumerable<string?>> ReadToEndAsync()
        {
            while (!EndOfStream)
            {
                yield return await ReadLineAsync();
            }
        }

        /// <summary>
        /// Reads the next line and uses it to populate a new instance of class T
        /// </summary>
        /// <typeparam name="T">The type to which the line will be converted</typeparam>
        /// <returns>One line as a strong typed object</returns>
        public async Task<T> ReadLineAsync<T>() where T : new()
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

                    if (field is null && property is null)
                    {
                        continue;
                    }

                    Type currentType = field?.FieldType ?? property!.PropertyType;
                    var value = await GetValue(currentType);
                    if (field != null)
                    {
                        field.SetValue(result, value);
                    }
                    else
                    {
                        property?.SetValue(result, value);
                    }
                }
            }
            else
            {
                foreach (var field in fields)
                {
                    field.SetValue(result, await GetValue(field.FieldType));
                }
                foreach (var property in props)
                {
                    property.SetValue(result, await GetValue(property.PropertyType));
                }
            }

            return result;
        }

        private async Task<object?> GetValue(Type currentType)
        {
            switch (Type.GetTypeCode(currentType))
            {
                case TypeCode.Int32:
                    return await ReadAsInt32Async();
                case TypeCode.Double:
                    return await ReadAsDoubleAsync();
                case TypeCode.String:
                    return await ReadAsync();
                case TypeCode.DateTime:
                    return await ReadAsDateTimeAsync();
                case TypeCode.Decimal:
                    return await ReadAsDecimalAsync();
                case TypeCode.Boolean:
                    return await ReadAsBooleanAsync();
                default:
                    if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        return await GetValue(Nullable.GetUnderlyingType(currentType)!);
                    }
                    return null;
            }
        }

        /// <summary>
        /// Reads until the end of the CSV file and returns all lines as an IEnumerable of type T
        /// </summary>
        /// <typeparam name="T">The type to which each line will be converted</typeparam>
        /// <returns>All lines as an enumerable of strong typed object</returns>
        public async IAsyncEnumerable<T> ReadToEndAsync<T>() where T : new()
        {
            while (!EndOfStream)
            {
                yield return await ReadLineAsync<T>();
            }
        }

        /// <summary>
        /// Reads the next field from the stream, without text qualifiers or separators
        /// </summary>
        /// <returns>The field value as <see cref="T:System.String"/></returns>
        public async Task<string?> ReadAsync()
        {
            var cache = new StringBuilder();
            char currentChar;
            isInText = false;
            string? field = null;
            bool foundLineSeparator = false;

            while (!EndOfStream)
            {
                //crawl the stream
                currentChar = await ReadCharAsync();
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
                        await ReadCharAsync();
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
        public async Task<bool?> ReadAsBooleanAsync()
        {
            var value = await ReadAsync();
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
        public async Task<DateTime?> ReadAsDateTimeAsync()
        {
            var value = await ReadAsync();
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
        public async Task<decimal?> ReadAsDecimalAsync()
        {
            var value = await ReadAsync();
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
        public async Task<double?> ReadAsDoubleAsync()
        {
            var value = await ReadAsync();
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
        public async Task<int?> ReadAsInt32Async()
        {
            var value = await ReadAsync();
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
        private async Task<char> ReadCharAsync()
        {
            if (reader == null)
            {
                throw new InvalidOperationException($"{nameof(reader)} was null");
            }
            var buffer = new char[1];
            await reader.ReadAsync(buffer, 0, buffer.Length);
            return buffer[0];
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
