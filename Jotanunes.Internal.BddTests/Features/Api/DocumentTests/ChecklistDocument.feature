# language: pt-BR
Funcionalidade: Document - Checklist de conformidade

Como membro da equipe da Jotanunes
Quero ver o checklist de documentos de uma solicitação
Para saber o que foi entregue e o que ainda falta

Contexto:
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81"
    E que existe uma obra cadastrada
    E que existe uma solicitação de material aberta para a empresa e a obra cadastradas

Cenário: Consultar o checklist de uma solicitação sem documentos enviados
    Quando eu consultar o checklist da solicitação cadastrada
    Então eu recebo uma resposta 200 OK
    E o checklist deve listar 10 documentos de habilitação sem nenhum atendido

Cenário: Consultar o checklist com um documento de habilitação aprovado
    Dado que existe um documento de habilitação aprovado enviado pela empresa cadastrada
    Quando eu consultar o checklist da solicitação cadastrada
    Então eu recebo uma resposta 200 OK
    E o item Cartão de CNPJ do checklist deve estar aprovado

Cenário: Consultar o checklist de uma solicitação inexistente
    Quando eu consultar o checklist de uma solicitação inexistente
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Solicitação não encontrada"
