using NETCONLib;
using System;
using System.Text;

namespace linker.ics
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string pulicName = args[0];
            string privateName = args[1];
            string type = args[2];
            StringBuilder sb = new StringBuilder();

            GetConnections(pulicName, privateName, out INetSharingConfiguration publicCon, out INetSharingConfiguration privateCon);
            if (publicCon == null)
            {
                sb.Append($"{pulicName} public device not found!");
            }
            else if (privateCon == null)
            {
                sb.Append($"{privateName} private device not found!");
            }
            else if (type == "enable")
            {
                try
                {
                    publicCon.EnableSharing(tagSHARINGCONNECTIONTYPE.ICSSHARINGTYPE_PUBLIC);
                }
                catch (Exception ex)
                {
                    sb.Append($"{ex.Message},may need to be reboot system");
                }
                try
                {
                    privateCon.EnableSharing(tagSHARINGCONNECTIONTYPE.ICSSHARINGTYPE_PRIVATE);
                }
                catch (Exception ex)
                {
                    sb.Append($"{ex.Message},may need to be reboot system");
                }
            }
            else if (type == "disable")
            {
                try
                {
                    publicCon.DisableSharing();
                }
                catch (Exception ex)
                {
                    sb.Append($"{ex.Message},may need to be reboot system");
                }
                try
                {
                    privateCon.DisableSharing();
                }
                catch (Exception ex)
                {
                    sb.Append($"{ex.Message},may need to be reboot system");
                }
            }
            else
            {
                sb.Append($"{type} command invalid");
            }

            string result = sb.ToString();
            if (string.IsNullOrEmpty(result))
            {
                Console.WriteLine($"{type} success");
            }
            else
            {
                Console.WriteLine(result);
            }
        }

        static void GetConnections(string publicName, string privateName, out INetSharingConfiguration publicCon, out INetSharingConfiguration privateCon)
        {
            publicCon = null;
            privateCon = null;

            try
            {
                INetSharingManager netSharingManager = new NetSharingManager();

                foreach (INetConnection connection in netSharingManager.EnumEveryConnection)
                {
                    INetConnectionProps props = netSharingManager.NetConnectionProps[connection];
                    INetSharingConfiguration sharingConfig = netSharingManager.INetSharingConfigurationForINetConnection[connection];
                    try
                    {
                        if (props.Name == publicName)
                        {
                            publicCon = sharingConfig;
                        }
                        if (props.Name == privateName)
                        {
                            privateCon = sharingConfig;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
