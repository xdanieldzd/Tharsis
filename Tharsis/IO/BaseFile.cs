using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace Tharsis.IO
{
    [DebuggerDisplay("{FilePath}")]
    public abstract class BaseFile
    {
        public string FilePath { get; private set; }

        public BaseFile(string path)
        {
            using (FileStream stream = new FileStream(this.FilePath = path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                if (stream.Length == 0) return;
                Parse(new BinaryReader(stream));
            }
        }

        protected virtual void Parse(BinaryReader reader)
        {
            throw new IOException(string.Format("Parse not implemented for {0}", this.GetType().FullName));
        }

        public virtual bool Save(string path)
        {
            throw new IOException(string.Format("Save not implemented for {0}", this.GetType().FullName));
        }
    }
}
