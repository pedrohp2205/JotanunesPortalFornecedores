namespace Jotanunes.Application.DTOs.SupplyRequests;

public class SupplyRequestDocumentsUpdateDto
{
    // Lista completa: o que não vier aqui deixa de ser exigido do fornecedor.
    public List<long> DocumentTypeIds { get; set; } = [];
}
