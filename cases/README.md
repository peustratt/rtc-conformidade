# Casos de teste

Cada caso é um arquivo `.json` dentro da pasta da calculadora que ele testa (`regime-regular/` ou `simples-nacional/`). Subpastas são permitidas, por exemplo `regime-regular/arredondamento/`.

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
| `esperado` | Recorte da resposta com só os campos que o caso confere; os demais são ignorados. Listas comparam pela posição. Texto compara exato (`"27.05"`); número compara como decimal (`27.05` confere com `"27.050"`) |

## Regras

- Só entram casos cujo valor esperado tem origem verificável. Sem origem, o caso fica de fora.
- O teste `ExecutorDosCasos` manda a `entrada` de cada caso para a calculadora e confere a resposta contra o `esperado` e as invariantes. Ele precisa da calculadora no ar.
- Valores monetários são lidos como `decimal`, nunca como `double`, para não introduzir erro de arredondamento no próprio teste.
- O teste `FormatoDosCasos` roda em todo `dotnet test`, sem precisar da calculadora. Ele confere se o JSON é válido, se os campos obrigatórios estão presentes, se o `id` segue o formato e não se repete, e se a `calculadora` bate com a pasta do arquivo.
