using System;
using System.Collections.Generic;
using System.Text;

namespace NextGenCapture_Common.Model.Export
{
    [Serializable]
    public class Attachment
    {
        public string FileName { get; set; }
        public byte[] FileData { get; set; }
    }
}
