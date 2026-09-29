# language: pt-BR
Funcionalidade: SupplyRequest - Cadastro com nova empresa

Como membro da equipe da Jotanunes
Quero abrir uma solicitação para uma empresa que ainda não está cadastrada
Para cadastrar a empresa, o acesso ao portal e a solicitação de uma só vez

Contexto:
    Dado que existe uma obra cadastrada

Cenário: Abrir uma solicitação cadastrando a empresa e o acesso
    Quando eu abrir uma solicitação de material cadastrando a empresa com CNPJ "11.222.333/0001-81" e o acesso "maria@alfa.com.br"
    Então eu recebo uma resposta 200 OK
    E a solicitação retornada deve estar aberta para a empresa com CNPJ "11.222.333/0001-81"
    E a empresa da solicitação deve ter o acesso "maria@alfa.com.br" exigindo troca de senha

Cenário: Não permitir cadastrar empresa com CNPJ já existente
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81"
    Quando eu abrir uma solicitação de material cadastrando a empresa com CNPJ "11.222.333/0001-81" e o acesso "maria@alfa.com.br"
    Então eu recebo uma resposta 400 Bad Request
    E eu recebo uma resposta de erro com a mensagem "Já existe uma empresa cadastrada com este CNPJ. Abra a solicitação informando o CompanyId da empresa existente."

Cenário: Não permitir cadastrar acesso com e-mail já utilizado
    Dado que existe outra empresa cadastrada
    E que existe um usuário de acesso "maria@alfa.com.br" para a outra empresa
    Quando eu abrir uma solicitação de material cadastrando a empresa com CNPJ "11.222.333/0001-81" e o acesso "maria@alfa.com.br"
    Então eu recebo uma resposta 400 Bad Request
    E eu recebo uma resposta de erro com a mensagem "Já existe um usuário cadastrado com este e-mail."
