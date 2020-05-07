namespace Simmetric.IO.Csv.Test
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NSubstitute;
    using System.IO;

    [TestClass]
    public class CsvWriterTests
    {
        private TextWriter underlyingWriter;

        [TestInitialize]
        public void TestInitialize()
        {
            underlyingWriter = Substitute.For<TextWriter>();
        }

        [TestMethod]
        public void WriteField_WriteSingleField_WritesToUnderlyingStream()
        {
            var format = CsvFormat.DefaultNoHeaders;
            string fieldToWrite = "ATextField";

            var writer = GetWriter(format);
            writer.WriteField(fieldToWrite);

            underlyingWriter.Received(1).Write(fieldToWrite);
        }

        [TestMethod]
        public void WriteField_ContentContainsSeparatorChar_ContentIsDelimited()
        {
            var format = CsvFormat.DefaultNoHeaders;
            string fieldToWrite = "A line; with, \ndelimiters\r\n in it.";

            var writer = GetWriter(format);
            writer.WriteField(fieldToWrite);

            underlyingWriter.Received(2).Write(format.TextQualifier.Value);
            underlyingWriter.Received(1).Write(fieldToWrite);
        }

        [TestMethod]
        public void WriteLine_MultipleFields_SeparatesFieldsCorrectly()
        {
            CsvFormat format = CsvFormat.DefaultNoHeaders;
            string[] lineToWrite = {"One", "2", "3.0", "Four", "2055-05-05"};
            var writer = GetWriter(format);

            writer.WriteLine(lineToWrite);
            writer.Flush();

            underlyingWriter.Received(1).Write(lineToWrite[0]);
            underlyingWriter.Received(1).Write(lineToWrite[1]);
            underlyingWriter.Received(1).Write(lineToWrite[2]);
            underlyingWriter.Received(1).Write(lineToWrite[3]);
            underlyingWriter.Received(1).Write(lineToWrite[4]);
            underlyingWriter.Received(4).Write(format.ColumnSeparator);
            underlyingWriter.Received(1).Write(format.LineSeparator);
            underlyingWriter.Received().Flush();
        }

        [TestMethod]
        public void Constructor_WithHeaders_WritesHeadersToUnderlyingStream()
        {
            string[] headersToWrite = new[] {"H1", "H2", "H3"};
            var csvFormat = CsvFormat.Default;
            csvFormat.Headers = headersToWrite;

            GetWriter(csvFormat);

            underlyingWriter.Received(1).Write(headersToWrite[0]);
            underlyingWriter.Received(1).Write(headersToWrite[1]);
            underlyingWriter.Received(1).Write(headersToWrite[2]);
            underlyingWriter.Received(2).Write(CsvFormat.DefaultNoHeaders.ColumnSeparator);
            underlyingWriter.Received(1).Write(CsvFormat.DefaultNoHeaders.LineSeparator);
        }

        private CsvWriter GetWriter(CsvFormat format)
        {
            return new CsvWriter(underlyingWriter, format);
        }
    }
}
