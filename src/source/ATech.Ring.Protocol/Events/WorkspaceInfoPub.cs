using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ATech.Ring.Protocol.Events
{
    public class WorkspaceInfoPub : IRingEvent
    {
        public virtual M Type => M.WORKSPACE_INFO_PUBLISH;
        public WorkspaceInfo WorkspaceInfoJson { get; set; }
        public Message AsMessage() => Message.FromString(Type, JsonConvert.SerializeObject(WorkspaceInfoJson, Formatting.None, new StringEnumConverter()));
    }
}