# language: pt-BR
Funcionalidade: SupplyRequest - Leitura

Como fornecedor autenticado
Quero consultar as solicitações de fornecimento da minha empresa
Para saber quais documentos preciso enviar

Contexto:
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81"
    E que existe outra empresa cadastrada
    E que existe uma obra cadastrada
    E que existe uma solicitação de material aberta para a empresa e a obra cadastradas
    E que existe uma solicitação de material aberta para a outra empresa na obra cadastrada
    E que eu estou autenticado com senha definitiva

Cenário: Listar apenas as solicitações da minha empresa
    Quando eu listar as solicitações da minha empresa
    Então eu recebo uma resposta 200 OK
    E a listagem de solicitações contém apenas a solicitação da minha empresa

Cenário: Consultar uma solicitação da minha empresa
    Quando eu consultar a solicitação cadastrada
    Então eu recebo uma resposta 200 OK
    E a solicitação retornada deve ser a solicitação cadastrada com as pendências de documentação

Cenário: Não permitir consultar a solicitação de outra empresa
    Quando eu consultar a solicitação da outra empresa
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Solicitação não encontrada"

Cenário: Consultar uma solicitação inexistente
    Quando eu consultar uma solicitação inexistente
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Solicitação não encontrada"
