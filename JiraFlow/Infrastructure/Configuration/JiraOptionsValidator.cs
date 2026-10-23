namespace JiraFlow.Infrastructure.Configuration;

public static class JiraOptionsValidator
{
    /// <summary>Devolve a lista de problemas encontrados. Lista vazia = configuração válida.</summary>
    public static IReadOnlyList<string> Validar(JiraOptions opcoes)
    {
        var erros = new List<string>();

        if (!Uri.TryCreate(opcoes.Url, UriKind.Absolute, out var url) || url.Scheme != Uri.UriSchemeHttps)
            erros.Add("Jira:Url ausente ou inválida (use uma URL https completa).");

        if (string.IsNullOrWhiteSpace(opcoes.Email))
            erros.Add("Jira:Email não informado.");

        if (string.IsNullOrWhiteSpace(opcoes.ApiToken))
            erros.Add("Jira:ApiToken não informado. Defina a variável de ambiente JIRAFLOW_Jira__ApiToken e reinicie o terminal/Visual Studio.");

        if (string.IsNullOrWhiteSpace(opcoes.Jql))
            erros.Add("Jira:Jql não informada.");

        if (string.IsNullOrWhiteSpace(opcoes.TargetStatus))
            erros.Add("Jira:TargetStatus não informado.");

        return erros;
    }
}
