# Casos de teste

Cada caso é um arquivo `.json` dentro da pasta da calculadora que ele testa (`regime-regular/` ou `simples-nacional/`).

## Formato

```json
{
  "id": "rr-arredondamento-001",
  "calculadora": "regime-regular",
  "endpoint": "POST /regime-geral",
  "descricao": "Nota com 3 itens em que a soma arredondada difere do total",
  "baseLegal": "LC 214/2025, art. ...",
  "origem": "https://github.com/ReceitaFederal/calculadora-rtc-regime-regular/issues/3",
  "entrada": {},
  "esperado": {}
}
```

| Campo | O que é |
| --- | --- |
| `id` | Identificador único, em minúsculas e com hífens |
| `calculadora` | `regime-regular` ou `simples-nacional` |
| `endpoint` | Método e caminho relativos à URL base da calculadora |
| `descricao` | Uma frase dizendo o que o caso verifica |
| `baseLegal` | Dispositivo legal ou nota técnica que define o resultado |
| `origem` | De onde veio o valor esperado: issue, documentação oficial ou revisão de um contador |
| `entrada` | Corpo da requisição, exatamente como a calculadora recebe |
| `esperado` | Só os campos da resposta que o caso confere; os demais são ignorados |

## Regras

- Só entram casos cujo valor esperado tem origem verificável. Sem origem, o caso fica de fora.
- Valores monetários são lidos como `decimal`, nunca como `double`, para não introduzir erro de arredondamento no próprio teste.
- O teste `FormatoDosCasos` confere os campos obrigatórios e os ids repetidos em todo `dotnet test`.
