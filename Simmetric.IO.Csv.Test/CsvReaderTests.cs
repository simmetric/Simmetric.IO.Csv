using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Threading.Tasks;

namespace Simmetric.IO.Csv.Test
{
    using System.Linq;
    using NSubstitute;

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
            var input = "OneOne;OneTwo\nTwoOne;TwoTwo\n";
            var expectedLineOneFieldOne = "OneOne";
            var expectedLineOneFieldTwo = "OneTwo";
            var expectedLineTwoFieldOne = "TwoOne";
            var expectedLineTwoFieldTwo = "TwoTwo";
            //default formatting
            var csvFormat = CsvFormat.DefaultNoHeaders;
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
            var csvFormat = CsvFormat.DefaultNoHeaders;
            SetupReader("1;2.222;3.333;2004-04-04 04:44:44;true;");

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
            var csvFormat = CsvFormat.Default;
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

        private void SetupReader(string contentToReturn)
        {
            underlyingReader.Peek().Returns(1);
            underlyingReader.Read().Returns(contentToReturn.First(), contentToReturn.Substring(1).ToCharArray().Select(c => (int) c).ToArray());
            underlyingReader.ReadAsync(Arg.Any<char[]>(), Arg.Any<int>(), Arg.Any<int>()).Returns(contentToReturn.First(), contentToReturn.Substring(1).ToCharArray().Select(c => (int)c).ToArray());
        }

        private CsvReader GetReader(CsvFormat format)
        {
            return new CsvReader(underlyingReader, format);
        }
    }
}
