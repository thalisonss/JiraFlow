using System.Text;
using System.Text.Json;
using JiraFlow.Application.Models;
using JiraFlow.Application.Ports;

namespace JiraFlow.Infrastructure.Jira;

/// <summary>
/// Adaptador: implementa IJiraGateway falando HTTP com a API REST do Jira Cloud.
/// Recebe o HttpClient já configurado (URL base e autenticação).
/// </summary>
public sealed class JiraHttpGateway : IJiraGateway
{
    private readonly HttpClient _client;

    public JiraHttpGateway(HttpClient client)
    {
        _client = client;
    }

    public async Task<Resultado<string>> AutenticarAsync()
    {
        using var resposta = await _client.GetAsync("/rest/api/3/myself");
        if (!resposta.IsSuccessStatusCode)
            return Resultado<string>.Falha((int)resposta.StatusCode, await resposta.Content.ReadAsStringAsync());

        using var doc = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        var accountId = doc.RootElement.GetProperty("accountId").GetString();
        return Resultado<string>.Ok((int)resposta.StatusCode, accountId);
    }

    public async Task<IReadOnlyList<string>> BuscarChavesAsync(string jql)
    {
        var chaves = new List<string>();
        string? proximoToken = null;

        do
        {
            var corpo = new
            {
                jql,
                fields = new[] { "status" },
                maxResults = 100,
                nextPageToken = proximoToken
            };

            using var resposta = await _client.PostAsync("/rest/api/3/search/jql", Json(corpo));
            resposta.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
            var raiz = doc.RootElement;

            foreach (var issue in raiz.GetProperty("issues").EnumerateArray())
                chaves.Add(issue.GetProperty("key").GetString()!);

            proximoToken = raiz.TryGetProperty("nextPageToken", out var token)
                ? token.GetString()
                : null;
        }
        while (proximoToken != null);

        return chaves;
    }

    public async Task<Resultado<string>> BuscarTransicaoAsync(string chave, string statusDestino)
    {
        using var resposta = await _client.GetAsync($"/rest/api/3/issue/{chave}/transitions");
        if (!resposta.IsSuccessStatusCode)
            return Resultado<string>.Falha((int)resposta.StatusCode, await resposta.Content.ReadAsStringAsync());

        using var doc = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());

        foreach (var transicao in doc.RootElement.GetProperty("transitions").EnumerateArray())
        {
            var nomeDestino = transicao.GetProperty("to").GetProperty("name").GetString();
            if (nomeDestino != null && nomeDestino.Equals(statusDestino, StringComparison.OrdinalIgnoreCase))
                return Resultado<string>.Ok((int)resposta.StatusCode, transicao.GetProperty("id").GetString());
        }

        return Resultado<string>.Ok((int)resposta.StatusCode, null);
    }

    public async Task<Resultado> MoverAsync(string chave, string transicaoId)
    {
        var corpo = new { transition = new { id = transicaoId } };
        using var resposta = await _client.PostAsync($"/rest/api/3/issue/{chave}/transitions", Json(corpo));
        return await ParaResultadoAsync(resposta);
    }

    public async Task<Resultado> AtribuirAsync(string chave, string accountId)
    {
        var corpo = new { accountId };
        using var resposta = await _client.PutAsync($"/rest/api/3/issue/{chave}/assignee", Json(corpo));
        return await ParaResultadoAsync(resposta);
    }

    // Endpoint do Service Desk: aceita texto simples e define a visibilidade de forma explícita.
    // public = true -> comentário visível ao cliente; public = false -> nota interna.
    public async Task<Resultado> ComentarAsync(string chave, string texto, bool interno)
    {
        var corpo = new { body = texto, @public = !interno };
        using var resposta = await _client.PostAsync($"/rest/servicedeskapi/request/{chave}/comment", Json(corpo));
        return await ParaResultadoAsync(resposta);
    }

    private static StringContent Json(object corpo) =>
        new(JsonSerializer.Serialize(corpo), Encoding.UTF8, "application/json");

    private static async Task<Resultado> ParaResultadoAsync(HttpResponseMessage resposta) =>
        resposta.IsSuccessStatusCode
            ? Resultado.Ok((int)resposta.StatusCode)
            : Resultado.Falha((int)resposta.StatusCode, await resposta.Content.ReadAsStringAsync());
}
