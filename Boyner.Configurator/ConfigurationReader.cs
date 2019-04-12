using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Boyner.Configurator
{
    public class ConfigurationReader
    {
        private string _applicationName, _connectionString;
        private double _refreshTimerIntervalInMs;
        private MongoDbContext _mongoDbContext;

        /// <summary>
        /// Retrieves data from storage, writes in text file and reads the data from file on further requests.
        /// </summary>
        /// <param name="applicationName"></param>
        /// <param name="connectionString"></param>
        /// <param name="refreshTimerIntervalInMs"></param>
        public ConfigurationReader(string applicationName, string connectionString, double refreshTimerIntervalInMs)
        {
            _applicationName = applicationName;
            _connectionString = connectionString;
            _refreshTimerIntervalInMs = refreshTimerIntervalInMs;
        }

        /// <summary>
        /// Retrieves data directly from storage.
        /// </summary>
        /// <param name="applicationName"></param>
        /// <param name="connectionString"></param>
        public ConfigurationReader(string applicationName, string connectionString)
        {
            _applicationName = applicationName;
            _connectionString = connectionString;
        }

        /// <summary>
        /// Retrieves data directly from storage with default connection string.
        /// </summary>
        /// <param name="applicationName"></param>
        public ConfigurationReader(string applicationName)
        {
            _applicationName = applicationName;
            _connectionString = "mongodb://localhost:27017";
        }

        public T GetValue<T>(string key)
        {
            _mongoDbContext = new MongoDbContext(_connectionString);
            Config config = null;
            string filePath = $"{_applicationName}.txt";

            if (_refreshTimerIntervalInMs == 0) //Directly from database, never checks file.
            {
                List<Config> configs = GetFromStorage(_applicationName, key, filePath);
                if (configs.Count == 0)
                {
                    throw new Exception($"Configurations for {_applicationName} not found in storage.");
                }
                config = configs.Find(x => x.Name == key);
                if (config == null)
                {
                    throw new Exception($"{key} for {_applicationName} not found in storage.");
                }
                if (string.IsNullOrWhiteSpace(config.Value))
                {
                    throw new Exception($"{key} for {_applicationName} is null or empty or whitespace in storage.");
                }
            }
            else
            {
                lock (_mongoDbContext)
                {
                    if (!File.Exists(filePath))
                    {
                        List<Config> configs = GetFromStorage(_applicationName, key, filePath);
                        if (configs.Count == 0)
                        {
                            throw new Exception($"Configurations for {_applicationName} not found in both storage and file.");
                        }
                        config = configs.Find(x => x.Name == key);
                        if (config == null)
                        {
                            throw new Exception($"{key} for {_applicationName} not found in both storage and file.");
                        }
                        if (string.IsNullOrWhiteSpace(config.Value))
                        {
                            throw new Exception($"{key} for {_applicationName} is null or empty or whitespace in both storage and file.");
                        }

                        WriteToFile(configs, filePath);
                    }
                    else
                    {
                        List<Config> configs = new List<Config>();
                        string text = File.ReadAllText(filePath);
                        string[] lines = text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
                        DateTime dateTime = new DateTime(Convert.ToInt64(lines[0]));
                        if (DateTime.Now >= dateTime.AddMilliseconds(_refreshTimerIntervalInMs))
                        {
                            configs = GetFromStorage(_applicationName, key, filePath);
                            config = configs.Find(x => x.Name == key);
                            WriteToFile(configs, filePath);
                        }
                        else
                        {
                            configs = ReadFromFile(key, filePath);
                            if (configs.Count == 0)
                            {
                                throw new Exception($"{key} for {_applicationName} not found in file.");
                            }
                            config = configs.Find(x => x.Name == key);

                        }
                    }

                }
            }

            var result = (T)Convert.ChangeType(config.Value, typeof(T), System.Globalization.CultureInfo.InvariantCulture);

            return result;
        }

        private List<Config> GetFromStorage(string applicationName, string key, string filePath)
        {
            List<Config> configs = new List<Config>();
            try
            {
                FilterDefinition<Config> filterDef = Builders<Config>.Filter.Eq("ApplicationName", applicationName) & Builders<Config>.Filter.Eq("IsActive", true);
                configs = _mongoDbContext.Configs.FindSync<Config>(filterDef).ToList();

            }
            catch (Exception)
            {
                configs = ReadFromFile(key, filePath);
            }
            return configs;

        }

        private void WriteToFile(List<Config> configs, string filePath)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(DateTime.Now.Ticks.ToString());
            foreach (var item in configs)
            {
                sb.AppendLine(item.Name + "#" + item.Value + "#" + item.Type);
            }
            using (StreamWriter sw = File.CreateText(filePath))
            {
                sw.Write(sb.ToString());
            }
        }

        private List<Config> ReadFromFile(string key, string filePath)
        {
            List<Config> configs = new List<Config>();
            try
            {
                string text = File.ReadAllText(filePath);
                string value = string.Empty;

                string[] lines = text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

                for (int i = 1; i < lines.Length; i++)
                {
                    string[] values = lines[i].Split("#", StringSplitOptions.RemoveEmptyEntries);
                    configs.Add(new Config()
                    {
                        Name = values[0],
                        Value = values[1],
                        Type = values[2]
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.ToString()}");
            }

            return configs;
        }
    }
}
