using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Exceptions;

namespace Jotanunes.Tests;

public class SupplyRequestTest
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
}
