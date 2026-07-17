namespace lstwoMODS.ImGui.Shared.Messages
{
    public class GameShutDownMessage
    {
        // Signal-only message: the type name is the whole payload, so the body stays empty.
        public IpcMessage Serialize() => new IpcMessage
        {
            Type    = nameof(GameShutDownMessage),
            Payload = ""
        };

        public static GameShutDownMessage Deserialize(IpcMessage message) => new GameShutDownMessage();
    }
}