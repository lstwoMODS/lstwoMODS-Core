namespace lstwoMODS.ImGui.Shared.Messages
{
    public class LoadIniSettingsMessage
    {
        public string WindowId   { get; set; }
        public string IniContent { get; set; }

        public IpcMessage Serialize() => IpcSerializer.Wrap(this);

        public static LoadIniSettingsMessage Deserialize(IpcMessage msg)
            => IpcSerializer.Unwrap<LoadIniSettingsMessage>(msg);
    }
}
