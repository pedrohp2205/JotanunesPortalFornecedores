# language: pt-BR
Funcionalidade: WorkSite - Cadastro

Como membro da equipe da Jotanunes
Quero cadastrar as obras
Para abrir solicitações de fornecimento para elas

Cenário: Cadastrar uma obra
    Quando eu cadastrar a obra "Residencial Aurora" com renovação a cada 15 dias
    Então eu recebo uma resposta 200 OK
    E a obra retornada deve ter o nome "Residencial Aurora" e renovação a cada 15 dias

Cenário: Não permitir cadastrar obra com período de renovação zerado
    Quando eu cadastrar a obra "Residencial Aurora" com renovação a cada 0 dias
    Então eu recebo uma resposta 400 Bad Request
    E eu recebo uma resposta de erro com a mensagem "Período de renovação deve ser maior que zero."
