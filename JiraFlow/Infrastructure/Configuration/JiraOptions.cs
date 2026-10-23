using JiraFlow.Application.Models;

namespace JiraFlow.Infrastructure.Configuration;

/// <summary>
/// Seção "Jira" do appsettings.json. O ApiToken não fica no JSON:
/// vem da variável de ambiente JIRAFLOW_Jira__ApiToken.
/// </summary>
public sealed class JiraOptions
{
    public string? Url { get; set; }
    public string? Email { get; set; }
    public string? ApiToken { get; set; }
    public string? Jql { get; set; }
    public string? TargetStatus { get; set; }
    public bool AssignToMe { get; set; } = true;
    public string? Comment { get; set; }
    public bool CommentInternal { get; set; }

    /// <summary>Converte as opções lidas do JSON nos parâmetros que o caso de uso entende.</summary>
    public ProcessamentoOptions ParaProcessamento() =>
        new(Jql!, TargetStatus!, AssignToMe, Comment, CommentInternal);
}
