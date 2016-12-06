using System;
using System.Collections.Generic;
using System.Linq;

namespace TestConsole
{

    public class MyRecordHandler : Simmetric.IO.Csv.ICsvRecordHandler
    {
        int rowNumber;
        System.Data.SqlClient.SqlConnection con;
        System.Data.SqlClient.SqlTransaction trn;

        //Called for each record in the CSV file
        public bool ProcessRecord(int recordNum, IEnumerable<string> fields, out string message)
        {
            //insert the data into a database table
            var com = new System.Data.SqlClient.SqlCommand("INSERT INTO table (id, name, address, city, dateofbirth) VALUES (@id, @name, @address, @city, @dateofbirth", con);
            com.Parameters.AddWithValue("@id", int.Parse(fields.ElementAt(0)));
            com.Parameters.AddWithValue("@name", fields.ElementAt(1));
            com.Parameters.AddWithValue("@address", fields.ElementAt(2));
            com.Parameters.AddWithValue("@city", fields.ElementAt(3));
            com.Parameters.AddWithValue("@dateofbirth", DateTime.Parse(fields.ElementAt(4)));
            com.ExecuteNonQuery();

            rowNumber++;
            message = null;
            return true;
        }

        //Called when opening a CSV file
        public void BeginProcessing(string fileName, IEnumerable<string> headers = null)
        {
            //initialize DB connection
            con = new System.Data.SqlClient.SqlConnection("your connectionstring here");
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
}
