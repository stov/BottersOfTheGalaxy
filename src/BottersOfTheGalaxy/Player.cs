using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

/**
 * Made with love by AntiSquid, Illedan and Wildum.
 * You can help children learn to code while you participate by donating to CoderDojo.
 **/
class Player
{
    // TODO:
    // Improve positioning around enemy tower
    // Add logic for bushes, spawns, groot
    // Code for multiple heroes
    // Fix positioning of 2nd hero/both heroes

    static void Main(string[] args)
    {
        GameState.MyItems = new List<Item>();

        /*
        GameState.Heroes = new List<Hero>();
        GameState.Heroes.Add(new Hero("Deadpool", 1380, 100, 80, 200, 1, 110));
        GameState.Heroes.Add(new Hero("Doctor Strange", 950, 300, 50, 200, 2, 245));
        GameState.Heroes.Add(new Hero("Hulk", 1450, 90, 80, 200, 1, 95));
        GameState.Heroes.Add(new Hero("Ironman", 820, 200, 60, 200, 2, 270));
        GameState.Heroes.Add(new Hero("Valkyrie", 1400, 155, 65, 200, 2, 130));
        */

        string[] inputs;
        GameState.MyTeam = int.Parse(Console.ReadLine());
        int bushAndSpawnPointCount = int.Parse(Console.ReadLine()); // useful from wood1, represents the number of bushes and the number of places where neutral units can spawn
        for (int i = 0; i < bushAndSpawnPointCount; i++)
        {
            inputs = Console.ReadLine().Split(' ');
            
            string entityType = inputs[0]; // BUSH, from wood1 it can also be SPAWN
            int x = int.Parse(inputs[1]);
            int y = int.Parse(inputs[2]);
            int radius = int.Parse(inputs[3]);
        }

        GameState.ItemCount = int.Parse(Console.ReadLine()); // useful from wood2
        GameState.Items = new List<Item>();
        for (int i = 0; i < GameState.ItemCount; i++)
        {
            GameState.Items.Add(new Item(Console.ReadLine().Split(' ')));
        }

        // game loop
        while (true)
        {
            GameState.Gold = int.Parse(Console.ReadLine());
            GameState.EnemyGold = int.Parse(Console.ReadLine());
            GameState.RoundType = int.Parse(Console.ReadLine()); // a positive value will show the number of heroes that await a command
            GameState.EntityCount = int.Parse(Console.ReadLine());

            GameState.Entities = new List<Entity>();
            for (int i = 0; i < GameState.EntityCount; i++)
            {
                GameState.Entities.Add(new Entity(Console.ReadLine().Split(' ')));
            }
            
            // Write an action using Console.WriteLine()
            // To debug: Console.Error.WriteLine("Debug messages...");
            if (GameState.RoundType < 0)
            {
                Actions.ChooseHero();
            }
            else
            {
                Commands.CommandHeroes();
            }
        }
    }
}

static class Commands
{
    public static void CommandHeroes()
    {
        foreach (Entity hero in GameState.MyHeroes)
        {
            if (hero.itemsOwned < 4 &&
                GameState.Items.Any(i => i.itemCost < GameState.Gold &&
                    i.itemName.ToLower().Contains("blade")
                    //&& i.itemName.ToLower().Contains("bronze") == false
                    ))
            {
                BuyItems(hero);
            }
            else
            {
                OrderHero(hero);
            }
        }
    }

