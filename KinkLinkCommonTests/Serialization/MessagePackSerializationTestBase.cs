using System.Reflection;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using MessagePack;
using MessagePack.Resolvers;
using Xunit;

namespace KinkLinkCommonTests.Serialization;

public abstract class MessagePackSerializationTestBase
{
    protected static readonly MessagePackSerializerOptions MessagePackOptions =
        MessagePackSerializerOptions.Standard
            .WithSecurity(MessagePackSecurity.UntrustedData)
            .WithResolver(ContractlessStandardResolver.Instance);

    protected static byte[] Serialize<T>(T obj)
    {
        return MessagePackSerializer.Serialize(obj, MessagePackOptions);
    }

    protected static T Deserialize<T>(byte[] data)
    {
        return MessagePackSerializer.Deserialize<T>(data, MessagePackOptions)
               ?? throw new InvalidOperationException("Deserialization returned null");
    }

    protected static void AssertRoundTrip<T>(T original)
    {
        var data = Serialize(original);
        var deserialized = Deserialize<T>(data);
        AssertEquivalent(original, deserialized);
    }

    protected static void AssertEquivalent<T>(T expected, T actual)
    {
        if (expected == null && actual == null)
            return;

        Assert.NotNull(expected);
        Assert.NotNull(actual);

        foreach (var property in typeof(T).GetProperties())
        {
            var expectedValue = property.GetValue(expected);
            var actualValue = property.GetValue(actual);

            if (property.PropertyType == typeof(DateTime))
            {
                Assert.Equal(
                    ((DateTime)expectedValue).TrimPrecision(),
                    ((DateTime)actualValue).TrimPrecision()
                );
            }
            else
            {
                Assert.Equal(expectedValue, actualValue);
            }
        }
    }

    protected static UserPermissions CreateFullPermissions()
    {
        return new UserPermissions(
            uid: "test-uid-123",
            expires: DateTime.UtcNow.AddDays(7),
            priority: RelationshipPriority.Serious,
            controlsPerm: true,
            controlsConfig: true,
            disableSafeword: false,
            interactions: (int)(InteractionPerms.CanApplyGag | InteractionPerms.CanLockGag | InteractionPerms.CanApplyWardrobe)
        );
    }

    protected static UserPermissions CreateMinimalPermissions()
    {
        return new UserPermissions(
            uid: "min-uid",
            expires: null,
            priority: RelationshipPriority.Casual,
            controlsPerm: false,
            controlsConfig: false,
            disableSafeword: false,
            interactions: 0
        );
    }

    protected static UserPermissions CreateExpiredPermissions()
    {
        return new UserPermissions(
            uid: "expired-uid",
            expires: DateTime.UtcNow.AddDays(-1),
            priority: RelationshipPriority.Devotional,
            controlsPerm: true,
            controlsConfig: true,
            disableSafeword: true,
            interactions: int.MaxValue
        );
    }

    protected static byte[] CreateInvalidMessagePack()
    {
        return new byte[] { 0xFF, 0xFE, 0xFD, 0xFC };
    }

    protected static byte[] CreateMessagePackWithWrongType<TWrong, TExpected>()
    {
        var wrongObj = CreateSampleObject<TWrong>();
        var data = Serialize(wrongObj);
        return data;
    }

    private static T CreateSampleObject<T>()
    {
        return (T)typeof(T).GetConstructors()[0].Invoke(new object[]
        {
            Guid.NewGuid().ToString(),
            true,
            "TestAlias",
            RelationshipPriority.Serious,
            true,
            DateTime.UtcNow.AddDays(1),
            "password"
        });
    }
}

public static class DateTimeExtensions
{
    public static DateTime TrimPrecision(this DateTime dt)
    {
        return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second, dt.Kind);
    }
}