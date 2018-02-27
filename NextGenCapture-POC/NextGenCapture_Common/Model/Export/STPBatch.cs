using NextGenCapture_Common.Model.APIGateway;
using System;
using System.Collections.Generic;
using System.Text;

namespace NextGenCapture_Common.Model.Export
{
    [Serializable]
    public class STPBatch
    {
        public string Destination { get; set; }
        public DateTime ReceivedDateTime { get; set; }
        public string FolderName { get; set; }
        public ApiMetaData BatchMetaData { get; set; }
        public string Status { get; set; }
        public List<Attachment> Attachments { get; set; }
    }
}
