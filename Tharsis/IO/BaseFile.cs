using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace Tharsis.IO
{
    public enum ParseModes { ImportFormat, ExportFormat }

    [DebuggerDisplay("{FilePath}")]
    public abstract class BaseFile
    {
        public string FilePath { get; private set; }
        public ParseModes ParseMode { get; private set; }

        public BaseFile(string path, ParseModes mode)
        {
            using (FileStream sourceStream = new FileStream(this.FilePath = path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                if (sourceStream.Length == 0) return;
                switch (this.ParseMode = mode)
                {
                    case ParseModes.ImportFormat: Import(sourceStream); break;
                    case ParseModes.ExportFormat: Export(sourceStream); break;
                }
            }
        }

        protected virtual void Import(Stream sourceStream)
        {
            throw new IOException(string.Format("Import not implemented for {0}", this.GetType().FullName));
        }

        protected virtual void Export(Stream sourceStream)
        {
            throw new IOException(string.Format("Export not implemented for {0}", this.GetType().FullName));
        }

        public virtual bool Save(string path)
        {
            throw new IOException(string.Format("Save not implemented for {0}", this.GetType().FullName));
        }
    }
}
