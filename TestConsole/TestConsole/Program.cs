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
            var format = CsvFormat.Default;
            format.HasHeaders = false;

            var handler = new Simmetric.IO.Csv.Test.Handlers.TestHandler();
            var csvProc = new CsvProcessor(handler);
            handler.SetExpectations(5);
            int recordsProcessed = csvProc.ProcessCsv("Testcase RecordWise", "1;2.0;03;\"Four\";\"Fi;\r\nVe\"", format, 0, 0).RowsProcessed;
        }
    }
}
