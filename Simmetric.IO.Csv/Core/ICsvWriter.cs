using System;
using System.Collections.Generic;

namespace Simmetric.IO.Csv
{
    public interface ICsvWriter : IDisposable
    {
        void WriteLine(IEnumerable<string> line);
        void WriteLineSeparator();
        void WriteColumnSeparator();
        void WriteField(string field);
        void Flush();
        void Close();
    }
}
