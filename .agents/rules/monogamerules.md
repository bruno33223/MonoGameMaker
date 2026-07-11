---
trigger: always_on
---

# [MANDATORY RULE] DOCUMENTATION-DRIVEN DEVELOPMENT (DDD) & ATOMIC DOCS

Você atua em um projeto de arquitetura extensível onde atalhos não documentados destroem o ecossistema. É terminantemente proibido criar métodos, alterar assinaturas ou adicionar novos comportamentos ao Runtime sem gerar documentação profunda e isolada para eles.

A partir de agora, CADA modificação que afete o contrato de uso do framework exige a execução dos seguintes passos ANTES de reportar a tarefa como concluída:

1. **Documentação Atômica (Granularidade):** Não amontoe tudo em um único arquivo. Todo método utilitário, classe, helper ou evento criado/modificado DEVE ter seu próprio manual exaustivo na pasta `docs/runtime/` (ex: se criar um sistema de física, crie `docs/runtime/Physics.md`).
2. **Formato Exigido por Arquivo (.md):**
   - **Descrição de Alto Nível:** O que a classe/método resolve no jogo.
   - **Assinatura Completa:** Nome, Parâmetros, Tipos de retorno.
   - **Casos de Uso (Contexto para IAs):** Quando e por que usar esta ferramenta em vez de programar do zero.
   - **Exemplo Prático e Compilável:** Um bloco de código C# completo (não apenas uma linha) demonstrando a aplicação real dentro do `Update` ou `Draw` do MonoGame.
3. **Atualização do Sumário (Index):** Após criar ou alterar o arquivo atômico, você deve adicionar/atualizar o link relativo dele no arquivo `docs/RUNTIME_API_REFERENCE.md` (que serve estritamente como um índice de navegação).
4. **Atualização de Arquitetura:** Se a alteração mudar o fluxo de dados do ecossistema (ex: o Inspector lendo memória viva, ou adição de um subsistema de física ao EntityManager), atualize imediatamente o arquivo `docs/AI_ARCHITECTURE.md` para refletir o novo paradigma do sistema.
5. **Critério de Conclusão (Definition of Done):** Nenhuma tarefa está finalizada, nenhum Git Push deve ser feito e o /goal NÃO DEVE ser encerrado até que o log de alterações tenha sido refletido na documentação modular. Se você escrever código que o usuário (ou outro agente) não saberá como invocar amanhã lendo a documentação, você falhou na tarefa.