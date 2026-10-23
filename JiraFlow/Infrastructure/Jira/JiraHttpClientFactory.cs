using System.Net.Http.Headers;
using System.Text;
using JiraFlow.Infrastructure.Configuration;

namespace JiraFlow.Infrastructure.Jira;

public static class JiraHttpClientFactory
{
    /// <summary>Cria o HttpClient com URL base e autenticação Basic. Quem chama deve descartá-lo (using).</summary>
    public static HttpClient Criar(JiraOptions opcoes)
    {
        var credencial = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{opcoes.Email}:{opcoes.ApiToken}"));

        var client = new HttpClient { BaseAddress = new Uri(opcoes.Url!) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credencial);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }
}
