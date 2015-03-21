using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace Tharsis
{
    [DebuggerDisplay("{FilePath}")]
    public abstract class BaseFile
    {
        public string FilePath { get; private set; }

        public BaseFile(string path)
        {
            using (MemoryStream stream = new MemoryStream(File.ReadAllBytes(this.FilePath = path)))
            {
                if (stream.Length == 0) return;
                Parse(new BinaryReader(stream));
            }
        }

        protected virtual void Parse(BinaryReader reader)
        {
            throw new Exception(string.Format("Parse not implemented for {0}", this.GetType().FullName));
        }

        public virtual bool Save(string path)
        {
            throw new Exception(string.Format("Save not implemented for {0}", this.GetType().FullName));
        }
    }
}
