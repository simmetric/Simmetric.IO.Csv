using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simmetric.IO.Csv.Test.Handlers
{
    public class TestHandler : ICsvRecordHandler
    {
        int expectedFields;
        public string[] LastRecord { get; private set; }
        public void SetExpectations(int fields)
        {
            this.expectedFields = fields;
        }

        public bool ProcessRecord(int recordNum, string[] fields, out string message)
        {
            LastRecord = fields;
            message = null;
            if (fields.Count() == this.expectedFields)
            {
                return true;
            }
            else
            {
                message = string.Format("Unexpected field count at record {0}", recordNum);
                return false;
            }
        }

        public void BeginProcessing(string fileName, string[] headers = null)
        {
        }

        public void EndProcessing()
        {
        }

        public string HandleRecordError(Exception ex)
        {
            return ex.Message;
        }
    }
}
