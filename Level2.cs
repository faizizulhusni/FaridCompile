using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace RelicEscape
{
    public class Level2 : BaseLevel
    {
        private List<Enemy> enemies;
        private List<InteractiveObject> objects;
        private List<ItemDrop> itemDrops;
        private List<MathPuzzle> puzzles;
        private List<Vector2> quicksandPositions;
        private Shop shop;

        private bool hasScrollOfAntimatter = false;
        private bool level2PuzzleSolved = false;
        private float scorpionSpawnTimer = 0f;
        private float scorpionSpawnDelay = 8f;

        public Level2(GameLevelManager game, Player player, Random random) : base(game, player, random)
        {
        }

        protected override void InitializeMap()
        {
            map = new TileType[MapWidth, MapHeight];
            quicksandPositions = new List<Vector2>();

            for (int x = 0; x < MapWidth; x++)
                for (int y = 0; y < MapHeight; y++)
                    map[x, y] = TileType.Sand;

            for (int x = 0; x < MapWidth; x++)
            {
                map[x, 0] = TileType.Cactus;
                map[x, MapHeight - 1] = TileType.Cactus;
            }
            for (int y = 0; y < MapHeight; y++)
            {
                map[0, y] = TileType.Cactus;
                map[MapWidth - 1, y] = TileType.Cactus;
            }

            map[10, 7] = TileType.Pyramid;
            map[9, 7] = TileType.Pyramid;
            map[11, 7] = TileType.Pyramid;
            map[10, 6] = TileType.Pyramid;
            map[10, 8] = TileType.Pyramid;
            map[10, 9] = TileType.Exit;

            PlaceQuicksandPatch(3, 3, 2, 2);
            PlaceQuicksandPatch(15, 3, 2, 2);
            PlaceQuicksandPatch(5, 10, 3, 2);
            PlaceQuicksandPatch(12, 11, 2, 2);

            map[4, 4] = TileType.Statue;
            map[7, 5] = TileType.Statue;
            map[13, 4] = TileType.Statue;
            map[16, 6] = TileType.Statue;
            map[6, 12] = TileType.Statue;
            map[14, 12] = TileType.Statue;

            PlaceCactusPatch(2, 2, 3, 2);
            PlaceCactusPatch(15, 2, 3, 2);
            PlaceCactusPatch(3, 12, 4, 2);
            PlaceCactusPatch(13, 12, 4, 2);

            map[17, 3] = TileType.Shop;
            map[5, 5] = TileType.Chest;
            map[15, 5] = TileType.Chest;
            map[8, 12] = TileType.Chest;
        }

        private void PlaceQuicksandPatch(int startX, int startY, int width, int height)
        {
            for (int x = startX; x < startX + width; x++)
            {
                for (int y = startY; y < startY + height; y++)
                {
                    if (x < MapWidth && y < MapHeight)
                    {
                        map[x, y] = TileType.Quicksand;
                        quicksandPositions.Add(new Vector2(x * tileSize, y * tileSize));
                    }
                }
            }
        }

        private void PlaceCactusPatch(int startX, int startY, int width, int height)
        {
            for (int x = startX; x < startX + width; x++)
                for (int y = startY; y < startY + height; y++)
                    if (x < MapWidth && y < MapHeight)
                        map[x, y] = TileType.Cactus;
        }

        protected override void InitializeEntities()
        {
            enemies = new List<Enemy>();
            objects = new List<InteractiveObject>();
            itemDrops = new List<ItemDrop>();
            puzzles = new List<MathPuzzle>();

            player.Inventory.RemoveAll(item => item == "Key");

            enemies.Add(new Enemy(new Vector2(tileSize * 5, tileSize * 3), EnemyType.Scorpion, true));
            enemies.Add(new Enemy(new Vector2(tileSize * 15, tileSize * 3), EnemyType.Scorpion, true));
            enemies.Add(new Enemy(new Vector2(tileSize * 4, tileSize * 11), EnemyType.Scorpion));
            enemies.Add(new Enemy(new Vector2(tileSize * 13, tileSize * 12), EnemyType.Scorpion));

            objects.Add(new InteractiveObject(new Vector2(tileSize * 5, tileSize * 5), "Chest", 64, 64));
            objects.Add(new InteractiveObject(new Vector2(tileSize * 15, tileSize * 5), "Chest", 64, 64));
            objects.Add(new InteractiveObject(new Vector2(tileSize * 8, tileSize * 12), "Chest", 64, 64));

            puzzles.Add(new MathPuzzle(new Vector2(tileSize * 10, tileSize * 5)));
            shop = new Shop(new Vector2(tileSize * 17, tileSize * 3));
        }

        public override void Update(float deltaTime, KeyboardState keyState, KeyboardState prevKeyState)
        {
            int previousHealth = player.Health;
            player.Update(deltaTime, keyState, prevKeyState);

            if (player.Health < previousHealth) game.TriggerDamageEffect();
            if (player.State == EntityState.Dead) { game.TriggerGameOver(); return; }

            player.Position.X = MathHelper.Clamp(player.Position.X, tileSize, (MapWidth - 2) * tileSize);
            player.Position.Y = MathHelper.Clamp(player.Position.Y, tileSize, (MapHeight - 2) * tileSize);

            CheckTileCollisions();
            CheckQuicksandCollisions();

            foreach (var enemy in enemies)
            {
                int healthBefore = player.Health;
                enemy.Update(deltaTime, player, random);
                if (player.Health < healthBefore) game.TriggerDamageEffect();
            }

            foreach (var item in itemDrops)
                item.Update(deltaTime);

            scorpionSpawnTimer += deltaTime;
            if (scorpionSpawnTimer >= scorpionSpawnDelay)
            {
                SpawnScorpionsFromQuicksand();
                scorpionSpawnTimer = 0f;
            }

            if (player.IsAttacking) CheckPlayerAttack();
            CheckItemPickups(keyState, prevKeyState);
            CheckObjectInteractions(keyState, prevKeyState);
            CheckPuzzleInteraction(keyState, prevKeyState);
            CheckShopInteraction(keyState, prevKeyState);
            CheckExitReached();
        }

        private void CheckQuicksandCollisions()
        {
            foreach (var quicksandPos in quicksandPositions)
            {
                Rectangle quicksandRect = new Rectangle((int)quicksandPos.X, (int)quicksandPos.Y, tileSize, tileSize);
                if (player.Bounds.Intersects(quicksandRect))
                {
                    player.QuicksandSlowTimer = 0.5f;
                    break;
                }
            }
        }

        private void SpawnScorpionsFromQuicksand()
        {
            if (quicksandPositions.Count > 0)
            {
                var quicksandPos = quicksandPositions[random.Next(0, quicksandPositions.Count)];
                enemies.Add(new Enemy(quicksandPos, EnemyType.Scorpion));
                game.ShowMessage("Scorpion emerged from quicksand!");
            }
        }

        private void CheckPlayerAttack()
        {
            if (player.HasDealtDamageThisAttack) return;

            foreach (var enemy in enemies)
            {
                if (enemy.State == EntityState.Dead) continue;

                float distance = Vector2.Distance(player.Position, enemy.Position);
                if (distance < player.AttackRange && player.IsAttacking)
                {
                    int damage = (int)Math.Ceiling(enemy.MaxHealth * (player.CurrentWeapon.DamagePercent / 100f));
                    enemy.TakeDamage(damage);
                    player.HasDealtDamageThisAttack = true;

                    if (enemy.State == EntityState.Dead && enemy.DropsKey)
                    {
                        itemDrops.Add(new ItemDrop(enemy.Position, "Key"));
                        enemy.DropsKey = false;
                        game.ShowMessage($"Key dropped! Press E to pick up ({player.GetKeyCount()}/2 keys)");
                    }

                    if (enemy.State == EntityState.Dead && enemy.Type == EnemyType.Scorpion)
                    {
                        if (random.Next(0, 100) < 30)
                        {
                            itemDrops.Add(new ItemDrop(enemy.Position, "Desert Coin"));
                            game.ShowMessage("Desert Coin dropped!");
                        }
                    }
                }
            }
        }

        private void CheckItemPickups(KeyboardState keyState, KeyboardState prevKeyState)
        {
            bool ePressed = keyState.IsKeyDown(Keys.E) && prevKeyState.IsKeyUp(Keys.E);

            for (int i = itemDrops.Count - 1; i >= 0; i--)
            {
                var item = itemDrops[i];
                float distance = Vector2.Distance(player.Position, item.Position);

                if (distance < 50f && ePressed)
                {
                    if (item.ItemName == "Key")
                    {
                        player.AddToInventory(item.ItemName);
                        int keyCount = player.GetKeyCount();
                        game.ShowMessage($"Picked up Key! ({keyCount}/2 keys)");
                        itemDrops.RemoveAt(i);

                        if (keyCount >= 2)
                        {
                            game.ShowMessage("You have both keys! Find the puzzle to claim the relic!");
                        }
                    }
                    else if (item.ItemName == "Desert Coin")
                    {
                        player.AddToInventory(item.ItemName);
                        game.ShowMessage($"Desert Coin collected! ({player.GetDesertCoinCount()} coins)");
                        itemDrops.RemoveAt(i);
                    }
                    else if (item.ItemName == "Scroll of Antimatter")
                    {
                        RelicItem scroll = new RelicItem("Scroll of Antimatter", Color.Purple, 75);
                        player.EquipRelic(scroll);
                        player.AddToInventory(item.ItemName);
                        hasScrollOfAntimatter = true;
                        game.ShowMessage("Scroll of Antimatter acquired! Ultimate power unlocked!");
                        itemDrops.RemoveAt(i);
                    }
                }
            }
        }

        private void CheckObjectInteractions(KeyboardState keyState, KeyboardState prevKeyState)
        {
            bool ePressed = keyState.IsKeyDown(Keys.E) && prevKeyState.IsKeyUp(Keys.E);

            foreach (var obj in objects)
            {
                float distance = Vector2.Distance(player.Position, obj.Position);

                if (obj.Type == "Chest" && !obj.IsActivated && distance < 60f)
                {
                    if (ePressed)
                    {
                        obj.IsActivated = true;
                        for (int i = 0; i < 3; i++)
                        {
                            itemDrops.Add(new ItemDrop(
                                new Vector2(obj.Position.X + random.Next(-20, 20), obj.Position.Y + random.Next(-20, 20)),
                                "Desert Coin"
                            ));
                        }
                        game.ShowMessage("Chest opened! Desert Coins found!");
                    }
                    else game.ShowMessage("Press E to open chest");
                }
            }
        }

        private void CheckPuzzleInteraction(KeyboardState keyState, KeyboardState prevKeyState)
        {
            bool ePressed = keyState.IsKeyDown(Keys.E) && prevKeyState.IsKeyUp(Keys.E);

            foreach (var puzzle in puzzles)
            {
                float distance = Vector2.Distance(player.Position, puzzle.Position);

                if (distance < 60f && !puzzle.IsSolved)
                {
                    if (player.GetKeyCount() >= 2)
                    {
                        if (ePressed)
                        {
                            puzzle.IsActive = true;
                        }
                        else
                        {
                            game.ShowMessage("Press E to solve puzzle (2 keys required)");
                        }
                    }
                    else
                    {
                        game.ShowMessage($"Need 2 keys to solve puzzle ({player.GetKeyCount()}/2)");
                    }
                }

                if (puzzle.IsActive)
                {
                    if (keyState.IsKeyDown(Keys.Enter) && prevKeyState.IsKeyUp(Keys.Enter))
                    {
                        int finalAnswer = puzzle.IsNegative ? -puzzle.PlayerAnswer : puzzle.PlayerAnswer;
                        if (finalAnswer == puzzle.Answer)
                        {
                            puzzle.IsSolved = true;
                            puzzle.IsActive = false;
                            level2PuzzleSolved = true;

                            itemDrops.Add(new ItemDrop(
                                new Vector2(puzzle.Position.X, puzzle.Position.Y - 50),
                                "Scroll of Antimatter"
                            ));
                            game.ShowMessage("Puzzle solved! Scroll of Antimatter appeared!");
                        }
                        else
                        {
                            game.ShowMessage("Wrong answer! Try again.");
                            puzzle.PlayerAnswer = 0;
                            puzzle.IsNegative = false;
                        }
                    }

                    // Handle minus key to toggle negative
                    if ((keyState.IsKeyDown(Keys.OemMinus) || keyState.IsKeyDown(Keys.Subtract)) && 
                        (prevKeyState.IsKeyUp(Keys.OemMinus) && prevKeyState.IsKeyUp(Keys.Subtract)))
                    {
                        puzzle.IsNegative = !puzzle.IsNegative;
                    }

                    for (Keys key = Keys.D0; key <= Keys.D9; key++)
                    {
                        if (keyState.IsKeyDown(key) && prevKeyState.IsKeyUp(key))
                        {
                            puzzle.PlayerAnswer = puzzle.PlayerAnswer * 10 + (int)(key - Keys.D0);
                        }
                    }
                    
                    // Also support numpad numbers
                    for (Keys key = Keys.NumPad0; key <= Keys.NumPad9; key++)
                    {
                        if (keyState.IsKeyDown(key) && prevKeyState.IsKeyUp(key))
                        {
                            puzzle.PlayerAnswer = puzzle.PlayerAnswer * 10 + (int)(key - Keys.NumPad0);
                        }
                    }

                    if (keyState.IsKeyDown(Keys.Back) && prevKeyState.IsKeyUp(Keys.Back))
                    {
                        puzzle.PlayerAnswer = puzzle.PlayerAnswer / 10;
                    }
                }
            }
        }

        private void CheckShopInteraction(KeyboardState keyState, KeyboardState prevKeyState)
        {
            bool ePressed = keyState.IsKeyDown(Keys.E) && prevKeyState.IsKeyUp(Keys.E);

            if (shop != null)
            {
                float distance = Vector2.Distance(player.Position, shop.Position);

                if (distance < 60f)
                {
                    if (ePressed)
                    {
                        shop.IsOpen = true;
                        game.ShowMessage("Shop opened! Press 1-3 to buy, TAB to close");
                    }
                    else if (!shop.IsOpen)
                    {
                        game.ShowMessage("Press E to open shop");
                    }
                }

                if (shop.IsOpen)
                {
                    if (keyState.IsKeyDown(Keys.Tab) && prevKeyState.IsKeyUp(Keys.Tab))
                    {
                        shop.IsOpen = false;
                    }

                    if (keyState.IsKeyDown(Keys.D1) && prevKeyState.IsKeyUp(Keys.D1))
                    {
                        if (player.GetDesertCoinCount() >= shop.Prices[0])
                        {
                            for (int i = 0; i < shop.Prices[0]; i++)
                                player.Inventory.Remove("Desert Coin");
                            player.Health = Math.Min(player.MaxHealth, player.Health + 30);
                            game.ShowMessage("Health Potion purchased! +30 HP");
                        }
                        else game.ShowMessage("Not enough Desert Coins!");
                    }

                    if (keyState.IsKeyDown(Keys.D2) && prevKeyState.IsKeyUp(Keys.D2))
                    {
                        if (player.GetDesertCoinCount() >= shop.Prices[1])
                        {
                            for (int i = 0; i < shop.Prices[1]; i++)
                                player.Inventory.Remove("Desert Coin");
                            player.Speed = 200f;
                            game.ShowMessage("Speed Boost purchased! Movement increased!");
                        }
                        else game.ShowMessage("Not enough Desert Coins!");
                    }

                    if (keyState.IsKeyDown(Keys.D3) && prevKeyState.IsKeyUp(Keys.D3))
                    {
                        if (player.GetDesertCoinCount() >= shop.Prices[2])
                        {
                            for (int i = 0; i < shop.Prices[2]; i++)
                                player.Inventory.Remove("Desert Coin");
                            player.CurrentWeapon.DamagePercent += 20;
                            game.ShowMessage("Damage Boost purchased! +20% damage!");
                        }
                        else game.ShowMessage("Not enough Desert Coins!");
                    }
                }
            }
        }

        private void CheckExitReached()
        {
            if (!hasScrollOfAntimatter) return;

            Rectangle exitRect = new Rectangle(10 * tileSize, 9 * tileSize, tileSize, tileSize);
            if (player.Bounds.Intersects(exitRect))
            {
                game.TransitionToCutscene("Entering the Infernal Depths...\nPrepare for the ultimate challenge!\nThe boss awaits...");
            }
        }

        public override void Draw(SpriteBatch spriteBatch, LevelTextures textures)
        {
            DrawMap(spriteBatch, textures);
            DrawObjects(spriteBatch, textures);
            DrawEnemies(spriteBatch, textures);
            DrawPuzzles(spriteBatch, textures);
            DrawShop(spriteBatch, textures);
            DrawItemDrops(spriteBatch, textures);
            DrawPlayer(spriteBatch, textures);
        }

        private void DrawObjects(SpriteBatch spriteBatch, LevelTextures textures)
        {
            foreach (var obj in objects)
            {
                if (obj.Type == "Chest")
                {
                    spriteBatch.Draw(textures.Pixel, obj.Bounds, obj.IsActivated ? Color.Yellow * 0.5f : Color.DarkGoldenrod);
                }
                DrawRect(spriteBatch, obj.Bounds, Color.Black, 2, textures.Pixel);
            }
        }

        private void DrawEnemies(SpriteBatch spriteBatch, LevelTextures textures)
        {
            foreach (var enemy in enemies)
            {
                if (enemy.State == EntityState.Dead) continue;

                Rectangle enemyRect = new Rectangle((int)enemy.Position.X, (int)enemy.Position.Y, 32, 32);

                if (textures.Scorpion != null)
                {
                    spriteBatch.Draw(textures.Scorpion, enemyRect, null, Color.White, 0f, Vector2.Zero, enemy.FacingDirection, 0f);
                }
                else
                {
                    spriteBatch.Draw(textures.Pixel, enemyRect, Color.DarkRed);
                    DrawRect(spriteBatch, enemyRect, Color.Black, 2, textures.Pixel);
                }

                float healthPercent = (float)enemy.Health / enemy.MaxHealth;
                Rectangle healthBar = new Rectangle((int)enemy.Position.X, (int)enemy.Position.Y - 8, (int)(32 * healthPercent), 4);
                spriteBatch.Draw(textures.Pixel, healthBar, Color.Red);
            }
        }

        private void DrawPuzzles(SpriteBatch spriteBatch, LevelTextures textures)
        {
            foreach (var puzzle in puzzles)
            {
                Rectangle puzzleRect = new Rectangle((int)puzzle.Position.X, (int)puzzle.Position.Y, 64, 64);
                Color puzzleColor = puzzle.IsSolved ? Color.Green : (puzzle.IsActive ? Color.Yellow : Color.Blue);
                spriteBatch.Draw(textures.Pixel, puzzleRect, puzzleColor * 0.7f);
                DrawRect(spriteBatch, puzzleRect, Color.White, 2, textures.Pixel);
            }
        }

        private void DrawShop(SpriteBatch spriteBatch, LevelTextures textures)
        {
            if (shop == null) return;

            Rectangle shopRect = new Rectangle((int)shop.Position.X, (int)shop.Position.Y, 64, 64);

            if (textures.Shop != null)
            {
                spriteBatch.Draw(textures.Shop, shopRect, Color.White);
            }
            else
            {
                spriteBatch.Draw(textures.Pixel, shopRect, new Color(160, 120, 80));
            }

            DrawRect(spriteBatch, shopRect, Color.Gold, 2, textures.Pixel);
        }

        private void DrawItemDrops(SpriteBatch spriteBatch, LevelTextures textures)
        {
            foreach (var item in itemDrops)
            {
                Rectangle glowRect = new Rectangle(item.Bounds.X - 4, item.Bounds.Y - 4, item.Bounds.Width + 8, item.Bounds.Height + 8);
                spriteBatch.Draw(textures.Pixel, glowRect, item.DisplayColor * 0.3f);

                Texture2D itemSprite = item.ItemName switch
                {
                    "Key" => textures.Key,
                    "Desert Coin" => textures.Coin,
                    "Scroll of Antimatter" => textures.Scroll,
                    _ => null
                };

                if (itemSprite != null)
                {
                    spriteBatch.Draw(itemSprite, item.Bounds, Color.White);
                }
                else
                {
                    spriteBatch.Draw(textures.Pixel, item.Bounds, item.DisplayColor);
                }

                DrawRect(spriteBatch, item.Bounds, Color.White, 2, textures.Pixel);
            }
        }

        public override void DrawUI(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font, int screenWidth, int screenHeight)
        {
            Rectangle uiPanel = new Rectangle(10, 10, 400, 200);
            spriteBatch.Draw(pixelTexture, uiPanel, Color.Black * 0.7f);
            DrawRect(spriteBatch, uiPanel, Color.Gold, 2, pixelTexture);

            int yPos = 20;

            Rectangle healthBarBg = new Rectangle(20, yPos, 250, 20);
            Rectangle healthBarFg = new Rectangle(20, yPos, (int)(250 * (player.Health / 100f)), 20);
            spriteBatch.Draw(pixelTexture, healthBarBg, Color.DarkRed);
            spriteBatch.Draw(pixelTexture, healthBarFg, Color.LimeGreen);
            DrawRect(spriteBatch, healthBarBg, Color.White, 2, pixelTexture);

            if (font != null)
            {
                spriteBatch.DrawString(font, $"HP: {player.Health}/100", new Vector2(25, yPos + 2), Color.White, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                yPos += 35;

                spriteBatch.DrawString(font, "Keys:", new Vector2(20, yPos), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                yPos += 25;

                int keyCount = player.GetKeyCount();
                for (int i = 0; i < 2; i++)
                {
                    Rectangle keyBox = new Rectangle(25 + i * 40, yPos, 30, 30);
                    Color keyColor = i < keyCount ? Color.Gold : Color.Gray * 0.3f;
                    spriteBatch.Draw(pixelTexture, keyBox, keyColor);
                    DrawRect(spriteBatch, keyBox, Color.White, 2, pixelTexture);
                }
                yPos += 45;

                spriteBatch.DrawString(font, "Desert Coins:", new Vector2(20, yPos), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                yPos += 25;

                int coinCount = player.GetDesertCoinCount();
                spriteBatch.DrawString(font, $"{coinCount} coins", new Vector2(25, yPos), Color.Orange, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                yPos += 30;

                spriteBatch.DrawString(font, $"Weapon: {player.CurrentWeapon.Name}", new Vector2(20, yPos), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                yPos += 25;
                spriteBatch.DrawString(font, $"Damage: {player.CurrentWeapon.DamagePercent}", new Vector2(20, yPos), Color.Yellow, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            }

            DrawLevel2Objectives(spriteBatch, pixelTexture, font);

            // Draw puzzle UI if active
            foreach (var puzzle in puzzles)
            {
                if (puzzle.IsActive && font != null)
                {
                    Rectangle puzzleUIRect = new Rectangle(screenWidth / 2 - 200, 100, 400, 120);
                    spriteBatch.Draw(pixelTexture, puzzleUIRect, Color.Black * 0.95f);
                    DrawRect(spriteBatch, puzzleUIRect, Color.Cyan, 3, pixelTexture);

                    Vector2 titlePos = new Vector2(puzzleUIRect.X + 10, puzzleUIRect.Y + 10);
                    spriteBatch.DrawString(font, "Math Puzzle", titlePos, Color.Cyan);

                    Vector2 questionPos = new Vector2(puzzleUIRect.X + 20, puzzleUIRect.Y + 40);
                    spriteBatch.DrawString(font, puzzle.Question, questionPos, Color.White, 0f, Vector2.Zero, 1.2f, SpriteEffects.None, 0f);

                    Vector2 answerPos = new Vector2(puzzleUIRect.X + 20, puzzleUIRect.Y + 70);
                    int displayAnswer = puzzle.IsNegative ? -puzzle.PlayerAnswer : puzzle.PlayerAnswer;
                    spriteBatch.DrawString(font, $"Your Answer: {displayAnswer}", answerPos, Color.Yellow);

                    Vector2 helpPos = new Vector2(puzzleUIRect.X + 20, puzzleUIRect.Y + 95);
                    spriteBatch.DrawString(font, "Type number, - for negative, BACKSPACE to delete, ENTER to submit", helpPos, Color.LightGray, 0f, Vector2.Zero, 0.55f, SpriteEffects.None, 0f);
                }
            }

            // Draw shop UI if open
            if (shop != null && shop.IsOpen && font != null)
            {
                Rectangle shopUIRect = new Rectangle(screenWidth / 2 - 150, screenHeight / 2 - 100, 300, 200);
                spriteBatch.Draw(pixelTexture, shopUIRect, Color.Black * 0.9f);
                DrawRect(spriteBatch, shopUIRect, Color.Gold, 3, pixelTexture);

                Vector2 titlePos = new Vector2(shopUIRect.X + 10, shopUIRect.Y + 10);
                spriteBatch.DrawString(font, "Desert Merchant", titlePos, Color.Gold);

                Vector2 itemPos = new Vector2(shopUIRect.X + 20, shopUIRect.Y + 40);
                for (int i = 0; i < shop.Items.Count; i++)
                {
                    spriteBatch.DrawString(font, $"{i + 1}. {shop.Items[i]} - {shop.Prices[i]} coins", itemPos, Color.White);
                    itemPos.Y += 25;
                }

                Vector2 coinPos = new Vector2(shopUIRect.X + 20, shopUIRect.Y + 130);
                spriteBatch.DrawString(font, $"Your coins: {player.GetDesertCoinCount()}", coinPos, Color.Yellow);

                Vector2 helpPos = new Vector2(shopUIRect.X + 20, shopUIRect.Y + 160);
                spriteBatch.DrawString(font, "Press 1-3 to buy, TAB to close", helpPos, Color.LightGray);
            }
        }

        private void DrawLevel2Objectives(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font)
        {
            Rectangle objectivePanel = new Rectangle(10, 220, 400, 150);
            spriteBatch.Draw(pixelTexture, objectivePanel, Color.Black * 0.7f);
            DrawRect(spriteBatch, objectivePanel, Color.Gold, 2, pixelTexture);

            if (font != null)
            {
                spriteBatch.DrawString(font, "Desert Objectives:", new Vector2(20, 230), Color.Gold, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

                int objY = 255;
                DrawObjective(spriteBatch, font, "Collect 2 Keys", player.GetKeyCount() >= 2, 20, objY);
                objY += 20;
                DrawObjective(spriteBatch, font, "Solve Math Puzzle", level2PuzzleSolved, 20, objY);
                objY += 20;
                DrawObjective(spriteBatch, font, "Acquire Scroll of Antimatter", hasScrollOfAntimatter, 20, objY);
                objY += 20;
                DrawObjective(spriteBatch, font, "Reach the Pyramid Exit", hasScrollOfAntimatter, 20, objY);
            }
        }

        private void DrawObjective(SpriteBatch spriteBatch, SpriteFont font, string text, bool completed, int x, int y)
        {
            if (font == null) return;

            Color color = completed ? Color.LimeGreen : Color.Gray;
            string prefix = completed ? "[X] " : "[ ] ";
            spriteBatch.DrawString(font, prefix + text, new Vector2(x, y), color, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
        }
    }
}