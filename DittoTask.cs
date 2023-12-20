using System;
using AndroidX.ConstraintLayout.Core.Motion.Utils;
using DittoSDK;
using Newtonsoft.Json;

namespace DittoXamarinAndroidTasksApp
{
    public partial class DittoTask
    {
        public const string CollectionName = "tasks";

        [JsonProperty("_id")]
        public string Id { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }

        [JsonProperty("isCompleted")]
        public bool IsCompleted { get; set;}
    }
}

