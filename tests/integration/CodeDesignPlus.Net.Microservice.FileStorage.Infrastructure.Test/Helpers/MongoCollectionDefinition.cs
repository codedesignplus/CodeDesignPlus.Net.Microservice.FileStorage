namespace CodeDesignPlus.Net.Microservice.FileStorage.Infrastructure.Test.Helpers;

/// <summary>
/// Shares a single Mongo container among all the repository tests.
/// </summary>
[CollectionDefinition(MongoContainerFixture.Collection)]
public class MongoCollectionDefinition : ICollectionFixture<MongoContainerFixture>
{
}
