# language: pt-BR
Funcionalidade: Document - Leitura

Como fornecedor autenticado
Quero consultar os documentos que a minha empresa enviou
Para acompanhar a avaliação da Jotanunes

Contexto:
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81"
    E que existe outra empresa cadastrada
    E que eu estou autenticado com senha definitiva
    E que existe um documento de habilitação pendente enviado pela empresa cadastrada
    E que existe um documento de habilitação enviado pela outra empresa

Cenário: Listar apenas os documentos da minha empresa
    Quando eu listar os documentos da minha empresa
    Então eu recebo uma resposta 200 OK
    E a listagem de documentos contém apenas o documento da minha empresa

Cenário: Consultar um documento da minha empresa
    Quando eu consultar o documento cadastrado
    Então eu recebo uma resposta 200 OK
    E o documento retornado deve ser o documento cadastrado

Cenário: Não permitir consultar o documento de outra empresa
    Quando eu consultar o documento da outra empresa
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Documento não encontrado"

Cenário: Não expor a análise automática ao fornecedor
    Dado que o documento cadastrado tem uma análise não conforme
    Quando eu listar os documentos da minha empresa
    Então eu recebo uma resposta 200 OK
    E a listagem de documentos contém apenas o documento da minha empresa
    E a resposta não contém o parecer da análise automática

Cenário: Ignorar o filtro por veredito da análise na frente do fornecedor
    Dado que o documento cadastrado tem uma análise não conforme
    Quando eu listar os documentos da minha empresa filtrando pelo veredito da análise
    Então eu recebo uma resposta 200 OK
    E a listagem ainda contém o documento cadastrado
    E a resposta não contém o parecer da análise automática

Cenário: Avisar o fornecedor quando o arquivo parece ser outro documento
    Dado que a análise do documento cadastrado identificou outro documento
    Quando eu consultar o documento cadastrado
    Então eu recebo uma resposta 200 OK
    E o documento retornado avisa que o arquivo pode não ser o documento certo
    E a resposta não contém o parecer da análise automática
