# `Mesh3DFactory` Manual da API

O `Mesh3DFactory` é a fábrica procedural de geometria tridimensional do MonoGameMaker. Capaz de sintetizar malhas em tempo de execução sem dependência de importação de arquivos `.fbx` ou `.obj`, ele gera corpos celestes esféricos, asteroides deformados organicamente via ruído procedural, estilhaços de debris poligonais, cubos, planos e carcaças de naves de combate espaciais com buffers indexados de GPU.

---

## Assinatura Completa

```csharp
namespace MonoGameMaker.Runtime.Graphics3D
{
    public static class Mesh3DFactory
    {
        // Primitivas Geométricas
        public static Mesh3D CreateSphereMesh(GraphicsDevice device, float radius = 1.0f, int rings = 32, int slices = 32);
        public static Mesh3D CreateAsteroidMesh(GraphicsDevice device, float radius = 20.0f, int subdivisions = 2, int seed = 0);
        public static Mesh3D CreateShardMesh(GraphicsDevice device, float size = 1.0f, int seed = 0);
        public static Mesh3D CreateCubeMesh(GraphicsDevice device, float size = 1f);
        public static Mesh3D CreatePlaneMesh(GraphicsDevice device, float width = 1f, float height = 1f);
        public static Mesh3D CreateQuadMesh(GraphicsDevice device, float width = 1.0f, float height = 1.0f);
        public static Mesh3D CreateShieldDomeMesh(GraphicsDevice device, float radius = 1.0f, int rings = 24, int slices = 24);
        public static Mesh3D CreateCylinderMesh(GraphicsDevice device, float radius = 1f, float height = 2f, int slices = 24);
        public static Mesh3D CreateConeMesh(GraphicsDevice device, float radius = 1f, float height = 2f, int slices = 24);

        // Naves e Entidades Espaciais
        public static Mesh3D CreatePlayerShipMesh(GraphicsDevice device, float scale = 1f);
        public static Mesh3D CreateViperMesh(GraphicsDevice device, float scale = 1f);
        public static Mesh3D CreateCruiserMesh(GraphicsDevice device, float scale = 1f);
        public static Mesh3D CreateRaiderMesh(GraphicsDevice device, float scale = 1f);
        public static Mesh3D CreateScarabMesh(GraphicsDevice device, float scale = 1f);
        public static Mesh3D CreateBossMesh(GraphicsDevice device, float scale = 1f);
        public static Mesh3D CreateTitanBossMesh(GraphicsDevice device, float scale = 1f);
        public static Mesh3D CreateVoidLeviathanMesh(GraphicsDevice device, float scale = 1f);
    }
}
```

---

## Casos de Uso (Contexto para IAs)

1. **Geração Orgânica de Cinturões de Asteroides:** Cada asteroide recebe um `seed` numérico único em `CreateAsteroidMesh(device, radius, subdivisions, seed)`. A fábrica subdivide um icosaedro base, aplicando perturbações harmônicas senoidais com cálculo exato de vetores normais para iluminação suave.
2. **Estilhaçamento de Debris em Combate:** Ao explodir uma rocha ou nave, despache de 3 a 5 estilhaços angulares facetados gerados via `CreateShardMesh` com rotações aleatórias.
3. **Escudo de Energia Dinâmico:** Cúpulas de força geradas por `CreateShieldDomeMesh` renderizam perfeitamente sob o shader `UberShield.fx` com Fresnel inverso e padrão hexagonal.

---

## Exemplo Prático e Compilável

O exemplo a seguir demonstra a criação e renderização de uma nave espacial e um asteroide iluminado em 3D:

```csharp
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameMaker.Runtime.Core;
using MonoGameMaker.Runtime.Graphics3D;

namespace MyGame.Graphics
{
    public class MeshDemo
    {
        private Mesh3D _shipMesh = null!;
        private Mesh3D _asteroidMesh = null!;
        private float _rotation = 0f;

        public void Initialize(GraphicsDevice graphicsDevice)
        {
            // Cria as malhas procedurais na memória da GPU
            _shipMesh = Mesh3DFactory.CreatePlayerShipMesh(graphicsDevice, scale: 20f);
            _asteroidMesh = Mesh3DFactory.CreateAsteroidMesh(graphicsDevice, radius: 45f, subdivisions: 2, seed: 42);
        }

        public void Update(float dt)
        {
            _rotation += dt * 0.8f;
        }

        public void Draw(GraphicsDevice graphicsDevice, Camera3D camera)
        {
            // Matrizes de mundo
            Matrix shipWorld = Matrix.CreateRotationY(_rotation) * Matrix.CreateTranslation(new Vector3(0, 0, 0));
            Matrix asteroidWorld = Matrix.CreateRotationX(_rotation * 0.5f) * Matrix.CreateTranslation(new Vector3(150, 0, 0));

            // Renderiza com luz direcional e rim light
            _shipMesh.Draw(
                graphicsDevice,
                shipWorld,
                camera.ViewMatrix,
                camera.ProjectionMatrix,
                diffuseColor: Color.LightCyan,
                lightDirection: new Vector3(0.5f, -0.8f, -0.4f),
                lightColor: Color.White);

            _asteroidMesh.Draw(
                graphicsDevice,
                asteroidWorld,
                camera.ViewMatrix,
                camera.ProjectionMatrix,
                diffuseColor: Color.DarkSlateGray,
                lightDirection: new Vector3(0.5f, -0.8f, -0.4f),
                lightColor: Color.SandyBrown);
        }

        public void Unload()
        {
            _shipMesh.Dispose();
            _asteroidMesh.Dispose();
        }
    }
}
```
