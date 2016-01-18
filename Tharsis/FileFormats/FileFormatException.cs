using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tharsis.FileFormats
{
    public class FileFormatException : Exception
    {
        public FileFormatException() : base() { }
        public FileFormatException(string message) : base(message) { }
        public FileFormatException(string message, Exception innerException) : base(message, innerException) { }
        public FileFormatException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
