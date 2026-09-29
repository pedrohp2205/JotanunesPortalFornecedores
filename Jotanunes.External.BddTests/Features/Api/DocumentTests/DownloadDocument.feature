# language: pt-BR
Funcionalidade: Document - Download

Como fornecedor autenticado
Quero baixar um documento que a minha empresa enviou
Para conferir o arquivo entregue

Contexto:
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81"
    E que existe outra empresa cadastrada
    E que eu estou autenticado com senha definitiva
    E que o arquivo do documento está disponível no armazenamento

Cenário: Baixar um documento da minha empresa
    Dado que existe um documento de habilitação pendente enviado pela empresa cadastrada
    Quando eu baixar o arquivo do documento cadastrado
    Então eu recebo uma resposta 200 OK
    E o arquivo recebido deve ser o arquivo armazenado

Cenário: Não permitir baixar o documento de outra empresa
    Dado que existe um documento de habilitação enviado pela outra empresa
    Quando eu baixar o arquivo do documento da outra empresa
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Documento não encontrado"
