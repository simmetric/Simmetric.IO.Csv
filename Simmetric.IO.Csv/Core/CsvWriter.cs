using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simmetric.IO.Csv
{
    /// <summary>
    /// Provides fast, non-cached, forward-only writing of values in CSV format.
    /// </summary>
    public class CsvWriter : IDisposable
    {
        /// <summary>
        /// The format used when writing CSV
        /// </summary>
        public CsvFormat Format { get; protected set; }
        Stream target;
        StreamWriter writer;

        /// <summary>
        /// Instantiates a CSV writer class to write the specified format to the specified stream.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="target"></param>
        public CsvWriter(System.IO.Stream target, CsvFormat format)
        {
            this.Format = format;
            this.target = target;
            this.writer = new StreamWriter(this.target);
            //write headers if necessary
            if (this.Format.HasHeaders)
            {
                if (this.Format.Headers == null)
                {
                    throw new NullReferenceException("CsvFormat.Headers must be filled to do this.");
                }

                WriteLine(this.Format.Headers);
            }
        }

        /// <summary>
        /// Writes the given set of fields to a CSV formatted line.
        /// </summary>
        /// <param name="line">Non-formatted, raw field values</param>
        public void WriteLine(string[] line)
        {
            if (line == null)
            {
                throw new ArgumentNullException("line");
            }

            for (int i = 0; i < line.Length; i++)
            {
                WriteField(line[i]);
                //append column separator, except at the end of the line
                if (i < line.Length - 1)
                {
                    WriteColumnSeparator();
                }
            }
            WriteLineSeparator();
        }

        /// <summary>
        /// Writes a line separator to the stream
        /// </summary>
        public void WriteLineSeparator()
        {

            this.writer.Write(this.Format.LineSeparator);
        }

        /// <summary>
        /// Writes a column separator to the stream
        /// </summary>
        public void WriteColumnSeparator()
        {
            this.writer.Write(this.Format.ColumnSeparator);
        }

        /// <summary>
        /// Writes a field value to the stream
        /// </summary>
        /// <param name="field"></param>
        public void WriteField(string field)
        {
            if(string.IsNullOrEmpty(field))
            {
                throw new ArgumentNullException("field");
            }

            //verify if text qualifiers must be added
            if (this.Format.TextQualifier.HasValue)
            {
                //remove text qualifiers from field
                field = field.Replace(this.Format.TextQualifier.Value, '\0');

                //write value
                //if selected TextQualificationOption applies, wrap in text qualifier
                if (
                    this.Format.TextQualification == CsvFormat.TextQualificationOption.ForAllFields || 
                    (this.Format.TextQualification == CsvFormat.TextQualificationOption.OnlyWhenNecessary && this.Format.ContainsSeparators(field)) || 
                    (this.Format.TextQualification == CsvFormat.TextQualificationOption.ForTextFields && (CsvFormat.ContainsText(field) || this.Format.ContainsSeparators(field))))
                {
                    this.writer.Write(this.Format.TextQualifier.Value);
                    this.writer.Write(field);
                    this.writer.Write(this.Format.TextQualifier.Value);
                }
            }
            else
            {
                this.writer.Write(field);
            }
        }

        /// <summary>
        /// Flushes the underlying stream
        /// </summary>
        public void Flush()
        {
            this.writer.Flush();
        }

        /// <summary>
        /// Closes the underlying stream
        /// </summary>
        public void Close()
        {
            this.writer.Close();
            this.target.Close();
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
            if(disposing)
            {
                if (this.writer != null)
                {
                    this.writer.Dispose();
                    this.writer = null;
                }
                if (this.target != null)
                {
                    this.target.Dispose();
                    this.target = null;
                }
            }
        }
    }
}
