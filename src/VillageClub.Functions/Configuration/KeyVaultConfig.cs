using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using System.Threading.Tasks;

namespace VillageClub.Functions.Configuration
{
    public static class KeyVaultConfig
    {
        public static IConfigurationBuilder AddKeyVaultConfiguration(this IConfigurationBuilder builder, string keyVaultName)
        {
            if (string.IsNullOrWhiteSpace(keyVaultName))
            {
                return builder;
            }

            var keyVaultUri = $"https://{keyVaultName}.vault.azure.net/";
            var credential = new DefaultAzureCredential();

            async Task<string> GetTokenAsync(string authority, string resource, string scope)
            {
                var token = await credential.GetTokenAsync(
                    new TokenRequestContext(new[] { "https://vault.azure.net/.default" }));
                return token.Token;
            }

            var client = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(GetTokenAsync));
            return builder.AddAzureKeyVault(keyVaultUri, client, new DefaultKeyVaultSecretManager());
        }
    }
}