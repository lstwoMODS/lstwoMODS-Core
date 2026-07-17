namespace lstwoMODS.ImGui.Shared
{
    // All values verified against Hexa.NET.ImGuizmo 2.2.9 via reflection.

    public enum ImGuizmoMode
    {
        Local = 0,
        World = 1,
    }

    [System.Flags]
    public enum ImGuizmoOperation
    {
        TranslateX   = 1,
        TranslateY   = 2,
        TranslateZ   = 4,
        Translate    = 7,
        RotateX      = 8,
        RotateY      = 16,
        RotateZ      = 32,
        RotateScreen = 64,
        Rotate       = 120,
        ScaleX       = 128,
        ScaleY       = 256,
        ScaleZ       = 512,
        Scale        = 896,
        Bounds       = 1024,
        ScaleXu      = 2048,
        ScaleYu      = 4096,
        ScaleZu      = 8192,
        Scaleu       = 14336,
        Universal    = 14463,
    }
}
