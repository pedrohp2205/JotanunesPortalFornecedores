# language: pt-BR
Funcionalidade: Document - Download

Como membro da equipe da Jotanunes
Quero baixar o arquivo de um documento enviado
Para conferir o conteúdo antes de avaliá-lo

Cenário: Baixar o arquivo de um documento
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81"
    E que existe um documento de habilitação pendente enviado pela empresa cadastrada
    E que o arquivo do documento cadastrado está disponível no armazenamento
    Quando eu baixar o arquivo do documento cadastrado
    Então eu recebo uma resposta 200 OK
    E o arquivo recebido deve ser o arquivo armazenado

Cenário: Baixar o arquivo de um documento inexistente
    Quando eu baixar o arquivo de um documento inexistente
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Documento não encontrado"
