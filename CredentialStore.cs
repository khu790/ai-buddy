using System.Runtime.InteropServices;
using System.Text;

namespace AiBuddy;

public static class CredentialStore
{
    private const string Target = "AI Buddy/OpenAI";
    private const uint Generic = 1;
    private const uint PersistLocalMachine = 2;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NativeCredential
    {
        public uint Flags;
        public uint Type;
        public string TargetName;
        public string? Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr Attributes;
        public string? TargetAlias;
        public string? UserName;
    }

    [DllImport("Advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredWrite(ref NativeCredential credential, uint flags);

    [DllImport("Advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredRead(string target, uint type, uint flags, out IntPtr credential);

    [DllImport("Advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CredDelete(string target, uint type, uint flags);

    [DllImport("Advapi32.dll")]
    private static extern void CredFree(IntPtr buffer);

    public static string? Read()
    {
        if (!CredRead(Target, Generic, 0, out var buffer)) return null;
        try
        {
            var credential = Marshal.PtrToStructure<NativeCredential>(buffer);
            if (credential.CredentialBlob == IntPtr.Zero || credential.CredentialBlobSize == 0) return null;
            if (credential.CredentialBlobSize > int.MaxValue) return null;
            var data = new byte[checked((int)credential.CredentialBlobSize)];
            try
            {
                Marshal.Copy(credential.CredentialBlob, data, 0, data.Length);
                return Encoding.Unicode.GetString(data).TrimEnd('\0');
            }
            finally { Array.Clear(data); }
        }
        finally { CredFree(buffer); }
    }

    public static void Write(string secret)
    {
        var data = Encoding.Unicode.GetBytes(secret + "\0");
        var blob = Marshal.AllocHGlobal(data.Length);
        try
        {
            Marshal.Copy(data, 0, blob, data.Length);
            var credential = new NativeCredential
            {
                Type = Generic,
                TargetName = Target,
                UserName = "OpenAI API key",
                CredentialBlobSize = (uint)data.Length,
                CredentialBlob = blob,
                Persist = PersistLocalMachine
            };
            if (!CredWrite(ref credential, 0))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "Windows Credential Manager could not save the API key.");
        }
        finally
        {
            Array.Clear(data);
            Marshal.FreeHGlobal(blob);
        }
    }

    public static void Delete() => CredDelete(Target, Generic, 0);
}
