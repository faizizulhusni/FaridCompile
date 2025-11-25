using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RelicEscape
{
    // Enums
    public enum TileType { Grass, Flower, Fence, Path, Sign, Exit, Tree, Water, Stone, Chest, Sand, Cactus, Pyramid, Quicksand, Statue, Shop }
    public enum EntityState { Idle, Walking, Attacking, Dead, Hurt }
    public enum DummyType { Scarecrow, Pumpkin }
    public enum EnemyType { Snake, Spider, Scorpion }

    // Shop Class - moved from inside MathPuzzle
    public class Shop
    {
        public Vector2 Position;
        public Rectangle Bounds;
        public bool IsOpen;
        public List<string> Items;
        public List<int> Prices;

        public Shop(Vector2 pos)
        {
            Position = pos;
            Bounds = new Rectangle((int)pos.X, (int)pos.Y, 64, 64);
            IsOpen = false;
            Items = new List<string> { "Health Potion", "Speed Boost", "Damage Boost" };
            Prices = new List<int> { 3, 5, 7 };
        }
    }

    // Math Puzzle Class
    public class MathPuzzle
    {
        public Vector2 Position;
        public Rectangle Bounds;
        public bool IsSolved;
        public string Question;
        public int Answer;
        public int PlayerAnswer;
        public bool IsActive;
        public bool IsNegative;

        public MathPuzzle(Vector2 pos)
        {
            Position = pos;
            Bounds = new Rectangle((int)pos.X, (int)pos.Y, 64, 64);
            IsSolved = false;
            IsActive = false;
            IsNegative = false;
            GenerateQuestion();
        }

        private void GenerateQuestion()
        {
            Random random = new Random();
            int a = random.Next(1, 10);
            int b = random.Next(1, 10);
            int operation = random.Next(0, 2); // Changed from 3 to 2, only generates 0 or 1

            switch (operation)
            {
                case 0: // Addition
                    Question = $"{a} + {b} = ?";
                    Answer = a + b;
                    break;
                case 1: // Subtraction
                    Question = $"{a} - {b} = ?";
                    Answer = a - b;
                    break;
            }
        }
    }

    // Item Drop Class
    public class ItemDrop
    {
        public Vector2 Position;
        public string ItemName;
        public Rectangle Bounds;
        public Color DisplayColor;
        public float BobTimer;

        public ItemDrop(Vector2 pos, string itemName)
        {
            Position = pos;
            ItemName = itemName;
            Bounds = new Rectangle((int)pos.X, (int)pos.Y, 24, 24);
            DisplayColor = itemName == "Watering Can" ? Color.LightBlue :
                          itemName == "Garden Spade" ? Color.SandyBrown :
                          itemName == "Key" ? Color.Gold :
                          itemName == "Spiritvine Blade" ? Color.Cyan :
                          itemName == "Desert Coin" ? Color.Orange :
                          itemName == "Scroll of Antimatter" ? Color.Purple :
                          itemName == "Fire Resistance Potion" ? Color.Cyan :
                          itemName == "Health Potion" ? Color.LimeGreen :
                          itemName == "Infernal Core" ? Color.OrangeRed : Color.White;
            BobTimer = 0f;
        }

        public void Update(float deltaTime)
        {
            BobTimer += deltaTime * 3f;
            float bobOffset = (float)Math.Sin(BobTimer) * 5f;
            Bounds = new Rectangle((int)Position.X, (int)(Position.Y + bobOffset), 24, 24);
        }
    }

    // Relic Item Class
    public class RelicItem
    {
        public string Name;
        public Color DisplayColor;
        public int DamagePercent;

        public RelicItem(string name, Color color, int damagePercent)
        {
            Name = name;
            DisplayColor = color;
            DamagePercent = damagePercent;
        }
    }

    // Training Dummy Class
    public class TrainingDummy
    {
        public Vector2 Position;
        public DummyType Type;
        public int Health, MaxHealth;
        public EntityState State;
        public Rectangle Bounds;
        public bool IsHit;
        public float HitFlashTimer;

        public TrainingDummy(Vector2 startPos, DummyType type)
        {
            Position = startPos;
            Type = type;
            MaxHealth = type == DummyType.Scarecrow ? 50 : 30;
            Health = MaxHealth;
            State = EntityState.Idle;
            Bounds = new Rectangle((int)Position.X, (int)Position.Y, 32, 32);
            IsHit = false;
            HitFlashTimer = 0f;
        }

        public void Update(float deltaTime)
        {
            if (State == EntityState.Dead) return;

            if (HitFlashTimer > 0)
            {
                HitFlashTimer -= deltaTime;
                if (HitFlashTimer <= 0)
                    IsHit = false;
            }

            Bounds = new Rectangle((int)Position.X, (int)Position.Y, 32, 32);
        }

        public void TakeDamage(int damage)
        {
            Health -= damage;
            IsHit = true;
            HitFlashTimer = 0.2f;
            if (Health <= 0)
            {
                Health = 0;
                State = EntityState.Dead;
            }
        }
    }

    // Enemy Class
    public class Enemy
    {
        public Vector2 Position;
        public EnemyType Type;
        public int Health, MaxHealth;
        public EntityState State;
        public float Speed;
        public Rectangle Bounds;
        public float DetectionRange, AttackRange, AttackCooldown;
        public bool DropsKey;
        public Vector2 WanderTarget;
        public float WanderTimer;
        public SpriteEffects FacingDirection;

        public Enemy(Vector2 startPos, EnemyType type, bool dropsKey = false)
        {
            Position = startPos;
            Type = type;
            MaxHealth = type == EnemyType.Snake ? 30 :
                       type == EnemyType.Spider ? 25 :
                       type == EnemyType.Scorpion ? 20 : 25;
            Health = MaxHealth;
            State = EntityState.Idle;
            Speed = type == EnemyType.Snake ? 60f :
                   type == EnemyType.Spider ? 50f :
                   type == EnemyType.Scorpion ? 70f : 50f;
            Bounds = new Rectangle((int)Position.X, (int)Position.Y, 32, 32);
            DetectionRange = type == EnemyType.Scorpion ? 150f : 200f;
            AttackRange = 35f;
            AttackCooldown = 0f;
            DropsKey = dropsKey;
            WanderTarget = startPos;
            WanderTimer = 0f;
            FacingDirection = SpriteEffects.None;
        }

        public void Update(float deltaTime, Player player, Random random)
        {
            if (State == EntityState.Dead) return;

            float distanceToPlayer = Vector2.Distance(Position, player.Position);
            if (AttackCooldown > 0) AttackCooldown -= deltaTime;

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

                if (distanceToPlayer < AttackRange && AttackCooldown <= 0 && player.State != EntityState.Dead)
                {
                    int damage = Type == EnemyType.Scorpion ? 8 : 5;
                    player.TakeDamage(damage);
                    AttackCooldown = Type == EnemyType.Scorpion ? 1.0f : 1.5f;
                }
            }
            else
            {
                WanderTimer -= deltaTime;
                if (WanderTimer <= 0)
                {
                    WanderTarget = Position + new Vector2(random.Next(-100, 100), random.Next(-100, 100));
                    WanderTimer = random.Next(2, 5);
                }

                Vector2 direction = WanderTarget - Position;
                if (direction.Length() > 5f)
                {
                    FacingDirection = direction.X < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                    direction.Normalize();
                    Position += direction * Speed * 0.5f * deltaTime;
                    State = EntityState.Walking;
                }
                else State = EntityState.Idle;
            }

            Bounds = new Rectangle((int)Position.X, (int)Position.Y, 32, 32);
        }

        public void TakeDamage(int damage)
        {
            Health -= damage;
            if (Health <= 0) { Health = 0; State = EntityState.Dead; }
        }
    }

    // NPC Class
    public class NPC
    {
        public Vector2 Position;
        public Rectangle Bounds;
        public string Name;
        public string DialogueText;
        public bool IsInteracted;

        public NPC(Vector2 pos, string name, string dialogue)
        {
            Position = pos;
            Name = name;
            DialogueText = dialogue;
            Bounds = new Rectangle((int)pos.X, (int)pos.Y, 32, 32);
            IsInteracted = false;
        }
    }

    // Interactive Object Class
    public class InteractiveObject
    {
        public Vector2 Position;
        public Rectangle Bounds;
        public string Type;
        public bool IsActivated;

        public InteractiveObject(Vector2 pos, string type, int width = 48, int height = 48)
        {
            Position = pos;
            Type = type;
            Bounds = new Rectangle((int)pos.X, (int)pos.Y, width, height);
            IsActivated = false;
        }
    }

    // Player Class
    public class Player
    {
        public Vector2 Position;
        public int Health, MaxHealth;
        public List<string> Inventory;
        public List<RelicItem> EquippedRelics;
        public EntityState State;
        public float Speed;
        public Rectangle Bounds;
        public bool IsAttacking;
        public float AttackCooldown;
        public int AttackRange;
        public RelicItem CurrentWeapon;
        public bool HasDealtDamageThisAttack;
        public SpriteEffects FacingDirection;
        public float HurtTimer = 0f;
        public float SpeedModifier = 1.0f;
        public float QuicksandSlowTimer = 0f;

        public Player(Vector2 startPos)
        {
            Position = startPos;
            Health = MaxHealth = 100;
            Inventory = new List<string>();
            EquippedRelics = new List<RelicItem>();
            State = EntityState.Idle;
            Speed = 150f;
            Bounds = new Rectangle((int)Position.X, (int)Position.Y, 32, 32);
            IsAttacking = false;
            AttackCooldown = 0f;
            AttackRange = 50;
            HasDealtDamageThisAttack = false;
            FacingDirection = SpriteEffects.None;
            CurrentWeapon = new RelicItem("Wooden Stick", Color.SandyBrown, 10);
        }

        public void Update(float deltaTime, Microsoft.Xna.Framework.Input.KeyboardState keyState, Microsoft.Xna.Framework.Input.KeyboardState prevKeyState)
        {
            if (State == EntityState.Dead) return;

            if (AttackCooldown > 0) AttackCooldown -= deltaTime;

            if (HurtTimer > 0)
            {
                HurtTimer -= deltaTime;
                if (HurtTimer <= 0)
                {
                    State = EntityState.Idle;
                }
            }

            if (QuicksandSlowTimer > 0)
            {
                QuicksandSlowTimer -= deltaTime;
                SpeedModifier = 0.4f;
            }
            else
            {
                SpeedModifier = 1.0f;
            }

            if (State == EntityState.Hurt) return;

            Vector2 movement = Vector2.Zero;
            if (keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.W) || keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Up)) movement.Y -= 1;
            if (keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.S) || keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Down)) movement.Y += 1;
            if (keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.A) || keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Left))
            {
                movement.X -= 1;
                FacingDirection = SpriteEffects.FlipHorizontally;
            }
            if (keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D) || keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Right))
            {
                movement.X += 1;
                FacingDirection = SpriteEffects.None;
            }

            if (movement != Vector2.Zero)
            {
                movement.Normalize();
                Position += movement * Speed * SpeedModifier * deltaTime;
                State = EntityState.Walking;
            }
            else State = EntityState.Idle;

            if (keyState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Space) && prevKeyState.IsKeyUp(Microsoft.Xna.Framework.Input.Keys.Space) && AttackCooldown <= 0)
            {
                IsAttacking = true;
                HasDealtDamageThisAttack = false;
                AttackCooldown = 0.5f;
            }
            else if (AttackCooldown <= 0)
            {
                IsAttacking = false;
                HasDealtDamageThisAttack = false;
            }

            Bounds = new Rectangle((int)Position.X, (int)Position.Y, 32, 32);
        }

        public void TakeDamage(int damage)
        {
            Health -= damage;
            if (Health <= 0)
            {
                Health = 0;
                State = EntityState.Dead;
            }
            else
            {
                State = EntityState.Hurt;
                HurtTimer = 0.3f;
            }
        }

        public void AddToInventory(string item) => Inventory.Add(item);
        public int GetItemCount(string itemName) => Inventory.Count(item => item == itemName);
        public bool HasItem(string itemName) => Inventory.Contains(itemName);

        public void EquipWeapon(string weaponName, int damage)
        {
            CurrentWeapon = new RelicItem(weaponName, Color.SandyBrown, damage);
        }

        public void EquipRelic(RelicItem relic)
        {
            if (relic.Name.Contains("Blade") || relic.Name.Contains("Knife"))
                CurrentWeapon = relic;
            else
                EquippedRelics.Add(relic);
        }

        public int GetKeyCount() => Inventory.Count(item => item == "Key");
        public int GetDesertCoinCount() => Inventory.Count(item => item == "Desert Coin");
        public int CalculateDamage(int enemyMaxHP) => (int)(enemyMaxHP * (CurrentWeapon.DamagePercent / 100f));
    }
}