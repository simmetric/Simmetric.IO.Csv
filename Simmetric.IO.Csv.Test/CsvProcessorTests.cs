using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Simmetric.IO.Csv.Test
{
    using System.Collections.Generic;
    using System.Linq;

    [TestClass]
    public class CsvProcessorTests
    {
        private ICsvHandler handler;
        private ICsvRecordHandler recordHandler;
        private ICsvSetHandler setHandler;

        [TestInitialize]
        public void TestInitialize()
        {
            handler = Substitute.For<ICsvHandler>();
            recordHandler = Substitute.For<ICsvRecordHandler>();
            setHandler = Substitute.For<ICsvSetHandler>();
        }

        [TestMethod]
        public void ProcessCsv_BasicHandler_CallsBeginProcessing()
        {
            var format = CsvFormat.DefaultNoHeaders;
            var documentName = "Testcase RecordWise";
            var fieldsToWrite = "Field1;Field2";

            var processor = GetBasicProcessor();
            processor.ProcessCsv(documentName, fieldsToWrite, format);

            handler.Received(1).BeginProcessing(documentName, null);
        }

        [TestMethod]
        public void ProcessCsv_Basic_CallsEndProcessing()
        {
            var format = CsvFormat.DefaultNoHeaders;
            var documentName = "Testcase RecordWise";
            var fieldsToWrite = "Field1;Field2";

            var processor = GetBasicProcessor();
            processor.ProcessCsv(documentName, fieldsToWrite, format);

            handler.Received(1).EndProcessing();
        }

        [TestMethod]
        public void ProcessCsv_RecordWise_CallsHandleRecordError()
        {
            var format = CsvFormat.DefaultNoHeaders;
            var documentName = "Testcase RecordWise";
            var fieldsToWrite = "Field1;Field2";
            SetupRecordHandlerForRecordError();

            var processor = GetRecordWiseProcessor();
            processor.ProcessCsv(documentName, fieldsToWrite, format);

            recordHandler.Received(1).HandleRecordError(Arg.Any<InvalidOperationException>());
        }

        [TestMethod]
        public void ProcessCsv_RecordWise_CallsProcessRecord()
        {
            var format = CsvFormat.DefaultNoHeaders;
            var documentName = "Testcase RecordWise";
            var fieldsToWrite = "1;2.0;03;Four;Five";

            var csvProc = GetRecordWiseProcessor();
            csvProc.ProcessCsv(documentName, fieldsToWrite, format);

            string message = null;
            recordHandler.Received(1).ProcessRecord(Arg.Any<int>(), Arg.Any<IEnumerable<string>>(), out message);
        }

        [TestMethod]
        public void ProcessCsv_SetWise_CallsHandleRecordError()
        {
            var format = CsvFormat.DefaultNoHeaders;
            var documentName = "Testcase SetWise";
            var fieldsToWrite = "Field1;Field2";
            SetupSetHandlerForRecordError();

            var processor = GetSetWiseProcessor();
            processor.ProcessCsv(documentName, fieldsToWrite, format, 1);

            setHandler.Received(1).HandleRecordError(Arg.Any<InvalidOperationException>());
        }

        [TestMethod]
        public void ProcessCsv_SetWise_CallsProcessRecordSet()
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
            csvProc.ProcessCsv(documentName, fieldsToWrite, format, setSize);

            IEnumerable<string> messages = null;
            setHandler.Received(2).ProcessRecordSet(Arg.Is<IEnumerable<IEnumerable<string>>>(list => list.Count() == expectedSets), out messages);
        }

        private CsvProcessor GetBasicProcessor()
        {
            return new CsvProcessor(handler);
        }

        private CsvProcessor GetRecordWiseProcessor()
        {
            return new CsvProcessor(recordHandler);
        }

        private CsvProcessor GetSetWiseProcessor()
        {
            return new CsvProcessor(setHandler);
        }

        private void SetupRecordHandlerForRecordError()
        {
            string message = null;
            recordHandler.ProcessRecord(Arg.Any<int>(), Arg.Any<IEnumerable<string>>(), out message)
                .Returns(func => { throw new InvalidOperationException("This record throws an exception"); });
        }

        private void SetupSetHandlerForRecordError()
        {
            IEnumerable<string> messages = null;
            setHandler.ProcessRecordSet(Arg.Any<IEnumerable<IEnumerable<string>>>(), out messages)
                .Returns(func => { throw new InvalidOperationException("This set throws an exception"); });
        }
    }
}
