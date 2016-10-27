using System;
using System.Collections.Generic;
using System.IO;

namespace Simmetric.IO.Csv
{
    /// <summary>
    /// Parses a CSV document and feeds rows to a supplied handler object.
    /// </summary>
    public class CsvProcessor
    {
        private ICsvHandler handler;
        private List<string[]> recordSet;

        /// <param name="handler">Instance of a class implementing ICsvHandler that performs a custom defined task using CSV record input</param>
        public CsvProcessor(ICsvHandler handler)
        {
            this.handler = handler;
        }

        /// <summary>
        /// Processes a CSV formatted <see cref="T:System.IO.Stream"/>.
        /// </summary>
        /// <param name="documentName">A descriptive name for the processed document, for logging purposes.</param>
        /// <param name="inputStream">A CSV formatted stream containing data to be processed.</param>
        /// <param name="format">A <see cref="T:Simmetric.IO.Csv.CsvFormat"/> object representing the formatting used in the stream.</param>
        public int ProcessStream(string documentName, Stream inputStream, CsvFormat format)
        {
            return ProcessStream(documentName, inputStream, null, format, 0, 0);
        }

        /// <summary>
        /// Processes a CSV formatted <see cref="T:System.IO.Stream"/>.
        /// </summary>
        /// <param name="documentName">A descriptive name for the processed document, for logging purposes.</param>
        /// <param name="inputStream">A CSV formatted stream containing data to be processed.</param>
        /// <param name="format">A <see cref="T:Simmetric.IO.Csv.CsvFormat"/> object representing the formatting used in the stream.</param>
        /// <param name="processingSetSize">To process records in sets instead of individually, enter a set size larger than 1. The CsvHandler must implement <see cref="Simmetric.IO.Csv.ICsvSetHandler"/> to support this.</param>
        public int ProcessStream(string documentName, Stream inputStream, CsvFormat format, int processingSetSize)
        {
            return ProcessStream(documentName, inputStream, null, format, processingSetSize, 0);
        }

