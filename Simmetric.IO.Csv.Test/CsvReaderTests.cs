namespace Simmetric.IO.Csv.Test
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using NSubstitute;
    using Simmetric.IO.Csv.Test.Class;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    [TestClass]
    public class CsvReaderTests
    {
        private TextReader underlyingReader;

        [TestInitialize]
        public void TestInitialize()
        {
            underlyingReader = Substitute.For<TextReader>();
        }

        [TestMethod]
        public void ReadLine_MultipleLinesMultipleFields_SplitsLinesAndFieldsCorrectly()
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

            var line = csvReader.ReadLine();
            Assert.AreEqual(expectedLineOneFieldOne, line.ElementAt(0));
            Assert.AreEqual(expectedLineOneFieldTwo, line.ElementAt(1));
            line = csvReader.ReadLine();
            Assert.AreEqual(expectedLineTwoFieldOne, line.ElementAt(0));
            Assert.AreEqual(expectedLineTwoFieldTwo, line.ElementAt(1));

        }

        [TestMethod]
        public void ReadAsType_AllTypes_CorrectTypeConversion()
        {
            var csvFormat = CsvFormat.SemicolonSeparatedNoHeaders;
            SetupReader("1;2.222;3.333;2004-04-04 04:44:44;true" + (char)3);

            var reader = GetReader(csvFormat);

            Assert.AreEqual(1, reader.ReadAsInt32());
            Assert.AreEqual(2.222, reader.ReadAsDouble());
            Assert.AreEqual(3.333m, reader.ReadAsDecimal());
            Assert.AreEqual(new DateTime(2004, 4, 4, 4, 44, 44), reader.ReadAsDateTime());
            Assert.AreEqual(true, reader.ReadAsBoolean());
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
        public void ReadLineGeneric_HasHeaders_ReturnsPopulatedObject()
        {
            var csvFormat = CsvFormat.SemicolonSeparatedWithHeaders;
            SetupReader(
                "myInt;myDouble;myString;myDateTime;myIntProperty;myNullableIntProperty\n1;2.3;some text;2018-01-01 12:34;5;" + (char)3
                );
            var reader = GetReader(csvFormat);

            var result = reader.ReadLine<GenericTestClass>();

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.myInt);
            Assert.AreEqual(2.3, result.myDouble);
            Assert.AreEqual("some text", result.myString);
            Assert.AreEqual(5, result.MyIntProperty);
            Assert.AreEqual(null, result.MyNullableIntProperty);
            Assert.AreEqual(new DateTime(2018, 1, 1, 12, 34, 0), result.myDateTime);
        }

        [TestMethod]
        public void ReadToEnd_MultipleLines_ReturnsMultipleLines()
        {
            SetupReader("1;2;3;4\n1;2;3;4\n1;2;3;4" + (char)3);
            var reader = GetReader(CsvFormat.SemicolonSeparatedNoHeaders);

            var result = reader.ReadToEnd();
            var count = 0;
            foreach(var item in result)
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
        public void ReadToEndGeneric_MultipleLines_ReturnsMultipleObjects()
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

            var result = reader.ReadToEnd<GenericTestClass>();
            var count = 0;
            foreach(var item in result)
            {
                count++;
                Assert.AreEqual(expectedResult, item);
            }
            Assert.AreEqual(2, count);
        }

        private void SetupReader(string contentToReturn)
        {
            underlyingReader.Peek().Returns(1); //it's hard to have Peek() return -1 when Read() has finished its run, so instead there is a delimiter at the end of every CSV literal
            underlyingReader.Read().Returns(contentToReturn.First(), contentToReturn.Substring(1).ToCharArray().Select(c => (int) c).ToArray());
            underlyingReader.ReadAsync(Arg.Any<char[]>(), Arg.Any<int>(), Arg.Any<int>()).Returns(contentToReturn.First(), contentToReturn.Substring(1).ToCharArray().Select(c => (int)c).ToArray());
        }

        private CsvReader GetReader(CsvFormat format)
        {
            return new CsvReader(underlyingReader, format);
        }
    }
}
