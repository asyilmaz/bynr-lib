using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Boyner.Configurator
{
    internal class Config
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonElement("Name")]
        [BsonRepresentation(BsonType.String)]
        public string Name { get; set; }

        [BsonElement("Type")]
        [BsonRepresentation(BsonType.String)]
        public string Type { get; set; }

        [BsonElement("Value")]
        [BsonRepresentation(BsonType.String)]
        public string Value { get; set; }

        [BsonElement("IsActive")]
        [BsonRepresentation(BsonType.Boolean)]
        public bool IsActive { get; set; }

        [BsonElement("ApplicationName")]
        [BsonRepresentation(BsonType.String)]
        public string ApplicationName { get; set; }
    }
}
