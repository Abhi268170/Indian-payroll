using Xunit;

namespace Payroll.Api.Tests.Infrastructure;

[CollectionDefinition("Integration")]
public sealed class IntegrationCollection
    : ICollectionFixture<PostgresFixture>,
      ICollectionFixture<RedisFixture>
{
}
