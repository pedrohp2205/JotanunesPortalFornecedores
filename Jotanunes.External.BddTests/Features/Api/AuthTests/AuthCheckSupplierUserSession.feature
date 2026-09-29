# language: pt-BR
Funcionalidade: Auth Check - SupplierUserSession

Como um usuário do sistema
Quero ter a segurança de que os endpoints da sessão exigem autenticação
Para garantir que apenas fornecedores autenticados consultem e alterem o próprio acesso

Contexto:
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81"

Esquema do Cenário: Verificar acesso aos endpoints da sessão quando o usuário não está autenticado
    Dado que eu não estou autenticado
    Quando <executarAcao>
    Então eu recebo uma resposta 401 Unauthorized

    Exemplos:
        | executarAcao                                                    |
        | eu solicitar os dados do usuário autenticado                    |
        | eu encerrar a minha sessão                                      |
        | eu trocar a minha senha informando a senha atual "Senha@1234"   |

Esquema do Cenário: Permitir os endpoints da sessão ao fornecedor com senha provisória
    Dado que eu estou autenticado com senha provisória
    Quando <executarAcao>
    Então eu recebo uma resposta <respostaEsperada>

    Exemplos:
        | executarAcao                                                    | respostaEsperada |
        | eu solicitar os dados do usuário autenticado                    | 200 OK           |
        | eu encerrar a minha sessão                                      | 204 No Content   |
        | eu trocar a minha senha informando a senha atual "Senha@1234"   | 200 OK           |
