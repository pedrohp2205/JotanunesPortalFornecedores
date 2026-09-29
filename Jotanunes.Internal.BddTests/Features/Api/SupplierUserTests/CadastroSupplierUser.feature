# language: pt-BR
Funcionalidade: SupplierUser - Cadastro

Como membro da equipe da Jotanunes
Quero criar o acesso ao portal para uma empresa fornecedora
Para que ela possa entrar e enviar a documentação

Contexto:
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81"

Cenário: Criar um acesso com senha provisória
    Quando eu criar o acesso "maria@alfa.com.br" com a senha provisória "Provisoria@1" para a empresa cadastrada
    Então eu recebo uma resposta 200 OK
    E o acesso retornado deve estar ativo e exigir a troca de senha

Cenário: Não permitir criar acesso com e-mail já utilizado
    Dado que existe um usuário de acesso "maria@alfa.com.br" ativo para a empresa cadastrada
    Quando eu criar o acesso "maria@alfa.com.br" com a senha provisória "Provisoria@1" para a empresa cadastrada
    Então eu recebo uma resposta 400 Bad Request
    E eu recebo uma resposta de erro com a mensagem "Já existe um usuário cadastrado com este e-mail."

Cenário: Não permitir criar acesso com senha provisória curta
    Quando eu criar o acesso "maria@alfa.com.br" com a senha provisória "curta" para a empresa cadastrada
    Então eu recebo uma resposta 400 Bad Request

Cenário: Criar acesso para uma empresa inexistente
    Quando eu criar o acesso "maria@alfa.com.br" com a senha provisória "Provisoria@1" para uma empresa inexistente
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Empresa não encontrada"
