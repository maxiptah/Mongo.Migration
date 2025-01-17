using System;
using System.Collections.Generic;
using System.Linq;
using Mongo.Migration.Documents;
using Mongo.Migration.Documents.Locators;
using Mongo.Migration.Exceptions;
using Mongo.Migration.Migrations;
using Mongo.Migration.Migrations.Locators;
using MongoDB.Bson;

namespace Mongo.Migration.Services
{
    using Microsoft.Extensions.Options;
    using Startup.DotNetCore;

    internal class VersionService : IVersionService
    {
        private static readonly string VERSION_FIELD_NAME = "Version";

        private readonly IMigrationLocator _migrationLocator;

        private readonly IRuntimeVersionLocator _runtimeVersionLocator;
        
        private readonly IStartUpVersionLocator _startUpVersionLocator;

        private string _versionFieldName;

        public VersionService(
            IMigrationLocator migrationLocator, 
            IRuntimeVersionLocator runtimeVersionLocator,
            IStartUpVersionLocator startUpVersionLocator, 
            IOptions<MongoMigrationSettings> mongoMigrationSettings)
        {
            _migrationLocator = migrationLocator;
            _runtimeVersionLocator = runtimeVersionLocator;
            _startUpVersionLocator = startUpVersionLocator;
            _versionFieldName = string.IsNullOrWhiteSpace(mongoMigrationSettings.Value.VersionFieldName)
                ? VERSION_FIELD_NAME
                : mongoMigrationSettings.Value.VersionFieldName;
        }

        public string GetVersionFieldName()
        {
            return _versionFieldName;
        }

        public DocumentVersion GetCurrentOrLatestMigrationVersion(Type type)
        {
            var latestVersion = _migrationLocator.GetLatestVersion(type);
            return GetCurrentVersion(type) ?? latestVersion;
        }
        
        public DocumentVersion GetCollectionVersion(Type type)
        {
            var version = GetCurrentOrLatestMigrationVersion(type);
            return _startUpVersionLocator.GetLocateOrNull(type) ?? version;
        }
        
        public DocumentVersion GetVersionOrDefault(BsonDocument document)
        {
            BsonValue value;
            document.TryGetValue(GetVersionFieldName(), out value);

            if (value != null)
                return value.AsString;

            return DocumentVersion.Default();
        }

        public void SetVersion(BsonDocument document, DocumentVersion version)
        {
            document[GetVersionFieldName()] = version.ToString();
        }

        public void DetermineVersion<TClass>(TClass instance) where TClass : class, IDocument
        {
            var type = typeof(TClass);
            var documentVersion = instance.Version.ToString();
            var latestVersion = _migrationLocator.GetLatestVersion(type);
            var currentVersion = _runtimeVersionLocator.GetLocateOrNull(type) ?? latestVersion;

            if (documentVersion == currentVersion)
                return;

            if (documentVersion == latestVersion)
                return;

            if (DocumentVersion.Default() == documentVersion)
            {
                SetVersion(instance, currentVersion, latestVersion);
                return;
            }

            throw new VersionViolationException(currentVersion.ToString(), documentVersion, latestVersion);
        }

        public DocumentVersion DetermineLastVersion(DocumentVersion version, List<IMigration> migrations,
            int currentMigration)
        {
            if (migrations.Last() != migrations[currentMigration])
                return migrations[currentMigration + 1].Version;
            return version;
        }

        private DocumentVersion? GetCurrentVersion(Type type)
        {
            return _runtimeVersionLocator.GetLocateOrNull(type);
        }

        private static void SetVersion<TClass>(
            TClass instance,
            DocumentVersion? currentVersion,
            DocumentVersion latestVersion) where TClass : class, IDocument
        {
            if (currentVersion < latestVersion)
            {
                instance.Version = currentVersion.ToString();
                return;
            }

            instance.Version = latestVersion;
        }
    }
}