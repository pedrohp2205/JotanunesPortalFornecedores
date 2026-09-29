# language: pt-BR
Funcionalidade: Company - Atualização

Como membro da equipe da Jotanunes
Quero atualizar os dados cadastrais de uma empresa fornecedora
Para manter o cadastro correto

Contexto:
    Dado que existe uma empresa cadastrada com CNPJ "11.222.333/0001-81"

Cenário: Atualizar os dados da empresa
    Quando eu atualizar a empresa cadastrada com a razão social "Construtora Renomeada Ltda"
    Então eu recebo uma resposta 200 OK
    E a empresa retornada deve ter a razão social "Construtora Renomeada Ltda"
    E a empresa retornada deve manter o CNPJ "11222333000181"

Cenário: Não permitir atualizar a empresa com telefone inválido
    Quando eu atualizar a empresa cadastrada com o telefone "123"
    Então eu recebo uma resposta 400 Bad Request
    E eu recebo uma resposta de erro com a mensagem "Telefone deve ter 10 ou 11 dígitos (com DDD)."

Cenário: Atualizar uma empresa inexistente
    Quando eu atualizar uma empresa inexistente
    Então eu recebo uma resposta 404 Not Found
    E eu recebo uma resposta de erro com a mensagem "Empresa não encontrada"
