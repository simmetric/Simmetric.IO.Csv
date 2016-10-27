# Simmetric.IO.Csv
.NET library for handling CSV files in all formats.

## Why a CSV library?
I'm sure we've all written a `while (streamReader.ReadLine()) { }` loop a million times thinking 'CSV is easy!' and then it always turned out to be more complicated than expected.
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

To write a handler, implement Simmetric.IO.Csv.ICsvRecordHandler or Simmetric.IO.Csv.ICsvSetHandler.
ICsvRecordHandler takes one record at a time, while ICsvSetHandler can take sets of a configurable number of records.

```csharp
public class MyRecordHandler : Simmetric.IO.Csv.ICsvRecordHandler
{
    int rowNumber;
    System.Data.SqlClient.Connection con;
    System.Data.SqlClient.Transaction trn;

    //Called for each record in the CSV file
    public bool ProcessRecord(string[] fields, out string message)
    {
        //insert the data into a database table
        var com = new System.Data.SqlClient.Command("INSERT INTO table (id, name, address, city, dateofbirth) VALUES (@id, @name, @address, @city, @dateofbirth", trn);
        com.Parameters.AddWithValue("@id", int.Parse(fields[0]));
        com.Parameters.AddWithValue("@name", fields[1]);
        com.Parameters.AddWithValue("@address", fields[2]);
        com.Parameters.AddWithValue("@city", fields[3]);
        com.Parameters.AddWithValue("@dateofbirth", DateTime.Parse(fields[4]));
        com.ExecuteNonQuery();

        rowNumber++;
    }

    //Called when opening a CSV file
    public void BeginProcessing(string fileName, string[] headers = null)
    {
        //initialize DB connection
        con = new System.Data.SqlClient.Connection("your connectionstring here");
        con.Open();
        trn = con.BeginTransaction();
        rowNumber = 0;
    }

    //Called after the last record in the CSV file is processed
    public void EndProcessing()
    {
        //commit transaction and close DB connection
        trn.Commit();
        con.Close();
        con.Dispose();
    }

    //Called when an unhandled exception occurs. Choose whether further processing should happen.
    public string HandleRecordError(Exception ex)
    {
        //to halt further record processing, throw an exception.
        //otherwise return a sensible message that describes the error.
        trn.Rollback();
        con.Dispose();
        throw new System.Exception("Processing stopped because: " + ex.Message);
    }
}
```

### Processing
Use the CsvProcessor to read a file, stream or string containing CSV formatted text. The processor reads the CSV and feeds each line to a handler class.

```csharp
var myHandler = new MyRecordHandler();
var processor = new CsvProcessor(myHandler);
processor.ProcessFile("C:\\My Documents\\myfile.csv", format);
```

### Reading
To simply read a CSV file row by row, use the CsvReader:
```csharp
var reader = new CsvReader(fileStream, format);
while (!reader.EndOfStream)
{
   //read the CSV line by line
   var line = reader.ReadLine();
   //or read each cell individually
   var id = reader.ReadAsInt();
   var name = reader.Read();
   var address = reader.Read();
   var city = reader.Read();
   var dateofbirth = reader.ReadAsDateTime();

   //note: each Read call advances the position of the CsvReader
}
```

### Writing
With the CsvWriter class you can output CSV formatted text to a stream.
```csharp
using (var writer = new CsvWriter(outputStream, format))
{
 //write a string array as a line
 writer.WriteLine(new string[]{"1", "Mike O'Toole", "1234 West Street\r\n12345 Springfield NY", "Springfield", "1980-01-01"});
 //or write cells individually
 writer.Write("1");
 writer.WriteColumnSeparator();
 writer.WriteLineSeparator();
}
```
