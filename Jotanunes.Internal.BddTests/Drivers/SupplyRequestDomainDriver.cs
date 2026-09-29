using Jotanunes.Application.DTOs.SupplyRequests;
using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;

namespace Jotanunes.Internal.BddTests.Drivers;

public class SupplyRequestDomainDriver
{
    public static SupplierType TipoFornecimento(string descricao)
    {
        return descricao switch
        {
            "material" => SupplierType.Material,
            "mão de obra" => SupplierType.ManpowerLabor,
            "material e mão de obra" => SupplierType.Material | SupplierType.ManpowerLabor,
            _ => throw new ArgumentException($"Tipo de fornecimento '{descricao}' não é reconhecido")
        };
    }

    public static SupplyRequest CriarSolicitacaoValida(long companyId, long workSiteId, SupplierType tipoFornecimento)
    {
        return new SupplyRequest(
            companyId,
            workSiteId,
            tipoFornecimento,
            tipoFornecimento == SupplierType.ManpowerLabor ? 3 : null);
    }

    public static SupplyRequestCreateDto CriarSupplyRequestCreateDto(long companyId, long workSiteId, SupplierType tipoFornecimento)
    {
        return new SupplyRequestCreateDto
        {
            CompanyId = companyId,
            WorkSiteId = workSiteId,
            SupplierType = tipoFornecimento,
            RequiredWorkerCount = tipoFornecimento == SupplierType.ManpowerLabor ? 3 : null
        };
    }
}
