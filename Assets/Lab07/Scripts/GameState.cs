using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lab07
{
    public struct ShipDef { public string id, name, desc, icon; public int speed, armor, handling, price; public Color color; }
    public struct BoostDef { public string id, name, desc, icon; public int price; public Color color; }
    public struct ShopItem { public string id, name, desc, icon, tab; public int price; public Color color; }

    /// <summary>Persistent player state (PlayerPrefs) + static game data. Everything the menus show is real, saved data.</summary>
    public static class GS
    {
        public static event Action Changed;
        static void Fire() { Changed?.Invoke(); }

        public const int Levels = 12;
        public static readonly string[] LevelNames = { "Launch Pad", "Dust Lane", "Asteroid Belt", "Ion Storm", "Crystal Rift", "Dead Orbit", "Solar Flare", "Black Gate", "Echo Void", "Nebula Run", "Iron Maze", "Nova Core" };

        public static readonly ShipDef[] Ships =
        {
            new ShipDef{ id="comet",   name="COMET",   desc="Balanced starter. Reliable in every sector.", icon="ship1", speed=5, armor=5, handling=6, price=0,    color=Pal.Cyan },
            new ShipDef{ id="falcon",  name="FALCON",  desc="Heavy hull. Slow to turn, hard to kill.",     icon="ship2", speed=4, armor=9, handling=3, price=1800, color=Pal.Amber },
            new ShipDef{ id="specter", name="SPECTER", desc="Razor-fast glider with a glass-thin hull.",   icon="ship3", speed=9, armor=2, handling=8, price=3500, color=Pal.Pink },
        };
        public static readonly BoostDef[] Boosts =
        {
            new BoostDef{ id="shield", name="Shield", desc="Absorb one hit", icon="shield", price=300, color=Pal.Cyan },
            new BoostDef{ id="magnet", name="Magnet", desc="Pull in coins",  icon="magnet", price=250, color=Pal.Amber },
            new BoostDef{ id="x2",     name="Score x2", desc="Double points", icon="bolt",  price=400, color=Pal.Pink },
        };
        public static readonly ShopItem[] ShopItems =
        {
            new ShopItem{ id="b_shield", name="Shield Pack", desc="3 charges", icon="shield", tab="Boosts", price=300, color=Pal.Cyan },
            new ShopItem{ id="b_magnet", name="Magnet Pack", desc="3 charges", icon="magnet", tab="Boosts", price=250, color=Pal.Amber },
            new ShopItem{ id="b_x2",     name="Score x2",    desc="3 charges", icon="bolt",   tab="Boosts", price=400, color=Pal.Pink },
            new ShopItem{ id="b_life",   name="Extra Life",  desc="+1 heart",  icon="heart",  tab="Boosts", price=600, color=Pal.Pink },
            new ShopItem{ id="s_aurora", name="Aurora Trail", desc="Cyan glow",  icon="sparkle", tab="Skins", price=900,  color=Pal.Cyan },
            new ShopItem{ id="s_ember",  name="Ember Trail",  desc="Amber glow", icon="sparkle", tab="Skins", price=900,  color=Pal.Amber },
            new ShopItem{ id="s_gold",   name="Gold Hull",    desc="Shiny paint", icon="ship1",  tab="Skins", price=2200, color=Pal.Amber },
            new ShopItem{ id="s_ghost",  name="Ghost Hull",   desc="Pale paint",  icon="ship3",  tab="Skins", price=2600, color=Pal.White },
            new ShopItem{ id="c_small",  name="Pouch",        desc="+500 coins",  icon="coin", tab="Coins", price=0, color=Pal.Amber },
            new ShopItem{ id="c_med",    name="Sack",         desc="+1,500 coins", icon="coin", tab="Coins", price=0, color=Pal.Amber },
            new ShopItem{ id="c_big",    name="Chest",        desc="+4,000 coins", icon="coin", tab="Coins", price=0, color=Pal.Amber },
            new ShopItem{ id="c_gem",    name="Gem Bundle",   desc="+10,000 coins", icon="gem", tab="Coins", price=0, color=Pal.Cyan },
        };
        public static int CoinPackAmount(string id) => id == "c_small" ? 500 : id == "c_med" ? 1500 : id == "c_big" ? 4000 : 10000;
        public static string CoinPackPrice(string id) => id == "c_small" ? "FREE" : id == "c_med" ? "$0.99" : id == "c_big" ? "$1.99" : "$3.99";

        // ---------------- values
        public static int Coins { get; private set; } = Get("l7_coins", 1250);
        public static int BestScore => Get("l7_best", 0);
        public static int Xp => Get("l7_xp", 640);
        public static float Sensitivity { get => PlayerPrefs.GetFloat("l7_sens", .6f); set { PlayerPrefs.SetFloat("l7_sens", value); } }
        public static bool Shake { get => Get("l7_shake", 1) == 1; set { Set("l7_shake", value ? 1 : 0); } }
        public static int Quality { get => Get("l7_q", 1); set { Set("l7_q", value); QualitySettings.SetQualityLevel(Mathf.Clamp(value * 2, 0, QualitySettings.names.Length - 1), true); } }
        public static int ShipIndex { get => Get("l7_ship", 0); set { Set("l7_ship", value); Fire(); } }
        public static int BoostIndex { get => Get("l7_boost", 0); set { Set("l7_boost", value); Fire(); } }

        static int Get(string k, int d) => PlayerPrefs.GetInt(k, d);
        static void Set(string k, int v) { PlayerPrefs.SetInt(k, v); }

        // ---------------- economy
        public static void AddCoins(int n) { Coins += n; Set("l7_coins", Coins); Fire(); }
        public static bool Spend(int n) { if (Coins < n) return false; Coins -= n; Set("l7_coins", Coins); Fire(); return true; }
        public static bool Owns(string id) => Get("l7_own_" + id, id == "comet" ? 1 : 0) == 1;
        public static void Own(string id) { Set("l7_own_" + id, 1); Fire(); }
        public static int BoostCount(string id) => Get("l7_cnt_" + id, id == "shield" ? 2 : 0);
        public static void AddBoost(string id, int n) { Set("l7_cnt_" + id, BoostCount(id) + n); Fire(); }
        public static void AddXp(int n) { Set("l7_xp", Xp + n); }

        // ---------------- campaign
        public static int Stars(int level) => Get("l7_stars_" + level, level == 1 ? 3 : level == 2 ? 2 : level == 3 ? 1 : 0);
        public static int Best(int level) => Get("l7_lbest_" + level, level == 1 ? 4200 : level == 2 ? 3650 : level == 3 ? 2100 : 0);
        public static bool Unlocked(int level) => level <= 1 || Stars(level - 1) > 0;
        public static int TotalStars { get { int s = 0; for (int i = 1; i <= Levels; i++) s += Stars(i); return s; } }
        public static int Cleared { get { int s = 0; for (int i = 1; i <= Levels; i++) if (Stars(i) > 0) s++; return s; } }

        public static void Submit(int level, int stars, int score)
        {
            if (stars > Stars(level)) Set("l7_stars_" + level, stars);
            if (score > Best(level)) Set("l7_lbest_" + level, score);
            if (score > BestScore) Set("l7_best", score);
            Fire();
        }

        public static void RecordBestScore(int score)
        {
            if (score > BestScore) Set("l7_best", score);
        }

        public static void ResetAll()
        {
            for (int level = 1; level <= Levels; level++)
            {
                PlayerPrefs.DeleteKey("l7_stars_" + level);
                PlayerPrefs.DeleteKey("l7_lbest_" + level);
            }
            foreach (var ship in Ships) PlayerPrefs.DeleteKey("l7_own_" + ship.id);
            foreach (var item in ShopItems) PlayerPrefs.DeleteKey("l7_own_" + item.id);
            foreach (var boost in Boosts) PlayerPrefs.DeleteKey("l7_cnt_" + boost.id);
            foreach (string key in new[] { "coins", "best", "xp", "sens", "shake", "q", "ship", "boost", "cnt_life" })
                PlayerPrefs.DeleteKey("l7_" + key);
            Coins = 1250;
            PlayerPrefs.Save();
            Fire();
        }

        // ---------------- session (what the player picked in the menus)
        public static class Session
        {
            public static int Mode;          // 0 campaign, 1 endless, 2 time attack
            public static int Level = 4;
            public static bool Win; public static int Score, CoinsEarned, Stars; public static float Accuracy; public static bool NewBest;
            public static string ModeName => Mode == 0 ? "Campaign" : Mode == 1 ? "Endless" : "Time Attack";
            public static string Title => Mode == 0 ? $"Level {Level} · {GS.LevelNames[Level - 1]}" : ModeName;
        }
    }
}
