using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Simmetric.IO.Csv.Test
{
    [TestClass]
    public class CsvFormatTests
    {
        [DataTestMethod]
        [DataRow(',', "\r\n", "a\r\nb", true)]
        [DataRow(',', "\r\n", "a,b", true)]
        [DataRow(',', "\r\n", "a,b\r\na,b", true)]
        [DataRow(',', "\r\n", "ab", false)]
        [DataRow(',', "\r\n", null, false)]
        [DataRow(',', "\r\n", "", false)]
        public void ContainsSeparators_WithInput_ReturnsExpectedValue(char columnSeparator, string lineSeparator, string input, bool expectedOutput)
        {
            var format = new CsvFormat
            {
                ColumnSeparator = columnSeparator,
                LineSeparator = lineSeparator
            };

            var result = format.ContainsSeparators(input);

            Assert.AreEqual(expectedOutput, result);
        }

        [DataTestMethod]
        [DataRow("a", true)]
        [DataRow("", false)]
        [DataRow(null, false)]
        [DataRow("0", false)]
        [DataRow(" 0 ", false)]
        [DataRow(" 0 1 ", true)]
        public void ContainsText_WithInput_ReturnsExpectedValue(string input, bool expectedValue)
        {
            var result = CsvFormat.ContainsText(input);

            Assert.AreEqual(expectedValue, result);
        }

        [TestMethod]
        public void Formats_CsvWithHeaders_ReturnsCsvFormat()
        {
            var csv = CsvFormat.CsvWithHeaders;

            Assert.AreEqual(',', csv.ColumnSeparator);
            Assert.AreEqual(true, csv.HasHeaders);
        }

        [TestMethod]
        public void Formats_CsvNoHeaders_ReturnsCsvFormat()
        {
            var csv = CsvFormat.CsvNoHeaders;

            Assert.AreEqual(',', csv.ColumnSeparator);
            Assert.AreEqual(false, csv.HasHeaders);
        }

        [TestMethod]
        public void Formats_TsvNoHeaders_ReturnsTsvFormat()
        {
            var tsv = CsvFormat.TsvNoHeaders;

            Assert.AreEqual('\t', tsv.ColumnSeparator);
            Assert.AreEqual(false, tsv.HasHeaders);
        }

        [TestMethod]
        public void Formats_TsvWithHeaders_ReturnsTsvFormat()
        {
            var tsv = CsvFormat.TsvWithHeaders;

            Assert.AreEqual('\t', tsv.ColumnSeparator);
            Assert.AreEqual(true, tsv.HasHeaders);
        }
    }
}
