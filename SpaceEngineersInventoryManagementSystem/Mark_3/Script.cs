using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;

namespace Mark_3
{
    class Script : MyGridProgram
    {

        //USER SETTING

        readonly string gridCallingCard = "SpaceTardsHomeBase";

        readonly string setupScreenName = "LCD panel InvMan Setup";
        readonly string mineralsScreenName = "LCD panel Minerals";
        readonly string componentsScreen1Name = "LCD panel Components 1";
        readonly string componentsScreen2Name = "LCD panel Components 2";
        readonly string gasInfoScreenName = "LCD panel Gas Info";
        readonly string projectionInfoScreenName1 = "LCD panel Projection Info 1";
        readonly string projectionInfoScreenName2 = "LCD panel Projection Info 2";
        readonly string projectorName = "Projector Ship Builder";

        readonly int yieldPointsPerRefinery = 6;

        //END OF USER SETTING

        //Finals (Readonly's)
        readonly int linesPerScreen = 17;

        readonly int mineralNameSpace = 12;
        readonly int mineralIngotSpace = 9;
        readonly int mineralOreSpace = 9;

        readonly int componentNameSpace = 31;
        readonly int componentProsSpace = 7;
        readonly int componentAmountSpace = 11;

        readonly double smallOxygenTankMaxCapaxity = 50000;
        readonly double largeOxygenTankMaxCapaxity = 100000;
        readonly double smallHydrogenTankMaxCapaxity = 80000;
        readonly double largeHydrogenTankMaxCapaxity = 2500000;

        //Variables
        string outTextMinerals;
        string outTextComponents;
        string outTextGasInfo;
        string outTextProjectiorInfo;

        IMyTextPanel setupScreen;
        IMyTextPanel mineralsScreen;
        IMyTextPanel gasInfoScreen;
        List<IMyTextPanel> componentsScreens;
        List<IMyTextPanel> projectorInfoScreens;

        List<Component> components;
        List<Mineral> minerals;
        List<Block> blocks;

        IMyProjector projector;
        List<IMyAssembler> assemblers;

        float iceAmount;
        double oxStor, oxCap, hyStor, hyCap, naStor, naCap;

