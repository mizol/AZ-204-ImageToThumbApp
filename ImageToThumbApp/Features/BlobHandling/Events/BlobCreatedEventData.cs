using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace ImageToThumbApp.Features.BlobHandling.Events
{
    // public class BlobCreatedEventData
    // {
    //     public string Api { get; set; }
    //     public string ClientRequestId { get; set; }
    //     public string RequestId { get; set; }
    //     public string ETag { get; set; }
    //     public string ContentType { get; set; }
    //     public long ContentLength { get; set; }
    //     public string BlobType { get; set; }
    //     public string Url { get; set; }
    //     public string Sequencer { get; set; }
    // }


    public class MyEventType
    {
        public string? Topic { get; set; }

        public string? Subject { get; set; }

        public string? EventType { get; set; }

        public DateTime EventTime { get; set; }

        public IDictionary<string, object>? Data { get; set; }
    }

    public class BlobCreatedEventData
    {
        [JsonPropertyName("api")]
        public string? Api { get; set; }

        [JsonPropertyName("clientRequestId")]
        public string? ClientRequestId { get; set; }

        [JsonPropertyName("requestId")]
        public string? RequestId { get; set; }

        [JsonPropertyName("etag")]
        public string? ETag { get; set; }

        [JsonPropertyName("contentType")]
        public string? ContentType { get; set; }

        [JsonPropertyName("contentLength")]
        public long ContentLength { get; set; }

        [JsonPropertyName("blobType")]
        public string? BlobType { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("sequencer")]
        public string? Sequencer { get; set; }
    }

}
