# language: pt-BR
Funcionalidade: Document - Checklist de conformidade

Como fornecedor autenticado
Quero ver o checklist de documentos de uma solicitação da minha empresa
Para saber o que já entreguei e o que ainda falta

Contexto:
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81"
    E que existe outra empresa cadastrada
    E que existe uma obra cadastrada
    E que existe uma solicitação de material aberta para a empresa e a obra cadastradas
    E que existe uma solicitação de material aberta para a outra empresa na obra cadastrada
    E que eu estou autenticado com senha definitiva

Cenário: Consultar o checklist de uma solicitação da minha empresa
    Dado que existe um documento de habilitação aprovado enviado pela empresa cadastrada
    Quando eu consultar o checklist da solicitação cadastrada
    Então eu recebo uma resposta 200 OK
    E o checklist deve ser da solicitação cadastrada com o Cartão de CNPJ aprovado

Cenário: Não permitir consultar o checklist da solicitação de outra empresa
    Quando eu consultar o checklist da solicitação da outra empresa
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Solicitação não encontrada"
