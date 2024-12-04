using Simmetric.IO.Csv.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Simmetric.IO.Csv
{
    /// <summary>
    /// Parses a CSV document and feeds rows to a supplied handler object.
    /// </summary>
    public sealed class AsyncCsvProcessor
    {
        private readonly IAsyncCsvHandler handler;
        private readonly IAsyncCsvRecordHandler? recordHandler;
        private readonly IAsyncCsvSetHandler? setHandler;
        private List<List<string?>> recordSet;

        /// <param name="handler">Instance of a class implementing ICsvHandler that performs a custom defined task using CSV record input</param>
        public AsyncCsvProcessor(IAsyncCsvHandler handler)
        {
            this.handler = handler;
            recordHandler = handler as IAsyncCsvRecordHandler;
            setHandler = handler as IAsyncCsvSetHandler;
            recordSet = new List<List<string?>>();
        }

        /// <summary>
        /// Processes a CSV formatted <see cref="T:System.IO.Stream"/>.
        /// </summary>
        /// <param name="documentName">A descriptive name for the processed document, for logging purposes.</param>
        /// <param name="inputStream">A CSV formatted stream containing data to be processed.</param>
        /// <param name="format">A <see cref="T:Simmetric.IO.Csv.CsvFormat"/> object representing the formatting used in the stream.</param>
        public async Task<int> ProcessStreamAsync(string documentName, Stream inputStream, CsvFormat format)
        {
            return await ProcessStreamAsync(documentName, inputStream, null, format, 0, 0);
        }

        /// <summary>
        /// Processes a CSV formatted <see cref="T:System.IO.Stream"/>.
        /// </summary>
        /// <param name="documentName">A descriptive name for the processed document, for logging purposes.</param>
        /// <param name="inputStream">A CSV formatted stream containing data to be processed.</param>
        /// <param name="format">A <see cref="T:Simmetric.IO.Csv.CsvFormat"/> object representing the formatting used in the stream.</param>
        /// <param name="processingSetSize">To process records in sets instead of individually, enter a set size larger than 1. The CsvHandler must implement <see cref="Simmetric.IO.Csv.ICsvSetHandler"/> to support this.</param>
        public async Task<int> ProcessStreamAsync(string documentName, Stream inputStream, CsvFormat format, int processingSetSize)
        {
            return await ProcessStreamAsync(documentName, inputStream, null, format, processingSetSize, 0);
        }

        /// <summary>
        /// Processes a CSV formatted <see cref="T:System.IO.Stream"/>.
        /// </summary>
        /// <param name="documentName">A descriptive name for the processed document, for logging purposes.</param>
        /// <param name="inputStream">A CSV formatted stream containing data to be processed.</param>
        /// <param name="outputMessageStream">Output messages will be written to this stream.</param>
        /// <param name="format">A <see cref="T:Simmetric.IO.Csv.CsvFormat"/> object representing the formatting used in the stream.</param>
        /// <param name="processingSetSize">To process records in sets instead of individually, enter a set size larger than 1. The CsvHandler must implement <see cref="Simmetric.IO.Csv.ICsvSetHandler"/> to support this.</param>
        /// <param name="startAtRecord">The parser will skip to the record number indicated.</param>
        /// <returns>The number of rows processed</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "Recordset"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "ICsvSetHandler")]
        public async Task<int> ProcessStreamAsync(string documentName, Stream inputStream, Stream? outputMessageStream, CsvFormat format, int processingSetSize, int startAtRecord)
        {
            if (inputStream == null)
            {
                throw new ArgumentNullException(nameof(inputStream));
            }
            if (format == null)
            {
                throw new ArgumentNullException(nameof(format));
            }
            if (processingSetSize > 0 && setHandler == null)
            {
                throw new InvalidOperationException(
                    "Recordset processing cannot be used on a CSV handler that does not implement ICsvSetHandler");
            }
            if (outputMessageStream != null && !outputMessageStream.CanWrite)
            {
                throw new ArgumentException("Stream is not ready for writing", nameof(outputMessageStream));
            }

            var reader = new AsyncCsvReader(inputStream, format);
            var fileStopWatch = new System.Diagnostics.Stopwatch();
            var recordStopWatch = new System.Diagnostics.Stopwatch();

            //ensure stream is set to position 0
            if (inputStream.Position > 0)
            {
                inputStream.Position = 0;
            }

            //headers are automatically extracted by CsvReader
            await handler.BeginProcessingAsync(documentName, format.Headers);

            //prepare output stream for writing
            StreamWriter? outputMessageWriter = outputMessageStream != null ? new StreamWriter(outputMessageStream) : null;
            fileStopWatch.Start();

            //skip to start record
            while (reader.LinePosition < startAtRecord)
            {
                await reader.ReadLineAsync();
            }

            //prepare recordset for set processing
            if (processingSetSize > 0)
            {
                recordSet = new List<List<string?>>(processingSetSize);
            }

            try
            {
                while (!reader.EndOfStream)
                {
                    try
                    {
                        if (processingSetSize < 1)
                        {
                            await ProcessRecordwiseAsync(reader, outputMessageWriter, recordStopWatch);
                        }
                        else
                        {
                            await ProcessSetWiseAsync(processingSetSize, reader, outputMessageWriter, recordStopWatch);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (outputMessageWriter != null)
                        {
                            //handled by CsvHandler
                            outputMessageWriter.WriteLine(string.Format("{0}: Error: {1}", reader.LinePosition, handler.HandleRecordErrorAsync(ex)));
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
                if (outputMessageWriter != null)
                {
                    //handled by CsvProcessor
                    outputMessageWriter.WriteLine(string.Format("{0}: Error: {1}", reader.LinePosition, ex.Message));
                }
                else
                {
                    throw;
                }
            }

            fileStopWatch.Stop();
            await handler.EndProcessingAsync();

            outputMessageWriter?.WriteLine(string.Format("Finished processing {0}, did {1} records in {2} seconds.", documentName, reader.LinePosition, fileStopWatch.ElapsedMilliseconds / 1000m));
            outputMessageWriter?.Flush();

            return reader.LinePosition;
        }

        /// <summary>
        /// Processes a CSV formatted file and logs output messages to a file in the same location with suffix '-output.txt'
        /// </summary>
        /// <param name="fileName">Full path to the file.</param>
        /// <param name="format">A <see cref="T:Simmetric.IO.Csv.CsvFormat"/> object representing the formatting used in the stream.</param>
        /// <returns>The number of rows processed</returns>
        public async Task<int> ProcessFile(string fileName, CsvFormat format)
        {
            return await ProcessFileAsync(fileName, format, 0, 0);
        }

        /// <summary>
        /// Processes a CSV formatted file and logs output messages to a file in the same location with suffix '-output.txt'
        /// </summary>
        /// <param name="fileName">Full path to the file.</param>
        /// <param name="format">A <see cref="T:Simmetric.IO.Csv.CsvFormat"/> object representing the formatting used in the stream.</param>
        /// <param name="processingSetSize">To process records in sets instead of individually, enter a set size larger than 1. The CsvHandler must implement <see cref="Simmetric.IO.Csv.ICsvSetHandler"/> to support this.</param>
        /// <returns>The number of rows processed</returns>
        public async Task<int> ProcessFileAsync(string fileName, CsvFormat format, int processingSetSize)
        {
            return await ProcessFileAsync(fileName, format, processingSetSize, 0);
        }

        /// <summary>
        /// Processes a CSV formatted file and logs output messages to a file in the same location with suffix '-output.txt'
        /// </summary>
        /// <param name="fileName">Full path to the file.</param>
        /// <param name="format">A <see cref="T:Simmetric.IO.Csv.CsvFormat"/> object representing the formatting used in the stream.</param>
        /// <param name="processingSetSize">To process records in sets instead of individually, enter a set size larger than 1. The CsvHandler must implement <see cref="Simmetric.IO.Csv.ICsvSetHandler"/> to support this.</param>
        /// <param name="startAtRecord">The parser will skip to the record number indicated.</param>
        /// <returns>The number of rows processed</returns>
        public async Task<int> ProcessFileAsync(string fileName, CsvFormat format, int processingSetSize, int startAtRecord)
        {
            var fileInput = File.Open(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            var fileOutput = File.Open(fileName + "-output.txt", FileMode.Create);

            int result = await ProcessStreamAsync(
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
        public async Task<CsvProcessorResult<string>> ProcessCsvAsync(string documentName, string csvContent, CsvFormat format)
        {
            return await ProcessCsvAsync(documentName, csvContent, format, 0, 0);
        }

        /// <summary>
        /// Processes a CSV formatted string.
        /// </summary>
        /// <param name="documentName">A descriptive name for the processed document, for logging purposes.</param>
        /// <param name="csvContent">A CSV formatted string</param>
        /// <param name="format">A <see cref="T:Simmetric.IO.Csv.CsvFormat"/> object representing the formatting used in the stream.</param>
        /// <param name="processingSetSize">To pera orocess records in sets instead of individually, enter a set size larger than 1. The CsvHandler must implement <see cref="Simmetric.IO.Csv.ICsvSetHandler"/> to support this.</param>
        /// <returns>An object containing the number of rows processed and the output messaging.</returns>
        public async Task<CsvProcessorResult<string>> ProcessCsvAsync(string documentName, string csvContent, CsvFormat format, int processingSetSize)
        {
            return await ProcessCsvAsync(documentName, csvContent, format, processingSetSize, 0);
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
        public async Task<CsvProcessorResult<string>> ProcessCsvAsync(string documentName, string csvContent, CsvFormat format, int processingSetSize, int startAtRecord)
        {
            var outputStream = new MemoryStream();

            int rowsProcessed = await ProcessStreamAsync(
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

        private async Task ProcessRecordwiseAsync(AsyncCsvReader reader, StreamWriter? outputMessageWriter, System.Diagnostics.Stopwatch stopWatch)
        {
            var line = await reader.ReadLineAsync();

            stopWatch.Reset();
            stopWatch.Start();

            string? message = null;
            if (recordHandler is null || !await recordHandler!.ProcessRecordAsync(reader.LinePosition, line, out message))
            {
                //if ProcessRecord returned false, log or output message
                outputMessageWriter?.WriteLine(string.Format("{0}: {1}. {2} sec", reader.LinePosition, message, stopWatch.ElapsedMilliseconds / 1000.0));
            }

            //flush the output every 100 records
            if (reader.LinePosition % 100 == 0)
            {
                outputMessageWriter?.Flush();
            }
        }

        private async Task ProcessSetWiseAsync(int setSize, AsyncCsvReader reader, StreamWriter? outputWriter, System.Diagnostics.Stopwatch stopWatch)
        {
            stopWatch.Reset();
            stopWatch.Start();

            recordSet.Add((await reader.ReadLineAsync()).ToList());

            //when a set is completed or the stream is at its end, feed the collected records to the handler
            IEnumerable<string>? messages = null;
            if (reader.LinePosition % setSize == 0 || (reader.EndOfStream && recordSet.Count > 0))
            {
                //feed recordset to handler
                if (!await setHandler!.ProcessRecordSetAsync(recordSet.ToArray(), out messages) && outputWriter != null && messages != null)
                {
                    //output messages
                    foreach (string message in messages)
                    {
                        outputWriter.WriteLine(string.Format("{0}: {1}. {2} sec", reader.LinePosition, message, (double)stopWatch.ElapsedMilliseconds / 1000.0));
                    }
                    outputWriter.Flush();
                }
                //reset recordset
                recordSet.Clear();
            }
        }
    }
}
