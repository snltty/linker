using common.libs.extends;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Text;
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
        public async Task<T> Load()
        {
            string fileName = GetTableName(typeof(T));
            try
            {
                if (File.Exists(fileName))
                {
                    string str = (await File.ReadAllTextAsync(fileName).ConfigureAwait(false));
                    return str.DeJson<T>();
                }
                else
                {
                    Logger.Instance.Warning($"{fileName} 配置文件缺失~");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"{fileName} 配置文件解析有误~ :{ex}");
            }
            return null;
        }
        public async Task<string> LoadString()
        {
            string fileName = GetTableName(typeof(T));
            if (File.Exists(fileName))
            {
                return (await File.ReadAllTextAsync(fileName).ConfigureAwait(false));
            }
            return string.Empty;
        }

        public async Task Save(T model)
        {
            try
            {
                string fileName = GetTableName(typeof(T));
                await File.WriteAllTextAsync(fileName, model.ToJsonIndented(), Encoding.UTF8).ConfigureAwait(false);
            }
            catch (Exception)
            {
            }
        }
        public async Task Save(string jsonStr)
        {
            try
            {
                string fileName = GetTableName(typeof(T));
                await File.WriteAllTextAsync(fileName, jsonStr, Encoding.UTF8).ConfigureAwait(false);
            }
            catch (Exception)
            {
            }
        }

        private string GetTableName(Type type)
        {
            var attrs = type.GetCustomAttributes(typeof(TableAttribute), false);
            if (attrs.Length > 0)
            {
                return $"{(attrs[0] as TableAttribute).Name}.json";
            }
            return $"{type.Name}.json";
        }
    }
}
