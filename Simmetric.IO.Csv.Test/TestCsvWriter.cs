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
            var writer = new CsvWriter(buffer, CsvFormat.Default);
            writer.WriteLine(new string[] { "One", "2", "3.0", "Fo;ur", "2055-05-05" });
            writer.Flush();

            buffer.Position = 0;
            var reader = new StreamReader(buffer);
            var result = reader.ReadToEnd();
            Assert.AreEqual("\"One\";\"2\";\"3.0\";\"Fo;ur\";\"2055-05-05\"" + Environment.NewLine, result);
        }
    }
}
