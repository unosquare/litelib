using System;
using System.Collections.Generic;
using System.Text;

namespace Unosquare.Labs.LiteLib.Tests.Helpers
{
    [System.AttributeUsage(System.AttributeTargets.Class |
               System.AttributeTargets.Struct)
]
    class CustomAttribute : System.Attribute
    {
        private string name;
        public double version;

        public CustomAttribute(string name)
        {
            this.name = name;
            version = 1.0;
        }
    }
}
