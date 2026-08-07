namespace lstwoMODS.ImGui.Shared.UI
{
    public class GizmoData : BaseUIElementData
    {
        // Camera matrices as flat float[16] column-major arrays
        public float[] ViewMatrix       { get; set; } = new float[16];
        public float[] ProjectionMatrix { get; set; } = new float[16];
        // Object transform matrix (input from mod, returned modified by overlay)
        public float[] ModelMatrix      { get; set; } = new float[16];
        public ImGuizmoOperation Operation { get; set; } = ImGuizmoOperation.Translate;
        public ImGuizmoMode      Mode      { get; set; } = ImGuizmoMode.World;
        // Optional snapping (null = no snap)
        public float? SnapValue { get; set; } = null;
        // Set by overlay when gizmo was used this frame
        public bool Changed { get; set; } = false;
    }
}
