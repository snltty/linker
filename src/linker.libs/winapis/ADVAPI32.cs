using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace linker.libs.winapis;

public static unsafe class ADVAPI32
{
    #region Structs
    public struct TOKEN_PRIVILEGES
    {
        public struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public uint Attributes;
        }
        public int PrivilegeCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = ANYSIZE_ARRAY)]
        public LUID_AND_ATTRIBUTES[] Privileges;
    }
    public class USEROBJECTFLAGS
    {
        public int fInherit ;
        public int fReserved;
        public int dwFlags;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct SECURITY_ATTRIBUTES
    {
        public int Length;
        public nint lpSecurityDescriptor;
        public bool bInheritHandle;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESS_INFORMATION
    {
        public nint hProcess;
        public nint hThread;
        public int dwProcessId;
        public int dwThreadId;
    }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct STARTUPINFO
    {
        public int cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public int dwX;
        public int dwY;
        public int dwXSize;
        public int dwYSize;
        public int dwXCountChars;
        public int dwYCountChars;
        public int dwFillAttribute;
        public int dwFlags;
        public short wShowWindow;
        public short cbReserved2;
        public nint lpReserved2;
        public nint hStdInput;
        public nint hStdOutput;
        public nint hStdError;
    }
    #endregion

    #region Enums
    public enum TOKEN_INFORMATION_CLASS
    {
        /// <summary>
            /// The buffer receives a TOKEN_USER structure that contains the user account of the token.
            /// </summary>
        TokenUser = 1,

        /// <summary>
            /// The buffer receives a TOKEN_GROUPS structure that contains the group accounts associated with the token.
            /// </summary>
        TokenGroups,

        /// <summary>
            /// The buffer receives a TOKEN_PRIVILEGES structure that contains the privileges of the token.
            /// </summary>
        TokenPrivileges,

        /// <summary>
            /// The buffer receives a TOKEN_OWNER structure that contains the default owner security identifier (SID) for newly created objects.
            /// </summary>
        TokenOwner,

        /// <summary>
            /// The buffer receives a TOKEN_PRIMARY_GROUP structure that contains the default primary group SID for newly created objects.
            /// </summary>
        TokenPrimaryGroup,

        /// <summary>
            /// The buffer receives a TOKEN_DEFAULT_DACL structure that contains the default DACL for newly created objects.
            /// </summary>
        TokenDefaultDacl,

        /// <summary>
            /// The buffer receives a TOKEN_SOURCE structure that contains the source of the token. TOKEN_QUERY_SOURCE access is needed to retrieve this information.
            /// </summary>
        TokenSource,

        /// <summary>
            /// The buffer receives a TOKEN_TYPE value that indicates whether the token is a primary or impersonation token.
            /// </summary>
        TokenType,

        /// <summary>
            /// The buffer receives a SECURITY_IMPERSONATION_LEVEL value that indicates the impersonation level of the token. If the access token is not an impersonation token, the function fails.
            /// </summary>
        TokenImpersonationLevel,

        /// <summary>
            /// The buffer receives a TOKEN_STATISTICS structure that contains various token statistics.
            /// </summary>
        TokenStatistics,

        /// <summary>
            /// The buffer receives a TOKEN_GROUPS structure that contains the list of restricting SIDs in a restricted token.
            /// </summary>
        TokenRestrictedSids,

        /// <summary>
            /// The buffer receives a DWORD value that indicates the Terminal Services session identifier that is associated with the token. 
            /// </summary>
        TokenSessionId,

        /// <summary>
            /// The buffer receives a TOKEN_GROUPS_AND_PRIVILEGES structure that contains the user SID, the group accounts, the restricted SIDs, and the authentication ID associated with the token.
            /// </summary>
        TokenGroupsAndPrivileges,

        /// <summary>
            /// Reserved.
            /// </summary>
        TokenSessionReference,

        /// <summary>
            /// The buffer receives a DWORD value that is nonzero if the token includes the SANDBOX_INERT flag.
            /// </summary>
        TokenSandBoxInert,

        /// <summary>
            /// Reserved.
            /// </summary>
        TokenAuditPolicy,

        /// <summary>
            /// The buffer receives a TOKEN_ORIGIN value. 
            /// </summary>
        TokenOrigin,

        /// <summary>
            /// The buffer receives a TOKEN_ELEVATION_TYPE value that specifies the elevation level of the token.
            /// </summary>
        TokenElevationType,

        /// <summary>
            /// The buffer receives a TOKEN_LINKED_TOKEN structure that contains a handle to another token that is linked to this token.
            /// </summary>
        TokenLinkedToken,

        /// <summary>
            /// The buffer receives a TOKEN_ELEVATION structure that specifies whether the token is elevated.
            /// </summary>
        TokenElevation,

        /// <summary>
            /// The buffer receives a DWORD value that is nonzero if the token has ever been filtered.
            /// </summary>
        TokenHasRestrictions,

        /// <summary>
            /// The buffer receives a TOKEN_ACCESS_INFORMATION structure that specifies security information contained in the token.
            /// </summary>
        TokenAccessInformation,

        /// <summary>
            /// The buffer receives a DWORD value that is nonzero if virtualization is allowed for the token.
            /// </summary>
        TokenVirtualizationAllowed,

        /// <summary>
            /// The buffer receives a DWORD value that is nonzero if virtualization is enabled for the token.
            /// </summary>
        TokenVirtualizationEnabled,

        /// <summary>
            /// The buffer receives a TOKEN_MANDATORY_LABEL structure that specifies the token's integrity level. 
            /// </summary>
        TokenIntegrityLevel,

        /// <summary>
            /// The buffer receives a DWORD value that is nonzero if the token has the UIAccess flag set.
            /// </summary>
        TokenUIAccess,

        /// <summary>
            /// The buffer receives a TOKEN_MANDATORY_POLICY structure that specifies the token's mandatory integrity policy.
            /// </summary>
        TokenMandatoryPolicy,

        /// <summary>
            /// The buffer receives the token's logon security identifier (SID).
            /// </summary>
        TokenLogonSid,

        /// <summary>
            /// The maximum value for this enumeration
            /// </summary>
        MaxTokenInfoClass
    }
    public enum LOGON_TYPE
    {
        LOGON32_LOGON_INTERACTIVE = 2,
        LOGON32_LOGON_NETWORK,
        LOGON32_LOGON_BATCH,
        LOGON32_LOGON_SERVICE,
        LOGON32_LOGON_UNLOCK = 7,
        LOGON32_LOGON_NETWORK_CLEARTEXT,
        LOGON32_LOGON_NEW_CREDENTIALS
    }
    public enum LOGON_PROVIDER
    {
        LOGON32_PROVIDER_DEFAULT,
        LOGON32_PROVIDER_WINNT35,
        LOGON32_PROVIDER_WINNT40,
        LOGON32_PROVIDER_WINNT50
    }
    [Flags]
    public enum CreateProcessFlags
    {
        CREATE_BREAKAWAY_FROM_JOB = 0x01000000,
        CREATE_DEFAULT_ERROR_MODE = 0x04000000,
        CREATE_NEW_CONSOLE = 0x00000010,
        CREATE_NEW_PROCESS_GROUP = 0x00000200,
        CREATE_NO_WINDOW = 0x08000000,
        CREATE_PROTECTED_PROCESS = 0x00040000,
        CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x02000000,
        CREATE_SEPARATE_WOW_VDM = 0x00000800,
        CREATE_SHARED_WOW_VDM = 0x00001000,
        CREATE_SUSPENDED = 0x00000004,
        CREATE_UNICODE_ENVIRONMENT = 0x00000400,
        DEBUG_ONLY_THIS_PROCESS = 0x00000002,
        DEBUG_PROCESS = 0x00000001,
        DETACHED_PROCESS = 0x00000008,
        EXTENDED_STARTUPINFO_PRESENT = 0x00080000,
        INHERIT_PARENT_AFFINITY = 0x00010000
    }
    public enum TOKEN_TYPE : int
    {
        TokenPrimary = 1,
        TokenImpersonation = 2
    }

    public enum SECURITY_IMPERSONATION_LEVEL : int
    {
        SecurityAnonymous = 0,
        SecurityIdentification = 1,
        SecurityImpersonation = 2,
        SecurityDelegation = 3,
    }

    #endregion

    #region Constants
    public const int TOKEN_DUPLICATE = 0x0002;
    public const uint MAXIMUM_ALLOWED = 0x2000000;
    public const int CREATE_NEW_CONSOLE = 0x00000010;
    public const int CREATE_NO_WINDOW = 0x08000000;
    public const int CREATE_UNICODE_ENVIRONMENT = 0x00000400;
    public const int STARTF_USESHOWWINDOW = 0x00000001;
    public const int DETACHED_PROCESS = 0x00000008;
    public const int TOKEN_ALL_ACCESS = 0x000f01ff;
    public const int PROCESS_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0xFFF;
    public const int STANDARD_RIGHTS_REQUIRED = 0x000F0000;
    public const int SYNCHRONIZE = 0x00100000;

    public const int IDLE_PRIORITY_CLASS = 0x40;
    public const int NORMAL_PRIORITY_CLASS = 0x20;
    public const int HIGH_PRIORITY_CLASS = 0x80;
    public const int REALTIME_PRIORITY_CLASS = 0x100;
    public const uint SE_PRIVILEGE_ENABLED_BY_DEFAULT = 0x00000001;
    public const uint SE_PRIVILEGE_ENABLED = 0x00000002;
    public const uint SE_PRIVILEGE_REMOVED = 0x00000004;
    public const uint SE_PRIVILEGE_USED_FOR_ACCESS = 0x80000000;
    public const int ANYSIZE_ARRAY = 1;

    public const int UOI_FLAGS = 1;
    public const int UOI_NAME = 2;
    public const int UOI_TYPE = 3;
    public const int UOI_USER_SID = 4;
    public const int UOI_HEAPSIZE = 5;
    public const int UOI_IO = 6;
    #endregion

    #region DLL Imports
    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool AdjustTokenPrivileges(nint tokenHandle,
       [MarshalAs(UnmanagedType.Bool)] bool disableAllPrivileges,
       ref TOKEN_PRIVILEGES newState,
       uint bufferLengthInBytes,
       ref TOKEN_PRIVILEGES previousState,
       out uint returnLengthInBytes);
    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern bool CreateProcessAsUser(
        nint hToken,
        string lpApplicationName,
        string lpCommandLine,
        ref SECURITY_ATTRIBUTES lpProcessAttributes,
        ref SECURITY_ATTRIBUTES lpThreadAttributes,
        bool bInheritHandles,
        uint dwCreationFlags,
        nint lpEnvironment,
        string lpCurrentDirectory,
        ref STARTUPINFO lpStartupInfo,
        out PROCESS_INFORMATION lpProcessInformation);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool AllocateLocallyUniqueId(out nint pLuid);

    [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = false)]
    public static extern SECUR32.WinErrors LsaNtStatusToWinError(SECUR32.WinStatusCodes status);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool GetTokenInformation(
        nint TokenHandle,
        TOKEN_INFORMATION_CLASS TokenInformationClass,
        nint TokenInformation,
        int TokenInformationLength,
        out int ReturnLength);

    [DllImport("advapi32.dll", SetLastError = true, BestFitMapping = false, ThrowOnUnmappableChar = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool LogonUser(
        [MarshalAs(UnmanagedType.LPStr)] string pszUserName,
        [MarshalAs(UnmanagedType.LPStr)] string pszDomain,
        [MarshalAs(UnmanagedType.LPStr)] string pszPassword,
        int dwLogonType,
        int dwLogonProvider,
        out nint phToken);

    [DllImport("advapi32", SetLastError = true), SuppressUnmanagedCodeSecurity]
    public static extern bool OpenProcessToken(nint ProcessHandle, int DesiredAccess, ref nint TokenHandle);
    [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool DuplicateTokenEx(
        nint hExistingToken,
        uint dwDesiredAccess,
        ref SECURITY_ATTRIBUTES lpTokenAttributes,
        SECURITY_IMPERSONATION_LEVEL ImpersonationLevel,
        TOKEN_TYPE TokenType,
        out nint phNewToken);

    [DllImport("advapi32.dll", SetLastError = false)]
    public static extern uint LsaNtStatusToWinError(uint status);
    #endregion

    [DllImport("advapi32", CharSet = CharSet.Auto, SetLastError = true), SuppressUnmanagedCodeSecurity]
    public static extern bool ConvertSidToStringSid(IntPtr sid, out string stringSid);

    [StructLayout(LayoutKind.Sequential)]
    public struct TOKEN_USER
    {
        public SID_AND_ATTRIBUTES User;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct SID_AND_ATTRIBUTES
    {
        public IntPtr Sid;
        public int Attributes;
    }

    [DllImport("Advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern bool LookupAccountName(
        string lpSystemName,
        string lpAccountName,
        IntPtr Sid,
        ref int cbSid,
        StringBuilder ReferencedDomainName,
        ref int cchReferencedDomainName,
        out int peUse
    );



    [DllImport("advapi32.dll", EntryPoint = "OpenSCManagerW", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern nint OpenSCManager(uint machineName, uint databaseName, uint dwAccess);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern nint OpenService(nint hSCManager, string lpServiceName, uint dwDesiredAccess);
    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseServiceHandle(nint hSCObject);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ControlService(nint hService, SERVICE_CONTROL dwControl, ref SERVICE_STATUS lpServiceStatus);

    [DllImport("advapi32", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool StartService(
        nint hService,
        int dwNumServiceArgs,
        string[] lpServiceArgVectors
    );

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeleteService(nint hService);
    [Flags]
    public enum SERVICE_TYPE : int
    {
        SERVICE_KERNEL_DRIVER = 0x00000001,
        SERVICE_FILE_SYSTEM_DRIVER = 0x00000002,
        SERVICE_WIN32_OWN_PROCESS = 0x00000010,
        SERVICE_WIN32_SHARE_PROCESS = 0x00000020,
        SERVICE_INTERACTIVE_PROCESS = 0x00000100
    }
    [Flags]
    public enum SERVICE_CONTROL : uint
    {
        STOP = 0x00000001,
        PAUSE = 0x00000002,
        CONTINUE = 0x00000003,
        INTERROGATE = 0x00000004,
        SHUTDOWN = 0x00000005,
        PARAMCHANGE = 0x00000006,
        NETBINDADD = 0x00000007,
        NETBINDREMOVE = 0x00000008,
        NETBINDENABLE = 0x00000009,
        NETBINDDISABLE = 0x0000000A,
        DEVICEEVENT = 0x0000000B,
        HARDWAREPROFILECHANGE = 0x0000000C,
        POWEREVENT = 0x0000000D,
        SESSIONCHANGE = 0x0000000E
    }
    public enum SERVICE_STATE : uint
    {
        SERVICE_STOPPED = 0x00000001,
        SERVICE_START_PENDING = 0x00000002,
        SERVICE_STOP_PENDING = 0x00000003,
        SERVICE_RUNNING = 0x00000004,
        SERVICE_CONTINUE_PENDING = 0x00000005,
        SERVICE_PAUSE_PENDING = 0x00000006,
        SERVICE_PAUSED = 0x00000007
    }
    public enum SERVICE_ACCESS : uint
    {
        /// <summary>
        /// Required to call the QueryServiceConfig and 
        /// QueryServiceConfig2 functions to query the service configuration.
        /// </summary>
        SERVICE_QUERY_CONFIG = 0x00001,

        /// <summary>
        /// Required to call the ChangeServiceConfig or ChangeServiceConfig2 function 
        /// to change the service configuration. Because this grants the caller 
        /// the right to change the executable file that the system runs, 
        /// it should be granted only to administrators.
        /// </summary>
        SERVICE_CHANGE_CONFIG = 0x00002,

        /// <summary>
        /// Required to call the QueryServiceStatusEx function to ask the service 
        /// control manager about the status of the service.
        /// </summary>
        SERVICE_QUERY_STATUS = 0x00004,

        /// <summary>
        /// Required to call the EnumDependentServices function to enumerate all 
        /// the services dependent on the service.
        /// </summary>
        SERVICE_ENUMERATE_DEPENDENTS = 0x00008,

        /// <summary>
        /// Required to call the StartService function to start the service.
        /// </summary>
        SERVICE_START = 0x00010,

        /// <summary>
        ///     Required to call the ControlService function to stop the service.
        /// </summary>
        SERVICE_STOP = 0x00020,

        /// <summary>
        /// Required to call the ControlService function to pause or continue 
        /// the service.
        /// </summary>
        SERVICE_PAUSE_CONTINUE = 0x00040,

        /// <summary>
        /// Required to call the EnumDependentServices function to enumerate all
        /// the services dependent on the service.
        /// </summary>
        SERVICE_INTERROGATE = 0x00080,

        /// <summary>
        /// Required to call the ControlService function to specify a user-defined
        /// control code.
        /// </summary>
        SERVICE_USER_DEFINED_CONTROL = 0x00100,

        /// <summary>
        /// Includes STANDARD_RIGHTS_REQUIRED in addition to all access rights in this table.
        /// </summary>
        SERVICE_ALL_ACCESS = ACCESS_MASK.STANDARD_RIGHTS_REQUIRED |
            SERVICE_QUERY_CONFIG |
            SERVICE_CHANGE_CONFIG |
            SERVICE_QUERY_STATUS |
            SERVICE_ENUMERATE_DEPENDENTS |
            SERVICE_START |
            SERVICE_STOP |
            SERVICE_PAUSE_CONTINUE |
            SERVICE_INTERROGATE |
            SERVICE_USER_DEFINED_CONTROL,

        GENERIC_READ = ACCESS_MASK.STANDARD_RIGHTS_READ |
            SERVICE_QUERY_CONFIG |
            SERVICE_QUERY_STATUS |
            SERVICE_INTERROGATE |
            SERVICE_ENUMERATE_DEPENDENTS,

        GENERIC_WRITE = ACCESS_MASK.STANDARD_RIGHTS_WRITE |
            SERVICE_CHANGE_CONFIG,

        GENERIC_EXECUTE = ACCESS_MASK.STANDARD_RIGHTS_EXECUTE |
            SERVICE_START |
            SERVICE_STOP |
            SERVICE_PAUSE_CONTINUE |
            SERVICE_USER_DEFINED_CONTROL,

        /// <summary>
        /// Required to call the QueryServiceObjectSecurity or 
        /// SetServiceObjectSecurity function to access the SACL. The proper
        /// way to obtain this access is to enable the SE_SECURITY_NAME 
        /// privilege in the caller's current access token, open the handle 
        /// for ACCESS_SYSTEM_SECURITY access, and then disable the privilege.
        /// </summary>
        ACCESS_SYSTEM_SECURITY = ACCESS_MASK.ACCESS_SYSTEM_SECURITY,

        /// <summary>
        /// Required to call the DeleteService function to delete the service.
        /// </summary>
        DELETE = ACCESS_MASK.DELETE,

        /// <summary>
        /// Required to call the QueryServiceObjectSecurity function to query
        /// the security descriptor of the service object.
        /// </summary>
        READ_CONTROL = ACCESS_MASK.READ_CONTROL,

        /// <summary>
        /// Required to call the SetServiceObjectSecurity function to modify
        /// the Dacl member of the service object's security descriptor.
        /// </summary>
        WRITE_DAC = ACCESS_MASK.WRITE_DAC,

        /// <summary>
        /// Required to call the SetServiceObjectSecurity function to modify 
        /// the Owner and Group members of the service object's security 
        /// descriptor.
        /// </summary>
        WRITE_OWNER = ACCESS_MASK.WRITE_OWNER,
    }
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct SERVICE_STATUS
    {
        public SERVICE_TYPE dwServiceType;
        public SERVICE_STATE dwCurrentState;
        public uint dwControlsAccepted;
        public uint dwWin32ExitCode;
        public uint dwServiceSpecificExitCode;
        public uint dwCheckPoint;
        public uint dwWaitHint;
    }

    [Flags]
    public enum ACCESS_MASK : uint
    {
        DELETE = 0x00010000,
        READ_CONTROL = 0x00020000,
        WRITE_DAC = 0x00040000,
        WRITE_OWNER = 0x00080000,
        SYNCHRONIZE = 0x00100000,

        STANDARD_RIGHTS_REQUIRED = 0x000F0000,

        STANDARD_RIGHTS_READ = 0x00020000,
        STANDARD_RIGHTS_WRITE = 0x00020000,
        STANDARD_RIGHTS_EXECUTE = 0x00020000,

        STANDARD_RIGHTS_ALL = 0x001F0000,

        SPECIFIC_RIGHTS_ALL = 0x0000FFFF,

        ACCESS_SYSTEM_SECURITY = 0x01000000,

        MAXIMUM_ALLOWED = 0x02000000,

        GENERIC_READ = 0x80000000,
        GENERIC_WRITE = 0x40000000,
        GENERIC_EXECUTE = 0x20000000,
        GENERIC_ALL = 0x10000000,

        DESKTOP_READOBJECTS = 0x00000001,
        DESKTOP_CREATEWINDOW = 0x00000002,
        DESKTOP_CREATEMENU = 0x00000004,
        DESKTOP_HOOKCONTROL = 0x00000008,
        DESKTOP_JOURNALRECORD = 0x00000010,
        DESKTOP_JOURNALPLAYBACK = 0x00000020,
        DESKTOP_ENUMERATE = 0x00000040,
        DESKTOP_WRITEOBJECTS = 0x00000080,
        DESKTOP_SWITCHDESKTOP = 0x00000100,

        WINSTA_ENUMDESKTOPS = 0x00000001,
        WINSTA_READATTRIBUTES = 0x00000002,
        WINSTA_ACCESSCLIPBOARD = 0x00000004,
        WINSTA_CREATEDESKTOP = 0x00000008,
        WINSTA_WRITEATTRIBUTES = 0x00000010,
        WINSTA_ACCESSGLOBALATOMS = 0x00000020,
        WINSTA_EXITWINDOWS = 0x00000040,
        WINSTA_ENUMERATE = 0x00000100,
        WINSTA_READSCREEN = 0x00000200,

        WINSTA_ALL_ACCESS = 0x0000037F
    }

    public enum SERVICE_START : uint
    {
        /// <summary>
        /// A device driver started by the system loader. This value is valid
        /// only for driver services.
        /// </summary>
        SERVICE_BOOT_START = 0x00000000,

        /// <summary>
        /// A device driver started by the IoInitSystem function. This value 
        /// is valid only for driver services.
        /// </summary>
        SERVICE_SYSTEM_START = 0x00000001,

        /// <summary>
        /// A service started automatically by the service control manager 
        /// during system startup. For more information, see Automatically 
        /// Starting Services.
        /// </summary>         
        SERVICE_AUTO_START = 0x00000002,

        /// <summary>
        /// A service started by the service control manager when a process 
        /// calls the StartService function. For more information, see 
        /// Starting Services on Demand.
        /// </summary>
        SERVICE_DEMAND_START = 0x00000003,

        /// <summary>
        /// A service that cannot be started. Attempts to start the service
        /// result in the error code ERROR_SERVICE_DISABLED.
        /// </summary>
        SERVICE_DISABLED = 0x00000004,
    }



    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern nint CreateServiceW(
        nint hSCManager,
        string lpServiceName,
        string lpDisplayName,
        uint dwDesiredAccess,
        uint dwServiceType,
        uint dwStartType,
        uint dwErrorControl,
        string lpBinaryPathName,
        uint lpLoadOrderGroup,
        uint lpdwTagId,
        uint lpdwTagId1,
        uint lpDependencies,
        uint lpServiceStartName,
        uint lpPassword);



    [DllImport("ntdll.dll", CharSet = CharSet.Auto)]
    public static extern uint NtOpenFile(nint* FileHandle, uint DesiredAccess, OBJECT_ATTRIBUTES* ObjectAttributes, IO_STATUS_BLOCK* IoStatusBlock, uint ShareAccess, uint OpenOptions);

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    public struct IO_STATUS_BLOCK
    {
        public uint status;
        public nint information;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct UNICODE_STRING : IDisposable
    {
        public ushort Length;
        public ushort MaximumLength;
        private nint buffer;

        public UNICODE_STRING(string s)
        {
            Length = (ushort)(s.Length * 2);
            MaximumLength = (ushort)(Length + 2);
            buffer = Marshal.StringToHGlobalUni(s);
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(buffer);
            buffer = nint.Zero;
        }

        public override string ToString()
        {
            return Marshal.PtrToStringUni(buffer);
        }
    }

    public struct OBJECT_ATTRIBUTES
    {
        public int Length;
        public nint RootDirectory;
        public nint ObjectName;
        public uint Attributes;
        public nint SecurityDescriptor;
        public nint SecurityQualityOfService;

    }
}
