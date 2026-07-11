---
trigger: always_on
---

# [MANDATORY RULE] DOCUMENTATION-DRIVEN DEVELOPMENT (DDD)

Você atua em um projeto de arquitetura extensível onde atalhos não documentados destroem o ecossistema. É terminantemente proibido criar métodos, alterar assinaturas ou adicionar novos comportamentos ao Runtime (ex: `entity.PlayAnimation("run")`, novos métodos em `IEntityScript`, ou ferramentas na IDE) sem atualizar a documentação central.

A partir de agora, CADA modificação que afete o contrato de uso do framework exige a execução dos seguintes passos ANTES de reportar a tarefa como concluída:

1. **Atualização do Dicionário de API:** Todo método utilitário, classe de física, helper ou evento criado para os scripts do MonoGame deve ser imediatamente documentado no arquivo `docs/RUNTIME_API_REFERENCE.md` (crie-o caso não exista). 
2. **Formato Exigido na Documentação:** - Nome do Método/Classe.
   - Parâmetros e Tipos esperados.
   - Comportamento exato.
   - Um exemplo prático de uma linha (Ex: `// Faz o jogador pular: entity.ApplyForce(new Vector2(0, -10));`).
3. **Atualização de Arquitetura:** Se a alteração mudar o fluxo de dados (ex: o Inspector agora edita a RAM diretamente, ou a cena não usa mais `Component.cs`), atualize imediatamente o arquivo `AI_ARCHITECTURE.md` (raiz do projeto) para refletir o novo estado de verdade do sistema.
4. **Critério de Conclusão (Definition of Done):** Nenhuma tarefa está finalizada, nenhum Git Push deve ser feito e o /goal NÃO DEVE ser encerrado até que o log de alterações tenha sido refletido na documentação. Se você escrever código que o usuário não saberá como usar amanhã, você falhou na tarefa.