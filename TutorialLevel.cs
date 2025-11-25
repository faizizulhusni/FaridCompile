using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace RelicEscape
{
    public class TutorialLevel : BaseLevel
    {
        private List<TrainingDummy> dummies;
        private List<NPC> npcs;
        private List<ItemDrop> itemDrops;

        private bool hasMovedTutorial = false;
        private bool hasAttackedTutorial = false;
        private bool hasPickedUpItem = false;
        private bool hasDefeatedDummy = false;
        private bool hasSpokenToNPC = false;
        private bool canExitTutorial = false;

        public TutorialLevel(GameLevelManager game, Player player, Random random) : base(game, player, random)
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
                map[x, 0] = TileType.Fence;
                map[x, MapHeight - 1] = TileType.Fence;
            }
            for (int y = 0; y < MapHeight; y++)
            {
                map[0, y] = TileType.Fence;
                map[MapWidth - 1, y] = TileType.Fence;
            }

            for (int x = 1; x < 10; x++)
                map[x, 7] = TileType.Path;

            PlaceFlowerPatch(5, 3, 3, 2);
            PlaceFlowerPatch(14, 3, 3, 2);
            PlaceFlowerPatch(5, 10, 3, 2);
            PlaceFlowerPatch(14, 10, 3, 2);

            map[2, 5] = TileType.Sign;
            map[10, 5] = TileType.Sign;
            map[18, 7] = TileType.Exit;
        }

        private void PlaceFlowerPatch(int startX, int startY, int width, int height)
        {
            for (int x = startX; x < startX + width; x++)
                for (int y = startY; y < startY + height; y++)
                    if (x < MapWidth && y < MapHeight)
                        map[x, y] = TileType.Flower;
        }

        protected override void InitializeEntities()
        {
            dummies = new List<TrainingDummy>();
            npcs = new List<NPC>();
            itemDrops = new List<ItemDrop>();

            dummies.Add(new TrainingDummy(new Vector2(tileSize * 12, tileSize * 7), DummyType.Scarecrow));
            dummies.Add(new TrainingDummy(new Vector2(tileSize * 10, tileSize * 4), DummyType.Pumpkin));
            dummies.Add(new TrainingDummy(new Vector2(tileSize * 10, tileSize * 10), DummyType.Pumpkin));

            npcs.Add(new NPC(
                new Vector2(tileSize * 15, tileSize * 7),
                "Old Gardener",
                "Welcome to the Sunflower Garden!\nCollect 3 Watering Cans and\ndefeat training dummies to complete tutorial."
            ));

            itemDrops.Add(new ItemDrop(new Vector2(tileSize * 6, tileSize * 4), "Watering Can"));
            itemDrops.Add(new ItemDrop(new Vector2(tileSize * 15, tileSize * 4), "Watering Can"));
            itemDrops.Add(new ItemDrop(new Vector2(tileSize * 11, tileSize * 11), "Watering Can"));
        }

        public override void Update(float deltaTime, KeyboardState keyState, KeyboardState prevKeyState)
        {
            player.Update(deltaTime, keyState, prevKeyState);

            if (player.State == EntityState.Walking && !hasMovedTutorial)
            {
                hasMovedTutorial = true;
                game.ShowMessage("Good! Now try attacking with SPACE");
            }

            if (player.IsAttacking && !hasAttackedTutorial)
            {
                hasAttackedTutorial = true;
                game.ShowMessage("Perfect! Find items and press E to pick up");
            }

            player.Position.X = MathHelper.Clamp(player.Position.X, tileSize, (MapWidth - 2) * tileSize);
            player.Position.Y = MathHelper.Clamp(player.Position.Y, tileSize, (MapHeight - 2) * tileSize);

            CheckTileCollisions();

            foreach (var item in itemDrops)
                item.Update(deltaTime);

            foreach (var dummy in dummies)
                dummy.Update(deltaTime);

            if (player.IsAttacking)
                CheckPlayerAttack();

            CheckItemPickups(keyState, prevKeyState);
            CheckNPCInteraction(keyState, prevKeyState);
            CheckTutorialCompletion();
            CheckExitReached();
        }

        private void CheckPlayerAttack()
        {
            if (player.HasDealtDamageThisAttack) return;

            foreach (var dummy in dummies)
            {
                if (dummy.State == EntityState.Dead) continue;

                float distance = Vector2.Distance(player.Position, dummy.Position);
                if (distance < player.AttackRange)
                {
                    dummy.TakeDamage(player.CurrentWeapon.DamagePercent);
                    player.HasDealtDamageThisAttack = true;
                    game.ShowMessage($"Hit! -{player.CurrentWeapon.DamagePercent} damage");

                    if (dummy.State == EntityState.Dead && !hasDefeatedDummy)
                    {
                        hasDefeatedDummy = true;
                        game.ShowMessage("Dummy defeated! Great job!");
                    }
                    break;
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

                if (distance < 50f)
                {
                    if (!hasPickedUpItem)
                        game.ShowMessage("Press E to pick up!");

                    if (ePressed)
                    {
                        if (item.ItemName == "Watering Can")
                        {
                            player.AddToInventory(item.ItemName);
                            game.ShowMessage($"Picked up {item.ItemName}! ({player.GetItemCount(item.ItemName)}/3)");
                            itemDrops.RemoveAt(i);

                            if (!hasPickedUpItem)
                            {
                                hasPickedUpItem = true;
                                game.ShowMessage("Great! Collect all 3 Watering Cans");
                            }
                        }
                        else if (item.ItemName == "Garden Spade")
                        {
                            player.EquipWeapon("Garden Spade", 25);
                            player.AddToInventory(item.ItemName);
                            itemDrops.RemoveAt(i);
                            game.ShowMessage("Garden Spade equipped! Damage increased!");
                        }
                    }
                }
            }
        }

        private void CheckNPCInteraction(KeyboardState keyState, KeyboardState prevKeyState)
        {
            bool ePressed = keyState.IsKeyDown(Keys.E) && prevKeyState.IsKeyUp(Keys.E);

            foreach (var npc in npcs)
            {
                float distance = Vector2.Distance(player.Position, npc.Position);

                if (distance < 60f)
                {
                    if (ePressed)
                    {
                        game.ShowDialogue(npc.Name, npc.DialogueText);
                        if (!hasSpokenToNPC)
                        {
                            hasSpokenToNPC = true;
                        }
                    }
                }
            }
        }

        private void CheckTutorialCompletion()
        {
            if (!canExitTutorial &&
                player.GetItemCount("Watering Can") >= 3 &&
                dummies.All(d => d.State == EntityState.Dead))
            {
                canExitTutorial = true;

                itemDrops.Add(new ItemDrop(
                    new Vector2(tileSize * 16, tileSize * 8),
                    "Garden Spade"
                ));

                game.ShowMessage("Tutorial complete! Garden Spade appeared! Head to the exit!");
            }
        }

        private void CheckExitReached()
        {
            if (!canExitTutorial) return;

            Rectangle exitRect = new Rectangle(18 * tileSize, 7 * tileSize, tileSize, tileSize);
            if (player.Bounds.Intersects(exitRect))
            {
                game.TransitionToLevel1();
            }
        }

        public override void Draw(SpriteBatch spriteBatch, LevelTextures textures)
        {
            DrawMap(spriteBatch, textures);
            DrawNPCs(spriteBatch, textures);
            DrawDummies(spriteBatch, textures);
            DrawItemDrops(spriteBatch, textures);
            DrawPlayer(spriteBatch, textures);
        }

        private void DrawNPCs(SpriteBatch spriteBatch, LevelTextures textures)
        {
            foreach (var npc in npcs)
            {
                Rectangle npcRect = new Rectangle((int)npc.Position.X, (int)npc.Position.Y, 32, 32);

                if (textures.NPC != null)
                {
                    spriteBatch.Draw(textures.NPC, npcRect, Color.White);
                }
                else
                {
                    spriteBatch.Draw(textures.Pixel, npcRect, new Color(245, 222, 179));
                    Rectangle hatRect = new Rectangle((int)npc.Position.X + 4, (int)npc.Position.Y - 8, 24, 8);
                    spriteBatch.Draw(textures.Pixel, hatRect, new Color(160, 82, 45));
                }

                DrawRect(spriteBatch, npcRect, Color.Black, 2, textures.Pixel);
            }
        }

        private void DrawDummies(SpriteBatch spriteBatch, LevelTextures textures)
        {
            foreach (var dummy in dummies)
            {
                if (dummy.State == EntityState.Dead)
                {
                    Rectangle deadRect = new Rectangle(
                        (int)dummy.Position.X, (int)dummy.Position.Y + 16, 32, 16);
                    spriteBatch.Draw(textures.Pixel, deadRect, Color.Gray * 0.5f);
                    continue;
                }

                Rectangle dummyRect = new Rectangle((int)dummy.Position.X, (int)dummy.Position.Y, 32, 32);

                Texture2D dummySprite = dummy.Type == DummyType.Scarecrow ? textures.Scarecrow : textures.Pumpkin;

                if (dummySprite != null)
                {
                    Color dummyColor = dummy.IsHit ? Color.Red : Color.White;
                    spriteBatch.Draw(dummySprite, dummyRect, dummyColor);
                }
                else
                {
                    Color dummyColor = dummy.Type == DummyType.Scarecrow
                        ? new Color(244, 164, 96)
                        : Color.Orange;

                    if (dummy.IsHit)
                        dummyColor = Color.Red;

                    spriteBatch.Draw(textures.Pixel, dummyRect, dummyColor);

                    Rectangle eye1 = new Rectangle((int)dummy.Position.X + 8, (int)dummy.Position.Y + 10, 4, 4);
                    Rectangle eye2 = new Rectangle((int)dummy.Position.X + 20, (int)dummy.Position.Y + 10, 4, 4);
                    spriteBatch.Draw(textures.Pixel, eye1, Color.Black);
                    spriteBatch.Draw(textures.Pixel, eye2, Color.Black);
                }

                DrawRect(spriteBatch, dummyRect, Color.Black, 2, textures.Pixel);

                float healthPercent = (float)dummy.Health / dummy.MaxHealth;
                Rectangle healthBar = new Rectangle(
                    (int)dummy.Position.X, (int)dummy.Position.Y - 8,
                    (int)(32 * healthPercent), 4);
                spriteBatch.Draw(textures.Pixel, healthBar, Color.LimeGreen);
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
                    "Watering Can" => textures.WateringCan,
                    "Garden Spade" => textures.GardenSpade,
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
                spriteBatch.DrawString(font, "Watering Cans:", new Vector2(20, yPos), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            }
            yPos += 25;

            int canCount = player.GetItemCount("Watering Can");
            for (int i = 0; i < 3; i++)
            {
                Rectangle canBox = new Rectangle(25 + i * 40, yPos, 30, 30);
                Color canColor = i < canCount ? Color.LightBlue : Color.Gray * 0.3f;
                spriteBatch.Draw(pixelTexture, canBox, canColor);
                DrawRect(spriteBatch, canBox, Color.White, 2, pixelTexture);
            }
            yPos += 45;

            if (font != null)
            {
                spriteBatch.DrawString(font, $"Weapon: {player.CurrentWeapon.Name}", new Vector2(20, yPos), Color.White, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
                yPos += 25;
                spriteBatch.DrawString(font, $"Damage: {player.CurrentWeapon.DamagePercent}", new Vector2(20, yPos), Color.Yellow, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);
            }

            DrawTutorialObjectives(spriteBatch, pixelTexture, font);
        }

        private void DrawTutorialObjectives(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font)
        {
            Rectangle objectivePanel = new Rectangle(10, 220, 400, 150);
            spriteBatch.Draw(pixelTexture, objectivePanel, Color.Black * 0.7f);
            DrawRect(spriteBatch, objectivePanel, Color.Gold, 2, pixelTexture);

            if (font != null)
            {
                spriteBatch.DrawString(font, "Tutorial Objectives:", new Vector2(20, 230), Color.Gold, 0f, Vector2.Zero, 0.7f, SpriteEffects.None, 0f);

                int objY = 255;
                DrawObjective(spriteBatch, font, "Move with WASD", hasMovedTutorial, 20, objY);
                objY += 20;
                DrawObjective(spriteBatch, font, "Attack with SPACE", hasAttackedTutorial, 20, objY);
                objY += 20;
                DrawObjective(spriteBatch, font, "Pick up items (E)", hasPickedUpItem, 20, objY);
                objY += 20;
                DrawObjective(spriteBatch, font, "Collect 3 Watering Cans", player.GetItemCount("Watering Can") >= 3, 20, objY);
                objY += 20;
                DrawObjective(spriteBatch, font, "Defeat all training dummies", dummies.All(d => d.State == EntityState.Dead), 20, objY);
                objY += 20;
                DrawObjective(spriteBatch, font, "Get Garden Spade & exit", canExitTutorial && player.HasItem("Garden Spade"), 20, objY);
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