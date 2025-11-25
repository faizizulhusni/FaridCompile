using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace RelicEscape
{
    public class Level3 : BaseLevel
    {
        private Boss boss;
        private List<Enemy> minions;
        private List<ItemDrop> itemDrops;
        private List<Vector2> lavaPositions;
        private List<InteractiveObject> objects;

        private bool bossDefeated = false;
        private bool hasFireResistancePotion = false;
        private float lavaDamageTimer = 0f;
        private float lavaDamageInterval = 1f;
        private float minionSpawnTimer = 0f;
        private float minionSpawnDelay = 15f;

        public Level3(GameLevelManager game, Player player, Random random) : base(game, player, random)
        {
        }

        protected override void InitializeMap()
        {
            map = new TileType[MapWidth, MapHeight];
            lavaPositions = new List<Vector2>();

            // Fill with stone (volcanic rock)
            for (int x = 0; x < MapWidth; x++)
                for (int y = 0; y < MapHeight; y++)
                    map[x, y] = TileType.Stone;

            // Border with statues (pillars)
            for (int x = 0; x < MapWidth; x++)
            {
                map[x, 0] = TileType.Statue;
                map[x, MapHeight - 1] = TileType.Statue;
            }
            for (int y = 0; y < MapHeight; y++)
            {
                map[0, y] = TileType.Statue;
                map[MapWidth - 1, y] = TileType.Statue;
            }

            // Central boss arena platform
            for (int x = 7; x <= 12; x++)
                for (int y = 5; y <= 9; y++)
                    map[x, y] = TileType.Pyramid; // Using pyramid as raised platform

            // Lava rivers around the arena
            PlaceLavaRiver(3, 3, 14, 2); // Top lava river
            PlaceLavaRiver(3, 10, 14, 2); // Bottom lava river
            PlaceLavaRiver(3, 5, 2, 5); // Left lava river
            PlaceLavaRiver(15, 5, 2, 5); // Right lava river

            // Safe paths
            CreateSafePath(5, 3, 5, 10); // Left path
            CreateSafePath(12, 3, 12, 10); // Right path

            // Entrance
            map[10, 2] = TileType.Path;
            map[10, 1] = TileType.Path;

            // Exit (appears after boss defeat)
            map[10, 13] = TileType.Exit;

            // Treasure chests for health potions
            map[2, 7] = TileType.Chest;
            map[17, 7] = TileType.Chest;
        }

        private void PlaceLavaRiver(int startX, int startY, int width, int height)
        {
            for (int x = startX; x < startX + width; x++)
            {
                for (int y = startY; y < startY + height; y++)
                {
                    if (x >= 0 && x < MapWidth && y >= 0 && y < MapHeight)
                    {
                        map[x, y] = TileType.Quicksand; // Using quicksand as lava
                        lavaPositions.Add(new Vector2(x * tileSize, y * tileSize));
                    }
                }
            }
        }

        private void CreateSafePath(int x, int startY, int endX, int endY)
        {
            for (int y = startY; y <= endY; y++)
            {
                if (x >= 0 && x < MapWidth && y >= 0 && y < MapHeight)
                    map[x, y] = TileType.Stone;
                if (endX >= 0 && endX < MapWidth && y >= 0 && y < MapHeight)
                    map[endX, y] = TileType.Stone;
            }
        }

        protected override void InitializeEntities()
        {
            boss = new Boss(new Vector2(tileSize * 10, tileSize * 7), random);
            minions = new List<Enemy>();
            itemDrops = new List<ItemDrop>();
            objects = new List<InteractiveObject>();

            // Health potion chests
            objects.Add(new InteractiveObject(new Vector2(tileSize * 2, tileSize * 7), "Chest", 64, 64));
            objects.Add(new InteractiveObject(new Vector2(tileSize * 17, tileSize * 7), "Chest", 64, 64));

            // Fire resistance potion spawn
            itemDrops.Add(new ItemDrop(new Vector2(tileSize * 10, tileSize * 3), "Fire Resistance Potion"));
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
            CheckLavaCollisions(deltaTime);

            // Update boss
            if (boss != null && boss.State != EntityState.Dead)
            {
                int healthBefore = player.Health;
                boss.Update(deltaTime, player, random);
                if (player.Health < healthBefore) game.TriggerDamageEffect();
            }

            // Check if boss was just defeated THIS frame
            if (boss != null && boss.State == EntityState.Dead && !bossDefeated)
            {
                bossDefeated = true;
                // Drop ultimate relic
                itemDrops.Add(new ItemDrop(boss.Position, "Infernal Core"));
                game.ShowMessage("Boss defeated! The Infernal Core appeared! Press E to claim it!");
            }

            // Update minions
            foreach (var minion in minions)
            {
                if (minion.State != EntityState.Dead)
                {
                    int healthBefore = player.Health;
                    minion.Update(deltaTime, player, random);
                    if (player.Health < healthBefore) game.TriggerDamageEffect();
                }
            }

            // Spawn minions periodically if boss is alive
            if (boss != null && boss.State != EntityState.Dead)
            {
                minionSpawnTimer += deltaTime;
                if (minionSpawnTimer >= minionSpawnDelay)
                {
                    SpawnMinions();
                    minionSpawnTimer = 0f;
                }
            }

            foreach (var item in itemDrops)
                item.Update(deltaTime);

            if (player.IsAttacking) CheckPlayerAttack();
            CheckItemPickups(keyState, prevKeyState);
            CheckObjectInteractions(keyState, prevKeyState);
            CheckExitReached();
        }

        private void CheckLavaCollisions(float deltaTime)
        {
            if (hasFireResistancePotion) return;

            lavaDamageTimer += deltaTime;
            if (lavaDamageTimer >= lavaDamageInterval)
            {
                foreach (var lavaPos in lavaPositions)
                {
                    Rectangle lavaRect = new Rectangle((int)lavaPos.X, (int)lavaPos.Y, tileSize, tileSize);
                    if (player.Bounds.Intersects(lavaRect))
                    {
                        player.TakeDamage(10);
                        game.ShowMessage("BURNING! -10 HP");
                        game.TriggerDamageEffect();
                        lavaDamageTimer = 0f;
                        break;
                    }
                }
            }
        }

        private void SpawnMinions()
        {
            // Spawn 2 fire scorpions
            Vector2 spawnPos1 = new Vector2(tileSize * 5, tileSize * 7);
            Vector2 spawnPos2 = new Vector2(tileSize * 14, tileSize * 7);

            minions.Add(new Enemy(spawnPos1, EnemyType.Scorpion));
            minions.Add(new Enemy(spawnPos2, EnemyType.Scorpion));

            game.ShowMessage("Fire minions summoned!");
        }

        private void CheckPlayerAttack()
        {
            if (player.HasDealtDamageThisAttack) return;

            // Check boss hit
            if (boss != null && boss.State != EntityState.Dead)
            {
                float distance = Vector2.Distance(player.Position, boss.Position);
                if (distance < player.AttackRange && player.IsAttacking)
                {
                    int damage = (int)Math.Ceiling(boss.MaxHealth * (player.CurrentWeapon.DamagePercent / 100f));
                    boss.TakeDamage(damage);
                    player.HasDealtDamageThisAttack = true;
                    game.ShowMessage($"Boss hit! -{damage} damage");
                }
            }

            // Check minion hits
            foreach (var minion in minions)
            {
                if (minion.State == EntityState.Dead) continue;

                float distance = Vector2.Distance(player.Position, minion.Position);
                if (distance < player.AttackRange && player.IsAttacking)
                {
                    int damage = (int)Math.Ceiling(minion.MaxHealth * (player.CurrentWeapon.DamagePercent / 100f));
                    minion.TakeDamage(damage);
                    player.HasDealtDamageThisAttack = true;

                    if (minion.State == EntityState.Dead)
                    {
                        // Chance to drop health potion
                        if (random.Next(0, 100) < 40)
                        {
                            itemDrops.Add(new ItemDrop(minion.Position, "Health Potion"));
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
                    if (item.ItemName == "Fire Resistance Potion")
                    {
                        player.AddToInventory(item.ItemName);
                        hasFireResistancePotion = true;
                        game.ShowMessage("Fire Resistance Potion! Lava won't hurt you!");
                        itemDrops.RemoveAt(i);
                    }
                    else if (item.ItemName == "Health Potion")
                    {
                        player.Health = Math.Min(player.MaxHealth, player.Health + 30);
                        game.ShowMessage("Health Potion! +30 HP");
                        itemDrops.RemoveAt(i);
                    }
                    else if (item.ItemName == "Infernal Core")
                    {
                        RelicItem infernalCore = new RelicItem("Infernal Core", Color.OrangeRed, 100);
                        player.EquipRelic(infernalCore);
                        player.AddToInventory(item.ItemName);
                        game.ShowMessage("INFERNAL CORE ACQUIRED! Ultimate power! Head to the exit!");
                        itemDrops.RemoveAt(i);
                    }
                }
                // Add proximity message for items
                else if (distance < 50f && !ePressed)
                {
                    game.ShowMessage($"Press E to pick up {item.ItemName}");
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
                        // Spawn health potion
                        itemDrops.Add(new ItemDrop(
                            new Vector2(obj.Position.X, obj.Position.Y + 20),
                            "Health Potion"
                        ));
                        game.ShowMessage("Chest opened! Health Potion found!");
                    }
                    else game.ShowMessage("Press E to open chest");
                }
            }
        }

        private void CheckExitReached()
        {
            if (!bossDefeated || !player.HasItem("Infernal Core")) return;

            Rectangle exitRect = new Rectangle(10 * tileSize, 13 * tileSize, tileSize, tileSize);
            if (player.Bounds.Intersects(exitRect))
            {
                game.TransitionToVictory(); // Add this method to Game1
                // OR simply:
                // game.ShowMessage("ULTIMATE VICTORY! Game Complete!");
            }
        }

        public override void Draw(SpriteBatch spriteBatch, LevelTextures textures)
        {
            DrawMap(spriteBatch, textures);
            DrawObjects(spriteBatch, textures);
            DrawMinions(spriteBatch, textures);
            DrawBoss(spriteBatch, textures);
            DrawItemDrops(spriteBatch, textures);
            DrawPlayer(spriteBatch, textures);
        }

        private void DrawObjects(SpriteBatch spriteBatch, LevelTextures textures)
        {
            foreach (var obj in objects)
            {
                if (obj.Type == "Chest")
                {
                    spriteBatch.Draw(textures.Pixel, obj.Bounds, obj.IsActivated ? Color.Gray * 0.5f : Color.DarkGoldenrod);
                }
                DrawRect(spriteBatch, obj.Bounds, Color.Black, 2, textures.Pixel);
            }
        }

        private void DrawBoss(SpriteBatch spriteBatch, LevelTextures textures)
        {
            if (boss == null || boss.State == EntityState.Dead) return;

            Rectangle bossRect = new Rectangle((int)boss.Position.X, (int)boss.Position.Y, 64, 64);

            // Draw boss (large, intimidating)
            spriteBatch.Draw(textures.Pixel, bossRect, Color.DarkRed);
            
            // Draw eyes
            Rectangle eye1 = new Rectangle((int)boss.Position.X + 12, (int)boss.Position.Y + 16, 8, 8);
            Rectangle eye2 = new Rectangle((int)boss.Position.X + 44, (int)boss.Position.Y + 16, 8, 8);
            spriteBatch.Draw(textures.Pixel, eye1, Color.OrangeRed);
            spriteBatch.Draw(textures.Pixel, eye2, Color.OrangeRed);

            // Draw horns
            Rectangle horn1 = new Rectangle((int)boss.Position.X + 8, (int)boss.Position.Y - 8, 12, 12);
            Rectangle horn2 = new Rectangle((int)boss.Position.X + 44, (int)boss.Position.Y - 8, 12, 12);
            spriteBatch.Draw(textures.Pixel, horn1, Color.Black);
            spriteBatch.Draw(textures.Pixel, horn2, Color.Black);

            DrawRect(spriteBatch, bossRect, Color.OrangeRed, 3, textures.Pixel);

            // Boss health bar
            float healthPercent = (float)boss.Health / boss.MaxHealth;
            Rectangle healthBarBg = new Rectangle((int)boss.Position.X, (int)boss.Position.Y - 20, 64, 8);
            Rectangle healthBarFg = new Rectangle((int)boss.Position.X, (int)boss.Position.Y - 20, (int)(64 * healthPercent), 8);
            spriteBatch.Draw(textures.Pixel, healthBarBg, Color.DarkRed);
            spriteBatch.Draw(textures.Pixel, healthBarFg, Color.Red);
            DrawRect(spriteBatch, healthBarBg, Color.OrangeRed, 2, textures.Pixel);
        }

        private void DrawMinions(SpriteBatch spriteBatch, LevelTextures textures)
        {
            foreach (var minion in minions)
            {
                if (minion.State == EntityState.Dead) continue;

                Rectangle minionRect = new Rectangle((int)minion.Position.X, (int)minion.Position.Y, 32, 32);

                if (textures.Scorpion != null)
                {
                    spriteBatch.Draw(textures.Scorpion, minionRect, null, Color.OrangeRed, 0f, Vector2.Zero, minion.FacingDirection, 0f);
                }
                else
                {
                    spriteBatch.Draw(textures.Pixel, minionRect, Color.OrangeRed);
                    DrawRect(spriteBatch, minionRect, Color.Black, 2, textures.Pixel);
                }

                float healthPercent = (float)minion.Health / minion.MaxHealth;
                Rectangle healthBar = new Rectangle((int)minion.Position.X, (int)minion.Position.Y - 8, (int)(32 * healthPercent), 4);
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
                    "Fire Resistance Potion" => null,
                    "Health Potion" => null,
                    "Infernal Core" => null,
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
            DrawRect(spriteBatch, uiPanel, Color.OrangeRed, 2, pixelTexture);

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

                // Boss health
                if (boss != null && boss.State != EntityState.Dead)
                {
                    spriteBatch.DrawString(font, "BOSS HP:", new Vector2(20, yPos), Color.OrangeRed, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                    yPos += 20;

                    Rectangle bossHealthBg = new Rectangle(20, yPos, 250, 15);
                    Rectangle bossHealthFg = new Rectangle(20, yPos, (int)(250 * ((float)boss.Health / boss.MaxHealth)), 15);
                    spriteBatch.Draw(pixelTexture, bossHealthBg, Color.DarkRed);
                    spriteBatch.Draw(pixelTexture, bossHealthFg, Color.Red);
                    DrawRect(spriteBatch, bossHealthBg, Color.OrangeRed, 2, pixelTexture);

                    spriteBatch.DrawString(font, $"{boss.Health}/{boss.MaxHealth}", new Vector2(25, yPos), Color.White, 0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0f);
                    yPos += 25;
                }

                spriteBatch.DrawString(font, $"Weapon: {player.CurrentWeapon.Name}", new Vector2(20, yPos), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                yPos += 25;
                spriteBatch.DrawString(font, $"Damage: {player.CurrentWeapon.DamagePercent}", new Vector2(20, yPos), Color.Yellow, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                yPos += 25;

                if (hasFireResistancePotion)
                {
                    spriteBatch.DrawString(font, "Fire Resistance: ACTIVE", new Vector2(20, yPos), Color.Cyan, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
                }
            }

            DrawBossObjectives(spriteBatch, pixelTexture, font);
        }

        private void DrawBossObjectives(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font)
        {
            Rectangle objectivePanel = new Rectangle(10, 220, 400, 120);
            spriteBatch.Draw(pixelTexture, objectivePanel, Color.Black * 0.7f);
            DrawRect(spriteBatch, objectivePanel, Color.OrangeRed, 2, pixelTexture);

            if (font != null)
            {
                spriteBatch.DrawString(font, "BOSS FIGHT:", new Vector2(20, 230), Color.OrangeRed, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

                int objY = 255;
                DrawObjective(spriteBatch, font, "Defeat the Infernal Boss", bossDefeated, 20, objY);
                objY += 20;
                DrawObjective(spriteBatch, font, "Claim the Infernal Core", player.HasItem("Infernal Core"), 20, objY);
                objY += 20;
                DrawObjective(spriteBatch, font, "Reach the Exit", bossDefeated && player.HasItem("Infernal Core"), 20, objY);
                objY += 20;

                spriteBatch.DrawString(font, "BEWARE THE LAVA!", new Vector2(20, objY), Color.OrangeRed, 0f, Vector2.Zero, 0.6f, SpriteEffects.None, 0f);
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

    // Boss Class
    public class Boss
    {
        public Vector2 Position;
        public int Health, MaxHealth;
        public EntityState State;
        public float Speed;
        public Rectangle Bounds;
        public float DetectionRange, AttackRange, AttackCooldown;
        public SpriteEffects FacingDirection;
        public float SpecialAttackCooldown;
        public float SpecialAttackDelay = 8f;

        public Boss(Vector2 startPos, Random random)
        {
            Position = startPos;
            MaxHealth = 500; // Boss has massive health
            Health = MaxHealth;
            State = EntityState.Idle;
            Speed = 40f; // Slower but powerful
            Bounds = new Rectangle((int)Position.X, (int)Position.Y, 64, 64);
            DetectionRange = 300f;
            AttackRange = 80f;
            AttackCooldown = 0f;
            SpecialAttackCooldown = 0f;
            FacingDirection = SpriteEffects.None;
        }

        public void Update(float deltaTime, Player player, Random random)
        {
            if (State == EntityState.Dead) return;

            float distanceToPlayer = Vector2.Distance(Position, player.Position);
            if (AttackCooldown > 0) AttackCooldown -= deltaTime;
            if (SpecialAttackCooldown > 0) SpecialAttackCooldown -= deltaTime;

            if (distanceToPlayer < DetectionRange)
            {
                Vector2 direction = player.Position - Position;
                if (direction != Vector2.Zero)
                {
                    FacingDirection = direction.X < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                    direction.Normalize();
                    Position += direction * Speed * deltaTime;
                    State = EntityState.Walking;
                }

                // Normal attack
                if (distanceToPlayer < AttackRange && AttackCooldown <= 0 && player.State != EntityState.Dead)
                {
                    int damage = 15; // Boss deals heavy damage
                    player.TakeDamage(damage);
                    AttackCooldown = 2.0f;
                }

                // Special attack (area damage)
                if (SpecialAttackCooldown <= 0 && distanceToPlayer < 150f && player.State != EntityState.Dead)
                {
                    player.TakeDamage(20);
                    SpecialAttackCooldown = SpecialAttackDelay;
                }
            }
            else
            {
                State = EntityState.Idle;
            }

            Bounds = new Rectangle((int)Position.X, (int)Position.Y, 64, 64);
        }

        public void TakeDamage(int damage)
        {
            Health -= damage;
            if (Health <= 0)
            {
                Health = 0;
                State = EntityState.Dead;
            }
        }
    }
}