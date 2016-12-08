using System;
using System.Collections.Generic;

namespace Simmetric.IO.Csv
{
    public interface ICsvReader : IDisposable
    {
        IEnumerable<string> ReadLine();
        string Read();
        bool? ReadAsBoolean();
        DateTime? ReadAsDateTime();
        decimal? ReadAsDecimal();
        double? ReadAsDouble();
        int? ReadAsInt32();
    }
}
