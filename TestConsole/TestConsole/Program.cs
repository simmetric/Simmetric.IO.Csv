using Simmetric.IO.Csv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var format = CsvFormat.DefaultNoHeaders;

            var handler = new MyRecordHandler();
            var csvProc = new CsvProcessor(handler);
            int recordsProcessed = csvProc.ProcessCsv("Testcase RecordWise", "1;2.0;03;\"Four\";\"Fi;\r\nVe\"", format, 0, 0).RowsProcessed;
        }
    }
}
