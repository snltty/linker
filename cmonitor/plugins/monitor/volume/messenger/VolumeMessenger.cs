using cmonitor.plugins.volume.report;
using cmonitor.server;
using common.libs;
using MemoryPack;
using System.IO.Compression;
using System.Text;

namespace cmonitor.plugins.volume.messenger
{
    public sealed class VolumeClientMessenger : IMessenger
    {
        private readonly VolumeReport volumeReport;
        public VolumeClientMessenger(VolumeReport volumeReport)
        {
            this.volumeReport = volumeReport;
        }

        [MessengerId((ushort)VolumeMessengerIds.Update)]
        public void Update(IConnection connection)
        {
            float value = MemoryPackSerializer.Deserialize<float>(connection.ReceiveRequestWrap.Payload.Span);
            volumeReport.SetVolume(value);
        }

        [MessengerId((ushort)VolumeMessengerIds.Mute)]
        public void Mute(IConnection connection)
        {
            bool value = MemoryPackSerializer.Deserialize<bool>(connection.ReceiveRequestWrap.Payload.Span);
            volumeReport.SetMute(value);
        }

        [MessengerId((ushort)VolumeMessengerIds.Play)]
        public void Play(IConnection connection)
        {
            string value = MemoryPackSerializer.Deserialize<string>(connection.ReceiveRequestWrap.Payload.Span);
            Task.Run(() =>
            {
                try
                {
                    using MemoryStream sourceStream = new MemoryStream(Encoding.ASCII.GetBytes(value));
                    using MemoryStream destStream = new MemoryStream();
                    using (GZipStream gzipStream = new GZipStream(sourceStream, CompressionMode.Decompress))
                    {
                        gzipStream.CopyTo(destStream);
                    }

                    byte[] uncompressedData = destStream.ToArray();
                    volumeReport.Play(uncompressedData);
                }
                catch (Exception ex)
                {
                    if (Logger.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    {
                        Logger.Instance.Error(ex);
                    }
                }
            });

        }
    }

}