    public static void OrderHero(Entity hero)
    {
        // TODO: Check grouping of units N/E/S/W + on hero (enemy & friendly)
        // Check range of GROOT vs range of enemy hero/units
        // MoveAttack

        Entity enemyHero = GameState.Entities
            .OrderBy(e => e.distanceFromEntity(hero))
            .FirstOrDefault(e => e.team != GameState.MyTeam && e.unitType == "HERO");
        Entity friendlyTower = GameState.Entities
            .FirstOrDefault(h => h.team == GameState.MyTeam && h.unitType == "TOWER");
        Entity enemyTower = GameState.Entities
            .FirstOrDefault(h => h.team != GameState.MyTeam && h.unitType == "TOWER");
        Entity unitThreat = GameState.Entities
            .FirstOrDefault(u => u.team != GameState.MyTeam && u.unitType == "UNIT"
            && u.distanceFromEntity(hero) > 300);
        Entity closestEnemyUnit = GameState.Entities.OrderBy(u => u.distanceFromEntity(friendlyTower))
            .FirstOrDefault(u => u.team != GameState.MyTeam && u.unitType == "UNIT");
        Entity furthestFriendlyUnit = GameState.Entities
            .OrderBy(u => u.distanceFromEntity(enemyTower))
            .FirstOrDefault(u => u.team == GameState.MyTeam &&
                u.unitType == "UNIT");

        // If very safe and GROOT nearby, kill GROOT
        if (GameState.IsEasyGrootKill)
        {
            KillGroot(hero);
            return;
        }

        if (GameState.AreUnitsAdvancing)
        {
            AdvanceWithUnits();
            return;
        }

        if (GameState.AreEnemyHeroesOverPushed)
        {
            KiteEnemyHeroesToTower();
            return;
        }


        // Retreat if required
        if (furthestFriendlyUnit == null)
        {
            if (hero.distanceFromEntity(friendlyTower) > 100)
            {
                Actions.Move(friendlyTower.x, friendlyTower.y);
                return;
            }
            else
            {
                // Do nothing, continue to attack phase
            }
        }
        else if (GameState.Entities
            .Any(u => u.unitType == "UNIT" &&
                u.team == GameState.MyTeam &&
                u.health < hero.attackDamage &&
                u.distanceFromEntity(hero) <= hero.attackRange))
        {
            // Can kill friendly unit for gold
            Console.Error.WriteLine("Deny");
            Entity killEntity = GameState.Entities
                .First(u => u.unitType == "UNIT" &&
                    u.team == GameState.MyTeam &&
                    u.health < hero.attackDamage &&
                    u.distanceFromEntity(hero) <= hero.attackRange);
            Actions.Attack(killEntity.unitId);
            return;
        }
        else if ((enemyTower.distanceFromEntity(hero) - 100) <= enemyTower.distanceFromEntity(furthestFriendlyUnit))
        {
            Console.Error.WriteLine("Retreat behind friendly unit");
            if (furthestFriendlyUnit.x > friendlyTower.x)
                Actions.Move(furthestFriendlyUnit.x - 50, furthestFriendlyUnit.y);
            else
                Actions.Move(furthestFriendlyUnit.x + 50, furthestFriendlyUnit.y);

            return;
        }

        // Attack
        // If enemy hero is in attack range, and enemy units are not
        // Then attack enemy hero
        if (enemyHero != null
            && unitThreat == null
            && enemyTower.distanceFromEntity(hero) > 300)
        {
            Console.Error.WriteLine("Attack enemy hero");
            Actions.Attack(enemyHero.unitId);
            return;
        }
        else
        {
            // Target closest unit by default
            Console.Error.WriteLine("Target closest unit");
            Entity targetEnemy = GameState.Entities
                .OrderBy(u => u.distanceFromEntity(hero))
                .FirstOrDefault(u => u.team != GameState.MyTeam && u.unitType == "UNIT");

            // If a unit is close and has lower health, target it instead
            Entity lowHealthEnemyUnit = GameState.Entities
                .OrderBy(u => u.health)
                .FirstOrDefault(u => u.team != GameState.MyTeam && u.unitType == "UNIT"
                    && u.health < targetEnemy.health && u.distanceFromEntity(targetEnemy) < 50);

            if (lowHealthEnemyUnit != null)
            {
                Console.Error.WriteLine("Target low health unit");
                targetEnemy = lowHealthEnemyUnit;
            }

            if (targetEnemy != null)
            {
                Console.Error.WriteLine("Attack unit");
                Actions.Attack(targetEnemy.unitId);
                return;
            }
            else
            {
                Console.Error.WriteLine("Attack enemy tower");
                Actions.Attack(enemyTower.unitId);
                return;
            }
        }
    }

