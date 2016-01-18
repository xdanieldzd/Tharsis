using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tharsis.Images
{
    public class ImageException : Exception
    {
        public ImageException() : base() { }
        public ImageException(string message) : base(message) { }
        public ImageException(string message, Exception innerException) : base(message, innerException) { }
        public ImageException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
