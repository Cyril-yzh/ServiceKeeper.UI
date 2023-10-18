using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;

namespace ServiceKeeper.UI.Utils
{
    public class FileLoader
    {
        private string DirectoryString { get; init; }

        public FileLoader(string DirectoryName)
        {
            try
            {
                DirectoryString = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DirectoryName);
                if (!Directory.Exists(DirectoryString))
                    Directory.CreateDirectory(DirectoryString);
            }
            catch
            {
                throw;
            }
        }

        public T Load<T>(string name)
        {
            try
            {
                string filePath = Path.Combine(DirectoryString, name);

                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);

                    T? result = JsonSerializer.Deserialize<T>(json);
                    return result == null ? throw new Exception($"加载{filePath}文件失败,加载内容为空") : result;
                }
                else
                {
                    throw new Exception($"无法找到{filePath}文件");
                }
            }
            catch
            {
                throw;
            }
        }

        public List<T> LoadAll<T>()
        {
            try
            {
                List<T> resultList = new();

                string[] fileNames = Directory.GetFiles(DirectoryString);

                foreach (string fileName in fileNames)
                {
                    string json = File.ReadAllText(fileName);
                    T? result = JsonSerializer.Deserialize<T>(json);

                    if (result != null)
                    {
                        resultList.Add(result);
                    }
                }

                return resultList;
            }
            catch (Exception ex)
            {
                throw new Exception($"加载文件夹{DirectoryString}内的内容时发生错误：{ex.Message}");
            }
        }

        public void Save<T>(T confObject, string name)
        {
            try
            {
                Delete(name);
                CheckPath(DirectoryString);
                string json = JsonSerializer.Serialize(confObject);
                string filePath = Path.Combine(DirectoryString, name);
                File.WriteAllText(filePath, json);
            }
            catch
            {
                throw;
            }
        }

        public void Delete(string name)
        {
            string filePath = Path.Combine(DirectoryString, name);

            if (File.Exists(filePath))
                File.Delete(filePath);
        }

        public static void CheckPath(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
    }
}
