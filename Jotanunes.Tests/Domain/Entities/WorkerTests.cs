using Jotanunes.Domain.Entities;
using Jotanunes.Domain.Enums;
using Jotanunes.Domain.Exceptions;

namespace Jotanunes.Tests.Domain.Entities;

public class WorkerTests
{
    private static Worker Cicero(long companyId = 1) => new(companyId, " Cicero Fernandes da Silva ", "529.982.247-25") { Id = 7 };

    private static SupplyRequest ManpowerRequest(int? requiredWorkerCount = 2) =>
        new(1, 1, SupplierType.ManpowerLabor, requiredWorkerCount) { Id = 10 };

    [Fact]
    public void Should_Create_Active_Worker_Normalizing_Cpf_And_Name()
    {
        var worker = Cicero();

        Assert.Equal("Cicero Fernandes da Silva", worker.Name);
        Assert.Equal("52998224725", worker.Cpf);
        Assert.True(worker.Active);
    }

    [Theory]
    [InlineData("", "529.982.247-25", "Nome do trabalhador não pode ser vazio.")]
    [InlineData("Cicero", "", "CPF do trabalhador não pode ser vazio.")]
    [InlineData("Cicero", "111.111.111-11", "CPF do trabalhador inválido.")]
    public void Should_Throw_Exception_When_Worker_Data_Is_Invalid(string name, string cpf, string message)
    {
        var ex = Assert.Throws<JotanunesException>(() => new Worker(1, name, cpf));

        Assert.Equal(message, ex.Message);
    }

    [Fact]
    public void Should_Allocate_Worker_In_Manpower_Request()
    {
        var allocation = new WorkerAllocation(ManpowerRequest(), Cicero(), activeAllocationCount: 1);

        Assert.True(allocation.IsActive);
        Assert.Equal(7, allocation.WorkerId);
        Assert.Equal(10, allocation.SupplyRequestId);
    }

    [Fact]
    public void Should_Not_Allocate_Beyond_Required_Worker_Count()
    {
        var ex = Assert.Throws<JotanunesException>(() => new WorkerAllocation(ManpowerRequest(2), Cicero(), activeAllocationCount: 2));

        Assert.Equal("A solicitação já tem os 2 trabalhadores necessários alocados. Desaloque um antes de alocar outro.", ex.Message);
    }

    [Fact]
    public void Should_Allocate_Without_Limit_When_Required_Worker_Count_Is_Not_Set()
    {
        var allocation = new WorkerAllocation(ManpowerRequest(null), Cicero(), activeAllocationCount: 50);

        Assert.True(allocation.IsActive);
    }

    [Fact]
    public void Should_Not_Allocate_In_Material_Request()
    {
        var request = new SupplyRequest(1, 1, SupplierType.Material) { Id = 10 };

        var ex = Assert.Throws<JotanunesException>(() => new WorkerAllocation(request, Cicero(), 0));

        Assert.Equal("Só é possível alocar trabalhadores em solicitações de mão de obra.", ex.Message);
    }

    [Fact]
    public void Should_Not_Allocate_In_Closed_Request()
    {
        var request = ManpowerRequest();
        request.Complete();

        Assert.Throws<JotanunesException>(() => new WorkerAllocation(request, Cicero(), 0));
    }

    [Fact]
    public void Should_Not_Allocate_Worker_From_Another_Company()
    {
        var ex = Assert.Throws<JotanunesException>(() => new WorkerAllocation(ManpowerRequest(), Cicero(companyId: 2), 0));

        Assert.Equal("Trabalhador não pertence à empresa da solicitação.", ex.Message);
    }

    [Fact]
    public void Should_Not_Allocate_Inactive_Worker()
    {
        var worker = Cicero();
        worker.Deactivate();

        var ex = Assert.Throws<JotanunesException>(() => new WorkerAllocation(ManpowerRequest(), worker, 0));

        Assert.Equal("Trabalhador inativo não pode ser alocado.", ex.Message);
    }

    [Fact]
    public void Should_Release_Allocation_Only_Once()
    {
        var allocation = new WorkerAllocation(ManpowerRequest(), Cicero(), 0);

        allocation.Release();

        Assert.False(allocation.IsActive);
        Assert.NotNull(allocation.ReleasedAt);
        Assert.Throws<JotanunesException>(() => allocation.Release());
    }
}
