using System;

namespace Simmetric.IO.Csv.Test.Class
{
    public class GenericTestClass
    {
        public int myInt;
        public double myDouble;
        public string myString;
        public DateTime myDateTime;

        public int myIntProperty { get; set; }
        public int? myNullableIntProperty { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is GenericTestClass other)
            {
                return myInt == other.myInt &&
                    myDouble == other.myDouble &&
                    myString == other.myString &&
                    myDateTime == other.myDateTime &&
                    myIntProperty == other.myIntProperty &&
                    myNullableIntProperty == other.myNullableIntProperty;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
