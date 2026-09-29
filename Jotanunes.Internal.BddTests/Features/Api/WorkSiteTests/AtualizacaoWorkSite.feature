# language: pt-BR
Funcionalidade: WorkSite - Atualização

Como membro da equipe da Jotanunes
Quero atualizar os dados de uma obra
Para ajustar o nome e o período de renovação dos documentos

Contexto:
    Dado que existe uma obra cadastrada

Cenário: Atualizar a obra
    Quando eu atualizar a obra cadastrada para "Residencial Aurora - Fase 2" com renovação a cada 15 dias
    Então eu recebo uma resposta 200 OK
    E a obra retornada deve ter o nome "Residencial Aurora - Fase 2" e renovação a cada 15 dias

Cenário: Não permitir atualizar a obra com período de renovação zerado
    Quando eu atualizar a obra cadastrada para "Residencial Aurora" com renovação a cada 0 dias
    Então eu recebo uma resposta 400 Bad Request

Cenário: Atualizar uma obra inexistente
    Quando eu atualizar uma obra inexistente
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Obra não encontrada"
