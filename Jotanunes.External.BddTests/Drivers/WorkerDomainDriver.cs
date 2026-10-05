using Jotanunes.Domain.Entities;

namespace Jotanunes.External.BddTests.Drivers;

public class WorkerDomainDriver
{
    public const string CPF_PADRAO = "529.982.247-25";
    public const string CPF_OUTRO = "111.444.777-35";

    public static Worker CriarTrabalhadorValido(long companyId, string nome = "José da Silva", string cpf = CPF_PADRAO)
    {
        return new Worker(companyId, nome, cpf);
    }
}
