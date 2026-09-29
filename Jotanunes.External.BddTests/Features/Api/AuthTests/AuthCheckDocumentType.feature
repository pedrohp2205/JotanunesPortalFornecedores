# language: pt-BR
Funcionalidade: Auth Check - DocumentType

Como um usuário do sistema
Quero ter a segurança de que o acesso aos tipos de documento está sendo corretamente verificado
Para garantir que apenas fornecedores autenticados e com senha definitiva possam consultá-los

Contexto:
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81"

Esquema do Cenário: Verificar acesso ao endpoint de tipos de documento
    Dado que eu <descricaoAutenticacao>
    Quando eu listar os tipos de documento da minha empresa
    Então eu recebo uma resposta <respostaEsperada>

    Exemplos:
        | descricaoAutenticacao                  | respostaEsperada |
        | não estou autenticado                  | 401 Unauthorized |
        | estou autenticado com senha provisória | 403 Forbidden    |
