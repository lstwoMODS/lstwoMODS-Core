using System.Runtime.InteropServices;

namespace lstwoMODS_Overlay;

/// <summary>One entry in a file dialog's type dropdown.</summary>
public class FileFilter
{
    public string Label   { get; set; } = "All files";
    public string Pattern { get; set; } = "*.*";

    public FileFilter() { }

    public FileFilter(string label, string pattern)
    {
        Label   = label;
        Pattern = pattern;
    }
}

public class FileDialogOptions
{
    public string Title { get; set; } = "Open";

    /// <summary>Type dropdown entries, in order. The first one is pre-selected.</summary>
    public FileFilter[] Filters { get; set; } = { new FileFilter() };

    public string? InitialDirectory { get; set; }

    /// <summary>Pre-filled file name. For a save dialog this is the suggested name.</summary>
    public string? FileName { get; set; }

    /// <summary>Appended by the save dialog when the user types a name without an extension.</summary>
    public string? DefaultExtension { get; set; }

    /// <summary>
    /// Window the dialog belongs to. An owned dialog is always above its owner in the z-order,
    /// which is the only reliable way to keep it in front of an overlay window that re-asserts
    /// topmost every frame. Leave zero and the dialog can open behind the overlay.
    /// </summary>
    public IntPtr OwnerHandle { get; set; } = IntPtr.Zero;
}

/// <summary>
/// The Win32 common file dialogs, for plugins that need the user to point at a file on disk.
///
/// Both entry points return immediately and run the dialog on a background STA thread: the dialog
/// pumps its own modal message loop, and running that on the render thread would freeze the
/// overlay for as long as the dialog is open. The callback therefore runs on that background
/// thread — marshal to wherever you need it yourself.
/// </summary>
public static class FileDialog
{
    /// <summary>Asks for an existing file. The callback gets null when the user cancels.</summary>
    public static void ShowOpen(FileDialogOptions options, Action<string?> onResult)
        => Run(options, onResult, save: false);

    /// <summary>Asks for a file name to write to. The callback gets null when the user cancels.</summary>
    public static void ShowSave(FileDialogOptions options, Action<string?> onResult)
        => Run(options, onResult, save: true);

    private static void Run(FileDialogOptions options, Action<string?> onResult, bool save)
    {
        var thread = new Thread(() =>
        {
            string? picked = null;
            try
            {
                picked = ShowBlocking(options, save);
            }
            catch (Exception ex)
            {
                Logger.LogError($"File dialog failed: {ex.Message}");
            }

            try
            {
                onResult(picked);
            }
            catch (Exception ex)
            {
                Logger.LogError($"File dialog callback threw: {ex}");
            }
        });

        // The Explorer-style dialog is a shell control and needs a single-threaded apartment.
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Name         = "FileDialog";
        thread.Start();
    }

    private static unsafe string? ShowBlocking(FileDialogOptions options, bool save)
    {
        // The dialog writes the full path back into this buffer, so it cannot be a marshalled
        // string field like the rest of them.
        var buffer = Marshal.AllocHGlobal(MaxPathChars * sizeof(char));

        try
        {
            var initialName = options.FileName ?? "";
            if (initialName.Length > MaxPathChars - 1) initialName = "";

            fixed (char* name = initialName)
            {
                Buffer.MemoryCopy(name, (void*)buffer, MaxPathChars * sizeof(char),
                    initialName.Length * sizeof(char));
            }
            ((char*)buffer)[initialName.Length] = '\0';

            var ofn = new OpenFileName
            {
                lStructSize     = Marshal.SizeOf(typeof(OpenFileName)),
                hwndOwner       = options.OwnerHandle,
                lpstrFilter     = BuildFilter(options.Filters),
                nFilterIndex    = 1,
                lpstrFile       = buffer,
                nMaxFile        = MaxPathChars,
                lpstrInitialDir = options.InitialDirectory,
                lpstrTitle      = options.Title,
                lpstrDefExt     = options.DefaultExtension?.TrimStart('.'),
                Flags           = OFN_EXPLORER | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR | OFN_HIDEREADONLY
                                  | (save ? OFN_OVERWRITEPROMPT : OFN_FILEMUSTEXIST),
            };

            var ok = save ? GetSaveFileNameW(ref ofn) : GetOpenFileNameW(ref ofn);
            if (!ok)
            {
                // 0 is a plain cancel; anything else is a real failure worth a line in the log.
                var error = CommDlgExtendedError();
                if (error != 0) Logger.LogError($"File dialog error 0x{error:x}");
                return null;
            }

            var path = Marshal.PtrToStringUni(buffer);
            return string.IsNullOrEmpty(path) ? null : path;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    /// <summary>Filters are one flat string of NUL-separated label/pattern pairs, terminated by an
    /// empty entry — the one part of this API that has not changed since Windows 3.1.</summary>
    private static string BuildFilter(FileFilter[]? filters)
    {
        if (filters == null || filters.Length == 0)
            filters = new[] { new FileFilter() };

        var text = "";
        foreach (var filter in filters)
            text += filter.Label + '\0' + filter.Pattern + '\0';

        return text + '\0';
    }

    // MAX_PATH is 260, but the dialog happily returns longer paths on a system with long paths
    // enabled, and truncating one silently would hand back a path that does not exist.
    private const int MaxPathChars = 4096;

    private const int OFN_HIDEREADONLY    = 0x00000004;
    private const int OFN_NOCHANGEDIR     = 0x00000008;
    private const int OFN_OVERWRITEPROMPT = 0x00000002;
    private const int OFN_PATHMUSTEXIST   = 0x00000800;
    private const int OFN_FILEMUSTEXIST   = 0x00001000;
    private const int OFN_EXPLORER        = 0x00080000;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct OpenFileName
    {
        public int     lStructSize;
        public IntPtr  hwndOwner;
        public IntPtr  hInstance;
        public string? lpstrFilter;
        public string? lpstrCustomFilter;
        public int     nMaxCustFilter;
        public int     nFilterIndex;
        public IntPtr  lpstrFile;
        public int     nMaxFile;
        public string? lpstrFileTitle;
        public int     nMaxFileTitle;
        public string? lpstrInitialDir;
        public string? lpstrTitle;
        public int     Flags;
        public short   nFileOffset;
        public short   nFileExtension;
        public string? lpstrDefExt;
        public IntPtr  lCustData;
        public IntPtr  lpfnHook;
        public string? lpTemplateName;
        public IntPtr  pvReserved;
        public int     dwReserved;
        public int     FlagsEx;
    }

    [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool GetOpenFileNameW(ref OpenFileName ofn);

    [DllImport("comdlg32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool GetSaveFileNameW(ref OpenFileName ofn);

    [DllImport("comdlg32.dll")]
    private static extern int CommDlgExtendedError();
}
