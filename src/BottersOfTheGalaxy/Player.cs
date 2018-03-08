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
        #region Initial setup
        GameState.MyTeam = int.Parse(Console.ReadLine());

        GameState.ObjectCount = int.Parse(Console.ReadLine()); // useful from wood1, represents the number of bushes and the number of places where neutral units can spawn
        GameState.Objects = new List<Object>();
        for (int i = 0; i < GameState.ObjectCount; i++)
        {
            GameState.Objects.Add(new Object(Console.ReadLine().Split(' ')));
        }

        GameState.ItemCount = int.Parse(Console.ReadLine()); // useful from wood2
        GameState.Items = new List<Item>();
        for (int i = 0; i < GameState.ItemCount; i++)
        {
            GameState.Items.Add(new Item(Console.ReadLine().Split(' ')));
        }
        #endregion

        // game loop
        while (true)
        {
            #region Game loop setup
            GameState.Gold = int.Parse(Console.ReadLine());
            GameState.EnemyGold = int.Parse(Console.ReadLine());
            GameState.RoundType = int.Parse(Console.ReadLine()); // a positive value will show the number of heroes that await a command

            GameState.EntityCount = int.Parse(Console.ReadLine());
            GameState.Entities = new List<Entity>();
            for (int i = 0; i < GameState.EntityCount; i++)
            {
                GameState.Entities.Add(new Entity(Console.ReadLine().Split(' ')));
            }
            #endregion

            #region Process an action
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
            #endregion
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
                    && i.itemName.ToLower().Contains("bronze") == false
                    ))
            {
                BuyWeapon(hero);
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
        Entity unitThreat = GameState.Entities
            .FirstOrDefault(u => u.team != GameState.MyTeam && u.unitType == "UNIT"
            && u.distanceFromEntity(hero) > 300);
        Entity closestEnemyUnit = GameState.Entities.OrderBy(u => u.distanceFromEntity(GameState.MyTower))
            .FirstOrDefault(u => u.team != GameState.MyTeam && u.unitType == "UNIT");
        Entity furthestFriendlyUnit = GameState.Entities
            .OrderBy(u => u.distanceFromEntity(GameState.EnemyTower))
            .FirstOrDefault(u => u.team == GameState.MyTeam &&
                u.unitType == "UNIT");
        var myUnitsAroundEnemyHero = GameState.Entities
            .Where(e => e.team == GameState.MyTeam
                && e.unitType == "UNIT"
                && e.distanceFromEntity(enemyHero) <= 400);
        var enemyUnitsAroundEnemyHero = GameState.Entities
            .Where(e => e.team == GameState.EnemyTeam
                && e.unitType == "UNIT"
                && e.distanceFromEntity(enemyHero) <= 400);

        //Ambush(hero);
        //return;

        // If very safe and GROOT nearby, kill GROOT
        /*if (GameState.IsEasyGrootKill(hero))
        {
            KillGroot(hero);
            return;
        }*/

        // If dying, get potion
        if (hero.itemsOwned < 4 &&
            GameState.Items.Any(i => i.itemCost < GameState.Gold &&
                i.itemName.ToLower().Contains("potion")
                && hero.health < 200
        ))
        {
            BuyPotion(hero);
        }

        Console.Error.WriteLine($"{myUnitsAroundEnemyHero.Count()} vs {enemyUnitsAroundEnemyHero.Count()}");
        if (myUnitsAroundEnemyHero.Count() > enemyUnitsAroundEnemyHero.Count() + 2
            && hero.distanceFromEntity(GameState.EnemyTower) > 300)
        {
            BeAggressive(hero);
            return;
        }
        else
        {
            PlayPassive(hero);
            return;
        }
        
        // Retreat if required
        if (furthestFriendlyUnit == null)
        {
            if (hero.distanceFromEntity(GameState.MyTower) > 100)
            {
                Actions.Move(GameState.MyTower.x, GameState.MyTower.y);
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
        else if ((GameState.EnemyTower.distanceFromEntity(hero) - 100) <= GameState.EnemyTower.distanceFromEntity(furthestFriendlyUnit))
        {
            Console.Error.WriteLine("Retreat behind friendly unit");
            if (furthestFriendlyUnit.x > GameState.MyTower.x)
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
            && GameState.EnemyTower.distanceFromEntity(hero) > 300)
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
                Actions.Attack(GameState.EnemyTower.unitId);
                return;
            }
        }
    }

    private static void Ambush(Entity hero)
    {
        Entity nearestEnemyHero = hero.nearestEntity("HERO", GameState.EnemyTeam);

        if (1 == 1 // nearestEnemyHero != null && hero.distanceFromEntity(nearestEnemyHero) < 200
            && ((hero.team == 0 && nearestEnemyHero.x < hero.x - 50) || (hero.team == 1 && nearestEnemyHero.x > hero.x + 50)))
        {
            int enemyUnitsNearEnemyHero =
                GameState.Entities.Count(e => e.team == GameState.EnemyTeam
                    && e.unitType == "UNIT"
                    && e.distanceFromEntity(nearestEnemyHero) < 200);
            int friendlyUnitsNearEnemyHero =
                GameState.Entities.Count(e => e.team == GameState.MyTeam
                    && e.unitType == "UNIT"
                    && e.distanceFromEntity(nearestEnemyHero) < 200);

            if (friendlyUnitsNearEnemyHero / 2 <= enemyUnitsNearEnemyHero)
            {
                Actions.Attack(nearestEnemyHero.unitId);
            }
            else
            {
                Actions.Wait();
            }

            return;
        }
        else
        {
            //Object bush = hero.nearestObject("BUSH");

            Object bush = GameState.Objects
                .OrderBy(b => b.distanceFromEntity(hero))
                .FirstOrDefault(b => b.entityType == "BUSH" 
                    && b.distanceFromEntity(GameState.MyTower) > 500
                    && Math.Abs(b.y - GameState.MyTower.y) < 200);

            if (bush == null)
            {
                Actions.Wait();
            }
            if (bush.x == hero.x && bush.y == hero.y)
            {
                Actions.Wait();
            }
            else
            {
                Actions.Move(bush.x, bush.y);
            }
            return;
        }
    }

    private static void KiteEnemyHeroesToTower()
    {
        throw new NotImplementedException();
    }

    private static void BeAggressive(Entity hero)
    {
        int buffer = 125;
        int teamMulti = GameState.MyTeam == 0 ? 1 : -1;

        Entity enemyHero = GameState.Entities
            .OrderBy(e => e.distanceFromEntity(hero))
            .First(e => e.team == GameState.EnemyTeam
                && e.unitType == "HERO");

        if (1 == 1)
        {
            //int attackX = hero.x - teamMulti *
            //    ((enemyHero.attackRange - enemyHero.distanceFromEntity(hero) + buffer)); // removed buffer from attackX
            //int attackY = hero.y > enemyHero.y ? hero.y - buffer : hero.y + buffer;
            //Actions.MoveAttack(attackX, attackY, enemyHero.unitId);
            Console.Error.WriteLine("ATTACK HERO");
            Actions.Attack(enemyHero.unitId);
            return;
        }
        
        if (enemyHero.distanceFromEntity(hero) - buffer <= enemyHero.attackRange)
        {
            int retreatX = hero.x - teamMulti *
                (enemyHero.attackRange - enemyHero.distanceFromEntity(hero) + buffer);
            int retreatY = hero.y > enemyHero.y ? hero.y - buffer : hero.y + buffer;

            //Actions.MoveAttack(retreatX, retreatY, hero.nearestEntity("UNIT", GameState.EnemyTeam).unitId);
            Actions.Move(retreatX, retreatY);
        }
        else if (GameState.EnemyTower.distanceFromEntity(hero) - buffer <= GameState.EnemyTower.attackRange)
        {
            int retreatX = hero.x - teamMulti *
                (GameState.EnemyTower.attackRange - GameState.EnemyTower.distanceFromEntity(hero) + buffer);
            int retreatY = hero.y > GameState.EnemyTower.y ? hero.y - buffer : hero.y + buffer;

            Actions.Move(retreatX, retreatY);
        }
        else
        {
            //Actions.Attack(hero.nearestEntity("UNIT", GameState.EnemyTeam).unitId);

            int attackX = hero.x - teamMulti *
                ((enemyHero.attackRange - enemyHero.distanceFromEntity(hero) + buffer) / 3); // removed buffer from attackX
            int attackY = hero.y > enemyHero.y ? hero.y - buffer : hero.y + buffer;
            Entity attackEnemy = GameState.Entities
                .OrderBy(e => e.distanceFromEntity(hero))
                .First(e => e.team == GameState.EnemyTeam);
            Actions.MoveAttack(attackX, attackY, attackEnemy.unitId);
            //Console.WriteLine($"FIREBALL {enemyHero.x} {enemyHero.y}");
        }
    }

    private static void PlayPassive(Entity hero)
    {
        int buffer = 125;
        int teamMulti = GameState.MyTeam == 0 ? 1 : -1;

        // If no units in front, retreat
        if (GameState.TeamHasEntities("UNIT", GameState.MyTeam) &&
            GameState.EnemyTower.distanceFromEntity(GameState.EnemyTower.nearestEntity("UNIT", GameState.MyTeam)) >
            GameState.EnemyTower.distanceFromEntity(hero))
        {
            Actions.Move(GameState.MyTower.x - (teamMulti * buffer), GameState.MyTower.y);
            return;
        }

        Entity enemyHero = GameState.Entities
            .OrderByDescending(e => e.attackRange - e.distanceFromEntity(hero))
            .First(e => e.team == GameState.EnemyTeam
                && e.unitType == "HERO");
        
        if (enemyHero.distanceFromEntity(hero) - buffer <= enemyHero.attackRange)
        {
            int retreatX = hero.x - teamMulti *
                (enemyHero.attackRange - enemyHero.distanceFromEntity(hero) + buffer);
            int retreatY = hero.y > enemyHero.y ? hero.y - buffer : hero.y + buffer;

            //Actions.MoveAttack(retreatX, retreatY, hero.nearestEntity("UNIT", GameState.EnemyTeam).unitId);
            Actions.Move(retreatX, retreatY);
        }
        else if (GameState.EnemyTower.distanceFromEntity(hero) - buffer <= GameState.EnemyTower.attackRange)
        {
            int retreatX = hero.x - teamMulti *
                (GameState.EnemyTower.attackRange - GameState.EnemyTower.distanceFromEntity(hero) + buffer);
            int retreatY = hero.y > GameState.EnemyTower.y ? hero.y - buffer : hero.y + buffer;

            Actions.Move(retreatX, retreatY);
        }
        else
        {
            //Actions.Attack(hero.nearestEntity("UNIT", GameState.EnemyTeam).unitId);

            int attackX = hero.x - teamMulti *
                ((enemyHero.attackRange - enemyHero.distanceFromEntity(hero) + buffer) / 3); // removed buffer from attackX
            int attackY = hero.y > enemyHero.y ? hero.y - buffer : hero.y + buffer;
            Entity attackEnemy = GameState.Entities
                .OrderBy(e => e.distanceFromEntity(hero))
                .First(e => e.team == GameState.EnemyTeam);
            Actions.MoveAttack(attackX, attackY, attackEnemy.unitId);
            //Console.WriteLine($"FIREBALL {enemyHero.x} {enemyHero.y}");
        }
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
                //&& (GameState.TeamHasEntities("UNIT", GameState.EnemyTeam) == false
                //    || g.distanceFromEntity(hero) - 100 < g.distanceFromEntity(g.nearestEntity("UNIT", GameState.EnemyTeam)))
                && (g.distanceFromEntity(hero) - 100 < g.distanceFromEntity(g.nearestEntity("HERO", GameState.EnemyTeam)))
                //&& (g.distanceFromEntity(GameState.MyTower) < g.distanceFromEntity(GameState.EnemyTower))
                );

        Actions.MoveAttack(targetGroot.x, targetGroot.y, targetGroot.unitId);
    }

    public static void BuyWeapon(Entity hero)
    {
        Item newItem = GameState.Items
            .OrderByDescending(i => i.itemCost)
            .FirstOrDefault(i => i.itemCost < GameState.Gold &&
                i.itemName.ToLower().Contains("blade")
                && i.itemName.ToLower().Contains("bronze") == false
                );

        if (newItem != null)
        {
            Actions.Buy(newItem.itemName);
            GameState.Gold = GameState.Gold - newItem.itemCost;
        }
    }

    public static void BuyPotion(Entity hero)
    {
        Item newItem = GameState.Items
            .OrderByDescending(i => i.itemCost)
            .FirstOrDefault(i => i.itemCost < GameState.Gold &&
                i.itemName.ToLower().Contains("potion")
                );

        if (newItem != null)
        {
            Actions.Buy(newItem.itemName);
            GameState.Gold = GameState.Gold - newItem.itemCost;
        }
    }
}

