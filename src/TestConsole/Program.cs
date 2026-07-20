using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameMaker.Runtime.Core;

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
                RunEntityRestoreTests();
                return;
            }

            using (var game = new Program())
                game.Run();
        }

        private static void RunEntityRestoreTests()
        {
            Console.WriteLine("Running EntityManager RestoreEntity tests...");
            var em = new EntityManager();

            // Test 1: CreateEntity generates different GUIDs
            var e1 = em.CreateEntity();
            var e2 = em.CreateEntity();
            if (e1.Id == Guid.Empty || e2.Id == Guid.Empty || e1.Id == e2.Id)
            {
                throw new Exception("FAIL: CreateEntity generated duplicate or empty GUIDs");
            }
            Console.WriteLine($"Test 1 Pass: CreateEntity works. E1: {e1.Id}, E2: {e2.Id}");

            // Test 2: RestoreEntity injects specific GUID
            var customId = Guid.NewGuid();
            var e3 = em.RestoreEntity(customId);
            if (e3.Id != customId)
            {
                throw new Exception("FAIL: RestoreEntity did not inject the correct GUID");
            }
            Console.WriteLine($"Test 2 Pass: RestoreEntity injected correct GUID: {e3.Id}");

            // Test 3: RestoreEntity doesn't collide with existing CreateEntity GUIDs
            var e4 = em.CreateEntity();
            if (e4.Id == customId)
            {
                throw new Exception("FAIL: Collision between CreateEntity and RestoreEntity GUID");
            }
            Console.WriteLine($"Test 3 Pass: No collision between CreateEntity and RestoreEntity GUIDs.");

            Console.WriteLine("ALL TESTS PASSED SUCCESSFULLY!");
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
