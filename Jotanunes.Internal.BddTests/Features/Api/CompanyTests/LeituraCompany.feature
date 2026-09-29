# language: pt-BR
Funcionalidade: Company - Leitura

Como membro da equipe da Jotanunes
Quero consultar as empresas fornecedoras cadastradas
Para acompanhar a situação de cada uma

Contexto:
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81"
    E que existe outra empresa cadastrada

Cenário: Listar empresas filtrando pelo CNPJ
    Quando eu listar as empresas filtrando pelo CNPJ "11.222.333/0001-81"
    Então eu recebo uma resposta 200 OK
    E a listagem de empresas contém apenas a empresa cadastrada

Cenário: Consultar uma empresa pelo identificador
    Quando eu consultar a empresa cadastrada
    Então eu recebo uma resposta 200 OK
    E os dados retornados devem ser da empresa cadastrada

Cenário: Consultar uma empresa inexistente
    Quando eu consultar uma empresa inexistente
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Empresa não encontrada"
