using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Tharsis
{
    interface IProcessableFile
    {
        void Parse(BinaryReader reader);
        void Dump(string path);
    }
}