class GameState
{
    public static int MyTeam { get; set; }
    public static int EnemyTeam {
        get
        {
            return MyTeam == 0 ? 1 : 0;
        }
    }
    public static Entity MyTower
    {
        get
        {
            return GameState.Entities
                .FirstOrDefault(e => e.team == GameState.MyTeam
                    && e.unitType == "TOWER");
        }
    }
    public static Entity EnemyTower
    {
        get
        {
            return GameState.Entities
                .FirstOrDefault(e => e.team != GameState.MyTeam
                    && e.unitType == "TOWER");
        }
    }
    public static int Gold { get; set; }
    public static int EnemyGold { get; set; }
    public static int RoundType { get; set; }
    public static int EntityCount { get; set; }
    public static int ItemCount { get; set; }
    public static int ObjectCount { get; set; }
    
    public static List<Entity> Entities;
    public static List<Object> Objects;
    public static List<Item> Items;

    public static List<Entity> MyHeroes
    {
        get
        {
            return Entities.Where(e => e.unitType == "HERO"
                && e.team == GameState.MyTeam).ToList<Entity>();
        }
    }

    public static bool IsEasyGrootKill(Entity hero)
    {
        if (GameState.Entities.Any(e => e.unitType == "GROOT") == false)
            return false;

        Entity targetGroot = GameState.Entities
            .OrderBy(g => g.distanceFromEntity(hero))
            .FirstOrDefault(g => g.unitType == "GROOT"
                //&& (GameState.TeamHasEntities("UNIT", GameState.EnemyTeam) == false
                //    || g.distanceFromEntity(hero) - 100 < g.distanceFromEntity(g.nearestEntity("UNIT", GameState.EnemyTeam)))
                && (g.distanceFromEntity(hero) - 100 < g.distanceFromEntity(g.nearestEntity("HERO", GameState.EnemyTeam)))
                //&& (g.distanceFromEntity(GameState.MyTower) < g.distanceFromEntity(GameState.EnemyTower))
                );

        return targetGroot != null;
}

