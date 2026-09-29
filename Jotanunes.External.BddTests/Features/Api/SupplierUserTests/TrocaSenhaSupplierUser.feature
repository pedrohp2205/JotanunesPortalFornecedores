# language: pt-BR
Funcionalidade: SupplierUser - Troca de senha

Como fornecedor autenticado
Quero trocar a minha senha
Para substituir a senha provisória recebida da Jotanunes

Contexto:
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81"
    E que eu estou autenticado com senha provisória

Cenário: Trocar a senha provisória libera o acesso ao portal
    Quando eu trocar a minha senha para "NovaSenha@2026"
    Então eu recebo uma resposta 200 OK
    E o usuário do novo token não deve mais precisar trocar a senha
    E com o novo token eu consigo listar as solicitações da minha empresa

Cenário: Não permitir trocar a senha informando a senha atual incorreta
    Quando eu trocar a minha senha informando a senha atual "senha-errada"
    Então eu recebo uma resposta 400 Bad Request
    E eu recebo uma resposta de erro com a mensagem "Senha atual incorreta."

Cenário: Não permitir que a nova senha seja igual à atual
    Quando eu trocar a minha senha para a senha atual
    Então eu recebo uma resposta 400 Bad Request
    E eu recebo uma resposta de erro com a mensagem "A nova senha deve ser diferente da senha atual."

Cenário: Não permitir nova senha curta
    Quando eu trocar a minha senha para "curta"
    Então eu recebo uma resposta 400 Bad Request
