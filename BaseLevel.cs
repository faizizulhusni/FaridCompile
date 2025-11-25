using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace RelicEscape
{
    public abstract class BaseLevel
    {
        protected GameLevelManager game;
        protected Player player;
        protected Random random;
        protected TileType[,] map;
        protected int tileSize = 64;

        public int MapWidth { get; protected set; } = 20;
        public int MapHeight { get; protected set; } = 15;
        public TileType[,] Map => map;

        public BaseLevel(GameLevelManager game, Player player, Random random)
        {
            this.game = game;
            this.player = player;
            this.random = random;
            InitializeMap();
            InitializeEntities();
        }

        protected abstract void InitializeMap();
        protected abstract void InitializeEntities();
        public abstract void Update(float deltaTime, KeyboardState keyState, KeyboardState prevKeyState);
        public abstract void Draw(SpriteBatch spriteBatch, LevelTextures textures);
        public abstract void DrawUI(SpriteBatch spriteBatch, Texture2D pixelTexture, SpriteFont font, int screenWidth, int screenHeight);

        protected void DrawMap(SpriteBatch spriteBatch, LevelTextures textures)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                for (int y = 0; y < MapHeight; y++)
                {
                    Rectangle tileRect = new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize);
                    TileType tile = map[x, y];

                    Texture2D tileTexture = GetTileTexture(tile, textures);
                    Color tileColor = GetTileColor(tile);

                    if (tileTexture != null)
                    {
                        spriteBatch.Draw(tileTexture, tileRect, Color.White);
                    }
                    else
                    {
                        spriteBatch.Draw(textures.Pixel, tileRect, tileColor);
                    }

                    if (tile == TileType.Flower && textures.Flower == null)
                    {
                        Rectangle flowerCenter = new Rectangle(
                            x * tileSize + tileSize / 2 - 8,
                            y * tileSize + tileSize / 2 - 8,
                            16, 16);
                        spriteBatch.Draw(textures.Pixel, flowerCenter, Color.Orange);
                    }

                    DrawRect(spriteBatch, tileRect, Color.Black * 0.1f, 1, textures.Pixel);
                }
            }
        }

        protected Texture2D GetTileTexture(TileType tile, LevelTextures textures)
        {
            return tile switch
            {
                TileType.Grass => textures.Grass,
                TileType.Flower => textures.Flower,
                TileType.Fence => textures.Fence,
                TileType.Path => textures.Path,
                TileType.Sign => textures.Sign,
                TileType.Exit => textures.Exit,
                TileType.Tree => textures.Tree,
                TileType.Water => textures.Water,
                TileType.Stone => textures.Stone,
                TileType.Chest => textures.Chest,
                TileType.Sand => textures.Sand,
                TileType.Cactus => textures.Cactus,
                TileType.Pyramid => textures.Pyramid,
                TileType.Quicksand => textures.Quicksand,
                TileType.Statue => textures.Statue,
                TileType.Shop => textures.Shop,
                _ => null
            };
        }

        protected Color GetTileColor(TileType tile)
        {
            return tile switch
            {
                TileType.Grass => new Color(124, 252, 0),
                TileType.Flower => Color.Yellow,
                TileType.Fence => new Color(139, 69, 19),
                TileType.Path => new Color(210, 180, 140),
                TileType.Sign => new Color(160, 82, 45),
                TileType.Exit => Color.Gold * ((float)Math.Sin(DateTime.Now.Millisecond / 100f) * 0.3f + 0.7f),
                TileType.Tree => Color.SaddleBrown,
                TileType.Water => Color.DodgerBlue,
                TileType.Stone => Color.Gray,
                TileType.Chest => Color.DarkGoldenrod,
                TileType.Sand => new Color(237, 201, 175),
                TileType.Cactus => Color.ForestGreen,
                TileType.Pyramid => new Color(205, 133, 63),
                TileType.Quicksand => new Color(181, 101, 29),
                TileType.Statue => new Color(192, 192, 192),
                TileType.Shop => new Color(160, 120, 80),
                _ => Color.ForestGreen
            };
        }

        protected void CheckTileCollisions()
        {
            int playerTileX = (int)(player.Position.X / tileSize);
            int playerTileY = (int)(player.Position.Y / tileSize);

            for (int x = playerTileX - 1; x <= playerTileX + 1; x++)
            {
                for (int y = playerTileY - 1; y <= playerTileY + 1; y++)
                {
                    if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight) continue;

                    TileType tile = map[x, y];
                    Rectangle tileRect = new Rectangle(x * tileSize, y * tileSize, tileSize, tileSize);

                    bool isBlocking = IsBlockingTile(tile, x, y);

                    if (isBlocking && player.Bounds.Intersects(tileRect))
                    {
                        Vector2 tileCenterVec = new Vector2(x * tileSize + tileSize / 2, y * tileSize + tileSize / 2);
                        Vector2 direction = player.Position - tileCenterVec;
                        if (direction != Vector2.Zero)
                        {
                            direction.Normalize();
                            player.Position += direction * 2f;
                        }
                    }
                }
            }
        }

        protected virtual bool IsBlockingTile(TileType tile, int x, int y)
        {
            return tile == TileType.Fence || tile == TileType.Sign || tile == TileType.Tree ||
                   tile == TileType.Water || tile == TileType.Cactus || tile == TileType.Statue;
        }

        protected void DrawRect(SpriteBatch spriteBatch, Rectangle rect, Color color, int width, Texture2D pixel)
        {
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, width), color);
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - width, rect.Width, width), color);
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, width, rect.Height), color);
            spriteBatch.Draw(pixel, new Rectangle(rect.Right - width, rect.Y, width, rect.Height), color);
        }

        protected void DrawPlayer(SpriteBatch spriteBatch, LevelTextures textures)
        {
            if (player.State == EntityState.Dead)
            {
                spriteBatch.Draw(textures.Pixel, player.Bounds, Color.DarkRed);
                return;
            }

            if (textures.Player != null)
            {
                Color playerColor = player.State == EntityState.Hurt ? Color.Red : Color.White;
                spriteBatch.Draw(textures.Player, player.Bounds, null, playerColor, 0f, Vector2.Zero, player.FacingDirection, 0f);
            }
            else
            {
                Color playerColor = player.State == EntityState.Hurt ? Color.Red : Color.Blue;
                spriteBatch.Draw(textures.Pixel, player.Bounds, playerColor);
            }

            DrawRect(spriteBatch, player.Bounds, Color.White, 2, textures.Pixel);

            if (player.IsAttacking)
            {
                Rectangle attackRect = new Rectangle(
                    (int)player.Position.X - 10, (int)player.Position.Y - 10, 52, 52);
                DrawRect(spriteBatch, attackRect, Color.Red, 2, textures.Pixel);
            }
        }
    }

    // Helper class to hold all textures
    public class LevelTextures
    {
        public Texture2D Grass, Flower, Fence, Path, Sign, Exit, Tree, Water, Stone, Chest;
        public Texture2D Sand, Cactus, Pyramid, Quicksand, Statue, Shop;
        public Texture2D Player, NPC, Scarecrow, Pumpkin, Snake, Spider, Scorpion;
        public Texture2D WateringCan, GardenSpade, Key, Blade, Coin, Scroll, Pixel;
    }
}