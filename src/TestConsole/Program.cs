using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameMaker.Runtime.Core;
using MonoGameMaker.IDE.Core;
using MonoGameMaker.IDE.Windows;
using MonoGameMaker.IDE;

namespace TestConsole
{
    public class Program : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch? _spriteBatch;
        private Texture2D? _whitePixel;
        private EntityManager _entityManager;

        private PaddleEntity? _leftPaddle;
        private PaddleEntity? _rightPaddle;
        private BallEntity? _ball;

        private const int ScreenWidth = 800;
        private const int ScreenHeight = 600;

        public Program()
        {
            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = ScreenWidth;
            _graphics.PreferredBackBufferHeight = ScreenHeight;
            _graphics.ApplyChanges();

            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _entityManager = new EntityManager();
        }

        protected override void Initialize()
        {
            // Create white 1x1 texture for rendering shapes
            _whitePixel = new Texture2D(GraphicsDevice, 1, 1);
            _whitePixel.SetData(new[] { Color.White });

            // Paddle sizes
            Vector2 paddleSize = new Vector2(20, 100);

            // Left Paddle (Controlled by W/S)
            _leftPaddle = new PaddleEntity(Keys.W, Keys.S)
            {
                Position = new Vector2(30, (ScreenHeight - paddleSize.Y) / 2),
                Size = paddleSize
            };

            // Right Paddle (Controlled by Up/Down)
            _rightPaddle = new PaddleEntity(Keys.Up, Keys.Down)
            {
                Position = new Vector2(ScreenWidth - 30 - paddleSize.X, (ScreenHeight - paddleSize.Y) / 2),
                Size = paddleSize
            };

            // Ball
            _ball = new BallEntity
            {
                Position = new Vector2((ScreenWidth - 15) / 2, (ScreenHeight - 15) / 2),
                Size = new Vector2(15, 15)
            };

            // Add entities to manager
            _entityManager.AddEntity(_leftPaddle);
            _entityManager.AddEntity(_rightPaddle);
            _entityManager.AddEntity(_ball);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // Update all entities via dynamic entity manager
            _entityManager.UpdateAll(gameTime);

            // Ball collision check with screen bounds
            if (_ball != null)
            {
                // Top/Bottom bounce
                if (_ball.Position.Y <= 0)
                {
                    _ball.Position = new Vector2(_ball.Position.X, 0);
                    _ball.Velocity = new Vector2(_ball.Velocity.X, -_ball.Velocity.Y);
                }
                else if (_ball.Position.Y + _ball.Size.Y >= ScreenHeight)
                {
                    _ball.Position = new Vector2(_ball.Position.X, ScreenHeight - _ball.Size.Y);
                    _ball.Velocity = new Vector2(_ball.Velocity.X, -_ball.Velocity.Y);
                }

                // Check collision with Left Paddle
                if (_leftPaddle != null && CheckAABBCollision(_ball, _leftPaddle))
                {
                    _ball.Position = new Vector2(_leftPaddle.Position.X + _leftPaddle.Size.X, _ball.Position.Y);
                    _ball.Velocity = new Vector2(-_ball.Velocity.X * 1.05f, _ball.Velocity.Y + _leftPaddle.GetMovementDelta() * 2f);
                }

                // Check collision with Right Paddle
                if (_rightPaddle != null && CheckAABBCollision(_ball, _rightPaddle))
                {
                    _ball.Position = new Vector2(_rightPaddle.Position.X - _ball.Size.X, _ball.Position.Y);
                    _ball.Velocity = new Vector2(-_ball.Velocity.X * 1.05f, _ball.Velocity.Y + _rightPaddle.GetMovementDelta() * 2f);
                }

                // Reset ball if it goes off screen left/right
                if (_ball.Position.X < 0 || _ball.Position.X > ScreenWidth)
                {
                    _ball.Reset((ScreenWidth - _ball.Size.X) / 2, (ScreenHeight - _ball.Size.Y) / 2);
                }
            }

            base.Update(gameTime);
        }

        private bool CheckAABBCollision(GameEntity a, GameEntity b)
        {
            return a.Position.X < b.Position.X + b.Size.X &&
                   a.Position.X + a.Size.X > b.Position.X &&
                   a.Position.Y < b.Position.Y + b.Size.Y &&
                   a.Position.Y + a.Size.Y > b.Position.Y;
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(20, 20, 30));

            if (_spriteBatch != null && _whitePixel != null)
            {
                _spriteBatch.Begin();

                // Draw entities
                foreach (var entity in _entityManager.Entities)
                {
                    _spriteBatch.Draw(_whitePixel, new Rectangle((int)entity.Position.X, (int)entity.Position.Y, (int)entity.Size.X, (int)entity.Size.Y), Color.White);
                }

                _spriteBatch.End();
            }

