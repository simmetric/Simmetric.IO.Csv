using System;
using System.Collections.Generic;

namespace Simmetric.IO.Csv
{    
    /// <summary>
    /// Reads a CSV document from the input stream
    /// </summary>
    public interface ICsvReader : IDisposable
    {
        /// <summary>
        /// Reads the next line and returns the field values as a <see cref="T:System.String[]"/>
        /// </summary>
        /// <returns>An enumerable of field values as strings</returns>
        IEnumerable<string?> ReadLine();
        /// <summary>
        /// Reads until the end of the CSV file and returns all lines as a nested IEnumerable&lt;string&gt;
        /// </summary>
        /// <returns>An enumerable of field values of all lines as strings</returns>
        IEnumerable<IEnumerable<string?>> ReadToEnd();
        /// <summary>
        /// Reads the next line and uses it to populate a new instance of class T
        /// </summary>
        /// <typeparam name="T">The type to which the line will be converted</typeparam>
        /// <returns>One line as a strong typed object</returns>
        T ReadLine<T>() where T : new();
        /// <summary>
        /// Reads until the end of the CSV file and returns all lines as an IEnumerable of type T
        /// </summary>
        /// <typeparam name="T">The type to which each line will be converted</typeparam>
        /// <returns>All lines as an enumerable of strong typed object</returns>
        IEnumerable<T> ReadToEnd<T>() where T : new();
        /// <summary>
        /// Reads the next field from the stream, without text qualifiers or separators
        /// </summary>
        /// <returns>The field value as <see cref="T:System.String"/></returns>
        string? Read();
        /// <summary>
        /// Reads the next field and returns the value as <see cref="T:System.Nullable`1"/>
        /// </summary>
        /// <returns>The converted value if the field is a valid bool, otherwise <c>null</c></returns>
        bool? ReadAsBoolean();
        /// <summary>
        /// Reads the next field and returns the value as <see cref="T:System.Nullable`1"/>
        /// </summary>
        /// <returns>The converted value if the field is a valid DateTime, otherwise <c>null</c></returns>
        DateTime? ReadAsDateTime();
        /// <summary>
        /// Reads the next field and returns the value as <see cref="T:System.Nullable`1"/>
        /// </summary>
        /// <returns>The converted value if the field is a valid decimal, otherwise <c>null</c></returns>
        decimal? ReadAsDecimal();
        /// <summary>
        /// Reads the next field and returns the value as <see cref="T:System.Nullable`1"/>
        /// </summary>
        /// <returns>The converted value if the field is a valid double, otherwise <c>null</c></returns>
        double? ReadAsDouble();
        /// <summary>
        /// Reads the next field and returns the value as <see cref="T:System.Nullable`1"/>
        /// </summary>
        /// <returns>The converted value if the field is a valid integer, otherwise <c>null</c></returns>
        int? ReadAsInt32();
    }
}
