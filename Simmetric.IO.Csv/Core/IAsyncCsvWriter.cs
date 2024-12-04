using System.Collections.Generic;
using System.Threading.Tasks;

namespace Simmetric.IO.Csv.Core
{
    /// <summary>
    /// Provides async, fast, non-cached, forward-only writing of values in CSV format.
    /// </summary>
    public interface IAsyncCsvWriter
    {
        /// <summary>
     /// Writes the given set of fields to a CSV formatted line.
     /// </summary>
     /// <param name="line">Non-formatted, raw field values</param>
        Task WriteLineAsync(IEnumerable<string?> line);
        /// <summary>
        /// Writes a line separator to the stream
        /// </summary>
        Task WriteLineSeparatorAsync();
        /// <summary>
        /// Writes a column separator to the stream
        /// </summary>
        Task WriteColumnSeparatorAsync();
        /// <summary>
        /// Writes a field value to the stream
        /// </summary>
        /// <param name="field"></param>
        Task WriteFieldAsync(string? field);
        /// <summary>
        /// Flushes the underlying stream
        /// </summary>
        Task FlushAsync();
        /// <summary>
        /// Closes the underlying stream
        /// </summary>
        Task CloseAsync();
    }
}