    public static bool TeamHasEntities(string unitType, int team)
    {
        return GameState.Entities
            .Any(e => e.unitType == unitType
                && e.team == team);
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
            Console.WriteLine("DEADPOOL");
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

class Object
{
    public string entityType { get; set; } // BUSH, from wood1 it can also be SPAWN
    public int x { get; set; }
    public int y { get; set; }
    public int radius { get; set; }

    public Object()
    {

    }

    public Object(string[] inputs)
    {
        entityType = inputs[0];
        x = int.Parse(inputs[1]);
        y = int.Parse(inputs[2]);
        radius = int.Parse(inputs[3]);
    }

    public int distanceFromEntity(Entity e)
    {
        int xDistance = Math.Abs(e.x - this.x);
        int yDistance = Math.Abs(e.y - this.y);

        return Convert.ToInt32(Math.Sqrt((xDistance * xDistance) + (yDistance * yDistance)));
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
    public int countDown1 { get; set; }
    public int countDown2 { get; set; }
    public int countDown3 { get; set; }
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
        countDown1 = int.Parse(inputs[13]); // all countDown and mana variables are useful starting in bronze
        countDown2 = int.Parse(inputs[14]);
        countDown3 = int.Parse(inputs[15]);
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

        if (xDistance + yDistance == 0)
            return 0;

        return Convert.ToInt32(Math.Sqrt((xDistance * xDistance) + (yDistance * yDistance)));
    }

    public Entity nearestEntity(string unitType)
    {
        Entity entity = GameState.Entities
            .OrderBy(e => e.distanceFromEntity(this))
            .First(e => e.unitType == unitType);
        return entity;
    }

    public Entity nearestEntity(string unitType, int team)
    {
        Entity entity = GameState.Entities
            .OrderBy(e => e.distanceFromEntity(this))
            .First(e => e.unitType == unitType
                && e.team == team);
        return entity;
    }

    public Object nearestObject(string entityType)
    {
        Object obj = GameState.Objects
            .OrderBy(o => o.distanceFromEntity(this))
            .First(o => o.entityType == entityType);
        return obj;
    }
}