    private static void KiteEnemyHeroesToTower()
    {
        throw new NotImplementedException();
    }

    private static void AdvanceWithUnits()
    {
        throw new NotImplementedException();
    }

    private static void KillGroot(Entity hero)
    {
        Entity targetGroot = GameState.Entities
            .OrderBy(g => g.distanceFromEntity(hero))
            .FirstOrDefault(g => g.unitType == "GROOT"
                && (GameState.EnemyHasNo("UNIT") || g.distanceFromEntity(hero) - 100 < g.distanceFromEntity(g.nearestEntity("UNIT", GameState.EnemyTeam)))
                && (g.distanceFromEntity(hero) - 100 < g.distanceFromEntity(g.nearestEntity("HERO", GameState.EnemyTeam)))
                && (g.distanceFromEntity(GameState.MyTower) < g.distanceFromEntity(GameState.EnemyTower)));
        Actions.MoveAttack(targetGroot.x, targetGroot.y, targetGroot.unitId);
    }

    public static void BuyItems(Entity hero)
    {
        Item newItem = GameState.Items
            .OrderByDescending(i => i.itemCost)
            .FirstOrDefault(i => i.itemCost < GameState.Gold &&
                i.itemName.ToLower().Contains("blade")
                //&& i.itemName.ToLower().Contains("bronze") == false
                );

        if (newItem != null)
        {
            Actions.Buy(newItem.itemName);
            //hero.items.Add(newItem);
        }
    }
}

static class Actions
{
    public static void ChooseHero()
    {
        if (GameState.MyHeroes.Count == 0)
        {
            Console.WriteLine("IRONMAN");
        }
        else
        {
            Console.WriteLine("VALKYRIE");
        }
    }

    public static void Wait()
    {
        Console.WriteLine("WAIT");
    }

    public static void Attack(int unitId)
    {
        Console.WriteLine($"ATTACK {unitId}");
    }

    public static void Move(int x, int y)
    {
        Console.WriteLine($"MOVE {x} {y}");
    }

    public static void AttackNearest(string unitType)
    {
        Console.WriteLine($"ATTACK_NEAREST {unitType}");
    }

    public static void MoveAttack(int x, int y, int unitId)
    {
        Console.WriteLine($"MOVE_ATTACK {x} {y} {unitId}");
    }

    public static void Buy(string itemName)
    {
        Console.WriteLine($"BUY {itemName}");
    }

    public static void Sell(string itemName)
    {
        Console.WriteLine($"SELL {itemName}");
    }
}

class Item
{
    public string itemName { get; set; } // contains keywords such as BRONZE, SILVER and BLADE, BOOTS connected by "_" to help you sort easier
    public int itemCost { get; set; } // BRONZE items have lowest cost, the most expensive items are LEGENDARY
    public int damage { get; set; } // keyword BLADE is present if the most important item stat is damage
    public int health { get; set; }
    public int maxHealth { get; set; }
    public int mana { get; set; }
    public int maxMana { get; set; }
    public int moveSpeed { get; set; } // keyword BOOTS is present if the most important item stat is moveSpeed
    public int manaRegeneration { get; set; }
    public int isPotion { get; set; } // 0 if it's not instantly consumed

    public Item(string[] inputs)
    {
        itemName = inputs[0];
        itemCost = int.Parse(inputs[1]);
        damage = int.Parse(inputs[2]);
        health = int.Parse(inputs[3]);
        maxHealth = int.Parse(inputs[4]);
        mana = int.Parse(inputs[5]);
        maxMana = int.Parse(inputs[6]);
        moveSpeed = int.Parse(inputs[7]);
        manaRegeneration = int.Parse(inputs[8]);
        isPotion = int.Parse(inputs[9]);
    }
}

class Entity
{
    public int unitId { get; set; }
    public int team { get; set; }
    public string unitType { get; set; }
    public int attackRange { get; set; }
    public int health { get; set; }
    public int mana { get; set; }
    public int attackDamage { get; set; }
    public int movementSpeed { get; set; }
    public int maxHealth { get; set; }
    public int maxMana { get; set; }

