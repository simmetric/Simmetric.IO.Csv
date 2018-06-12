using System;
using System.Collections.Generic;

namespace Simmetric.IO.Csv
{
    public interface ICsvReader : IDisposable
    {
        IEnumerable<string> ReadLine();
        IEnumerable<IEnumerable<string>> ReadToEnd();
        T ReadLine<T>() where T : new();
        IEnumerable<T> ReadToEnd<T>() where T : new();
        string Read();
        bool? ReadAsBoolean();
        DateTime? ReadAsDateTime();
        decimal? ReadAsDecimal();
        double? ReadAsDouble();
        int? ReadAsInt32();
    }
}