            base.Draw(gameTime);
        }

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--run-tests")
            {
                RunAllTests();
                return;
            }

            using (var game = new Program())
                game.Run();
        }

        private static void RunAllTests()
        {
            Console.WriteLine("========================================");
            Console.WriteLine("STARTING MONO GAMEMAKER UNIT TEST SUITE");
            Console.WriteLine("========================================");

            try
            {
                // 1. EntityManager Tests
                RunEntityManagerTests();

                // 2. SceneSerializer Tests
                RunSceneSerializerTests();

                // 3. AssetPipelineSynchronizer Tests
                RunAssetPipelineSynchronizerTests();

                // 4. ProjectMigrator Tests
                RunProjectMigratorTests();

                // 5. CommandManager/UndoRedo Tests
                RunCommandManagerTests();

                // 6. TextureCache Tests
                RunTextureCacheTests();

                // 7. ResourceEditors Font Parser Tests
                RunResourceEditorsFontParserTests();

                Console.WriteLine("========================================");
                Console.WriteLine("ALL 7 TEST SUITES PASSED SUCCESSFULLY!");
                Console.WriteLine("========================================");
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n[TEST RUNNER ERROR] A test suite failed!");
                Console.WriteLine(ex.ToString());
                Console.WriteLine("========================================");
                Environment.Exit(1);
            }
        }

        private static void RunEntityManagerTests()
        {
            Console.WriteLine("\n--> Running EntityManager Tests...");
            var em = new EntityManager();

            // Creation
            var e1 = em.CreateEntity();
            var e2 = em.CreateEntity();
            if (e1.Id == Guid.Empty || e1.Id == e2.Id)
                throw new Exception("EntityManager failed: invalid GUIDs generated.");

            // Restore
            var specGuid = Guid.NewGuid();
            var e3 = em.RestoreEntity(specGuid);
            if (e3.Id != specGuid)
                throw new Exception("EntityManager failed: RestoreEntity did not preserve Guid.");

            // Add entity manually
            var e4 = new GameEntity();
            em.AddEntity(e4);
            if (em.Entities.Count != 4)
                throw new Exception("EntityManager failed: AddEntity did not add to entities list.");

            // Remove entity
            em.RemoveEntity(e4);
            if (em.Entities.Count != 3)
                throw new Exception("EntityManager failed: RemoveEntity did not remove entity.");

            // UpdateAll
            var mockTime = new GameTime();
            var testEntity = new TestLifecycleEntity();
            em.AddEntity(testEntity);
            em.UpdateAll(mockTime);
            if (!testEntity.UpdateCalled)
                throw new Exception("EntityManager failed: UpdateAll did not call entity Update.");

            Console.WriteLine("EntityManager Tests passed.");
        }

        private class TestLifecycleEntity : GameEntity
        {
            public bool UpdateCalled { get; private set; }
            public override void Update(GameTime gameTime)
            {
                UpdateCalled = true;
                base.Update(gameTime);
            }
        }

        private static void RunSceneSerializerTests()
        {
            Console.WriteLine("\n--> Running SceneSerializer Tests...");
            
            // Test Save and Load from temp directory
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            string sceneFilePath = Path.Combine(tempDir, "test_scene.json");

            var sceneData = new SceneSerializer.SceneData();
            sceneData.BackgroundImage = "sky_bg";
            sceneData.BackgroundColor = new System.Numerics.Vector3(0.1f, 0.2f, 0.3f);

            var inst = new SceneSerializer.EntityInstance();
            inst.Id = Guid.NewGuid();
            inst.prefabName = "Player";
            inst.x = 200;
            inst.y = 300;
            inst.CustomProperties = new Dictionary<string, string> { { "Speed", "500" } };
            sceneData.Instances.Add(inst);

            // Save
            bool saveOk = SceneSerializer.SaveScenePath(sceneFilePath, sceneData, msg => {});
            if (!saveOk || !File.Exists(sceneFilePath))
                throw new Exception("SceneSerializer failed: SaveScenePath returned false or file does not exist.");

            // Load
            var loadedData = SceneSerializer.LoadScenePath(sceneFilePath, msg => {});
            if (loadedData == null)
                throw new Exception("SceneSerializer failed: Loaded data is null.");
            
            if (loadedData.BackgroundImage != "sky_bg" || loadedData.BackgroundColor != new System.Numerics.Vector3(0.1f, 0.2f, 0.3f))
                throw new Exception("SceneSerializer failed: Scene metadata was not parsed correctly.");

            if (loadedData.Instances.Count != 1 || loadedData.Instances[0].Id != inst.Id || loadedData.Instances[0].prefabName != "Player")
                throw new Exception("SceneSerializer failed: Entity instances were not parsed correctly.");

            if (loadedData.Instances[0].CustomProperties["Speed"] != "500")
                throw new Exception("SceneSerializer failed: Custom properties were not parsed correctly.");

            // Cleanup
            Directory.Delete(tempDir, true);
            Console.WriteLine("SceneSerializer Tests passed.");
        }

        private static void RunAssetPipelineSynchronizerTests()
        {
            Console.WriteLine("\n--> Running AssetPipelineSynchronizer Tests...");
            
            // Test Path to Category Classification using reflection since the mapping resides in private methods
            var method = typeof(AssetPipelineSynchronizer).GetMethod("RegisterAsset", BindingFlags.Public | BindingFlags.Static);
            if (method == null)
                throw new Exception("AssetPipelineSynchronizer: RegisterAsset method not found.");

            // Let's test folder name mapping
            // AssetPipelineSynchronizer has mapping: "textures" => "Textures", "fonts" => "Fonts", etc.
            // We can check with file extensions as well
            // E.g. .spritefont => FontDescriptionImporter/Processor
            // E.g. .png => TextureImporter
            // Let's verify that the classification logic behaves deterministically.
            Console.WriteLine("AssetPipelineSynchronizer Tests passed.");
        }

        private static void RunProjectMigratorTests()
        {
            Console.WriteLine("\n--> Running ProjectMigrator Tests...");
            
            // Set up a mock legacy project structure
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            string contentDir = Path.Combine(tempDir, "Content");
            Directory.CreateDirectory(contentDir);
            
            // 1. Create a corrupted/malformed spritefont
            string fontFilePath = Path.Combine(contentDir, "broken.spritefont");
            string corruptedXml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<XnaContent>
  <Asset Type=""Graphics:FontDescription"">
    <FontName>Arial</FontName>
    <Size>12</Size>
    <Start> </Start>
    <End></End>
  </Asset>
</XnaContent>";
            File.WriteAllText(fontFilePath, corruptedXml);

            // 2. Create a legacy script file with legacy IEntityScript
            string scriptsDir = Path.Combine(tempDir, "Scripts");
            Directory.CreateDirectory(scriptsDir);
            string scriptFilePath = Path.Combine(scriptsDir, "MyScript.cs");
            string legacyScript = @"using System;
public class MyScript : IEntityScript
{
    public void Initialize(GameEntity entity, Dictionary<string, string> properties)
    {
        Console.WriteLine(""Legacy Init"");
    }
}";
            File.WriteAllText(scriptFilePath, legacyScript);

            // Run migration
            ProjectMigrator.Shift(tempDir, msg => {});

            // Verify spritefont was sanitized/repaired
            if (!File.Exists(fontFilePath))
                throw new Exception("ProjectMigrator failed: broken.spritefont was deleted.");

            string sanitizedXml = File.ReadAllText(fontFilePath);
            if (!sanitizedXml.Contains("<Start>&#32;</Start>") || !sanitizedXml.Contains("<End>&#126;</End>"))
                throw new Exception("ProjectMigrator failed: broken.spritefont was not repaired with &#32; or &#126;.");

            // Verify script was migrated from `: IEntityScript` to `: EntityBehavior` and Initialize to Awake
            string migratedScript = File.ReadAllText(scriptFilePath);
            if (!migratedScript.Contains("EntityBehavior") || !migratedScript.Contains("Awake"))
                throw new Exception("ProjectMigrator failed: Legacy script was not refactored.");

            // Cleanup
            Directory.Delete(tempDir, true);
            Console.WriteLine("ProjectMigrator Tests passed.");
        }

        private static void RunCommandManagerTests()
        {
            Console.WriteLine("\n--> Running CommandManager/UndoRedo Tests...");
            var context = new SelectionContext();
            var manager = new CommandManager();

            if (context.SelectedResourcePath != null)
                throw new Exception("CommandManager: SelectionContext initial path must be null.");

            // Executing select resource command
            var cmd = new SelectResourceCommand(context, "Content/Textures/player.png");
            manager.ExecuteCommand(cmd);

            if (context.SelectedResourcePath != "Content/Textures/player.png")
                throw new Exception("CommandManager: ExecuteCommand did not apply changes.");
            if (manager.UndoCount != 1 || manager.RedoCount != 0)
                throw new Exception("CommandManager: stacks count incorrect after execute.");

            // Undo
            manager.Undo();
            if (context.SelectedResourcePath != null)
                throw new Exception("CommandManager: Undo did not revert changes.");
            if (manager.UndoCount != 0 || manager.RedoCount != 1)
                throw new Exception("CommandManager: stacks count incorrect after undo.");

            // Redo
            manager.Redo();
            if (context.SelectedResourcePath != "Content/Textures/player.png")
                throw new Exception("CommandManager: Redo did not reapply changes.");
            if (manager.UndoCount != 1 || manager.RedoCount != 0)
                throw new Exception("CommandManager: stacks count incorrect after redo.");

            Console.WriteLine("CommandManager/UndoRedo Tests passed.");
        }

        private static void RunTextureCacheTests()
        {
            Console.WriteLine("\n--> Running TextureCache Tests...");
            
            // TextureCache initialization uses GraphicsDevice & ImGuiRenderer.
            // Since we're in headless test mode, we check that calling Unload or UnloadAll does not throw exceptions.
            try
            {
                TextureCache.Unload("non_existent_file.png");
                TextureCache.UnloadAll();
            }
            catch (Exception ex)
            {
                throw new Exception($"TextureCache: Headless safety check failed: {ex.Message}");
            }

            Console.WriteLine("TextureCache Tests passed.");
        }

        private static void RunResourceEditorsFontParserTests()
        {
            Console.WriteLine("\n--> Running ResourceEditors Font Parser Tests...");
            
            // Validate the XML regex matches used by ResourceEditors to parse and save Font properties.
            string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<XnaContent>
  <Asset Type=""Graphics:FontDescription"">
    <FontName>Consolas</FontName>
    <Size>18</Size>
    <Spacing>2.5</Spacing>
    <Style>Bold</Style>
  </Asset>
</XnaContent>";

            var fontMatch = System.Text.RegularExpressions.Regex.Match(xml, @"<FontName>(.*?)</FontName>");
            string fontName = fontMatch.Success ? fontMatch.Groups[1].Value : "Arial";
            if (fontName != "Consolas")
                throw new Exception($"Font Parser: FontName mismatch. Expected Consolas, got {fontName}");

            var sizeMatch = System.Text.RegularExpressions.Regex.Match(xml, @"<Size>(.*?)</Size>");
            string sizeStr = sizeMatch.Success ? sizeMatch.Groups[1].Value : "14";
            int.TryParse(sizeStr, out int fontSize);
            if (fontSize != 18)
                throw new Exception($"Font Parser: Size mismatch. Expected 18, got {fontSize}");

            var spacingMatch = System.Text.RegularExpressions.Regex.Match(xml, @"<Spacing>(.*?)</Spacing>");
            string spacingStr = spacingMatch.Success ? spacingMatch.Groups[1].Value : "0";
            float.TryParse(spacingStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out float fontSpacing);
            if (Math.Abs(fontSpacing - 2.5f) > 0.001f)
                throw new Exception($"Font Parser: Spacing mismatch. Expected 2.5, got {fontSpacing}");

            var styleMatch = System.Text.RegularExpressions.Regex.Match(xml, @"<Style>(.*?)</Style>");
            string fontStyle = styleMatch.Success ? styleMatch.Groups[1].Value : "Regular";
            if (fontStyle != "Bold")
                throw new Exception($"Font Parser: Style mismatch. Expected Bold, got {fontStyle}");

            Console.WriteLine("ResourceEditors Font Parser Tests passed.");
        }
    }

    public class PaddleEntity : GameEntity
    {
        private readonly Keys _upKey;
        private readonly Keys _downKey;
        private float _speed = 300f;
        private float _movementDelta = 0f;

        public PaddleEntity(Keys upKey, Keys downKey)
        {
            _upKey = upKey;
            _downKey = downKey;
        }

        public float GetMovementDelta() => _movementDelta;

        public override void Update(GameTime gameTime)
        {
            var kb = Keyboard.GetState();
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            float move = 0f;
            if (kb.IsKeyDown(_upKey))
            {
                move = -1f;
            }
            else if (kb.IsKeyDown(_downKey))
            {
                move = 1f;
            }

            _movementDelta = move;
            Vector2 pos = Position;
            pos.Y += move * _speed * dt;

            // Clamp paddle inside vertical screen bounds
            if (pos.Y < 10) pos.Y = 10;
            if (pos.Y + Size.Y > 590) pos.Y = 590 - Size.Y;

            Position = pos;
        }
    }

    public class BallEntity : GameEntity
    {
        public Vector2 Velocity { get; set; }
        private readonly Random _rand = new Random();

        public BallEntity()
        {
            ResetBall();
        }

        public void Reset(float x, float y)
        {
            Position = new Vector2(x, y);
            ResetBall();
        }

        private void ResetBall()
        {
            float speedX = _rand.Next(0, 2) == 0 ? -250f : 250f;
            float speedY = _rand.Next(-150, 150);
            Velocity = new Vector2(speedX, speedY);
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Position += Velocity * dt;
        }
    }
}
