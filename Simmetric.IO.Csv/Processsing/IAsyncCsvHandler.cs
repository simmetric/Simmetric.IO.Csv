using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Simmetric.IO.Csv
{
    /// <summary>
    /// Base interface for AsyncCsvHandler classes.
    /// </summary>
    public interface IAsyncCsvHandler
    {
        /// <summary>
        /// Called before processing of a file, stream or string begins
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="headers"></param>
        Task BeginProcessingAsync(string fileName, IEnumerable<string?>? headers);
        /// <summary>
        /// Called when processing of a file, stream or string has ended
        /// </summary>
        Task EndProcessingAsync();
        /// <summary>
        /// Called when an exception occurs in the handler's ProcessRecord or ProcessRecordSet method.
        /// </summary>
        /// <param name="ex">The exception thrown by the handler class</param>
        /// <returns>A message line which will be written to the output</returns>
        Task HandleRecordErrorAsync(Exception ex);
    }

    /// <summary>
    /// Handler  classes implementing this interface receive a call for every individual CSV record
    /// </summary>
    public interface IAsyncCsvRecordHandler : IAsyncCsvHandler
    {
        /// <summary>
        /// Process a single record from the CSV document
        /// </summary>
        /// <param name="recordNumber"></param>
        /// <param name="fields">All fields of the record as a string enumerable</param>
        /// <param name="message"></param>
        /// <returns></returns>
        Task<bool> ProcessRecordAsync(int recordNumber, IEnumerable<string?> fields, out string message);
    }

    /// <summary>
    /// Handler classes implementing this interface receive a set of a predefined number of records to process at a time
    /// </summary>
    public interface IAsyncCsvSetHandler : IAsyncCsvHandler
    {
        /// <summary>
        /// Process a set of consecutive records from the CSV document
        /// </summary>
        /// <param name="records">A multidimensional array where the first dimension is the record index, and the second dimension is the column index</param>
        /// <param name="messages"></param>
        /// <returns></returns>
        Task<bool> ProcessRecordSetAsync(IEnumerable<IEnumerable<string?>> records, out IEnumerable<string> messages);
    }
}
