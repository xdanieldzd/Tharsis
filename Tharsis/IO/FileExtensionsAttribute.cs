using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tharsis.IO
{
    public class FileExtensionsAttribute : Attribute
    {
        public string SourceExtension;
        public string TargetExtension;

        public FileExtensionsAttribute(string sourceExt, string targetExt)
        {
            SourceExtension = sourceExt;
            TargetExtension = targetExt;
        }
    }
}
