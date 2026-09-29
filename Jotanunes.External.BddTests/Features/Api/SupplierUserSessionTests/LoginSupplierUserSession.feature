# language: pt-BR
Funcionalidade: SupplierUserSession - Login

Como fornecedor com acesso ao Portal do Fornecedor
Quero entrar com meu e-mail e senha
Para acessar as solicitações da minha empresa

Contexto:
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81"

Cenário: Entrar com credenciais válidas
    Dado que existe um fornecedor "maria@alfa.com.br" com senha definitiva para a empresa cadastrada
    Quando eu entrar com o e-mail "maria@alfa.com.br" e a senha correta
    Então eu recebo uma resposta 200 OK
    E eu recebo um par de tokens vinculado à empresa cadastrada

Cenário: Entrar com senha provisória indica que a senha deve ser trocada
    Dado que existe um fornecedor "maria@alfa.com.br" com senha provisória para a empresa cadastrada
    Quando eu entrar com o e-mail "maria@alfa.com.br" e a senha correta
    Então eu recebo uma resposta 200 OK
    E o usuário do token deve precisar trocar a senha

Esquema do Cenário: Mesma mensagem para senha incorreta e e-mail inexistente
    Dado que existe um fornecedor "maria@alfa.com.br" com senha definitiva para a empresa cadastrada
    Quando eu entrar com o e-mail "<email>" e a senha "senha-errada"
    Então eu recebo uma resposta 401 Unauthorized
    E eu recebo uma resposta de erro com a mensagem "E-mail ou senha inválidos."

    Exemplos:
        | email               |
        | maria@alfa.com.br   |
        | ninguem@alfa.com.br |

Cenário: Não permitir a entrada de um fornecedor inativo
    Dado que existe um fornecedor "maria@alfa.com.br" inativo para a empresa cadastrada
    Quando eu entrar com o e-mail "maria@alfa.com.br" e a senha correta
    Então eu recebo uma resposta 401 Unauthorized
    E eu recebo uma resposta de erro com a mensagem "Usuário inativo. Procure o responsável da Jotanunes."

Cenário: Bloquear o acesso após tentativas inválidas seguidas
    Dado que existe um fornecedor "maria@alfa.com.br" com senha definitiva para a empresa cadastrada
    E que o fornecedor errou a senha 5 vezes seguidas
    Quando eu entrar com o e-mail "maria@alfa.com.br" e a senha correta
    Então eu recebo uma resposta 401 Unauthorized
    E eu recebo uma resposta de erro com a mensagem "Acesso bloqueado temporariamente por excesso de tentativas. Tente novamente em alguns minutos."
