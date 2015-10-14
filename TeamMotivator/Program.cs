using System;
using System.Collections.Generic;
using LeagueSharp;
using LeagueSharp.Common;

namespace TeamMotivator
{
    class Program
    {
        public static Obj_AI_Base Player = ObjectManager.Player;
        public static List<string> Messages;
        public static List<string> Starts;
        public static List<string> Endings;
        public static List<string> Smileys;
        public static List<string> Greetings;
        public static Dictionary<GameEventId, int> Rewards;
        public static Random rand = new Random();

        public static Menu Settings;

        public static int kills = 0;
        public static int deaths = 0;
        public static float congratzTime = 0;
        public static float lastMessage = 0;

        static void Main(string[] args)
        {
            setupMenu();
            setupMessages();
            setupRewards();

            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
            CustomEvents.Game.OnGameEnd += Game_OnGameEnd;
            Game.OnStart += Game_OnGameStart;
            Game.OnNotify += Game_OnGameNotifyEvent;
            Game.OnUpdate += Game_OnGameUpdate;
        }

        static void setupMenu()
        {
            Settings = new Menu("TeamMotivator", "TeamMotivator", true);
            Settings.AddItem(new MenuItem("sayGreeting", "Greeting").SetValue(true));
            Settings.AddItem(new MenuItem("sayGreetingAllChat", "All Chat Greeting").SetValue(true));
            Settings.AddItem(new MenuItem("sayGreetingDelayMin", "Greeting Min Delay").SetValue(new Slider(30, 10, 120)));
            Settings.AddItem(new MenuItem("sayGreetingDelayMax", "Greeting Max Delay").SetValue(new Slider(90, 10, 120)));
            Settings.AddItem(new MenuItem("sayCongratulate", "Congratulate Players").SetValue(true));
            Settings.AddItem(new MenuItem("sayCongratulateDelayMin", "Congratulate Min Delay").SetValue(new Slider(7, 5, 100)));
            Settings.AddItem(new MenuItem("sayCongratulateDelayMax", "Congratulate Max Delay").SetValue(new Slider(15, 5, 100)));
            Settings.AddItem(new MenuItem("sayCongratulateInterval", "Interval Between Messages").SetValue(new Slider(30, 5, 600)));
            Settings.AddToMainMenu();
        }

        static void setupRewards()
        {
            Rewards = new Dictionary<GameEventId, int>
            {
                { GameEventId.OnChampionDie, 1 },  // champion kill
                { GameEventId.OnTurretDamage, 1 }, // turret kill
            };
        }

        static void setupMessages()
        {
            Messages = new List<string>
            {
                "gj", "good job", "very gj", "very good job", "wp", "well played",
                "well", "nicely played", "np", "amazing",
                "nice", "nice1", "nice one", "well done", "sweet", "good"
            };

            Starts = new List<string>
            {
                "", " ", "that was ",
                "  ", "wow ", "wow, "
            };

            Endings = new List<string>
            {
                "", " m8", " mate",
                " friend", " team",  " guys",
                " friends", " boys", " man"
            };

            Smileys = new List<string>
            {
                "",
                " ^^",
                " :p"
            };

            Greetings = new List<string>
            {
                "gl", "good luck", "hf", "have fun", "Good Luck & Have Fun",
                "Let's Fun", "gl hf", "gl and hf", "gl & hf",
                "Good Luck, Have Fun", "Let's all have a nice game!"
            };
        }

        static string getRandomElement( List<string> collection, bool firstEmpty = true )
        {
            if (firstEmpty && rand.Next(3) == 0)
                return collection[0];

            return collection[rand.Next(collection.Count)];
        }

        static string generateMessage()
        {
            string message = getRandomElement(Starts);
            message += getRandomElement(Messages, false);
            message += getRandomElement(Endings);
            message += getRandomElement(Smileys);
            return message;
        }

        static string generateGreeting()
        {
            string greeting = getRandomElement(Greetings, false);
            greeting += getRandomElement(Smileys);
            return greeting;
        }

        static void sayCongratulations()
        {
            if (Settings.Item("sayCongratulate").GetValue<bool>() && Game.ClockTime > lastMessage + Settings.Item("sayCongratulateInterval").GetValue<Slider>().Value)
            {
                lastMessage = Game.ClockTime;
                Game.Say(generateMessage());
            }
        }

        static void sayGreeting()
        {
            if( Settings.Item("sayGreetingAllChat").GetValue<bool>() )
            {
                Game.Say("/all " + generateGreeting());
            }
            else
            {
                Game.Say(generateGreeting());
            }
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            Game.PrintChat("<font color = \"#2fff0a\">TeamMotivator by xaxixeo</font>");
        }


        static void Game_OnGameStart(EventArgs args)
        {
            if( !Settings.Item("sayGreeting").GetValue<bool>() )
            {
                return;
            }

            int minDelay = Settings.Item("sayGreetingDelayMin").GetValue<Slider>().Value;
            int maxDelay = Settings.Item("sayGreetingDelayMax").GetValue<Slider>().Value;

            // greeting message
            Utility.DelayAction.Add(rand.Next(Math.Min(minDelay, maxDelay), Math.Max(minDelay, maxDelay)) * 1000, sayGreeting);
        }

        static void Game_OnGameEnd(EventArgs args)
        {
            Utility.DelayAction.Add((new Random(Environment.TickCount).Next(100, 1001)), () => Game.Say("/all gg wp"));
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            // champ kill message
            if (kills > deaths && congratzTime < Game.ClockTime && congratzTime != 0)
            {
                sayCongratulations();

                kills = 0;
                deaths = 0;
                congratzTime = 0;
            }
            else if (kills != deaths && congratzTime < Game.ClockTime)
            {
                kills = 0;
                deaths = 0;
                congratzTime = 0;
            }            
        }

        static void Game_OnGameNotifyEvent(GameNotifyEventArgs args)
        {
            if( Rewards.ContainsKey( args.EventId ) )
            {
                Obj_AI_Base Killer = ObjectManager.GetUnitByNetworkId<Obj_AI_Base>((int)args.NetworkId);
                
                if( Killer.IsAlly )
                {
                    // we will not congratulate ourselves lol :D
                    if( (kills == 0 && !Killer.IsMe) || kills > 0 )
                    {
                        kills += Rewards[args.EventId];
                    }
                }
                else
                {
                    deaths += Rewards[args.EventId];
                }
            }
            else
            {
                return;
            }

            int minDelay = Settings.Item("sayCongratulateDelayMin").GetValue<Slider>().Value;
            int maxDelay = Settings.Item("sayCongratulateDelayMax").GetValue<Slider>().Value;
     
            congratzTime = Game.ClockTime + rand.Next( Math.Min(minDelay, maxDelay), Math.Max(minDelay, maxDelay) );
        }
    }
}