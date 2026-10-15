# `CombatContext` Manual da API

O `CombatContext` é o padrão *Context Object* canônico do MonoGameMaker projetado para eliminar o problema de *Parameter Tunneling* no loop de simulação de combate. Agregando variáveis dinâmicas de quadro (delta time, tempo total, coordenadas e vitalidade do jogador) e referências a serviços centrais (câmera, partículas, sintetizador e áudio), ele opera como uma instância única, pré-alocada e reutilizada a cada frame com garantia estrita de **Zero-GC**.

---

## Assinatura Completa

```csharp
namespace MonoGameMaker.Runtime.Combat
{
    public sealed class CombatContext
    {
        public float DeltaTime { get; }
        public float WorldDeltaTime { get; }
        public float TotalGameTime { get; }
        public bool IsTimeDilated { get; set; }

        public Camera3D? Camera { get; set; }
        public ParticleSystem3D? Particles { get; set; }
        public AudioManager? Audio { get; set; }
        public SoundSynthesizer? SoundSynth { get; set; }
        public InputService? Input { get; set; }

        public Vector3 PlayerPosition { get; set; }
        public bool IsPlayerAlive { get; set; }

        public Action? OnEmpDetonation { get; set; }
        public Action? OnCryoDetonation { get; set; }
        public Action<int>? OnScoreAdded { get; set; }

        public void PrepareFrame(
            float dt, 
            float worldDt, 
            float totalTime, 
            Vector3 playerPos, 
            bool playerAlive);
    }
}
```

---

## Casos de Uso (Contexto para IAs)

1. **Eliminação de Métodos com 10+ Parâmetros:** No código legado do Space_v7, métodos como `EnemyBase.Update(dt, playerPos, projectiles, assets, particles, sound, camera)` propagavam dependências de forma rígida. Com o `CombatContext`, o contrato é unificado em `UpdateCombat(CombatContext ctx)`.
2. **Ciclo de Vida Limpo por Frame (`PrepareFrame`):** A cena de combate atualiza os dados mutáveis uma única vez por quadro sem alocar novos objetos na memória gerenciada.
3. **Desacoplamento de Subsistemas:** Comportamentos de inimigos, chefes e armas do jogador acessam áudio procedural (`ctx.SoundSynth`), emissores de faíscas (`ctx.Particles`) e trauma de câmera (`ctx.Camera`) diretamente do contexto injetado.

---

## Exemplo Prático e Compilável

O exemplo a seguir demonstra como a cena de combate prepara o `CombatContext` e o injeta nas entidades ativas:

```csharp
using Microsoft.Xna.Framework;
using MonoGameMaker.Runtime.Audio;
using MonoGameMaker.Runtime.Combat;
using MonoGameMaker.Runtime.Core;
using MonoGameMaker.Runtime.Particles;

namespace MyGame.Combat
{
    public class CombatScene
    {
        private readonly CombatContext _context;
        private readonly Camera3D _camera;
        private readonly ParticleSystem3D _particles;
        private readonly SoundSynthesizer _synth;

        public CombatScene()
        {
            _camera = new Camera3D();
            _particles = new ParticleSystem3D(2048);
            _synth = new SoundSynthesizer();

            // Instância única reutilizada indefinidamente
            _context = new CombatContext
            {
                Camera = _camera,
                Particles = _particles,
                SoundSynth = _synth,
                Audio = AudioManager.Instance,
                Input = InputService.Instance
            };
        }

        public void Update(GameTime gameTime, Vector3 playerPos, bool playerAlive)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float total = (float)gameTime.TotalGameTime.TotalSeconds;

            // Prepara as variáveis do frame sem alocar memória
            _context.PrepareFrame(dt, dt, total, playerPos, playerAlive);

            // Injeta o contexto em qualquer entidade combatente
            // Exemplo: enemy.UpdateCombat(_context);
        }
    }
}
```
