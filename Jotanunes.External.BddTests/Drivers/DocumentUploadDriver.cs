namespace Jotanunes.External.BddTests.Drivers;

public class DocumentUploadDriver
{
    public static readonly byte[] ArquivoPdf = "%PDF-1.4 documento de teste"u8.ToArray();
    public static readonly byte[] ArquivoTexto = "conteúdo que não é PDF, PNG nem JPEG"u8.ToArray();

    public static MultipartFormDataContent CriarFormulario(
        long documentTypeId,
        byte[] arquivo,
        string nomeArquivo = "documento.pdf",
        long? supplyRequestId = null,
        long? workerId = null)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent(documentTypeId.ToString()), "DocumentTypeId" },
            { new ByteArrayContent(arquivo), "File", nomeArquivo }
        };

        if (supplyRequestId.HasValue)
        {
            form.Add(new StringContent(supplyRequestId.Value.ToString()), "SupplyRequestId");
        }

        if (workerId.HasValue)
        {
            form.Add(new StringContent(workerId.Value.ToString()), "WorkerId");
        }

        return form;
    }
}
