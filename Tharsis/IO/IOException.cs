using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tharsis.IO
{
    public class IOException : Exception
    {
        public IOException() : base() { }
        public IOException(string message) : base(message) { }
        public IOException(string message, Exception innerException) : base(message, innerException) { }
        public IOException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
