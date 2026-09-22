using Jotanunes.Domain.Entities;

namespace Jotanunes.Application.Interfaces;

// Notificações por e-mail ao fornecedor. Best-effort: falhas de envio são logadas e nunca propagadas.
public interface ISupplierNotificationService
{
    Task DocumentApproved(Document document);
    Task DocumentRejected(Document document);
    Task CompanyEligible(Company company);
    Task SupplyRequestCreated(SupplyRequest supplyRequest);
    Task SupplyRequestCompleted(SupplyRequest supplyRequest);
    Task SupplyRequestCancelled(SupplyRequest supplyRequest);
    Task Welcome(SupplierUser user, string temporaryPassword);
    Task TemporaryPasswordIssued(SupplierUser user, string temporaryPassword);
    Task PasswordResetRequested(SupplierUser user, string token);
}
