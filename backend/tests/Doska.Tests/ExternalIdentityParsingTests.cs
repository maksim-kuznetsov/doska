using System.Text.Json;
using Doska.Api.Infrastructure;

namespace Doska.Tests;

/// <summary>Разбор ответа профиля провайдера: Google userinfo и mock-ответ login.yandex.ru/info.</summary>
public sealed class ExternalIdentityParsingTests
{
    [Fact]
    public void Yandex_identity_uses_default_email_and_real_name_and_trusts_email()
    {
        using var document = JsonDocument.Parse(
            """
            {
              "id": "1000034426",
              "default_email": "test@yandex.ru",
              "real_name": "Иван Иванов",
              "display_name": "Ivan"
            }
            """);

        var identity = ExternalAuthentication.ParseYandexIdentity(document.RootElement);

        Assert.Equal("1000034426", identity.Subject);
        Assert.Equal("test@yandex.ru", identity.Email);
        Assert.Equal("Иван Иванов", identity.Name);
        Assert.True(identity.EmailVerified);
    }

    [Fact]
    public void Yandex_identity_falls_back_to_emails_array_display_name_and_numeric_id()
    {
        using var document = JsonDocument.Parse(
            """
            {
              "id": 42,
              "emails": ["secondary@yandex.ru", "other@yandex.ru"],
              "display_name": "Ivan"
            }
            """);

        var identity = ExternalAuthentication.ParseYandexIdentity(document.RootElement);

        Assert.Equal("42", identity.Subject);
        Assert.Equal("secondary@yandex.ru", identity.Email);
        Assert.Equal("Ivan", identity.Name);
        Assert.True(identity.EmailVerified);
    }

    [Fact]
    public void Yandex_identity_without_email_yields_null_email()
    {
        using var document = JsonDocument.Parse("""{ "id": "7", "display_name": "Ivan" }""");

        var identity = ExternalAuthentication.ParseYandexIdentity(document.RootElement);

        Assert.Null(identity.Email);
        Assert.True(identity.EmailVerified);
    }

    [Fact]
    public void Google_identity_reads_verified_boolean_and_string()
    {
        using var verified = JsonDocument.Parse(
            """{ "sub": "123", "email": "a@example.com", "name": "A", "email_verified": true }""");
        using var unverified = JsonDocument.Parse(
            """{ "sub": "456", "email": "b@example.com", "email_verified": "false" }""");

        var yes = ExternalAuthentication.ParseGoogleIdentity(verified.RootElement);
        var no = ExternalAuthentication.ParseGoogleIdentity(unverified.RootElement);

        Assert.Equal("123", yes.Subject);
        Assert.True(yes.EmailVerified);
        Assert.Equal("456", no.Subject);
        Assert.False(no.EmailVerified);
    }
}
