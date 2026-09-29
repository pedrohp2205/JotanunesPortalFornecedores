# language: pt-BR
Funcionalidade: Company - Cadastro

Como membro da equipe da Jotanunes
Quero cadastrar empresas fornecedoras
Para que elas possam receber acesso ao Portal do Fornecedor

Cenário: Cadastrar uma empresa com dados válidos
    Quando eu cadastrar a empresa com CNPJ "11.222.333/0001-81"
    Então eu recebo uma resposta 200 OK
    E a empresa cadastrada deve ter o CNPJ "11222333000181"
    E a empresa cadastrada deve estar aguardando documentação

Cenário: Não permitir cadastrar empresa com CNPJ já existente
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81"
    Quando eu cadastrar a empresa com CNPJ "11.222.333/0001-81"
    Então eu recebo uma resposta 400 Bad Request
    E eu recebo uma resposta de erro com a mensagem "Já existe uma empresa cadastrada com este CNPJ."

Cenário: Não permitir cadastrar empresa com CNPJ inválido
    Quando eu cadastrar a empresa com CNPJ "11.222.333/0001-00"
    Então eu recebo uma resposta 400 Bad Request
    E eu recebo uma resposta de erro com a mensagem "CNPJ inválido."
