using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace RelicEscape
{
    public class Level1 : BaseLevel
    {
        private List<Enemy> enemies;
        private List<InteractiveObject> objects;
        private List<ItemDrop> itemDrops;

        private bool hasRelicBlade = false;
        private bool vineIsPushed = false;
        private float enemyRespawnTimer = 0f;
        private float enemyRespawnDelay = 10f;

        public Level1(GameLevelManager game, Player player, Random random) : base(game, player, random)
        {
        }

        protected override void InitializeMap()
        {
            map = new TileType[MapWidth, MapHeight];

            for (int x = 0; x < MapWidth; x++)
                for (int y = 0; y < MapHeight; y++)
                    map[x, y] = TileType.Grass;

            for (int x = 0; x < MapWidth; x++)
            {
                map[x, 0] = TileType.Tree;
                map[x, MapHeight - 1] = TileType.Tree;
            }
            for (int y = 0; y < MapHeight; y++)
            {
                map[0, y] = TileType.Tree;
                map[MapWidth - 1, y] = TileType.Tree;
            }

            for (int x = 1; x < 15; x++) map[x, 10] = TileType.Water;

            map[3, 3] = TileType.Tree;
            map[5, 4] = TileType.Tree;
            map[10, 3] = TileType.Tree;
            map[12, 5] = TileType.Tree;
            map[4, 7] = TileType.Tree;
            map[11, 8] = TileType.Tree;

            map[13, 13] = TileType.Chest;
            map[14, 13] = TileType.Exit;
        }

        protected override void InitializeEntities()
        {
            enemies = new List<Enemy>();
            objects = new List<InteractiveObject>();
            itemDrops = new List<ItemDrop>();

            Vector2 snakePos = FindValidSpawnPosition(6, 5);
            Vector2 spiderPos = FindValidSpawnPosition(10, 6);

            enemies.Add(new Enemy(snakePos, EnemyType.Snake, true));
            enemies.Add(new Enemy(spiderPos, EnemyType.Spider, true));

            objects.Add(new InteractiveObject(new Vector2(tileSize * 2, tileSize * 9), "Vine", 64, 64));
            objects.Add(new InteractiveObject(new Vector2(tileSize * 13, tileSize * 13), "Chest", 64, 64));
        }

        private Vector2 FindValidSpawnPosition(int gridX, int gridY)
        {
            if (map[gridX, gridY] != TileType.Water && map[gridX, gridY] != TileType.Tree)
            {
                return new Vector2(gridX * tileSize, gridY * tileSize);
            }

            for (int x = gridX - 1; x <= gridX + 1; x++)
            {
                for (int y = gridY - 1; y <= gridY + 1; y++)
                {
                    if (x >= 0 && x < MapWidth && y >= 0 && y < MapHeight)
                    {
                        if (map[x, y] != TileType.Water && map[x, y] != TileType.Tree)
                        {
                            return new Vector2(x * tileSize, y * tileSize);
                        }
                    }
                }
            }

            return new Vector2(gridX * tileSize, gridY * tileSize);
        }

        protected override bool IsBlockingTile(TileType tile, int x, int y)
        {
            bool isWaterBlocking = tile == TileType.Water;
            if (tile == TileType.Water && x == 2 && y == 10 && vineIsPushed)
                isWaterBlocking = false;

            return base.IsBlockingTile(tile, x, y) || isWaterBlocking;
        }

        public override void Update(float deltaTime, KeyboardState keyState, KeyboardState prevKeyState)
        {
            if (enemies.Count == 0)
            {
                InitializeEntities();
            }

            int previousHealth = player.Health;
            player.Update(deltaTime, keyState, prevKeyState);

            if (player.Health < previousHealth) game.TriggerDamageEffect();
            if (player.State == EntityState.Dead) { game.TriggerGameOver(); return; }

            player.Position.X = MathHelper.Clamp(player.Position.X, tileSize, (MapWidth - 2) * tileSize);
            player.Position.Y = MathHelper.Clamp(player.Position.Y, tileSize, (MapHeight - 2) * tileSize);

            CheckTileCollisions();

            foreach (var enemy in enemies)
            {
                if (enemy.State != EntityState.Dead)
                {
                    int healthBefore = player.Health;
                    enemy.Update(deltaTime, player, random);
                    if (player.Health < healthBefore) game.TriggerDamageEffect();
                }
            }

            foreach (var item in itemDrops)
                item.Update(deltaTime);

            if (player.IsAttacking) CheckPlayerAttack();
            CheckItemPickups(keyState, prevKeyState);
            CheckObjectInteractions(keyState, prevKeyState);

            enemyRespawnTimer += deltaTime;
            if (enemyRespawnTimer >= enemyRespawnDelay)
            {
                RespawnEnemies();
                enemyRespawnTimer = 0f;
            }

            if (hasRelicBlade) CheckExitReached();
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
                        bool shouldDropKey = false;

                        if (enemy.Type == EnemyType.Snake && player.GetKeyCount() == 0)
                            shouldDropKey = true;
                        else if (enemy.Type == EnemyType.Spider && player.GetKeyCount() == 1)
                            shouldDropKey = true;
                        else if (enemy.Type == EnemyType.Snake && player.GetKeyCount() == 2)
                            shouldDropKey = true;

                        if (shouldDropKey)
                        {
                            itemDrops.Add(new ItemDrop(enemy.Position, "Key"));
                            enemy.DropsKey = false;
                            game.ShowMessage($"Key dropped! Press E to pick up ({player.GetKeyCount()}/3 keys)");
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
                        game.ShowMessage($"Picked up Key! ({keyCount}/3 keys)");
                        itemDrops.RemoveAt(i);
                    }
                    else if (item.ItemName == "Spiritvine Blade")
                    {
                        RelicItem spiritvineBlade = new RelicItem("Spiritvine Blade", Color.Cyan, 50);
                        player.EquipRelic(spiritvineBlade);
                        player.AddToInventory(item.ItemName);
                        hasRelicBlade = true;
                        game.ShowMessage("Spiritvine Blade equipped! Damage increased!");
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

                if (obj.Type == "Vine" && !obj.IsActivated && distance < 60f && ePressed)
                {
                    obj.Position.Y = tileSize * 10 - 16;
                    obj.IsActivated = true;
                    vineIsPushed = true;
                    map[2, 10] = TileType.Stone;
                    game.ShowMessage("Stone pushed! Bridge created!");
                }

                if (obj.Type == "Chest" && !obj.IsActivated && distance < 60f)
                {
                    if (player.GetKeyCount() >= 3)
                    {
                        if (ePressed)
                        {
                            hasRelicBlade = true;
                            obj.IsActivated = true;
                            itemDrops.Add(new ItemDrop(new Vector2(obj.Position.X + 20, obj.Position.Y + 40), "Spiritvine Blade"));
                            game.ShowMessage("Chest opened! Spiritvine Blade appeared! Press E to pick up");
                        }
                        else game.ShowMessage("Press E to open chest (3 keys required)");
                    }
                    else game.ShowMessage($"Need 3 keys to open! ({player.GetKeyCount()}/3)");
                }
            }
        }

        private void RespawnEnemies()
        {
            foreach (var enemy in enemies.Where(e => e.State == EntityState.Dead))
            {
                int x, y;
                do
                {
                    x = random.Next(2, MapWidth - 2);
                    y = random.Next(2, 8);
                } while (map[x, y] == TileType.Water || map[x, y] == TileType.Tree);

                enemy.Position = new Vector2(x * tileSize, y * tileSize);
                enemy.Health = enemy.MaxHealth;
                enemy.State = EntityState.Idle;
                enemy.DropsKey = true;
            }
        }

        private void CheckExitReached()
        {
            if (!player.HasItem("Spiritvine Blade")) return;

            Rectangle exitRect = new Rectangle(14 * tileSize, 13 * tileSize, tileSize, tileSize);
            if (player.Bounds.Intersects(exitRect))
            {
                game.TransitionToCutscene("Welcome to the Desert of Echoes\nFind 2 keys and solve the puzzle\nto claim the Scroll of Antimatter!");
            }
        }

        public override void Draw(SpriteBatch spriteBatch, LevelTextures textures)
        {
            DrawMap(spriteBatch, textures);
            DrawObjects(spriteBatch, textures);
            DrawEnemies(spriteBatch, textures);
            DrawItemDrops(spriteBatch, textures);
            DrawPlayer(spriteBatch, textures);
        }

        private void DrawObjects(SpriteBatch spriteBatch, LevelTextures textures)
        {
            foreach (var obj in objects)
            {
                if (obj.Type == "Vine")
                {
                    spriteBatch.Draw(textures.Pixel, obj.Bounds, obj.IsActivated ? Color.Brown : Color.DarkGreen);
                }
                else if (obj.Type == "Chest")
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

                Texture2D enemyTex = enemy.Type == EnemyType.Snake ? textures.Snake : textures.Spider;
                if (enemyTex != null)
                {
                    spriteBatch.Draw(enemyTex, enemyRect, null, Color.White, 0f, Vector2.Zero, enemy.FacingDirection, 0f);
                }
                else
                {
                    Color enemyColor = enemy.Type == EnemyType.Snake ? Color.LimeGreen : Color.DarkViolet;
                    spriteBatch.Draw(textures.Pixel, enemyRect, enemyColor);
                    DrawRect(spriteBatch, enemyRect, Color.Black, 2, textures.Pixel);
                }

                float healthPercent = (float)enemy.Health / enemy.MaxHealth;
                Rectangle healthBar = new Rectangle((int)enemy.Position.X, (int)enemy.Position.Y - 8, (int)(32 * healthPercent), 4);
                spriteBatch.Draw(textures.Pixel, healthBar, Color.Red);
            }
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
                    "Spiritvine Blade" => textures.Blade,
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
            }
            yPos += 35;

            if (font != null)
            {
                spriteBatch.DrawString(font, "Keys:", new Vector2(20, yPos), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            }
            yPos += 25;

            int keyCount = player.GetKeyCount();
            for (int i = 0; i < 3; i++)
            {
                Rectangle keyBox = new Rectangle(25 + i * 40, yPos, 30, 30);
                Color keyColor = i < keyCount ? Color.Gold : Color.Gray * 0.3f;
                spriteBatch.Draw(pixelTexture, keyBox, keyColor);
                DrawRect(spriteBatch, keyBox, Color.White, 2, pixelTexture);
            }
            yPos += 45;

            if (font != null)
            {
                spriteBatch.DrawString(font, $"Weapon: {player.CurrentWeapon.Name}", new Vector2(20, yPos), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                yPos += 25;
                spriteBatch.DrawString(font, $"Damage: {player.CurrentWeapon.DamagePercent}", new Vector2(20, yPos), Color.Yellow, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            }
        }
    }
}