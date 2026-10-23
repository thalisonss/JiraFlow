namespace JiraFlow.Application.Models;

/// <summary>
/// Totais de uma execução. Útil para o log final e para verificar o resultado em testes.
/// </summary>
public sealed record ResumoProcessamento(int Encontrados, int Movidos, int SemTransicao, int Falhas);
