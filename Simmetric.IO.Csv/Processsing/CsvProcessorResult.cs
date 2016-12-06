namespace Simmetric.IO.Csv
{
    /// <summary>
    /// Contains information on the result of a processed CSV document
    /// </summary>
    /// <typeparam name="T">The type of the output container</typeparam>
    public class CsvProcessorResult<T>
    {
        /// <summary>
        /// The number of rows processed (successfully and otherwise)
        /// </summary>
        public int RowsProcessed { get; protected set; }
        /// <summary>
        /// The output messages from the process
        /// </summary>
        public T Output { get; protected set; }

        internal CsvProcessorResult(int rowsProcessed, T output)
        {
            this.RowsProcessed = rowsProcessed;
            this.Output = output;
        }
    }
}
