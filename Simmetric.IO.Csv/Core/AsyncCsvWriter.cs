using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Simmetric.IO.Csv.Core
{

    /// <summary>
    /// Provides fast, non-cached, forward-only writing of values in CSV format.
    /// </summary>
    public class AsyncCsvWriter : IAsyncCsvWriter
    {
        /// <summary>
        /// The format used when writing CSV
        /// </summary>
        public CsvFormat Format { get; protected set; }
        //Stream target;
        TextWriter? writer;

        /// <summary>
        /// Instantiates a CSV writer class to write the specified format to the specified stream.
        /// </summary>
        /// <param name="format">The CSV format</param>
        /// <param name="writer">The TextWriter that receives output</param>
        public AsyncCsvWriter(System.IO.TextWriter writer, CsvFormat format)
        {
            Format = format;
            this.writer = writer;
            //write headers if necessary
            if (Format.HasHeaders)
            {
                if (Format.Headers == null)
                {
                    throw new InvalidOperationException($"{nameof(CsvFormat.Headers)} must be filled when {nameof(CsvFormat.HasHeaders)} is true");
                }

                WriteLineAsync(Format.Headers).Wait();
            }
        }
        /// <summary>
        /// Instantiates a CSV writer class to write the specified format to the specified stream.
        /// </summary>
        /// <param name="format">The CSV format</param>
        /// <param name="stream">The stream that receives output, will be wrapped in a StreamWriter</param>
        public AsyncCsvWriter(Stream stream, CsvFormat format) : this(new StreamWriter(stream), format)
        {
            //empty constructor for backwards compatibiltiy, inherits this(TextWriter, CsvFormat)
        }

        /// <summary>
        /// Writes the given set of fields to a CSV formatted line.
        /// </summary>
        /// <param name="line">Non-formatted, raw field values</param>
        public async Task WriteLineAsync(IEnumerable<string?> line)
        {
            if (line == null)
            {
                throw new ArgumentNullException(nameof(line));
            }

            for (int i = 0; i < line.Count(); i++)
            {
                await WriteFieldAsync(line.ElementAt(i));
                //append column separator, except at the end of the line
                if (i < line.Count() - 1)
                {
                    await WriteColumnSeparatorAsync();
                }
            }
            await WriteLineSeparatorAsync();
        }

        /// <summary>
        /// Writes a line separator to the stream
        /// </summary>
        public async Task WriteLineSeparatorAsync()
        {
            if (writer == null)
            {
                throw new InvalidOperationException($"{nameof(writer)} was null");
            }
            await writer.WriteAsync(Format.LineSeparator);
        }

        /// <summary>
        /// Writes a column separator to the stream
        /// </summary>
        public async Task WriteColumnSeparatorAsync()
        {
            if (writer == null)
            {
                throw new InvalidOperationException($"{nameof(writer)} was null");
            }
            await writer.WriteAsync(Format.ColumnSeparator);
        }

        /// <summary>
        /// Writes a field value to the stream
        /// </summary>
        /// <param name="field"></param>
        public async Task WriteFieldAsync(string? field)
        {
            if (writer == null)
            {
                throw new InvalidOperationException($"{nameof(writer)} was null");
            }

            //verify if text qualifiers must be added
            if (field == null)
            {
                await writer.WriteAsync(string.Empty);
            }
            else if (Format.TextQualifier.HasValue)
            {
                //remove text qualifiers from field
                field = field.Replace(Format.TextQualifier.Value, '\0');

                //write value
                //if selected TextQualificationOption applies, wrap in text qualifier
                if (
                    Format.TextQualification == CsvFormat.TextQualificationOption.ForAllFields ||
                    (Format.TextQualification == CsvFormat.TextQualificationOption.OnlyWhenNecessary && Format.ContainsSeparators(field)) ||
                    (Format.TextQualification == CsvFormat.TextQualificationOption.ForTextFields && (CsvFormat.ContainsText(field) || Format.ContainsSeparators(field))))
                {
                    await writer.WriteAsync(Format.TextQualifier.Value);
                    await writer.WriteAsync(field);
                    await writer.WriteAsync(Format.TextQualifier.Value);
                }
                else
                {
                    await writer.WriteAsync(field);
                }
            }
            else
            {
                await writer.WriteAsync(field);
            }
        }

        /// <summary>
        /// Flushes the underlying stream
        /// </summary>
        public async Task FlushAsync()
        {
            if (writer == null)
            {
                throw new InvalidOperationException($"{nameof(writer)} was null");
            }
            await writer.FlushAsync();
        }

        /// <summary>
        /// Closes the underlying stream
        /// </summary>
        public async Task CloseAsync()
        {
            if (writer == null)
            {
                throw new InvalidOperationException($"{nameof(writer)} was null");
            }
            writer.Close();
        }

        /// <summary>
        /// Releases all resources used by the <see cref="T:Simmetric.IO.Csv.CsvWriter"/> object
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all resources used by the <see cref="T:Simmetric.IO.Csv.CsvWriter"/> object
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (writer != null)
                {
                    writer.Dispose();
                    writer = null;
                }
            }
        }
    }
}