        public Program()
        {
            InitializeVariables();
            SetupData();

            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public void Save()
        {
        }

        public void Main(string argument, UpdateType updateSource)
        {
            //Clears the textfields that represent the outputs
            ClearTexts();

            //Gets the fullAmount float from the setupScreen
            LoadFullAmounts();

            //Moves items that should be stored in the main storage there
            SortInventories();

            //Counts items and gasses
            CountStock();

            //Makes assemblers build what they should
            UpdateAssemblers();

            //Projector Info

            //Write texts
            WriteText();



            UpdateScreens();
        }

        public class Mineral
        {
            private string name;
            private string ingotId;
            private float ingotAmount;
            private string oreId;
            private float oreAmount;
            private float fullAmount;
            private float refineFactor;

            public Mineral(string name, string ingotId, float ingotAmount, string oreId, float oreAmount, float fullAmount, float refineFactor)
            {
                this.name = name;
                this.ingotId = ingotId;
                this.ingotAmount = ingotAmount;
                this.oreId = oreId;
                this.oreAmount = oreAmount;
                this.fullAmount = fullAmount;
                this.refineFactor = refineFactor;
            }

            public Mineral(string name, float refineFactor)
            {
                this.name = name;
                this.ingotId = "MyObjectBuilder_Ingot/" + name;
                this.ingotAmount = 0;
                this.oreId = "MyObjectBuilder_Ore/" + name;
                this.oreAmount = 0;
                this.fullAmount = 1;
                this.refineFactor = refineFactor;
            }

            public Mineral(): this("unknown","unknown",0,"unknown",0,1,1)
            {
            }

            public void AddIngot(float value)
            {
                ingotAmount += value;
            }

            public void AddOre(float value)
            {
                oreAmount += value;
            }

            public void ResetAmount()
            {
                oreAmount = 0;
                ingotAmount = 0;
            }
            
            

            public string Name { get { return this.name; } }
            public string IngotId { get { return this.ingotId;  } }
            public float IngotAmount { get { return this.ingotAmount; } set { this.ingotAmount = value; } }
            public string OreId { get { return this.oreId; } }
            public float OreAmount { get { return this.oreAmount; } set { this.oreAmount = value; } }
            public float FullAmount { get { return this.fullAmount; } set { this.fullAmount = value; } }
            public float RefineFactor { get { return this.refineFactor; } }
        }


        public class Component
        {
            private string name;
            private string id;
            private float amount;
            private float fullAmount;
            private MyDefinitionId blueprint;
            private List<KeyValuePair<Mineral,float>> neededMinerals;

            public Component(string name, string id, float amount, float fullAmount, MyDefinitionId blueprint, List<KeyValuePair<Mineral,float>> neededMinerals)
            {
                this.name = name;
                this.id = id;
                this.amount = amount;
                this.fullAmount = fullAmount;
                this.blueprint = blueprint;
                this.neededMinerals = neededMinerals;
            }

            public Component(string name, string id, string blueprintStringEnd, List<KeyValuePair<Mineral,float>> neededMinerals)
            {
                this.name = name;
                this.id = id;
                this.amount = 0;
                this.fullAmount = 1;
                this.blueprint = MyDefinitionId.Parse("MyObjectBuilder_BlueprintDefinition/" + blueprintStringEnd);
                this.neededMinerals = neededMinerals;
            }

            public void addAmount(float value)
            {
                this.amount += value;
            }

            public string Name { get { return this.name; } }
            public string Id { get { return this.id; } }
            public float Amount { get { return this.amount; } set { this.amount = value; } }
            public float FullAmount { get { return this.fullAmount; } set { this.fullAmount = value; } }
            public MyDefinitionId Blueprint { get { return this.blueprint; } }
            public List<KeyValuePair<Mineral, float>> NeededMinerals { get { return this.neededMinerals; } }
        }

        public class Block
        {
            //TODO
        }

        private void SetupData()
        {
            SetupMinerals();
            SetupComponents();
            SetupBlocks();
        }

        private void SetupMinerals()
        {
            minerals.Add(new Mineral("Cobalt",0.3f));
            minerals.Add(new Mineral("Gold", 0.01f));
            minerals.Add(new Mineral("Iron", 0.7f));
            minerals.Add(new Mineral("Magnesium", 0.007f));
            minerals.Add(new Mineral("Nickel", 0.4f));
            minerals.Add(new Mineral("Platinum", 0.005f));
            minerals.Add(new Mineral("Silicon", 0.7f));
            minerals.Add(new Mineral("Silver", 0.01f));
            minerals.Add(new Mineral("Stone", 0.9f));
            minerals.Add(new Mineral("Uranium", 0.007f));
        }

        private void SetupComponents()
        {
            components.Add(new Component("200mm Missile Container", "MyObjectBuilder_AmmoMagazine/Missile200mm", "Missile200mm", NeededMineralsFromString("Iron;18.33;Nickel;2.33;Silicon;0.07;Uranium;0.03;Platinum;0.01;Magnesium;0.4")));
            components.Add(new Component("25x184mm NATO Ammo Container", "MyObjectBuilder_AmmoMagazine/NATO_25x184mm", "NATO_25x184mmMagazine", NeededMineralsFromString("Iron;13.33;Nickel;1.67;Magnesium;1")));
            components.Add(new Component("5p56x45mm NATO Magazine", "MyObjectBuilder_AmmoMagazine/NATO_5p56x45mm", "NATO_5p56x45mmMagazine", NeededMineralsFromString("Iron;0.27;Nickel;0.07;Magnesium;0.05")));
            components.Add(new Component("Bulletproof Glass", "MyObjectBuilder_Component/BulletproofGlass", "BulletproofGlass", NeededMineralsFromString("Silicon;5")));
            components.Add(new Component("Canvas", "MyObjectBuilder_Component/Canvas", "Canvas", NeededMineralsFromString("Silicon;11.67;Iron;0.67")));
            components.Add(new Component("Computer", "MyObjectBuilder_Component/Computer", "ComputerComponent", NeededMineralsFromString("Iron;0.17;Silicon;0.07")));
            components.Add(new Component("Construction Component", "MyObjectBuilder_Component/Construction", "ConstructionComponent", NeededMineralsFromString("Iron;3.33")));
            components.Add(new Component("Detector Components", "MyObjectBuilder_Component/Detector", "DetectorComponent", NeededMineralsFromString("Iron;1.67;Nickel;5")));
            components.Add(new Component("Display", "MyObjectBuilder_Component/Display", "Display", NeededMineralsFromString("Iron;0.33;Silicon;1.67")));
            components.Add(new Component("Explosives", "MyObjectBuilder_Component/Explosives", "ExplosivesComponent", NeededMineralsFromString("Silicon;0.17;Magnesium;0.67")));
            components.Add(new Component("Girder", "MyObjectBuilder_Component/Girder", "GirderComponent", NeededMineralsFromString("Iron;2.33")));
            components.Add(new Component("Gravity Generator Components", "MyObjectBuilder_Component/GravityGenerator", "GravityGeneratorComponent", NeededMineralsFromString("Silver;1.67;Gold;3.33;Cobalt;73.33;Iron;200")));
            components.Add(new Component("Interior Plate", "MyObjectBuilder_Component/InteriorPlate", "InteriorPlate", NeededMineralsFromString("Iron;1.17")));
            components.Add(new Component("Large Tube", "MyObjectBuilder_Component/LargeTube", "LargeTube", NeededMineralsFromString("Iron;10")));
            components.Add(new Component("Medical Components", "MyObjectBuilder_Component/Medical", "MedicalComponent", NeededMineralsFromString("Iron;20;Nickel;23.33;Silver;6.67")));
            components.Add(new Component("Metal Grid", "MyObjectBuilder_Component/MetalGrid", "MetalGrid", NeededMineralsFromString("Iron;4;Nickel;1.67;Cobalt;1")));
            components.Add(new Component("Motor", "MyObjectBuilder_Component/Motor", "MotorComponent", NeededMineralsFromString("Iron;6.67;Nickel;1.67")));
            components.Add(new Component("Power Cell", "MyObjectBuilder_Component/PowerCell", "PowerCell", NeededMineralsFromString("Iron;3.33;Silicon;0.33;Nickel;0.67")));
            components.Add(new Component("Radio-Communication Components", "MyObjectBuilder_Component/RadioCommunication", "RadioCommunicationComponent", NeededMineralsFromString("Iron;2.67;Silicon;0.33")));
            components.Add(new Component("Reactor Components", "MyObjectBuilder_Component/Reactor", "ReactorComponent", NeededMineralsFromString("Iron;5;Gravel;6.67;Silver;1.67")));
            components.Add(new Component("Shield Component", "MyObjectBuilder_Component/Shield", "ShieldComponent", NeededMineralsFromString("Cobalt;10;Silver;0.83;Gold;1.67;Platinum;0.67")));
            components.Add(new Component("Small Tube", "MyObjectBuilder_Component/SmallTube", "SmallTube", NeededMineralsFromString("Iron;1.67")));
            components.Add(new Component("Solar Cell", "MyObjectBuilder_Component/SolarCell", "SolarCell", NeededMineralsFromString("Nickel;3.33;Silicon;2.67")));
            components.Add(new Component("Steel Plate", "MyObjectBuilder_Component/SteelPlate", "SteelPlate", NeededMineralsFromString("Iron;7")));
            components.Add(new Component("Superconductor Conduits", "MyObjectBuilder_Component/Superconductor", "Superconductor", NeededMineralsFromString("Iron;3.33;Gold;0.67")));
            components.Add(new Component("Thruster Components", "MyObjectBuilder_Component/Thrust", "ThrustComponent", NeededMineralsFromString("Iron;10;Cobalt;3.33;Gold;0.33;Platinum;0.13")));
        }

        private void SetupBlocks()
        {
            //TODO
        }

        private Mineral FindMineralByName(string name)
        {
            foreach (Mineral m in minerals)
            {
                if (m.Name == name)
                    return m;
            }
            return null;
        }

        private Mineral FindMineralByIngotId(string id)
        {
            foreach (Mineral m in minerals)
            {
                if (m.IngotId == id)
                    return m;
            }
            return null;
        }

        private Mineral FindMineralByOreId(string id)
        {
            foreach (Mineral m in minerals)
            {
                if (m.OreId == id)
                    return m;
            }
            return null;
        }

        private List<KeyValuePair<Mineral, float>> NeededMineralsFromString(string s)
        {
            List<KeyValuePair<Mineral, float>> list = new List<KeyValuePair<Mineral, float>>();

            string[] args = s.Split(';');
            for (int i = 0; i < args.Length; i += 2)
            {
                float y;
                float.TryParse(args[i + 1], out y);
                list.Add(new KeyValuePair<Mineral, float>(FindMineralByName(args[i]), y));
            }

            return list;
        }

        private Component FindComponentByName(string name)
        {
            foreach (Component c in components)
            {
                if (c.Name == name)
                    return c;
            }
            return null;
        }

        private Component FindComponentById(string id)
        {
            foreach (Component c in components)
            {
                if (c.Id == id)
                    return c;
            }
            return null;
        }

        private void WriteText()
        {
            //Gas Info
            outTextGasInfo += "ICE:" + "\n" + (iceAmount/1000).ToString("n1") + "kg" + "\n" + "\n";
            outTextGasInfo += "OXYGEN: (" + ((oxStor / oxCap) * 100).ToString("n1") + "%)" + "\n" +
                     oxStor.ToString("n0") + " / " + oxCap.ToString("n0") + " L" + "\n" + "\n" +
                     "HYDROGEN: (" + ((hyStor / hyCap) * 100).ToString("n1") + "%)" + "\n" +
                     hyStor.ToString("n0") + " / " + hyCap.ToString("n0") + " L" + "\n" + "\n";
            if (naCap > 0)
            {
                outTextGasInfo += "UNKNOWN: (" + ((naStor / naCap) * 100).ToString("n1") + "%)" + "\n" +
                                 naStor.ToString("n0") + " / " + naCap.ToString("n0") + " L" + "\n" + "\n" +
                                 "TANK MAX CAPACITY MUST " + "\n" + "BE UPDATED IN SCRIPT!!!";
            }


            //Mineral
            foreach (Mineral m in minerals)
            {
                float factor = m.RefineFactor * (float)Math.Pow(1.09, yieldPointsPerRefinery);
                if (factor > 1)
                    factor = 1;
                outTextMinerals += (m.Name + ":").PadRight(mineralNameSpace) + ( ( (m.IngotAmount / m.FullAmount) * 100).ToString("n1") + "%").PadLeft(mineralIngotSpace) + ("+" + (((m.OreAmount * factor) / m.FullAmount) * 100).ToString("n1") + "%").PadLeft(mineralOreSpace) + "\n";
            }


            //Component
            foreach (Component c in components)
            {
                outTextComponents += (c.Name + ":").PadRight(componentNameSpace) + (( (c.Amount / c.FullAmount) * 100 ).ToString("n1") + "%").PadLeft(componentProsSpace) + ((c.Amount).ToString("n0")).PadLeft(componentAmountSpace) + "\n";
            }


            //Projection
            outTextProjectiorInfo = "Not supported right now...";
        }

        private void UpdateAssemblers()
        {
            bool empty = true;
            
            foreach (IMyAssembler a in assemblers)
            {
                if (!a.IsQueueEmpty)
                {
                    empty = false;
                }
            }
            
            if (empty)
            {
                foreach (Component c in components)
                {
                    if (c.FullAmount > c.Amount)
                    {
                        try
                        {
                            foreach (IMyAssembler asmblr in assemblers)
                            {
                                asmblr.AddQueueItem(c.Blueprint, (VRage.MyFixedPoint)((int)(((c.FullAmount - c.Amount) / assemblers.Count) + 1)));
                            }
                        }
                        catch (Exception e)
                        {

                        }
                    }
                }
            }
        }

        private void CountStock()
        {
            oxStor = 0; oxCap = 0; hyStor = 0; hyCap = 0; naStor = 0; naCap = 0;
            foreach (Mineral m in minerals)
            {
                m.ResetAmount();
            }
            foreach (Component c in components)
            {
                c.Amount = 0;
            }
            iceAmount = 0;

            var blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName("", blocks);
            foreach (var b in blocks)
            {
                if (b.HasInventory)
                {
                    for (int i = 0; i < b.InventoryCount; i++)
                    {
                        IMyInventory inv = b.GetInventory(i);
                        if (inv.GetItems().Count > 0)
                        {
                            for (int j = 0; inv.GetItems().Count > j; j++)
                            {
                                if ("" + inv.GetItems()[j].GetDefinitionId() == "MyObjectBuilder_Ore/Ice")
                                {
                                    float y = 0;
                                    float.TryParse("" + inv.GetItems()[j].Amount, out y);
                                    iceAmount += y;
                                    continue;
                                }
                                foreach (Mineral m in minerals)
                                {
                                    if ("" + inv.GetItems()[j].GetDefinitionId() == m.IngotId)
                                    {
                                        float y = 0;
                                        float.TryParse("" + inv.GetItems()[j].Amount, out y);
                                        m.IngotAmount += y;
                                        break;
                                    }
                                    if ("" + inv.GetItems()[j].GetDefinitionId() == m.OreId)
                                    {
                                        float y = 0;
                                        float.TryParse("" + inv.GetItems()[j].Amount, out y);
                                        m.OreAmount += y;
                                        break;
                                    }
                                }
                                foreach (Component c in components)
                                {
                                    if ("" + inv.GetItems()[j].GetDefinitionId() == c.Id)
                                    {
                                        float y = 0;
                                        float.TryParse("" + inv.GetItems()[j].Amount, out y);
                                        c.Amount += y;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                if (b.GetType().Name == "MyGasTank")
                {
                    IMyGasTank t = (IMyGasTank)b;
                    if (t.Capacity == smallOxygenTankMaxCapaxity || t.Capacity == largeOxygenTankMaxCapaxity)
                    {
                        oxStor += t.FilledRatio * t.Capacity;
                        oxCap += t.Capacity;
                    }
                    else if (t.Capacity == smallHydrogenTankMaxCapaxity || t.Capacity == largeHydrogenTankMaxCapaxity)
                    {
                        hyStor += t.FilledRatio * t.Capacity;
                        hyCap += t.Capacity;
                    }
                    else
                    {
                        naStor += t.FilledRatio * t.Capacity;
                        naCap += t.Capacity;
                    }
                }
            }
        }

        private void SortInventories()
        {
            var blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName("", blocks);
            List<IMyInventory> storage = new List<IMyInventory>();
            assemblers.Clear();
            foreach (var b in blocks)
            {
                if (b.GetType().Name == "MyAssembler")
                {
                    if (b.CustomData.Contains(gridCallingCard)) { 
                        assemblers.Add((IMyAssembler)b);
                    }
                }
                if (b.HasInventory && b.CustomData.Contains("MainStorage"))
                {
                    storage.Add(b.GetInventory(0));
                }
            }
            int currentStorage = 0;
            foreach (var b in blocks)
            {
                IMyInventory inv = null;
                if (b.GetType().Name == "MyAssembler" || b.GetType().Name == "MyRefinery")
                {
                    inv = b.GetInventory(1);
                }
                else if (b.HasInventory) {
                    if (!b.CustomData.Contains("MainStorage"))
                    {
                        inv = b.GetInventory(0);
                    }
                }
                if (inv != null)
                { 
                    int numItems = inv.GetItems().Count;
                    for (int i = numItems - 1; i >= 0; i--)
                    {
                        if (FindMineralByIngotId("" + inv.GetItems()[i].GetDefinitionId()) == null && FindMineralByOreId("" + inv.GetItems()[i].GetDefinitionId()) == null && FindComponentById("" + inv.GetItems()[i].GetDefinitionId()) == null)
                        {
                            continue;
                        }
                        if ("" + inv.GetItems()[i].GetDefinitionId() == "MyObjectBuilder_Ingot/Uranium")
                        {
                            continue;
                        }
                        bool found = false;
                        while (!found)
                        {
                            if (currentStorage > storage.Count - 1)
                            {
                                break;
                            }
                            if (!storage[currentStorage].IsFull)
                            {
                                found = true;
                            }
                            else
                            {
                                currentStorage++;
                            }
                        }
                        if (found)
                        {
                            inv.TransferItemTo(storage[currentStorage], i, null, true, null);
                        }
                    }
                }
            }
        }

        private void LoadFullAmounts()
        {
            string input = setupScreen.GetPublicText();
            string[] lines = input.Split('\n');

            int i = 0;
            for (; i < lines.Length; i++)
            {
                if (lines[i] == "")
                {
                    break;
                }
                string[] args = lines[i].Split(';');
                if (args.Length >= 2)
                {
                    float y;
                    float.TryParse(args[1], out y);
                    FindMineralByName(args[0]).FullAmount = y;
                }
            }
            i++;
            for (; i < lines.Length; i++)
            {
                string[] args = lines[i].Split(';');
                if (args.Length >= 2)
                {
                    float y;
                    float.TryParse(args[1], out y);
                    FindComponentByName(args[0]).FullAmount = y;
                }
            }
        }

        private void InitializeVariables()
        {
            setupScreen = GridTerminalSystem.GetBlockWithName(setupScreenName) as IMyTextPanel;
            mineralsScreen = GridTerminalSystem.GetBlockWithName(mineralsScreenName) as IMyTextPanel;
            gasInfoScreen = GridTerminalSystem.GetBlockWithName(gasInfoScreenName) as IMyTextPanel;

            componentsScreens = new List<IMyTextPanel>();
            componentsScreens.Add(GridTerminalSystem.GetBlockWithName(componentsScreen1Name) as IMyTextPanel);
            componentsScreens.Add(GridTerminalSystem.GetBlockWithName(componentsScreen2Name) as IMyTextPanel);

            projectorInfoScreens = new List<IMyTextPanel>();
            projectorInfoScreens.Add(GridTerminalSystem.GetBlockWithName(projectionInfoScreenName1) as IMyTextPanel);
            projectorInfoScreens.Add(GridTerminalSystem.GetBlockWithName(projectionInfoScreenName2) as IMyTextPanel);

            components = new List<Component>();
            minerals = new List<Mineral>();
            blocks = new List<Block>();

            projector = GridTerminalSystem.GetBlockWithName(projectorName) as IMyProjector;
            assemblers = new List<IMyAssembler>();
        }

        private void ClearTexts()
        {
            outTextComponents = outTextGasInfo = outTextMinerals = outTextProjectiorInfo = "";
        }

        private void UpdateScreens()
        {
            mineralsScreen.WritePublicText(outTextMinerals);
            gasInfoScreen.WritePublicText(outTextGasInfo);

            string[] lines = outTextComponents.Split('\n');
            string text1 = "";
            string text2 = "";
            for (int i = 0; i < lines.Length; i++)
            {
                if (i < linesPerScreen)
                {
                    text1 += lines[i] + "\n";
                }
                else
                {
                    text2 += lines[i] + "\n";
                }
            }
            componentsScreens[0].WritePublicText(text1);
            componentsScreens[1].WritePublicText(text2);

            lines = outTextProjectiorInfo.Split('\n');
            text1 = "";
            text2 = "";
            for (int i = 0; i < lines.Length; i++)
            {
                if (i < linesPerScreen)
                {
                    text1 += lines[i] + "\n";
                }
                else
                {
                    text2 += lines[i] + "\n";
                }
            }
            projectorInfoScreens[0].WritePublicText(text1);
            projectorInfoScreens[1].WritePublicText(text2);
        }


        //Useful Text
        /*
         
Cobalt;1000000
Gold;10000
Iron;100000000
Magnesium;10000
Nickel;1000000
Platinum;50000
Silicon;1000000
Silver;100000
Stone;500000
Uranium;10000

Bulletproof Glass;1000
Canvas;1000
Computer;1000
Construction Component;1000
Detector Components;1000
Display;1000
Explosives;100
Girder;1000
Gravity Generator Components;100
Interior Plate;20000
Large Tube;1000
Medical Components;100
Metal Grid;1000
200mm Missile Container;10
Motor;1000
25x184mm NATO Ammo Container;10
5p56x45mm NATO Magazine;10
Power Cell;1000
Radio-Communication Components;1000
Reactor Components;1000
Shield Component;10
Small Tube;1000
Solar Cell;1000
Steel Plate;100000
Superconductor Conduits;1000
Thruster Components;1000

        0: Armor blocks ; 389
        1: Interior Wall ; 1
        2: Parachute Hatch ; 1
        3: Control Stations ; 1
        4: Flight Seat ; 1
        5: Cockpit ; 1
        6: Remote Control ; 1
        7: Passenger Seat ; 1
        8: Battery ; 1
        9: Solar Panel ; 1
        10: Small Reactor ; 1
        11: Large Reactor ; 1
        12: Medical Room ; 1
        13: Cryo Chamber ; 1
        14: Hydrogen Thrusters ; 1
        15: Large Hydrogen Thruster ; 1
        16: Large Atmospheric Thruster ; 1
        17: Atmospheric Thrusters ; 1
        18: Ion Thrusters ; 1
        19: Large Ion Thruster ; 1
        20: Wheel 1x1 ; 1
        21: Wheel 3x3 ; 1
        22: Wheel 5x5 ; 1
        23: Wheel Suspension 1x1 Right ; 1
        24: Wheel Suspension 3x3 Right ; 1
        25: Wheel Suspension 5x5 Left ; 1
        26: Gyroscope ; 1
        27: Ore Detector ; 1
        28: Antenna ; 1
        29: Beacon ; 1
        30: Laser Antenna ; 1
        31: Refinery ; 1
        32: Assembler ; 1
        33: Speed Module ; 1
        34: Yield Module ; 1
        35: Power Efficiency Module ; 1
        36: Arc furnace ; 1
        37: Projector ; 1
        38: Small Cargo Container ; 1
        39: Large Cargo Container ; 1
        40: Interior Turret ; 1
        41: Gatling Turret ; 1
        42: Missile Turret ; 1
        43: Rocket Launcher ; 1
        44: Warhead ; 1
        45: Welder ; 1
        46: Grinder ; 1
        47: Drill ; 1
        48: Sliding Door ; 1
        49: Door ; 1
        50: Airtight Hangar Door ; 1
        51: Blast doors ; 2
        52: Blast door edge ; 1
        53: Blast door corner inverted ; 1
        54: Blast door corner ; 1
        55: Cover Walls ; 1
        56: Half Cover Wall ; 1
        57: Steel Catwalks ; 1
        58: Steel Catwalk Plate ; 1
        59: Steel Catwalk Corner ; 1
        60: Steel Catwalk Two Sides ; 1
        61: Stairs ; 1
        62: Ramp ; 1
        63: Interior Pillar ; 1
        64: Vertical Window ; 1
        65: Diagonal Window ; 1
        66: Passage ; 1
        67: Decoy ; 1
        68: Interior Light ; 1
        69: Spotlight ; 1
        70: Corner Light ; 1
        71: Corner Light - Double ; 1
        72: Programmable block ; 1
        73: Control Panel ; 1
        74: Camera ; 1
        75: Sound Block ; 1
        76: Sensor ; 1
        77: Timer Block ; 1
        78: Button Panel ; 1
        79: LCD Panel ; 1
        80: Text panel ; 1
        81: Wide LCD panel ; 1
        82: Corner LCD Top ; 1
        83: Corner LCD Flat Bottom ; 1
        84: Corner LCD Flat Top ; 1
        85: Corner LCD Bottom ; 1
        86: Conveyor Tube ; 1
        87: Curved Conveyor Tube ; 1
        88: Conveyor Junction ; 1
        89: Connector ; 1
        90: Collector ; 1
        91: Conveyor Sorter ; 1
        92: Landing Gear ; 1
        93: Piston ; 1
        94: Advanced Rotor ; 1
        95: Rotor ; 1
        96: Merge Block ; 1
        97: Air Vent ; 1
        98: O2/H2 Generator ; 1
        99: Oxygen Tank ; 1
        100: Hydrogen Tank ; 1
        101: Oxygen Farm ; 1
        102: Gravity Generator ; 1
        103: Spherical Gravity Generator ; 1
        104: Artificial Mass ; 1
        105: Jump Drive ; 1
        106: Window 1x1 Flat ; 1
        107: Window 1x1 Flat Inv. ; 1
        108: Window 1x2 Flat ; 1
        109: Window 1x2 Flat Inv. ; 1
        110: Window 2x3 Flat ; 1
        111: Window 2x3 Flat Inv. ; 1
        112: Window 3x3 Flat ; 1
        113: Window 3x3 Flat Inv. ; 1
        114: Window 1x1 Slope ; 1
        115: Window 1x2 Slope ; 1
        116: Window 1x1 Face ; 1
        117: Window 1x1 Inv. ; 1
        118: Window 1x1 Side ; 1
        119: Window 1x1 Side Inv ; 1
        120: Window 1x2 Face ; 1
        121: Window 1x2 Inv. ; 1
        122: Window 1x2 Side Left ; 1
        123: Window 1x2 Side Left Inv ; 1
        124: Window 1x2 Side Right ; 1
        125: Window 1x2 Side Right Inv ; 1
        126: BuildAndRepairSystem ; 1
        127: Small Shield Generator ; 1
        128: Shield Capacitor ; 1
        129: Shield Flux Coil ; 1
        130: Large Shield Generator ; 1
        131: Heavy Mining Drill ; 1

        */
    }
}
