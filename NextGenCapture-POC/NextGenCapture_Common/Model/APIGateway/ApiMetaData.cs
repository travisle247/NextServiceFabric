using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace NextGenCapture_Common.Model.APIGateway
{
    [Serializable]
    public class ApiMetaData
    {
        [JsonProperty("CHANNEL_CODE")]
        public string ChannelCode { get; set; }
        [JsonProperty("MASTER_ACCOUNT")]
        public string MasterAccount { get; set; }
        [JsonProperty("PRIORITY")]
        public string Priority { get; set; }
        [JsonProperty("QUEUE")]
        public string Queue { get; set; }
        [JsonProperty("SUB_ACCOUNT")]
        public string SubAccount { get; set; }
        [JsonProperty("SUB_TOPIC")]
        public string SubTopic { get; set; }
        [JsonProperty("TOPIC")]
        public string Topic { get; set; }
        [JsonProperty("ENTRYCHANNEL")]
        public string EntryChannel { get; set; }
        [JsonProperty("TXTN_ID")]
        public string TransactionId { get; set; }
        [JsonProperty("FORCE_VALIDATION")]
        public string ForceValidation { get; set; }       
    }
}
