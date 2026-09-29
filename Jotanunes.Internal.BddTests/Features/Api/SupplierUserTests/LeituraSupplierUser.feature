# language: pt-BR
Funcionalidade: SupplierUser - Leitura

Como membro da equipe da Jotanunes
Quero consultar os acessos ao portal de uma empresa
Para saber quem pode enviar documentos em nome dela

Cenário: Listar os acessos da empresa
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81"
    E que existe um usuário de acesso "maria@alfa.com.br" ativo para a empresa cadastrada
    Quando eu listar os acessos da empresa cadastrada
    Então eu recebo uma resposta 200 OK
    E a listagem de acessos contém o usuário "maria@alfa.com.br"

Cenário: Listar os acessos de uma empresa inexistente
    Quando eu listar os acessos de uma empresa inexistente
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Empresa não encontrada"
