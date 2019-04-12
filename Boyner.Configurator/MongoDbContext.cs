using MongoDB.Driver;

namespace Boyner.Configurator
{
    internal class MongoDbContext
    {
        private readonly IMongoDatabase _database = null;

        internal MongoDbContext(string connectionString)
        {
            var client = new MongoClient(connectionString);
            if (client != null)
                _database = client.GetDatabase("ConfigDB");
        }

        internal IMongoCollection<Config> Configs
        {
            get
            {
                return _database.GetCollection<Config>("Config");
            }
        }

    }
}