    public int x { get; set; }
    public int y { get; set; }

    public string heroType { get; set; }
    public int manaRegeneration { get; set; }
    public int goldValue { get; set; }
    public int isVisible { get; set; }
    public int itemsOwned { get; set; }

    public List<Item> items { get; set; }
    
    public Entity()
    {
        items = new List<Item>();
    }

    public Entity(string[] inputs)
    {
        unitId = int.Parse(inputs[0]);
        team = int.Parse(inputs[1]);
        unitType = inputs[2]; // UNIT, HERO, TOWER, can also be GROOT from wood1
        x = int.Parse(inputs[3]);
        y = int.Parse(inputs[4]);
        attackRange = int.Parse(inputs[5]);
        health = int.Parse(inputs[6]);
        maxHealth = int.Parse(inputs[7]);
        //shield = int.Parse(inputs[8]), // useful in bronze
        attackDamage = int.Parse(inputs[9]);
        movementSpeed = int.Parse(inputs[10]);
        //stunDuration = int.Parse(inputs[11]), // useful in bronze
        goldValue = int.Parse(inputs[12]);
        //countDown1 = int.Parse(inputs[13]), // all countDown and mana variables are useful starting in bronze
        //countDown2 = int.Parse(inputs[14]),
        //countDown3 = int.Parse(inputs[15]),
        mana = int.Parse(inputs[16]);
        maxMana = int.Parse(inputs[17]);
        manaRegeneration = int.Parse(inputs[18]);
        heroType = inputs[19]; // DEADPOOL, VALKYRIE, DOCTOR_STRANGE, HULK, IRONMAN
        isVisible = int.Parse(inputs[20]); // 0 if it isn't
        itemsOwned = int.Parse(inputs[21]); // useful from wood1
    }

    public bool isRanged
    {
        get { return attackRange > 150; }
    }

    public int distanceFromEntity(Entity e)
    {
        int xDistance = Math.Abs(e.x - this.x);
        int yDistance = Math.Abs(e.y - this.y);

        return Convert.ToInt32(Math.Sqrt((xDistance * xDistance) + (yDistance * yDistance)));
    }

    public Entity nearestEntity(string unitType, int team)
    {
        Entity entity = GameState.Entities
            .OrderBy(e => e.distanceFromEntity(this))
            .FirstOrDefault(e => e.unitType == "unitType"
                && e.team == team);
        return entity;
    }
}

class Hero : Entity
{
    public Hero(string heroType, int health, int mana, int attackDamage,
        int movementSpeed, int manaRegeneration, int attackRange)
    {
        unitType = "HERO";
        this.heroType = heroType;
        this.health = health;
        this.mana = mana;
        this.attackDamage = attackDamage;
        this.movementSpeed = movementSpeed;
        this.manaRegeneration = manaRegeneration;
        this.attackRange = attackRange;
    }
}

class GameState
{
    public static int MyTeam { get; set; }
    public static int EnemyTeam {
        get
        {
            return MyTeam == 1 ? 0 : 1;
        }
    }
    public static int Gold { get; set; }
    public static int EnemyGold { get; set; }
    public static int RoundType { get; set; }
    public static int EntityCount { get; set; }
    public static int ItemCount { get; set; }
    
    public static List<Hero> Heroes;
    public static List<Entity> Entities;
    public static List<Item> Items;
    public static List<Item> MyItems;

    public static List<Entity> MyHeroes
    {
        get
        {
            return Entities.Where(e => e.unitType == "HERO").ToList<Entity>();
        }
    }

    public static bool IsEasyGrootKill
    {
        get
        {
            Entity targetGroot

            return false;
        }
    }
    public static bool AreUnitsAdvancing
    {
        get
        {
            return false;
        }
    }
    public static bool AreEnemyHeroesOverPushed
    {
        get
        {
            return false;
        }
    }
}
