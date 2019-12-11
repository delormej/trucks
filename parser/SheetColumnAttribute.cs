using System;

namespace Trucks
{
    [System.AttributeUsage(System.AttributeTargets.Property)]
    public class SheetColumnAttribute : System.Attribute
    {
        public string Header;
        public SheetColumnAttribute(string header)
        {
            this.Header = header;
        }
    }
}