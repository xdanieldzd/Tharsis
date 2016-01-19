using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tharsis.IO
{
    public class FileExtensionsAttribute : Attribute
    {
        public string ImportExtension;
        public string ExportExtension;

        public FileExtensionsAttribute(string importExt, string exportExt)
        {
            ImportExtension = importExt;
            ExportExtension = exportExt;
        }
    }
}
