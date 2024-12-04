namespace Simmetric.IO.Csv.Test
{
    using System.Collections.Generic;
    using System.Linq;
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NSubstitute;
    using System.IO;
    using System.Threading.Tasks;
    using NSubstitute.ExceptionExtensions;
    using Simmetric.IO.Csv.Processsing;

    [TestClass]
    public class AsyncCsvProcessorTests
    {
        private IAsyncCsvHandler handler;
        private IAsyncCsvRecordHandler recordHandler;
        private IAsyncCsvSetHandler setHandler;

        [TestInitialize]
        public void TestInitialize()
        {
            handler = Substitute.For<IAsyncCsvHandler>();
            recordHandler = Substitute.For<IAsyncCsvRecordHandler>();
            setHandler = Substitute.For<IAsyncCsvSetHandler>();
        }

        [TestMethod]
        public async Task ProcessCsvAsync_BasicHandler_CallsBeginProcessing()
        {
            var format = CsvFormat.DefaultNoHeaders;
            var documentName = "Testcase RecordWise";
            var fieldsToWrite = "Field1;Field2";

            var processor = GetBasicProcessor();
            await processor.ProcessCsvAsync(documentName, fieldsToWrite, format);

            await handler.Received(1).BeginProcessingAsync(documentName, null);
        }

        [TestMethod]
        public async Task ProcessCsvAsync_BasicHandler_CallsEndProcessing()
        {
            var format = CsvFormat.DefaultNoHeaders;
            var documentName = "Testcase RecordWise";
            var fieldsToWrite = "Field1;Field2";

            var processor = GetBasicProcessor();
            await processor.ProcessCsvAsync(documentName, fieldsToWrite, format);

            await handler.Received(1).EndProcessingAsync();
        }

        [TestMethod]
        public async Task ProcessCsvAsync_RecordWise_CallsHandleRecordError()
        {
            var format = CsvFormat.DefaultNoHeaders;
            var documentName = "Testcase RecordWise";
            var fieldsToWrite = "Field1;Field2";
            SetupRecordHandlerForRecordError();

            var processor = GetRecordWiseProcessor();
            await processor.ProcessCsvAsync(documentName, fieldsToWrite, format);

            await recordHandler.Received(1).HandleRecordErrorAsync(Arg.Any<InvalidOperationException>());
        }

        [TestMethod]
        public async Task ProcessCsvAsync_RecordWise_CallsProcessRecord()
        {
            var format = CsvFormat.DefaultNoHeaders;
            var documentName = "Testcase RecordWise";
            var fieldsToWrite = "1;2.0;03;Four;Five";

            var csvProc = GetRecordWiseProcessor();
            await csvProc.ProcessCsvAsync(documentName, fieldsToWrite, format);
            await recordHandler.Received(1).ProcessRecordAsync(Arg.Any<int>(), Arg.Any<IEnumerable<string>>(), out _);
        }

        [TestMethod]
        public async Task ProcessCsvAsync_SetWise_CallsHandleRecordError()
        {
            var format = CsvFormat.DefaultNoHeaders;
            var documentName = "Testcase SetWise";
            var fieldsToWrite = "Field1;Field2";
            SetupSetHandlerForRecordError();

            var processor = GetSetWiseProcessor();
            await processor.ProcessCsvAsync(documentName, fieldsToWrite, format, 1);

            await setHandler.Received(1).HandleRecordErrorAsync(Arg.Any<InvalidOperationException>());
        }

        [TestMethod]
        public async Task ProcessCsvAsync_SetWise_CallsProcessRecordSet()
        {
            var format = CsvFormat.DefaultNoHeaders;
            format.HasHeaders = false;
            var documentName = "Testcase SetWise";
            var setSize = 2;
            var expectedSets = 2;
            var fieldsToWrite = @"1;2.0;03;Four;Five
1;2.0;03;Four;Five
1;2.0;03;Four;Five
1;2.0;03;Four;Five";

            var csvProc = GetSetWiseProcessor();
            await csvProc.ProcessCsvAsync(documentName, fieldsToWrite, format, setSize);

            await setHandler.Received(2).ProcessRecordSetAsync(Arg.Is<IEnumerable<IEnumerable<string>>>(list => list.Count() == expectedSets), out IEnumerable<string> messages);
        }

        [TestMethod]
        public async Task ProcessStreamAsync_FromStart_WritesToHandler()
        {
            const string DocumentName = "doc";
            var format = CsvFormat.SemicolonSeparatedNoHeaders;
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.WriteLine(string.Join(new string(new char[] { format.ColumnSeparator }), "a", "b", "c"));
            writer.Flush();
            stream.Position = 0;
            var sut = GetRecordWiseProcessor();

            await sut.ProcessStreamAsync(DocumentName, stream, format);

            await recordHandler.Received(1).ProcessRecordAsync(1, Arg.Any<IEnumerable<string>>(), out string message);
            Assert.IsNull(message);

        }

        private AsyncCsvProcessor GetBasicProcessor()
        {
            return new AsyncCsvProcessor(handler);
        }

        private AsyncCsvProcessor GetRecordWiseProcessor()
        {
            return new AsyncCsvProcessor(recordHandler);
        }

        private AsyncCsvProcessor GetSetWiseProcessor()
        {
            return new AsyncCsvProcessor(setHandler);
        }

        private void SetupRecordHandlerForRecordError()
        {
            recordHandler.ProcessRecordAsync(Arg.Any<int>(), Arg.Any<IEnumerable<string>>(), out string message)
                .ThrowsAsync(new InvalidOperationException("This record throws an exception"));
        }

        private void SetupSetHandlerForRecordError()
        {
            setHandler.ProcessRecordSetAsync(Arg.Any<IEnumerable<IEnumerable<string>>>(), out IEnumerable<string> messages)
                .ThrowsAsync(new InvalidOperationException("This set throws an exception"));
        }
    }
}
