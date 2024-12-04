using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Simmetric.IO.Csv.Core
{
    /// <summary>
    /// Asynchronously reads a CSV document from the input stream
    /// </summary>
    public interface IAsyncCsvReader : IDisposable
    {
        /// <summary>
        /// Reads the next line and returns the field values as a <see cref="T:System.String[]"/>
        /// </summary>
        /// <returns>An enumerable of field values as strings</returns>
        Task<IEnumerable<string?>> ReadLineAsync();
        /// <summary>
        /// Reads until the end of the CSV file and returns all lines as a nested IEnumerable&lt;string&gt;
        /// </summary>
        /// <returns>An enumerable of field values of all lines as strings</returns>
        IAsyncEnumerable<IEnumerable<string?>> ReadToEndAsync();
        /// <summary>
        /// Reads the next line and uses it to populate a new instance of class T
        /// </summary>
        /// <typeparam name="T">The type to which the line will be converted</typeparam>
        /// <returns>One line as a strong typed object</returns>
        Task<T> ReadLineAsync<T>() where T : new();
        /// <summary>
        /// Reads until the end of the CSV file and returns all lines as an IEnumerable of type T
        /// </summary>
        /// <typeparam name="T">The type to which each line will be converted</typeparam>
        /// <returns>All lines as an enumerable of strong typed object</returns>
        IAsyncEnumerable<T> ReadToEndAsync<T>() where T : new();
        /// <summary>
        /// Reads the next field from the stream, without text qualifiers or separators
        /// </summary>
        /// <returns>The field value as <see cref="T:System.String"/></returns>
        Task<string?> ReadAsync();
        /// <summary>
        /// Reads the next field and returns the value as <see cref="T:System.Nullable`1"/>
        /// </summary>
        /// <returns>The converted value if the field is a valid bool, otherwise <c>null</c></returns>
        Task<bool?> ReadAsBooleanAsync();
        /// <summary>
        /// Reads the next field and returns the value as <see cref="T:System.Nullable`1"/>
        /// </summary>
        /// <returns>The converted value if the field is a valid DateTime, otherwise <c>null</c></returns>
        Task<DateTime?> ReadAsDateTimeAsync();
        /// <summary>
        /// Reads the next field and returns the value as <see cref="T:System.Nullable`1"/>
        /// </summary>
        /// <returns>The converted value if the field is a valid decimal, otherwise <c>null</c></returns>
        Task<decimal?> ReadAsDecimalAsync();
        /// <summary>
        /// Reads the next field and returns the value as <see cref="T:System.Nullable`1"/>
        /// </summary>
        /// <returns>The converted value if the field is a valid double, otherwise <c>null</c></returns>
        Task<double?> ReadAsDoubleAsync();
        /// <summary>
        /// Reads the next field and returns the value as <see cref="T:System.Nullable`1"/>
        /// </summary>
        /// <returns>The converted value if the field is a valid integer, otherwise <c>null</c></returns>
        Task<int?> ReadAsInt32Async();
    }
}
