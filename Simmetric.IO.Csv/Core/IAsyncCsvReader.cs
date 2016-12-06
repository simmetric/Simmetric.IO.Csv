using System;
using System.Threading.Tasks;

namespace Simmetric.IO.Csv
{
    public interface IAsyncCsvReader : IDisposable
    {
        Task<string[]> ReadLineAsync();
        Task<string> ReadAsync();
    }
}