        /// <summary>
        /// Processes a CSV formatted <see cref="T:System.IO.Stream"/>.
        /// </summary>
        /// <param name="documentName">A descriptive name for the processed document, for logging purposes.</param>
        /// <param name="inputStream">A CSV formatted stream containing data to be processed.</param>
        /// <param name="outputStream">Output messages will be written to this stream.</param>
        /// <param name="format">A <see cref="T:Simmetric.IO.Csv.CsvFormat"/> object representing the formatting used in the stream.</param>
        /// <param name="processingSetSize">To process records in sets instead of individually, enter a set size larger than 1. The CsvHandler must implement <see cref="Simmetric.IO.Csv.ICsvSetHandler"/> to support this.</param>
        /// <param name="startAtRecord">The parser will skip to the record number indicated.</param>
        /// <returns>The number of rows processed</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "Recordset"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "ICsvSetHandler")]
        public int ProcessStream(string documentName, Stream inputStream, Stream outputStream, CsvFormat format, int processingSetSize, int startAtRecord)
        {
            if (inputStream == null)
            {
                throw new ArgumentNullException("inputStream");
            }
            if (format == null)
            {
                throw new ArgumentNullException("format");
            }

            var reader = new CsvReader(inputStream, format);
            string[] headers = null;
            var fileStopWatch = new System.Diagnostics.Stopwatch();
            var recordStopWatch = new System.Diagnostics.Stopwatch();
            StreamWriter outputWriter = null;

            //ensure stream is set to position 0
            if (inputStream.Position > 0)
            {
                inputStream.Position = 0;
            }

            //skip header row and extract headers
            if (format.HasHeaders)
            {
                headers = reader.ReadLine();
            }

            handler.BeginProcessing(documentName, headers);

            //prepare output stream for writing
            if (outputStream != null && outputStream.CanWrite)
            {
                outputWriter = new StreamWriter(outputStream);
            }

            fileStopWatch.Start();

            //skip to start record
            while (reader.LinePosition < startAtRecord)
            {
                reader.ReadLine();
            }

            //prepare recordset for set processing
            if (processingSetSize > 1)
            {
                if (this.handler is ICsvSetHandler)
                {
                    this.recordSet = new List<string[]>(processingSetSize);
                }
                else
                {
                    throw new InvalidOperationException("Recordset processing cannot be used on a CSV handler that does not implement ICsvSetHandler");
                }
            }

            try
            {
                while (!reader.EndOfStream)
                {
                    try
                    {
                        if (processingSetSize < 1)
                        {
                            ProcessRecordwise(reader, outputWriter, recordStopWatch);
                        }
                        else
                        {
                            ProcessSetWise(processingSetSize, reader, outputWriter, recordStopWatch);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (outputWriter != null)
                        {
                            //handled by CsvHandler
                            outputWriter.WriteLine(string.Format("{0}: Error: {1}", reader.LinePosition, this.handler.HandleRecordError(ex)));
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (outputWriter != null)
                {
                    //handled by CsvProcessor
                    outputWriter.WriteLine(string.Format("{0}: Error: {1}", reader.LinePosition, ex.Message));
                }
                else
                {
                    throw;
                }
            }

            fileStopWatch.Stop();
            handler.EndProcessing();
           
            if (outputWriter != null)
            {
                outputWriter.WriteLine(string.Format("Finished processing {0}, did {1} records in {2} seconds.", documentName, reader.LinePosition, fileStopWatch.ElapsedMilliseconds / 1000m));
                outputWriter.Flush();
            }

            return reader.LinePosition;
        }

        /// <summary>
        /// Processes a CSV formatted file and logs output messages to a file in the same location with suffix '-output.txt'
        /// </summary>
        /// <param name="fileName">Full path to the file.</param>
        /// <param name="format">A <see cref="T:Simmetric.IO.Csv.CsvFormat"/> object representing the formatting used in the stream.</param>
        /// <returns>The number of rows processed</returns>
        public int ProcessFile(string fileName, CsvFormat format)
        {
            return ProcessFile(fileName, format, 0, 0);
        }

        /// <summary>
        /// Processes a CSV formatted file and logs output messages to a file in the same location with suffix '-output.txt'
        /// </summary>
        /// <param name="fileName">Full path to the file.</param>
        /// <param name="format">A <see cref="T:Simmetric.IO.Csv.CsvFormat"/> object representing the formatting used in the stream.</param>
        /// <param name="processingSetSize">To process records in sets instead of individually, enter a set size larger than 1. The CsvHandler must implement <see cref="Simmetric.IO.Csv.ICsvSetHandler"/> to support this.</param>
        /// <returns>The number of rows processed</returns>
        public int ProcessFile(string fileName, CsvFormat format, int processingSetSize)
        {
            return ProcessFile(fileName, format, processingSetSize, 0);
        }

        /// <summary>
        /// Processes a CSV formatted file and logs output messages to a file in the same location with suffix '-output.txt'
        /// </summary>
        /// <param name="fileName">Full path to the file.</param>
        /// <param name="format">A <see cref="T:Simmetric.IO.Csv.CsvFormat"/> object representing the formatting used in the stream.</param>
        /// <param name="processingSetSize">To process records in sets instead of individually, enter a set size larger than 1. The CsvHandler must implement <see cref="Simmetric.IO.Csv.ICsvSetHandler"/> to support this.</param>
        /// <param name="startAtRecord">The parser will skip to the record number indicated.</param>
        /// <returns>The number of rows processed</returns>
        public int ProcessFile(string fileName, CsvFormat format, int processingSetSize, int startAtRecord)
        {
            var fileInput = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            var fileOutput = File.Open(fileName + "-output.txt", FileMode.Create);

            int result = ProcessStream(
                Path.GetFileNameWithoutExtension(fileName),
                fileOutput,
                fileInput,
                format,
                processingSetSize,
                startAtRecord
            );

            fileInput.Dispose();
            fileOutput.Dispose();

            return result;
        }

        /// <summary>
        /// Processes a CSV formatted string.
        /// </summary>
        /// <param name="documentName">A descriptive name for the processed document, for logging purposes.</param>
        /// <param name="csvContent">A CSV formatted string</param>
        /// <param name="format">A <see cref="T:Simmetric.IO.Csv.CsvFormat"/> object representing the formatting used in the stream.</param>
        /// <returns>An object containing the number of rows processed and the output messaging.</returns>
        public CsvProcessorResult<string> ProcessCsv(string documentName, string csvContent, CsvFormat format)
        {
            return ProcessCsv(documentName, csvContent, format, 0, 0);
        }

        /// <summary>
        /// Processes a CSV formatted string.
        /// </summary>
        /// <param name="documentName">A descriptive name for the processed document, for logging purposes.</param>
        /// <param name="csvContent">A CSV formatted string</param>
        /// <param name="format">A <see cref="T:Simmetric.IO.Csv.CsvFormat"/> object representing the formatting used in the stream.</param>
        /// <param name="processingSetSize">To pera orocess records in sets instead of individually, enter a set size larger than 1. The CsvHandler must implement <see cref="Simmetric.IO.Csv.ICsvSetHandler"/> to support this.</param>
        /// <returns>An object containing the number of rows processed and the output messaging.</returns>
        public CsvProcessorResult<string> ProcessCsv(string documentName, string csvContent, CsvFormat format, int processingSetSize)
        {
            return ProcessCsv(documentName, csvContent, format, processingSetSize, 0);
        }

        /// <summary>
        /// Processes a CSV formatted string.
        /// </summary>
        /// <param name="documentName">A descriptive name for the processed document, for logging purposes.</param>
        /// <param name="csvContent">A CSV formatted string</param>
        /// <param name="format">A <see cref="T:Simmetric.IO.Csv.CsvFormat"/> object representing the formatting used in the stream.</param>
        /// <param name="processingSetSize">To pera orocess records in sets instead of individually, enter a set size larger than 1. The CsvHandler must implement <see cref="Simmetric.IO.Csv.ICsvSetHandler"/> to support this.</param>
        /// <param name="startAtRecord">The parser will skip to the record number indicated.</param>
        /// <returns>An object containing the number of rows processed and the output messaging.</returns>
        public CsvProcessorResult<string> ProcessCsv(string documentName, string csvContent, CsvFormat format, int processingSetSize, int startAtRecord)
        {
            var outputStream = new MemoryStream();

            int rowsProcessed = ProcessStream(
                documentName,
                new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent)),
                outputStream,
                format,
                processingSetSize,
                startAtRecord
            );

            var output = new StreamReader(outputStream).ReadToEnd();
            outputStream.Dispose();

            return new CsvProcessorResult<string>(rowsProcessed, output);
        }

        private void ProcessRecordwise(CsvReader reader, StreamWriter outputWriter, System.Diagnostics.Stopwatch stopWatch)
        {
            var line = reader.ReadLine();

            stopWatch.Reset();
            stopWatch.Start();

            string message;
            if (!((ICsvRecordHandler)this.handler).ProcessRecord(line, out message))
            {
                //log or output message
                if (outputWriter != null)
                {
                    outputWriter.WriteLine(string.Format("{0}: {1}. {2} sec", reader.LinePosition, message, (double)stopWatch.ElapsedMilliseconds / 1000.0));
                }
            }

            //flush the output every 100 records
            if (reader.LinePosition % 100 == 0 && outputWriter != null)
            {
                outputWriter.Flush();
            }
        }

        private void ProcessSetWise(int setSize, CsvReader reader, StreamWriter outputWriter, System.Diagnostics.Stopwatch stopWatch)
        {
            stopWatch.Reset();
            stopWatch.Start();

            var line = reader.ReadLine();
            this.recordSet.Add(line);

            //when a set is completed or the stream is at its end, feed the collected records to the handler
            string[] messages;
            if (reader.LinePosition % setSize == 0 || (reader.EndOfStream && recordSet.Count > 0))
            {
                //feed recordset to handler
                if (!((ICsvSetHandler)this.handler).ProcessRecordSet(recordSet.ToArray(), out messages) && outputWriter != null)
                {
                    //output messages
                    foreach (string message in messages)
                    {
                        outputWriter.WriteLine(string.Format("{0}: {1}. {2} sec", reader.LinePosition, message, (double)stopWatch.ElapsedMilliseconds / 1000.0));
                    }
                    outputWriter.Flush();
                }
                //reset recordset
                this.recordSet.Clear();
            }
        }
    }
}
