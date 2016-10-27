using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Simmetric.IO.Csv.Test
{
    [TestClass]
    public class TestCsvProcessor
    {
        [TestMethod]
        public void RecordWise()
        {
            var format = CsvFormat.Default;
            format.HasHeaders = false;

            var handler = new Simmetric.IO.Csv.Test.Handlers.TestHandler();
            var csvProc = new CsvProcessor(handler);
            handler.SetExpectations(5);
            int recordsProcessed = csvProc.ProcessCsv("Testcase RecordWise", "1;2.0;03;\"Four\";\"Fi;\r\nVe\"", format, 0, 0).RowsProcessed;
            
            Assert.AreEqual(1, recordsProcessed);
            Assert.AreEqual(5, handler.LastRecord.Length, "Unexpected number of fields.");
            Assert.AreEqual("1;2.0;03;Four;Fi;\r\nVe", string.Join(";", handler.LastRecord), "CSV not properly parsed");
        }

        [TestMethod]
        public void RecordSetWise()
        {

        }

        [TestMethod]
        public void ProcessStream()
        {

        }

        [TestMethod]
        public void ProcessCsv()
        {

        }

        [TestMethod]
        public void ProcessFile()
        {

        }
    }
}
