namespace DraftPuck.Tests.Infrastructure;

public class MappingConfigurationTests
{
    [Fact]
    public void EveryDtoPropertyIsMappedOrExplicitlyIgnored()
    {
        // Fails when a DTO gains a property no profile maps, before it reaches the client as a silent default.
        TestServices.MapperConfiguration.AssertConfigurationIsValid();
    }
}
