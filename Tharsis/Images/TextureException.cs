using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tharsis.Images
{
    public class TextureException : Exception
    {
        public TextureException() : base() { }
        public TextureException(string message) : base(message) { }
        public TextureException(string message, Exception innerException) : base(message, innerException) { }
        public TextureException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
