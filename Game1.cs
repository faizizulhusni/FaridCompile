using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace RelicEscape
{
    // Main Game Class
    public class GameLevelManager : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        private GameState currentGameState;
        private Player player;
        private Random random;

        // Level instances
        private TutorialLevel tutorialLevel;
        private Level1 level1;
        private Level2 level2;
        private Level3 level3;
        private BaseLevel currentLevel;

        private Matrix cameraTransform;
        private Vector2 cameraPosition, cameraShakeOffset;
        private float cameraZoom = 2.0f, cameraShakeIntensity = 0f, cameraShakeDuration = 0f;
        private float damageFlashAlpha = 0f;

        private KeyboardState prevKeyState;
        private Texture2D pixelTexture;
        private SpriteFont font;

        // Sprite textures
        private Texture2D playerSprite;
        private Texture2D npcSprite;
        private Texture2D scarecrowSprite;
        private Texture2D pumpkinSprite;
        private Texture2D snakeTexture;
        private Texture2D spiderTexture;
        private Texture2D scorpionTexture;

        // Tile textures
        private Texture2D grassTexture;
        private Texture2D flowerTexture;
        private Texture2D fenceTexture;
        private Texture2D pathTexture;
        private Texture2D signTexture;
        private Texture2D exitTexture;
        private Texture2D treeTexture;
        private Texture2D waterTexture;
        private Texture2D stoneTexture;
        private Texture2D chestTexture;
        private Texture2D sandTexture;
        private Texture2D cactusTexture;
        private Texture2D pyramidTexture;
        private Texture2D quicksandTexture;
        private Texture2D statueTexture;
        private Texture2D shopTexture;

        // Item textures
        private Texture2D wateringCanSprite;
        private Texture2D gardenSpadeSprite;
        private Texture2D keyTexture;
        private Texture2D bladeTexture;
        private Texture2D coinTexture;
        private Texture2D scrollTexture;

        private string displayMessage = "";
        private bool showMessage = false;
        private float messageTimer = 0f;

        private string dialogueText = "";
        private bool showDialogue = false;
        private float dialogueTimer = 0f;
        private string npcName = "";

        // Transition
        private float transitionTimer = 0f;
        private float transitionDuration = 2f;
        private bool transitionInProgress = false;

        // Victory cutscene
        private string victoryText = "Tutorial Complete! You're ready for adventure!";
        private string displayedVictoryText = "";
        private float typewriterTimer = 0f;
        private int charIndex = 0;
        private bool victoryTextComplete = false;

        // Cutscene
        private string cutsceneText = "Forward to the Sands of Remembrance";
        private string displayedCutsceneText = "";
        private float cutsceneTypewriterTimer = 0f, cutsceneTypewriterSpeed = 0.05f;
        private int cutsceneCharIndex = 0;
        private float cutscenePauseTimer = 0f, cutscenePauseDuration = 5f;
        private bool cutsceneTextComplete = false;

        public GameLevelManager()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;
        }

        protected override void Initialize()
        {
            random = new Random();
            currentGameState = GameState.Tutorial;

            player = new Player(new Vector2(64 * 2, 64 * 7));

            // Initialize level instances
            tutorialLevel = new TutorialLevel(this, player, random);
            level1 = new Level1(this, player, random);
            level2 = new Level2(this, player, random);
            level3 = new Level3(this, player, random);

            currentLevel = tutorialLevel;

            base.Initialize();
        }

        private SpriteFont CreateBasicFont()
        {
            try
            {
                var fontTexture = new Texture2D(GraphicsDevice, 1, 1);
                fontTexture.SetData(new[] { Color.White });

                var glyphBounds = new List<Rectangle>();
                var cropping = new List<Rectangle>();
                var characters = new List<char>();
                var kerning = new List<Vector3>();

                string charSet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+-×÷=?: ";

                foreach (char c in charSet)
                {
                    glyphBounds.Add(new Rectangle(0, 0, 8, 12));
                    cropping.Add(new Rectangle(0, 0, 8, 12));
                    characters.Add(c);
                    kerning.Add(new Vector3(0, 8, 1));
                }

                return new SpriteFont(fontTexture, glyphBounds, cropping, characters, 12, 0, kerning, ' ');
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to create basic font: {ex.Message}");
                return null;
            }
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            pixelTexture = new Texture2D(GraphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });

            try
            {
                font = Content.Load<SpriteFont>("Font");
            }
            catch
            {
                try
                {
                    font = Content.Load<SpriteFont>("DefaultFont");
                }
                catch
                {
                    font = CreateBasicFont();
                }
            }

            // Load textures
            LoadTextureWithDebug("Grass", ref grassTexture);
            LoadTextureWithDebug("Flower", ref flowerTexture);
            LoadTextureWithDebug("Fence", ref fenceTexture);
            LoadTextureWithDebug("Path", ref pathTexture);
            LoadTextureWithDebug("Tree", ref signTexture);
            LoadTextureWithDebug("Exit", ref exitTexture);
            LoadTextureWithDebug("Tree", ref treeTexture);
            LoadTextureWithDebug("Water", ref waterTexture);
            LoadTextureWithDebug("Stone", ref stoneTexture);
            LoadTextureWithDebug("Chest", ref chestTexture);
            LoadTextureWithDebug("Sand", ref sandTexture);
            LoadTextureWithDebug("Cactus", ref cactusTexture);
            LoadTextureWithDebug("Pyramid", ref pyramidTexture);
            LoadTextureWithDebug("Quicksand", ref quicksandTexture);
            LoadTextureWithDebug("Statue", ref statueTexture);
            LoadTextureWithDebug("Shop", ref shopTexture);

            LoadTextureWithDebug("Player", ref playerSprite);
            LoadTextureWithDebug("NPC", ref npcSprite);
            LoadTextureWithDebug("Scarecrow", ref scarecrowSprite);
            LoadTextureWithDebug("Pumpkin", ref pumpkinSprite);
            LoadTextureWithDebug("Snake", ref snakeTexture);
            LoadTextureWithDebug("Spider", ref spiderTexture);
            LoadTextureWithDebug("Scorpion", ref scorpionTexture);

            LoadTextureWithDebug("WateringCan", ref wateringCanSprite);
            LoadTextureWithDebug("GardenSpade", ref gardenSpadeSprite);
            LoadTextureWithDebug("Key", ref keyTexture);
            LoadTextureWithDebug("Blade", ref bladeTexture);
            LoadTextureWithDebug("Coin", ref coinTexture);
            LoadTextureWithDebug("Scroll", ref scrollTexture);
        }

        private void LoadTextureWithDebug(string textureName, ref Texture2D textureVariable)
        {
            try
            {
                textureVariable = Content.Load<Texture2D>(textureName);
                System.Diagnostics.Debug.WriteLine($"Successfully loaded: {textureName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FAILED to load: {textureName} - {ex.Message}");
                textureVariable = null;
            }
        }

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            KeyboardState keyState = Keyboard.GetState();

            if (messageTimer > 0)
            {
                messageTimer -= deltaTime;
                if (messageTimer <= 0) showMessage = false;
            }

            if (dialogueTimer > 0)
            {
                dialogueTimer -= deltaTime;
                if (dialogueTimer <= 0) showDialogue = false;
            }

            if (damageFlashAlpha > 0)
            {
                damageFlashAlpha -= deltaTime * 5f;
                if (damageFlashAlpha < 0) damageFlashAlpha = 0;
            }

            if (cameraShakeDuration > 0)
            {
                cameraShakeDuration -= deltaTime;
                float shakeAmount = cameraShakeIntensity * (cameraShakeDuration / 0.2f);
                cameraShakeOffset = new Vector2(
                    (float)(random.NextDouble() * 2 - 1) * shakeAmount,
                    (float)(random.NextDouble() * 2 - 1) * shakeAmount
                );
                if (cameraShakeDuration <= 0) cameraShakeOffset = Vector2.Zero;
            }

            switch (currentGameState)
            {
                case GameState.Tutorial:
                case GameState.Level1:
                case GameState.Level2:
                    currentLevel.Update(deltaTime, keyState, prevKeyState);
                    UpdateCamera();
                    break;
                case GameState.Transition:
                    UpdateTransition(deltaTime);
                    break;
                case GameState.Level2Transition:
                    UpdateLevel2Transition(deltaTime);
                    break;
                case GameState.Level3:
                    currentLevel.Update(deltaTime, keyState, prevKeyState);
                    UpdateCamera();
                    break;
                case GameState.Level3Transition:
                    UpdateLevel3Transition(deltaTime);
                    break;
                case GameState.Victory:
                    UpdateVictory(deltaTime);
                    break;
                case GameState.GameOver:
                    UpdateGameOver(keyState);
                    break;
                case GameState.Cutscene:
                    UpdateCutscene(deltaTime, keyState);
                    break;
            }

            prevKeyState = keyState;
            base.Update(gameTime);
        }

        private void UpdateTransition(float deltaTime)
        {
            transitionTimer += deltaTime;
            if (transitionTimer >= transitionDuration)
            {
                transitionInProgress = false;
                currentGameState = GameState.Level1;
                currentLevel = level1;
                player.Position = new Vector2(64 * 2, 64 * 2);
                ShowMessage("Kill the snake first, you will know why...");
            }
        }

        private void UpdateLevel2Transition(float deltaTime)
        {
            transitionTimer += deltaTime;
            if (transitionTimer >= transitionDuration)
            {
                transitionInProgress = false;
                currentGameState = GameState.Level2;
                currentLevel = level2;
                player.Position = new Vector2(64 * 2, 64 * 2);
                ShowMessage("Welcome to the Desert! Find keys and solve the puzzle!");
            }
        }

        private void UpdateLevel3Transition(float deltaTime)
        {
            transitionTimer += deltaTime;
            if (transitionTimer >= transitionDuration)
            {
                transitionInProgress = false;
                currentGameState = GameState.Level3;
                currentLevel = level3;
                player.Position = new Vector2(64 * 10, 64 * 2);
                ShowMessage("BOSS FIGHT! Defeat the Infernal Guardian!");
            }
        }

        private void UpdateVictory(float deltaTime)
        {
            if (!victoryTextComplete)
            {
                typewriterTimer += deltaTime;
                if (typewriterTimer >= 0.05f)
                {
                    typewriterTimer = 0f;
                    if (charIndex < victoryText.Length)
                        displayedVictoryText += victoryText[charIndex++];
                    else
                        victoryTextComplete = true;
                }
            }
        }

        private void UpdateGameOver(KeyboardState keyState)
        {
            if (keyState.IsKeyDown(Keys.R) && prevKeyState.IsKeyUp(Keys.R)) RestartGame();
        }

        private void UpdateCutscene(float deltaTime, KeyboardState keyState)
        {
            if (!cutsceneTextComplete)
            {
                cutsceneTypewriterTimer += deltaTime;
                if (cutsceneTypewriterTimer >= cutsceneTypewriterSpeed)
                {
                    cutsceneTypewriterTimer = 0f;
                    if (cutsceneCharIndex < cutsceneText.Length)
                        displayedCutsceneText += cutsceneText[cutsceneCharIndex++];
                    else
                        cutsceneTextComplete = true;
                }
            }
            else
            {
                cutscenePauseTimer += deltaTime;
                if (cutscenePauseTimer >= cutscenePauseDuration)
                {
                    if (currentLevel is Level1)
                    {
                        player.Inventory.RemoveAll(item => item == "Key");
                        currentGameState = GameState.Level2Transition;
                        transitionTimer = 0f;
                        transitionInProgress = true;
                    }
                    else if (currentLevel is Level2)
                    {
                        player.Inventory.RemoveAll(item => item == "Key");
                        currentGameState = GameState.Level3Transition;
                        transitionTimer = 0f;
                        transitionInProgress = true;
                    }
                    else
                    {
                        RestartGame();
                    }
                }
            }
        }

        public void TriggerDamageEffect()
        {
            cameraShakeDuration = 0.2f;
            cameraShakeIntensity = 5f;
            damageFlashAlpha = 0.5f;
        }

        private void UpdateCamera()
        {
            var screenCenter = new Vector2(graphics.PreferredBackBufferWidth / 2f, graphics.PreferredBackBufferHeight / 2f);
            cameraPosition = player.Position + cameraShakeOffset;

            float minX = screenCenter.X / cameraZoom;
            float maxX = (currentLevel.MapWidth * 64) - screenCenter.X / cameraZoom;
            float minY = screenCenter.Y / cameraZoom;
            float maxY = (currentLevel.MapHeight * 64) - screenCenter.Y / cameraZoom;

            cameraPosition.X = MathHelper.Clamp(cameraPosition.X, minX, maxX);
            cameraPosition.Y = MathHelper.Clamp(cameraPosition.Y, minY, maxY);

            cameraTransform = Matrix.CreateTranslation(-cameraPosition.X, -cameraPosition.Y, 0) *
                            Matrix.CreateScale(cameraZoom) *
                            Matrix.CreateTranslation(screenCenter.X, screenCenter.Y, 0);
        }

        public void ShowMessage(string message)
        {
            displayMessage = message;
            showMessage = true;
            messageTimer = 3f;
        }

        public void ShowDialogue(string name, string text)
        {
            npcName = name;
            dialogueText = text;
            showDialogue = true;
            dialogueTimer = 5f;
        }

        public void TransitionToLevel1()
        {
            currentGameState = GameState.Transition;
            transitionTimer = 0f;
            transitionInProgress = true;
        }

        public void TransitionToCutscene(string text)
        {
            currentGameState = GameState.Cutscene;
            cutsceneText = text;
            displayedCutsceneText = "";
            cutsceneCharIndex = 0;
            cutsceneTypewriterTimer = 0f;
            cutscenePauseTimer = 0f;
            cutsceneTextComplete = false;
        }

        public void TransitionToVictory()
        {
            currentGameState = GameState.Victory;
            victoryText = "ULTIMATE VICTORY!\nYou have conquered all challenges!\nThe Infernal Core is yours!\nYou are the ultimate Relic Hunter!";
            displayedVictoryText = "";
            charIndex = 0;
            typewriterTimer = 0f;
            victoryTextComplete = false;
        }

        public void TriggerGameOver()
        {
            currentGameState = GameState.GameOver;
        }

        private void RestartGame()
        {
            currentGameState = GameState.Tutorial;
            showMessage = false;
            cameraShakeOffset = Vector2.Zero;
            cameraShakeDuration = 0f;
            damageFlashAlpha = 0f;

            player = new Player(new Vector2(64 * 2, 64 * 7));
            tutorialLevel = new TutorialLevel(this, player, random);
            level1 = new Level1(this, player, random);
            level2 = new Level2(this, player, random);
            level3 = new Level3(this, player, random);
            currentLevel = tutorialLevel;
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            switch (currentGameState)
            {
                case GameState.Tutorial:
                case GameState.Level1:
                case GameState.Level2:
                case GameState.Level3:
                    DrawPlaying();
                    break;
                case GameState.Transition:
                case GameState.Level2Transition:
                case GameState.Level3Transition:
                    DrawTransition();
                    break;
                case GameState.Victory:
                    DrawVictory();
                    break;
                case GameState.GameOver:
                    DrawGameOver();
                    break;
                case GameState.Cutscene:
                    DrawCutscene();
                    break;
            }

            base.Draw(gameTime);
        }

        private void DrawPlaying()
        {
            spriteBatch.Begin(transformMatrix: cameraTransform, samplerState: SamplerState.PointClamp);
            
            var textures = new LevelTextures
            {
                Grass = grassTexture, Flower = flowerTexture, Fence = fenceTexture,
                Path = pathTexture, Sign = signTexture, Exit = exitTexture,
                Tree = treeTexture, Water = waterTexture, Stone = stoneTexture,
                Chest = chestTexture, Sand = sandTexture, Cactus = cactusTexture,
                Pyramid = pyramidTexture, Quicksand = quicksandTexture,
                Statue = statueTexture, Shop = shopTexture,
                Player = playerSprite, NPC = npcSprite,
                Scarecrow = scarecrowSprite, Pumpkin = pumpkinSprite,
                Snake = snakeTexture, Spider = spiderTexture, Scorpion = scorpionTexture,
                WateringCan = wateringCanSprite, GardenSpade = gardenSpadeSprite,
                Key = keyTexture, Blade = bladeTexture,
                Coin = coinTexture, Scroll = scrollTexture,
                Pixel = pixelTexture
            };

            currentLevel.Draw(spriteBatch, textures);
            
            spriteBatch.End();

            if (damageFlashAlpha > 0)
            {
                spriteBatch.Begin();
                DrawDamageFlash();
                spriteBatch.End();
            }

            spriteBatch.Begin();
            DrawUI();
            spriteBatch.End();
        }

        private void DrawTransition()
        {
            spriteBatch.Begin();
            Rectangle fullScreen = new Rectangle(0, 0, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
            float alpha = transitionTimer / transitionDuration;
            spriteBatch.Draw(pixelTexture, fullScreen, Color.Black * alpha);

            if (font != null)
            {
                string text = currentGameState == GameState.Transition ? "Entering Level 1..." : 
                             currentGameState == GameState.Level2Transition ? "Entering Level 2..." :
                             currentGameState == GameState.Level3Transition ? "Entering Level 3 - BOSS FIGHT..." :
                             "Loading...";
                Vector2 textSize = font.MeasureString(text);
                Vector2 textPos = new Vector2(
                    graphics.PreferredBackBufferWidth / 2 - textSize.X / 2,
                    graphics.PreferredBackBufferHeight / 2 - textSize.Y / 2);
                spriteBatch.DrawString(font, text, textPos, Color.White);
            }
            spriteBatch.End();
        }

        private void DrawVictory()
        {
            spriteBatch.Begin();
            Rectangle fullScreen = new Rectangle(0, 0, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
            spriteBatch.Draw(pixelTexture, fullScreen, Color.Black * 0.8f);

            if (font != null && displayedVictoryText.Length > 0)
            {
                Vector2 textSize = font.MeasureString(displayedVictoryText);
                Vector2 textPos = new Vector2(
                    graphics.PreferredBackBufferWidth / 2 - textSize.X / 2,
                    graphics.PreferredBackBufferHeight / 2 - textSize.Y / 2);
                spriteBatch.DrawString(font, displayedVictoryText, textPos + new Vector2(3, 3), Color.Black);
                spriteBatch.DrawString(font, displayedVictoryText, textPos, Color.Gold);

                if (victoryTextComplete)
                {
                    string pressKey = "Press ESC to exit";
                    Vector2 keySize = font.MeasureString(pressKey);
                    Vector2 keyPos = new Vector2(
                        graphics.PreferredBackBufferWidth / 2 - keySize.X / 2,
                        graphics.PreferredBackBufferHeight / 2 + 50);
                    spriteBatch.DrawString(font, pressKey, keyPos, Color.White);
                }
            }
            spriteBatch.End();
        }

        private void DrawGameOver()
        {
            spriteBatch.Begin();
            Rectangle fullScreen = new Rectangle(0, 0, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
            spriteBatch.Draw(pixelTexture, fullScreen, Color.Black);

            if (font != null)
            {
                string gameOverText = "GAME OVER";
                Vector2 gameOverSize = font.MeasureString(gameOverText);
                Vector2 gameOverPos = new Vector2(
                    graphics.PreferredBackBufferWidth / 2 - gameOverSize.X / 2,
                    graphics.PreferredBackBufferHeight / 2 - 50);
                spriteBatch.DrawString(font, gameOverText, gameOverPos + new Vector2(3, 3), Color.Black);
                spriteBatch.DrawString(font, gameOverText, gameOverPos, Color.Red);

                string instructionText = "Press R to Restart or ESC to Exit";
                Vector2 instructionSize = font.MeasureString(instructionText);
                Vector2 instructionPos = new Vector2(
                    graphics.PreferredBackBufferWidth / 2 - instructionSize.X / 2,
                    graphics.PreferredBackBufferHeight / 2 + 20);
                spriteBatch.DrawString(font, instructionText, instructionPos, Color.Gray);
            }
            spriteBatch.End();
        }

        private void DrawCutscene()
        {
            spriteBatch.Begin();
            Rectangle fullScreen = new Rectangle(0, 0, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);
            spriteBatch.Draw(pixelTexture, fullScreen, Color.Black);

            if (font != null && displayedCutsceneText.Length > 0)
            {
                Vector2 textSize = font.MeasureString(displayedCutsceneText);
                Vector2 textPos = new Vector2(
                    graphics.PreferredBackBufferWidth / 2 - textSize.X / 2,
                    graphics.PreferredBackBufferHeight / 2 - textSize.Y / 2);
                spriteBatch.DrawString(font, displayedCutsceneText, textPos + new Vector2(3, 3), Color.Black);
                spriteBatch.DrawString(font, displayedCutsceneText, textPos, Color.Yellow);
            }
            spriteBatch.End();
        }

        private void DrawDamageFlash()
        {
            int edge = 40;
            spriteBatch.Draw(pixelTexture, new Rectangle(0, 0, graphics.PreferredBackBufferWidth, edge), Color.Red * damageFlashAlpha);
            spriteBatch.Draw(pixelTexture, new Rectangle(0, graphics.PreferredBackBufferHeight - edge, graphics.PreferredBackBufferWidth, edge), Color.Red * damageFlashAlpha);
            spriteBatch.Draw(pixelTexture, new Rectangle(0, 0, edge, graphics.PreferredBackBufferHeight), Color.Red * damageFlashAlpha);
            spriteBatch.Draw(pixelTexture, new Rectangle(graphics.PreferredBackBufferWidth - edge, 0, edge, graphics.PreferredBackBufferHeight), Color.Red * damageFlashAlpha);
        }

        private void DrawUI()
        {
            currentLevel.DrawUI(spriteBatch, pixelTexture, font, graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight);

            if (font != null)
            {
                string controls = "WASD: Move | SPACE: Attack | E: Interact | ESC: Exit";
                Vector2 controlsSize = font.MeasureString(controls);
                Vector2 controlsPos = new Vector2(
                    graphics.PreferredBackBufferWidth / 2 - controlsSize.X / 2 * 0.6f,
                    graphics.PreferredBackBufferHeight - 30);
                spriteBatch.DrawString(font, controls, controlsPos, Color.White, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
            }

            if (showMessage && messageTimer > 0 && font != null)
            {
                Vector2 msgSize = font.MeasureString(displayMessage);
                Vector2 msgPos = new Vector2(
                    graphics.PreferredBackBufferWidth / 2 - msgSize.X / 2 * 0.8f,
                    graphics.PreferredBackBufferHeight - 80);
                Rectangle msgBg = new Rectangle(
                    (int)(msgPos.X - 10), (int)(msgPos.Y - 10),
                    (int)(msgSize.X * 0.8f + 20), (int)(msgSize.Y * 0.8f + 20));
                spriteBatch.Draw(pixelTexture, msgBg, Color.Black * 0.9f);
                DrawRect(msgBg, Color.Gold, 2);
                spriteBatch.DrawString(font, displayMessage, msgPos, Color.White, 0f, Vector2.Zero, 0.8f, SpriteEffects.None, 0f);
            }

            if (showDialogue && dialogueTimer > 0 && font != null)
            {
                Rectangle dialogueBg = new Rectangle(
                    graphics.PreferredBackBufferWidth / 2 - 300,
                    graphics.PreferredBackBufferHeight - 200,
                    600, 150);
                spriteBatch.Draw(pixelTexture, dialogueBg, Color.Black * 0.9f);
                DrawRect(dialogueBg, Color.Gold, 3);

                Vector2 namePos = new Vector2(dialogueBg.X + 20, dialogueBg.Y + 15);
                spriteBatch.DrawString(font, npcName, namePos, Color.Gold, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);

                Vector2 textPos = new Vector2(dialogueBg.X + 20, dialogueBg.Y + 50);
                spriteBatch.DrawString(font, dialogueText, textPos, Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            }
        }

        private void DrawRect(Rectangle rect, Color color, int width)
        {
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, width), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Bottom - width, rect.Width, width), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, width, rect.Height), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.Right - width, rect.Y, width, rect.Height), color);
        }
    }

    public enum GameState
    {
        Tutorial = 0,
        Level1 = 1,
        Level2 = 2,
        Transition = 3,
        Victory = 4,
        GameOver = 5,
        Cutscene = 6,
        Level2Transition = 7,
        Level3 = 8, // <-- Add this line
        Level3Transition = 9 // <-- Add this if you use Level3Transition elsewhere
    }
}