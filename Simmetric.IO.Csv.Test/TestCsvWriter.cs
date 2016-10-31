using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Simmetric.IO.Csv.Test
{
    [TestClass]
    public class TestCsvWriter
    {
        [TestMethod]
        public void Formatting()
        {
            var buffer = new MemoryStream();
            var writer = new CsvWriter(buffer, CsvFormat.DefaultNoHeaders);
            writer.WriteLine(new [] { "One", "2", "3.0", "Fo;ur", "2055-05-05" });
            writer.Flush();

            buffer.Position = 0;
            var reader = new StreamReader(buffer);
            var result = reader.ReadToEnd();
            Assert.AreEqual("\"One\";\"2\";\"3.0\";\"Fo;ur\";\"2055-05-05\"" + Environment.NewLine, result);
        }

        [TestMethod]
        public void Headers()
        {
            var buffer = new MemoryStream();
            var csvFormat = CsvFormat.Default;
            csvFormat.Headers = new []{ "H1", "H2", "H3" }; 
            var writer = new CsvWriter(buffer, CsvFormat.DefaultNoHeaders);
        }
    }
}
