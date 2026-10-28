using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Exceptions;

namespace Jotanunes.Tests.Domain.Entities;

public class SupplyRequestTests
{
    private static SupplyRequest ManpowerRequest(int? requiredWorkerCount = 30)
    {
        return new SupplyRequest(1, 1, SupplierType.ManpowerLabor, requiredWorkerCount);
    }

    [Fact]
    public void Should_Create_Open_Request_With_Required_Worker_Count()
    {
        var request = ManpowerRequest(30);

        Assert.Equal(SupplyRequestStatus.Open, request.Status);
        Assert.Equal(SupplierType.ManpowerLabor, request.SupplierType);
        Assert.Equal(30, request.RequiredWorkerCount);
        Assert.Null(request.ClosedAt);
    }

    [Fact]
    public void Should_Create_Manpower_Request_Without_Required_Worker_Count()
    {
        var request = ManpowerRequest(null);

        Assert.Null(request.RequiredWorkerCount);
    }

    [Fact]
    public void Should_Create_Material_Request_Without_Required_Worker_Count()
    {
        var request = new SupplyRequest(1, 1, SupplierType.Material);

        Assert.Equal(SupplierType.Material, request.SupplierType);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void Should_Throw_Exception_When_Supplier_Type_Is_Not_A_Single_Type(int value)
    {
        var ex = Assert.Throws<JotanunesException>(() => new SupplyRequest(1, 1, (SupplierType)value));
        Assert.Equal("Tipo de fornecimento da solicitação deve ser Material ou Mão de obra.", ex.Message);
    }

    [Fact]
    public void Should_Throw_Exception_When_Required_Worker_Count_Is_Negative()
    {
        var ex = Assert.Throws<JotanunesException>(() => ManpowerRequest(-1));
        Assert.Equal("Quantidade de trabalhadores necessária não pode ser negativa.", ex.Message);
    }

    [Fact]
    public void Should_Throw_Exception_When_Material_Request_Has_Required_Worker_Count()
    {
        var ex = Assert.Throws<JotanunesException>(() => new SupplyRequest(1, 1, SupplierType.Material, 5));
        Assert.Equal("Quantidade de trabalhadores necessária só se aplica a solicitações de mão de obra.", ex.Message);
    }

    [Fact]
    public void Should_Update_Required_Worker_Count()
    {
        var request = ManpowerRequest(30);

        request.UpdateRequiredWorkerCount(25);

        Assert.Equal(25, request.RequiredWorkerCount);
    }

    [Fact]
    public void Should_Move_To_InProgress_Once_And_Stay_There()
    {
        var request = ManpowerRequest();

        request.MarkInProgress();
        request.MarkInProgress();

        Assert.Equal(SupplyRequestStatus.InProgress, request.Status);
    }

    [Fact]
    public void Should_Complete_And_Register_Closing_Date()
    {
        var request = ManpowerRequest();
        request.MarkInProgress();

        request.Complete();

        Assert.Equal(SupplyRequestStatus.Completed, request.Status);
        Assert.True(request.IsClosed);
        Assert.NotNull(request.ClosedAt);
    }

    [Fact]
    public void Should_Cancel_An_Open_Request()
    {
        var request = ManpowerRequest();

        request.Cancel();

        Assert.Equal(SupplyRequestStatus.Cancelled, request.Status);
        Assert.NotNull(request.ClosedAt);
    }

    [Fact]
    public void Should_Not_Accept_Changes_After_Closed()
    {
        var request = ManpowerRequest();
        request.Complete();

        const string message = "Solicitação encerrada não aceita alterações nem novos documentos.";

        Assert.Equal(message, Assert.Throws<JotanunesException>(() => request.Cancel()).Message);
        Assert.Equal(message, Assert.Throws<JotanunesException>(() => request.Complete()).Message);
        Assert.Equal(message, Assert.Throws<JotanunesException>(() => request.MarkInProgress()).Message);
        Assert.Equal(message, Assert.Throws<JotanunesException>(() => request.UpdateRequiredWorkerCount(10)).Message);
        Assert.Equal(message, Assert.Throws<JotanunesException>(() => request.EnsureNotClosed()).Message);
    }

    private static DocumentType Type(long id, SupplierType appliesTo = SupplierType.ManpowerLabor)
    {
        return new DocumentType($"COD{id}", $"Documento {id}", DocumentCategory.Recurring, appliesTo, DocumentSubject.Company) { Id = id };
    }

    [Fact]
    public void Should_Set_The_Required_Documents()
    {
        var request = ManpowerRequest();

        request.SetRequiredDocuments([Type(10), Type(20)]);

        Assert.Equal(new long[] { 10, 20 }, request.RequiredDocuments.Select(r => r.DocumentTypeId));
    }

    [Fact]
    public void Should_Replace_The_Whole_Selection_Keeping_What_Stayed()
    {
        var request = ManpowerRequest();
        request.SetRequiredDocuments([Type(10), Type(20)]);
        var kept = request.RequiredDocuments.Single(r => r.DocumentTypeId == 10);

        request.SetRequiredDocuments([Type(10), Type(30)]);

        Assert.Equal(new long[] { 10, 30 }, request.RequiredDocuments.Select(r => r.DocumentTypeId));
        Assert.Same(kept, request.RequiredDocuments.Single(r => r.DocumentTypeId == 10));
    }

    [Fact]
    public void Should_Ignore_Repeated_Document_Types()
    {
        var request = ManpowerRequest();

        request.SetRequiredDocuments([Type(10), Type(10)]);

        Assert.Single(request.RequiredDocuments);
    }

    [Fact]
    public void Should_Not_Require_A_Document_Type_From_Another_Supplier_Type()
    {
        var request = ManpowerRequest();

        var ex = Assert.Throws<JotanunesException>(() => request.SetRequiredDocuments([Type(10, SupplierType.Material)]));

        Assert.Equal("O tipo de documento \"Documento 10\" não se aplica a este tipo de fornecimento.", ex.Message);
    }

    [Fact]
    public void Should_Not_Require_An_Inactive_Document_Type()
    {
        var request = ManpowerRequest();
        var type = Type(10);
        type.Deactivate();

        var ex = Assert.Throws<JotanunesException>(() => request.SetRequiredDocuments([type]));

        Assert.Equal("O tipo de documento \"Documento 10\" está inativo.", ex.Message);
    }

    [Fact]
    public void Should_Not_Change_The_Selection_Of_A_Closed_Request()
    {
        var request = ManpowerRequest();
        request.Complete();

        Assert.Equal(
            "Solicitação encerrada não aceita alterações nem novos documentos.",
            Assert.Throws<JotanunesException>(() => request.SetRequiredDocuments([Type(10)])).Message);
    }
}
