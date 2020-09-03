using System;

namespace Simmetric.IO.Csv.Test.Class
{
    public class GenericTestClass
    {
        public int myInt;
        public double myDouble;
        public string myString;
        public DateTime myDateTime;

        public int MyIntProperty { get; set; }
        public int? MyNullableIntProperty { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is GenericTestClass other)
            {
                return myInt == other.myInt &&
                    myDouble == other.myDouble &&
                    myString == other.myString &&
                    myDateTime == other.myDateTime &&
                    MyIntProperty == other.MyIntProperty &&
                    MyNullableIntProperty == other.MyNullableIntProperty;
            }

            return false;
        }

        public override int GetHashCode() => base.GetHashCode();
    }
}
