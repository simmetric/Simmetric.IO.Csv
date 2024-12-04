namespace Simmetric.IO.Csv.Test
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NSubstitute;
    using Simmetric.IO.Csv.Core;
    using System.IO;
    using System.Threading.Tasks;

    [TestClass]
    public class AsyncCsvWriterTests
    {
        private TextWriter underlyingWriter;

        [TestInitialize]
        public void TestInitialize()
        {
            underlyingWriter = Substitute.For<TextWriter>();
        }

        [TestMethod]
        public async Task WriteFieldAsync_WriteSingleField_WritesToUnderlyingStream()
        {
            var format = CsvFormat.SemicolonSeparatedNoHeaders;
            string fieldToWrite = "ATextField";

            var writer = GetWriter(format);
            await writer.WriteFieldAsync(fieldToWrite);

            await underlyingWriter.Received(1).WriteAsync(fieldToWrite);
        }

        [TestMethod]
        public async Task WriteFieldAsync_ContentContainsSeparatorChar_ContentIsDelimited()
        {
            var format = CsvFormat.SemicolonSeparatedNoHeaders;
            string fieldToWrite = "A line; with, \ndelimiters\r\n in it.";

            var writer = GetWriter(format);
            await writer.WriteFieldAsync(fieldToWrite);

            await underlyingWriter.Received(2).WriteAsync(format.TextQualifier.Value);
            await underlyingWriter.Received(1).WriteAsync(fieldToWrite);
        }

        [TestMethod]
        public async Task WriteLineAsync_MultipleFields_SeparatesFieldsCorrectly()
        {
            CsvFormat format = CsvFormat.SemicolonSeparatedNoHeaders;
            string[] lineToWrite = {"One", "2", "3.0", "Four", "2055-05-05"};
            var writer = GetWriter(format);

            await writer.WriteLineAsync(lineToWrite);
            await writer.FlushAsync();

            await underlyingWriter.Received(1).WriteAsync(lineToWrite[0]);
            await underlyingWriter.Received(1).WriteAsync(lineToWrite[1]);
            await underlyingWriter.Received(1).WriteAsync(lineToWrite[2]);
            await underlyingWriter.Received(1).WriteAsync(lineToWrite[3]);
            await underlyingWriter.Received(1).WriteAsync(lineToWrite[4]);
            await underlyingWriter.Received(4).WriteAsync(format.ColumnSeparator);
            await underlyingWriter.Received(1).WriteAsync(format.LineSeparator);
            await underlyingWriter.Received().FlushAsync();
        }

        [TestMethod]
        public async Task Constructor_WithHeaders_WritesHeadersToUnderlyingStream()
        {
            string[] headersToWrite = new[] {"H1", "H2", "H3"};
            var csvFormat = CsvFormat.SemicolonSeparatedWithHeaders;
            csvFormat.Headers = headersToWrite;

            GetWriter(csvFormat);

            await underlyingWriter.Received(1).WriteAsync(headersToWrite[0]);
            await underlyingWriter.Received(1).WriteAsync(headersToWrite[1]);
            await underlyingWriter.Received(1).WriteAsync(headersToWrite[2]);
            await underlyingWriter.Received(2).WriteAsync(CsvFormat.SemicolonSeparatedNoHeaders.ColumnSeparator);
            await underlyingWriter.Received(1).WriteAsync(CsvFormat.SemicolonSeparatedNoHeaders.LineSeparator);
        }

        private AsyncCsvWriter GetWriter(CsvFormat format)
        {
            return new AsyncCsvWriter(underlyingWriter, format);
        }
    }
}
