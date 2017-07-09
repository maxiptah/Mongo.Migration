﻿using System;
using Mongo.Migration.Documents;
using Mongo.Migration.Documents.Serializers;
using Mongo.Migration.Services.Interceptors;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Mongo.Migration.Services.MongoDB
{
    internal class MongoRegistrater : IMongoRegistrater
    {
        private readonly MigrationInterceptorProvider _provider;

        private readonly DocumentVersionSerializer _serializer;

        public MongoRegistrater(DocumentVersionSerializer serializer, MigrationInterceptorProvider provider)
        {
            _serializer = serializer;
            _provider = provider;
        }

        public void Registrate()
        {
            BsonSerializer.RegisterSerializationProvider(_provider);

            try
            {
                BsonSerializer.RegisterSerializer(typeof(DocumentVersion), _serializer);
            }
            catch (BsonSerializationException Exception)
            {
                // Catch if Serializer was registered already, not great. But for testing it must be catched.
            }
        }
    }
}