using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace DaruDaru.Utilities
{
    internal static class Explorer
    {
        public static void OpenUri(string uriString)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = uriString, UseShellExecute = true }).Dispose();
            }
            catch
            {
            }
        }

        public static void Open(string path)
        {
            try
            {
                Process.Start("explorer", $"\"{path}\"").Dispose();
            }
            catch
            {
            }
        }

        public static int GetDirectoryCount(IEnumerable<string> paths)
            => paths.ToPathAndName().Select(e => e.Key).Distinct().Count();

        public static void OpenAndSelect(string path)
        {
            using (var pidl = new PidlHelper(path))
                pidl.Open(null);
        }

        public static void OpenAndSelect(params string[] paths)
        {
            OpenAndSelect((IEnumerable<string>)paths);
        }

        public static void OpenAndSelect(IEnumerable<string> paths)
        {
            foreach (var path in paths.ToPathAndName().GroupBy(e => e.Key))
                OpenAndSelect(path.Key, path.Select(e => e.Value));
        }

        public static void OpenAndSelect(string parentDirectory, IEnumerable<string> names)
        {
            using (var pidl = new PidlHelper(parentDirectory))
                pidl.Open(names);
        }

        private static IEnumerable<KeyValuePair<string, string>> ToPathAndName(this IEnumerable<string> paths)
        {
            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                {
                    var info = new DirectoryInfo(path);
                    yield return new KeyValuePair<string, string>(info.Parent.FullName, info.Name);
                }

                else if (File.Exists(path))
                {
                    var info = new FileInfo(path);
                    yield return new KeyValuePair<string, string>(info.Directory.FullName, info.Name);
                }
            }
        }

        private sealed class PidlHelper : IDisposable
        {
            public PidlHelper(string path)
            {
                Marshal.ThrowExceptionForHR(
                    NativeMethods.SHGetDesktopFolder(
                        out IShellFolder shellFolder));

                uint attr = 0;
                shellFolder.ParseDisplayName(
                    IntPtr.Zero,
                    null,
                    path,
                    out uint pchEaten,
                    out IntPtr ptr,
                    ref attr);

                this.DirOrFile = path;
                this.ShellFolder = shellFolder;
                this.IntPtr = ptr;
            }
            public PidlHelper(PidlHelper parent, string path)
            {
                var IID_IShellFolder = typeof(IShellFolder).GUID;

                Marshal.ThrowExceptionForHR(
                    parent.ShellFolder.BindToObject(
                        parent.IntPtr,
                        null,
                        ref IID_IShellFolder,
                        out IShellFolder shellFolder));

                uint attr = 0;
                shellFolder.ParseDisplayName(
                    IntPtr.Zero,
                    null,
                    path,
                    out uint pchEaten,
                    out IntPtr ptr,
                    ref attr);

                this.DirOrFile = path;
                this.ShellFolder = shellFolder;
                this.IntPtr = ptr;
            }
            ~PidlHelper()
            {
                this.Dispose(false);
            }

            private string DirOrFile { get; }
            private IShellFolder ShellFolder { get; }
            private IntPtr IntPtr { get; }

            public void Open(IEnumerable<string> fileNames, bool edit = false)
            {
                var subItems = new List<PidlHelper>();

                if (fileNames != null)
                    subItems.AddRange(fileNames.Select(e => new PidlHelper(this, e)));

                try
                {
                    var arr = subItems.Count > 0 ? subItems.Select(e => e.IntPtr).ToArray() : null;

                    var result = NativeMethods.SHOpenFolderAndSelectItems(
                        this.IntPtr,
                        (uint)subItems.Count,
                        arr,
                        edit ? 1 : 0);

                    Marshal.ThrowExceptionForHR(result);
                }
                finally
                {
                    subItems.ForEach(e => e.Dispose());
                }

            }

            private bool m_disposed = false;
            public void Dispose()
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                if (this.m_disposed)
                    return;
                this.m_disposed = true;

                if (disposing)
                    NativeMethods.ILFree(this.IntPtr);
            }

            [Flags]
            private enum SHCONT : ushort
            {
                SHCONTF_CHECKING_FOR_CHILDREN = 0x0010,
                SHCONTF_FOLDERS = 0x0020,
                SHCONTF_NONFOLDERS = 0x0040,
                SHCONTF_INCLUDEHIDDEN = 0x0080,
                SHCONTF_INIT_ON_FIRST_NEXT = 0x0100,
                SHCONTF_NETPRINTERSRCH = 0x0200,
                SHCONTF_SHAREABLE = 0x0400,
                SHCONTF_STORAGE = 0x0800,
                SHCONTF_NAVIGATION_ENUM = 0x1000,
                SHCONTF_FASTITEMS = 0x2000,
                SHCONTF_FLATLIST = 0x4000,
                SHCONTF_ENABLE_ASYNC = 0x8000
            }

            [ComImport]
            [Guid("000214E6-0000-0000-C000-000000000046")]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            [ComConversionLoss]
            interface IShellFolder
            {
                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                void ParseDisplayName(IntPtr hwnd, [In, MarshalAs(UnmanagedType.Interface)] IBindCtx pbc, [In, MarshalAs(UnmanagedType.LPWStr)] string pszDisplayName, [Out] out uint pchEaten, [Out] out IntPtr ppidl, [In, Out] ref uint pdwAttributes);

                [PreserveSig]
                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                int EnumObjects([In] IntPtr hwnd, [In] SHCONT grfFlags, [MarshalAs(UnmanagedType.Interface)] out IEnumIDList ppenumIDList);

                [PreserveSig]
                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                int BindToObject([In] IntPtr pidl, [In, MarshalAs(UnmanagedType.Interface)] IBindCtx pbc, [In] ref Guid riid, [Out, MarshalAs(UnmanagedType.Interface)] out IShellFolder ppv);

                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                void BindToStorage([In] ref IntPtr pidl, [In, MarshalAs(UnmanagedType.Interface)] IBindCtx pbc, [In] ref Guid riid, out IntPtr ppv);

                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                void CompareIDs([In] IntPtr lParam, [In] ref IntPtr pidl1, [In] ref IntPtr pidl2);

                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                void CreateViewObject([In] IntPtr hwndOwner, [In] ref Guid riid, out IntPtr ppv);

                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                void GetAttributesOf([In] uint cidl, [In] IntPtr apidl, [In, Out] ref uint rgfInOut);

                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                void GetUIObjectOf([In] IntPtr hwndOwner, [In] uint cidl, [In] IntPtr apidl, [In] ref Guid riid, [In, Out] ref uint rgfReserved, out IntPtr ppv);

                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                void GetDisplayNameOf([In] ref IntPtr pidl, [In] uint uFlags, out IntPtr pName);

                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                void SetNameOf([In] IntPtr hwnd, [In] ref IntPtr pidl, [In, MarshalAs(UnmanagedType.LPWStr)] string pszName, [In] uint uFlags, [Out] IntPtr ppidlOut);
            }

            [ComImport]
            [Guid("000214F2-0000-0000-C000-000000000046")]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            private interface IEnumIDList
            {
                [PreserveSig]
                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                int Next(uint celt, IntPtr rgelt, out uint pceltFetched);

                [PreserveSig]
                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                int Skip([In] uint celt);

                [PreserveSig]
                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                int Reset();

                [PreserveSig]
                [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
                int Clone([MarshalAs(UnmanagedType.Interface)] out IEnumIDList ppenum);
            }

            private static class NativeMethods
            {
                [DllImport("shell32.dll", CharSet = CharSet.Unicode,
                    SetLastError = true)]
                public static extern int SHGetDesktopFolder(
                    [MarshalAs(UnmanagedType.Interface)] out IShellFolder ppshf);

                [DllImport("shell32.dll")]
                public static extern int SHOpenFolderAndSelectItems(
                    [In] IntPtr pidlFolder,
                    uint cidl,
                    [In, Optional, MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl,
                    int dwFlags);

                [DllImport("shell32.dll")]
                public static extern void ILFree(
                    [In] IntPtr pidl);
            }
        }
    }
}
