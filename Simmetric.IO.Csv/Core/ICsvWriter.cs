using System;
using System.Collections.Generic;

namespace Simmetric.IO.Csv
{    
    /// <summary>
    /// Provides fast, non-cached, forward-only writing of values in CSV format.
    /// </summary>
    public interface ICsvWriter : IDisposable
    {
        /// <summary>
        /// Writes the given set of fields to a CSV formatted line.
        /// </summary>
        /// <param name="line">Non-formatted, raw field values</param>
        void WriteLine(IEnumerable<string?> line);
        /// <summary>
        /// Writes a line separator to the stream
        /// </summary>
        void WriteLineSeparator();
        /// <summary>
        /// Writes a column separator to the stream
        /// </summary>
        void WriteColumnSeparator();
        /// <summary>
        /// Writes a field value to the stream
        /// </summary>
        /// <param name="field"></param>
        void WriteField(string? field);
        /// <summary>
        /// Flushes the underlying stream
        /// </summary>
        void Flush();
        /// <summary>
        /// Closes the underlying stream
        /// </summary>
        void Close();
    }
}
