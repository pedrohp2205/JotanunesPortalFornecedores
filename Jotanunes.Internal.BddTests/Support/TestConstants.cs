namespace Jotanunes.Internal.BddTests.Support;

internal static class TestConstants
{
    internal const long ID_INEXISTENTE = -1;

    internal const string SENHA_PADRAO = "Senha@1234";

    internal const string CNPJ_EMPRESA = "11.222.333/0001-81";
    internal const string CNPJ_OUTRA_EMPRESA = "11.444.777/0001-61";

    internal const long TIPO_DOCUMENTO_CARTAO_CNPJ = 1;
    internal const long TIPO_DOCUMENTO_RG_CPF_SOCIOS = 10;
    internal const int TOTAL_TIPOS_DOCUMENTO_CADASTRADOS = 20;

    internal static readonly long[] TIPOS_DOCUMENTO_HABILITACAO_OBRIGATORIOS = [1, 2, 3, 4, 5, 10];
}
