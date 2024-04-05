using common.libs.extends;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace common.libs.database
{
    /// <summary>
    /// 配置文件缓存
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IConfigDataProvider<T> where T : class, new()
    {
        /// <summary>
        /// 加载
        /// </summary>
        /// <returns></returns>
        Task<T> Load();
        /// <summary>
        /// 加载
        /// </summary>
        /// <returns></returns>
        Task<string> LoadString();
        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task Save(T model);
        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="jsonStr"></param>
        /// <returns></returns>
        Task Save(string jsonStr);
    }

    /// <summary>
    /// 配置文件的文件缓存
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class ConfigDataFileProvider<T> : IConfigDataProvider<T> where T : class, new()
    {
        SemaphoreSlim slim = new SemaphoreSlim(1);
        FileStream fs = null;
        StreamWriter writer = null;
        StreamReader reader = null;
        public ConfigDataFileProvider()
        {
            string path = GetTableName(typeof(T));
            fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            reader = new StreamReader(fs, Encoding.UTF8);
            writer = new StreamWriter(fs, Encoding.UTF8);
        }

        public async Task<T> Load()
        {
            await slim.WaitAsync();
            string fileName = GetTableName(typeof(T));
            try
            {
                fs.Seek(0, SeekOrigin.Begin);
                string str = await reader.ReadToEndAsync().ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(str))
                {
                    return default;
                }
                return str.DeJson<T>();
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"{fileName} 配置文件解析有误~ :{ex}");
            }
            finally
            {
                slim.Release();
            }
            return default;
        }
        public async Task<string> LoadString()
        {
            await slim.WaitAsync();
            try
            {
                fs.Seek(0, SeekOrigin.Begin);
                return await reader.ReadToEndAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
            }
            finally
            {
                slim.Release();
            }
            return string.Empty;
        }

        public async Task Save(T model)
        {
            await slim.WaitAsync();
            try
            {

                fs.Seek(0, SeekOrigin.Begin);
                await writer.WriteAsync(model.ToJsonFormat()).ConfigureAwait(false);
                await writer.FlushAsync();
            }
            catch (Exception)
            {
            }
            finally
            {
                slim.Release();
            }
        }
        public async Task Save(string jsonStr)
        {
            await slim.WaitAsync();
            try
            {
                fs.Seek(0, SeekOrigin.Begin);
                await writer.WriteAsync(jsonStr).ConfigureAwait(false);
                await writer.FlushAsync();
            }
            catch (Exception)
            {
            }
            finally
            {
                slim.Release();
            }
        }

        private string GetTableName(Type type)
        {
            var attrs = type.GetCustomAttributes(typeof(TableAttribute), false);
            string file = $"./configs/{type.Name}.json";
            if (attrs.Length > 0)
            {
                file = $"./configs/{(attrs[0] as TableAttribute).Name}.json";
            }
            string path = Path.GetDirectoryName(file);
            if (Directory.Exists(path) == false)
            {
                Directory.CreateDirectory(path);
            }

            return file;
        }
    }
}
