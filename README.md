# Simmetric.IO.Csv
.NET library for (asynchronously) handling CSV files in various formats, including TSV, .

## Why a CSV library?
I'm sure we've all written a `while (streamReader.ReadLine()) { }` loop a million times thinking 'CSV is easy!' and then it always turned out to be more complicated than expected, often due to text delimiters.
Different separator characters, date and number formats can be a real pain, not to mention when a CSV file contains quoted text strings that may or may not contain separator characters. In that case you need more than a ReadLine() loop.

Simmetric.IO.Csv offers basic classes to read and write CSV, but also a fully functional scaffold that minimizes the code you have to write. You only have to write a class that implements ICsvRecordHandler (or ICsvSetHandler to handle more than 1 record at once) and fill in the gaps.

## Usage
### Format
First, it's important to know what format your CSV file is in. Otherwise all bets are off. Declare a CsvFormat object to specifiy row and column delimiters, and other options.
```csharp
var format = new CsvFormat { 
   ColumnSeparator = ";",  //how are columns separated?
   LineSeparator = "\r\n", //how are lines separated? a string is allowed as input, but each character in the string separates a line on its own. this is so you can enter \r\n here
   HasHeaders = false,     //does the file have a header row?
   TextQualification       //are columns wrapped in quotes?
       = CsvFormat.TextQualificationOption.OnlyWhenNecessary, //only if a cell contains separator characters. choose this if there is no text qualification in your CSV file
   TextQualifier = '"' 
};
```

### Handlers
A handler class takes lines of CSV and processes them. Your handler class will do the actual work such as import the CSV into a database, or convert it to a different file format.

To write a handler, implement Simmetric.IO.Csv.IAsyncCsvRecordHandler or Simmetric.IO.Csv.IAsyncCsvSetHandler.
IAsyncCsvRecordHandler takes one record at a time, while IAsyncCsvSetHandler can take sets of a configurable number of records.

```csharp
public class MyRecordHandler : Simmetric.IO.Csv.IAsyncCsvRecordHandler
{
    int rowNumber;
    System.Data.SqlClient.SqlConnection con;
    System.Data.SqlClient.SqlTransaction trn;

    //Called for each record in the CSV file
    public async Task<bool> ProcessRecordAsync(int recordNum, string[] fields, out string message)
    {
        //insert the data into a database table
        var com = new System.Data.SqlClient.SqlCommand("INSERT INTO table (id, name, address, city, dateofbirth) VALUES (@id, @name, @address, @city, @dateofbirth", con);
        com.Parameters.AddWithValue("@id", int.Parse(fields[0]));
        com.Parameters.AddWithValue("@name", fields[1]);
        com.Parameters.AddWithValue("@address", fields[2]);
        com.Parameters.AddWithValue("@city", fields[3]);
        com.Parameters.AddWithValue("@dateofbirth", DateTime.Parse(fields[4]));
        await com.ExecuteNonQueryAsync();

        rowNumber++;
    }

    //Called when opening a CSV file
    public async Task BeginProcessingAsync(string fileName, string[] headers = null)
    {
        //initialize DB connection
        con = new System.Data.SqlClient.SqlConnection("your connectionstring here");
        con.Open();
        trn = await con.BeginTransactionAsync();
        rowNumber = 0;
    }

    //Called after the last record in the CSV file is processed
    public async Task EndProcessingAsync()
    {
        //commit transaction and close DB connection
        await trn.CommitAsync();
        await con.CloseAsync();
        await con.DisposeAsync();
    }

    //Called when an unhandled exception occurs. Choose whether further processing should happen.
    public async Task<string> HandleRecordErrorAsync(Exception ex)
    {
        //to halt further record processing, throw an exception.
        //otherwise return a sensible message that describes the error.
        await trn.RollbackAsync();
        await con.DisposeAsync();
        throw new System.Exception("Processing stopped because: " + ex.Message);
    }
}
```

### Processing
Use the AsyncCsvProcessor to read a file, stream or string containing CSV formatted text. The processor reads the CSV and feeds each line to a handler class.

```csharp
var myHandler = new MyRecordHandler();
var processor = new AsyncCsvProcessor(myHandler);
await processor.ProcessFileAsync("C:\\My Documents\\myfile.csv", format);
```

### Reading
To simply read a CSV file row by row, use the CsvReader:
```csharp
using var reader = new AsyncCsvReader(fileStream, format);
while (!reader.EndOfStream)
{
   //read the CSV line by line
   IEnumerable<string> line = reader.ReadLine();
   
   //or read each cell individually
   int id = await reader.ReadAsIntAsync();
   string name = await reader.ReadAsync();
   string address = await reader.ReadAsync();
   string city = await reader.ReadAsync();
   DateTime dateofbirth = await reader.ReadAsDateTimeAsync();
   
   //read a line and return a populated object
   //note: the CSV must have headers that correspond to field names in a class
   class Person 
   {
      public int ID { get; set; }
      public string Name { get; set; }
      public string Address { get; set; }
      public string City { get; set; }
      public DateTime DateOfBirth { get; set; }
   }
   Person person = await reader.ReadLineAsync<Person>();

   //note: each Read call advances the position of the AsyncCsvReader
}
```

It is also possible to read a CSV file to the end and return all rows as an iterator:
```csharp
using var reader = new AsyncCsvReader(fileStream, format);
await foreach (IEnumerable<string> line in reader.ReadToEndAsync())
{
   //line contains all fields as strings, just like ReadLineAsync()
}

class Person 
{
   public int ID { get; set; }
   public string Name { get; set; }
   public string Address { get; set; }
   public string City { get; set; }
   public DateTime DateOfBirth { get; set; }
}
await foreach (Person person in reader.ReadToEndAsync<Person>())
{
   //person is an instance of class Person
}
```

### Writing
With the CsvWriter class you can output CSV formatted text to a stream.
```csharp
using (var writer = new AsyncCsvWriter(outputStream, format))
{
 //write a string array as a line
 await writer.WriteLineAsync(new string[]{"1", "Mike O'Toole", "1234 West Street\r\n12345 Springfield NY", "Springfield", "1980-01-01"});
 //or write cells individually
 await writer.WriteAsync("1");
 await writer.WriteColumnSeparatorAsync();
 await writer.WriteLineSeparatorAsync();
}
```
