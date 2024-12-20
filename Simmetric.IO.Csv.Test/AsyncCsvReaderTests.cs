﻿namespace Simmetric.IO.Csv.Test
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NSubstitute;
    using Simmetric.IO.Csv.Core;
    using Simmetric.IO.Csv.Test.Class;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    [TestClass]
    public class AsyncCsvReaderTests
    {
        private TextReader underlyingReader;

        [TestMethod]
        public async Task ReadLineAsync_MultipleLinesMultipleFields_SplitsLinesAndFieldsCorrectly()
        {
            var input = "OneOne;OneTwo\nTwoOne;TwoTwo" + (char)3;
            var expectedLineOneFieldOne = "OneOne";
            var expectedLineOneFieldTwo = "OneTwo";
            var expectedLineTwoFieldOne = "TwoOne";
            var expectedLineTwoFieldTwo = "TwoTwo";
            //default formatting
            var csvFormat = CsvFormat.SemicolonSeparatedNoHeaders;
            csvFormat.LineSeparator = "\n";
            SetupReader(input);

            var csvReader = GetReader(csvFormat);

            var line = await csvReader.ReadLineAsync();
            Assert.AreEqual(expectedLineOneFieldOne, line.ElementAt(0));
            Assert.AreEqual(expectedLineOneFieldTwo, line.ElementAt(1));
            line = await csvReader.ReadLineAsync();
            Assert.AreEqual(expectedLineTwoFieldOne, line.ElementAt(0));
            Assert.AreEqual(expectedLineTwoFieldTwo, line.ElementAt(1));

        }

        [TestMethod]
        public async Task ReadAsTypeAsync_AllTypes_CorrectTypeConversion()
        {
            var csvFormat = CsvFormat.SemicolonSeparatedNoHeaders;
            SetupReader("1;2.222;3.333;2004-04-04 04:44:44;true" + (char)3);

            var reader = GetReader(csvFormat);

            Assert.AreEqual(1, await reader.ReadAsInt32Async());
            Assert.AreEqual(2.222, await reader.ReadAsDoubleAsync());
            Assert.AreEqual(3.333m, await reader.ReadAsDecimalAsync());
            Assert.AreEqual(new DateTime(2004, 4, 4, 4, 44, 44), await reader.ReadAsDateTimeAsync());
            Assert.AreEqual(true, await reader.ReadAsBooleanAsync());
        }

        [TestMethod]
        public void Constructor_HasHeaders_ExtractsHeadersCorrectly()
        {
            var csvFormat = CsvFormat.SemicolonSeparatedWithHeaders;
            var csvContent = "Header1;Header2;Header3" + Environment.NewLine;
            var expectedHeaderOne = "Header1";
            var expectedHeaderTwo = "Header2";
            var expectedHeaderThree = "Header3";
            SetupReader(csvContent);

            var reader = GetReader(csvFormat);

            Assert.IsNotNull(csvFormat.Headers);
            Assert.AreEqual(expectedHeaderOne, reader.Format.Headers.ElementAt(0));
            Assert.AreEqual(expectedHeaderTwo, reader.Format.Headers.ElementAt(1));
            Assert.AreEqual(expectedHeaderThree, reader.Format.Headers.ElementAt(2));
        }

        [TestMethod]
        public async Task ReadLineAsyncGeneric_HasHeaders_ReturnsPopulatedObject()
        {
            var csvFormat = CsvFormat.SemicolonSeparatedWithHeaders;
            SetupReader(
                "myInt;myDouble;myString;myDateTime;myIntProperty;myNullableIntProperty\n1;2.3;some text;2018-01-01 12:34;5;" + (char)3
                );
            var reader = GetReader(csvFormat);

            var result = await reader.ReadLineAsync<GenericTestClass>();

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.myInt);
            Assert.AreEqual(2.3, result.myDouble);
            Assert.AreEqual("some text", result.myString);
            Assert.AreEqual(5, result.MyIntProperty);
            Assert.AreEqual(null, result.MyNullableIntProperty);
            Assert.AreEqual(new DateTime(2018, 1, 1, 12, 34, 0), result.myDateTime);
        }

        [TestMethod]
        public async Task ReadToEndAsync_MultipleLines_ReturnsMultipleLines()
        {
            SetupReader("1;2;3;4\n1;2;3;4\n1;2;3;4" + (char)3);
            var reader = GetReader(CsvFormat.SemicolonSeparatedNoHeaders);

            var result = reader.ReadToEndAsync();
            var count = 0;
            await foreach(var item in result)
            {
                count++;
                CollectionAssert.AreEqual(
                    new List<string>
                    {
                        "1",
                        "2",
                        "3",
                        "4"
                    },
                    item.ToList());
            }

            Assert.AreEqual(3, count);
        }

        [TestMethod]
        public async Task ReadToEndAsyncGeneric_MultipleLines_ReturnsMultipleObjects()
        {
            SetupReader(
                "myInt;myDouble;myString;myDateTime;myIntProperty;myNullableIntProperty\n1;2.3;some text;2018-01-01 12:34;5;null\n1;2.3;some text;2018-01-01 12:34;5;null" + (char)3
                );
            var reader = GetReader(CsvFormat.SemicolonSeparatedWithHeaders);
            var expectedResult = new GenericTestClass
            {
                myInt = 1,
                myDouble = 2.3,
                myString = "some text",
                myDateTime = new DateTime(2018, 1, 1, 12, 34, 00),
                MyIntProperty = 5,
                MyNullableIntProperty = null
            };

            var result = reader.ReadToEndAsync<GenericTestClass>();
            var count = 0;
            await foreach(var item in result)
            {
                count++;
                Assert.AreEqual(expectedResult, item);
            }
            Assert.AreEqual(2, count);
        }

        private void SetupReader(string contentToReturn)
        {
            var memoryStream = new MemoryStream(contentToReturn.Select(c=>(byte)c).ToArray());
            underlyingReader = new StreamReader(memoryStream);
        }

        private AsyncCsvReader GetReader(CsvFormat format)
        {
            return new AsyncCsvReader(underlyingReader, format);
        }
    }
}
