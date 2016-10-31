using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;

namespace Simmetric.IO.Csv.Test
{
    [TestClass]
    public class TestCsvReader
    {
        [TestMethod]
        public void Formatting()
        {
            var input = "\"1;One\"\tOne One;\"1;;\t; ;One\"" + Environment.NewLine + "2\"Two\";\"2;Two\"";

            //default formatting
            var csvFormat = CsvFormat.DefaultNoHeaders;
            var stream = CreateStream(input);

            using (var csvReader = new CsvReader(stream, csvFormat))
            {
                var line = csvReader.ReadLine();
                Assert.AreEqual("1;One\tOne One", line[0]);
                Assert.AreEqual("1;;\t; ;One", line[1]);

                line = csvReader.ReadLine();
                Assert.AreEqual("2Two", line[0]);
                Assert.AreEqual("2;Two", line[1]);
            }

            //no text qualifier
            stream = CreateStream(input);
            csvFormat.TextQualifier = null;
            using (var csvReader = new CsvReader(stream, csvFormat))
            {
                var startLine = csvReader.LinePosition;

                Assert.AreEqual("\"1", csvReader.Read());
                Assert.AreEqual("One\"\tOne One", csvReader.Read());
                Assert.AreEqual("\"1", csvReader.Read());
                Assert.AreEqual(null, csvReader.Read());
                Assert.AreEqual("\t", csvReader.Read());
                Assert.AreEqual(" ", csvReader.Read());
                Assert.AreEqual("One\"", csvReader.Read());

                Assert.AreEqual(++startLine, csvReader.LinePosition);

                Assert.AreEqual("2\"Two\"", csvReader.Read());
                Assert.AreEqual("\"2", csvReader.Read());
                Assert.AreEqual("Two\"", csvReader.Read());
            }

            //alternate column separator
            stream = CreateStream(input);
            csvFormat.ColumnSeparator = '\t';
            using (var csvReader = new CsvReader(stream, csvFormat))
            {
                var startLine = csvReader.LinePosition;

                Assert.AreEqual("\"1;One\"", csvReader.Read());
                Assert.AreEqual("One One;\"1;;", csvReader.Read());
                Assert.AreEqual("; ;One\"", csvReader.Read());

                Assert.AreEqual(++startLine, csvReader.LinePosition);

                Assert.AreEqual("2\"Two\";\"2;Two\"", csvReader.Read());
            }

            //alternate line separator
            stream = CreateStream(input);
            csvFormat.LineSeparator = "One";
            using (var csvReader = new CsvReader(stream, csvFormat))
            {
                var startLine = csvReader.LinePosition;

                Assert.AreEqual("\"1;", csvReader.Read());
                Assert.AreEqual(++startLine, csvReader.LinePosition);

                Assert.AreEqual("\"", csvReader.Read());
                Assert.AreEqual(null, csvReader.Read());
                Assert.AreEqual(++startLine, csvReader.LinePosition);

                Assert.AreEqual(" ", csvReader.Read());
                Assert.AreEqual(++startLine, csvReader.LinePosition);

                Assert.AreEqual(";\"1;;", csvReader.Read());
                Assert.AreEqual("; ;", csvReader.Read());
                Assert.AreEqual(++startLine, csvReader.LinePosition);

                Assert.AreEqual("\"" + Environment.NewLine + "2\"Two\";\"2;Two\"", csvReader.Read());
            }
        }

        [TestMethod]
        public void Types()
        {
            var csvFormat = CsvFormat.DefaultNoHeaders;
            var stream = CreateStream("1;2.222;3.333;2004-04-04 04:44:44;true");
            using (var reader = new CsvReader(stream, csvFormat))
            {
                Assert.AreEqual(1, reader.ReadAsInt32());
                Assert.AreEqual(2.222, reader.ReadAsDouble());
                Assert.AreEqual(3.333m, reader.ReadAsDecimal());
                Assert.AreEqual(new DateTime(2004, 4, 4, 4, 44, 44), reader.ReadAsDateTime());
                Assert.AreEqual(true, reader.ReadAsBoolean());
            }
        }

        [TestMethod]
        public void Headers()
        {
            var csvFormat = CsvFormat.Default;
            var stream = CreateStream("Header1;Header2;Header3" + Environment.NewLine + "1;2;3" + Environment.NewLine + "4;5;6");
            using (var reader = new CsvReader(stream, csvFormat))
            {
                Assert.IsNotNull(csvFormat.Headers);
                Assert.AreEqual(string.Join(";", csvFormat.Headers), "Header1;Header2;Header3");

                var line = reader.ReadLine();
                Assert.AreEqual(string.Join(";", line), "1;2;3");
            }
        }

        [TestMethod]
        public async Task Async()
        {
            var input = "\"1;One\"\tOne One;\"1;;\t; ;One\"" + Environment.NewLine + "2\"Two\";\"2;Two\"";

            //default formatting
            var csvFormat = CsvFormat.DefaultNoHeaders;
            var stream = CreateStream(input);

            using (var csvReader = new CsvReader(stream, csvFormat))
            {
                var line = await csvReader.ReadLineAsync();
                Assert.AreEqual("1;One\tOne One", line[0]);
                Assert.AreEqual("1;;\t; ;One", line[1]);

                line = await csvReader.ReadLineAsync();
                Assert.AreEqual("2Two", line[0]);
                Assert.AreEqual("2;Two", line[1]);
            }
        }

        private Stream CreateStream(string input)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(input);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        private void AppendToStream(ref Stream stream, string input)
        {
            stream.Position= stream.Length;
            var writer = new StreamWriter(stream);
            writer.Write(input);
            writer.Flush();
            stream.Position = 0;
        }
    }
}
