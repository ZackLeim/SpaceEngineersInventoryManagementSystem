using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;

namespace Mark_4
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

        readonly double oxygenTankMaxCapaxity = 100000;
        readonly double hydrogenTankMaxCapaxity = 2500000;

        //Variables
        System.Text.StringBuilder outTextMinerals;
        System.Text.StringBuilder outTextComponents;
        System.Text.StringBuilder outTextGasInfo;
        System.Text.StringBuilder outTextProjectiorInfo;

        IMyTextPanel setupScreen;
        IMyTextPanel mineralsScreen;
        IMyTextPanel gasInfoScreen;
        List<IMyTextPanel> componentsScreens;
        List<IMyTextPanel> projectorInfoScreens;

        IDictionary<string, Mineral> mineralDic;
        IDictionary<string, Component> componentDic;
        List<Mineral> mineralList;
        List<Component> componentList;

        IMyProjector projector;
        List<IMyAssembler> assemblers;

        float iceAmount;
        double oxStor, oxCap, hyStor, hyCap, naStor, naCap;

        public Program()
        {
            InitializeVariables();
            SetupData();

            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void Save()
        {
        }

        public void Main(string argument, UpdateType updateSource)
        {
            ClearStringBuilders();

            LoadFullAmounts();

            SortAndCount();

            UpdateAssemblers();

            CalculateProjection();

            WriteText();

            ScreenDump();
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

            public Mineral() : this("unknown", "unknown", 0, "unknown", 0, 1, 1)
            {
            }

            public void ResetAmount()
            {
                oreAmount = 0;
                ingotAmount = 0;
            }



            public string Name { get { return this.name; } }
            public string IngotId { get { return this.ingotId; } }
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
            private List<KeyValuePair<Mineral, float>> neededMinerals;

            public Component(string name, string id, float amount, float fullAmount, MyDefinitionId blueprint, List<KeyValuePair<Mineral, float>> neededMinerals)
            {
                this.name = name;
                this.id = id;
                this.amount = amount;
                this.fullAmount = fullAmount;
                this.blueprint = blueprint;
                this.neededMinerals = neededMinerals;
            }

            public Component(string name, string id, string blueprintStringEnd, List<KeyValuePair<Mineral, float>> neededMinerals)
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

            componentDic = new Dictionary<string, Component>();
            mineralDic = new Dictionary<string, Mineral>();

            projector = GridTerminalSystem.GetBlockWithName(projectorName) as IMyProjector;
            assemblers = new List<IMyAssembler>();

            outTextMinerals = new System.Text.StringBuilder();
            outTextComponents = new System.Text.StringBuilder();
            outTextGasInfo = new System.Text.StringBuilder();
            outTextProjectiorInfo = new System.Text.StringBuilder();

            mineralList = new List<Mineral>();
            componentList = new List<Component>();
        }

        private void SetupData()
        {
            //Minerals

            Mineral m;
            m = new Mineral("Cobalt", 0.3f);
            mineralDic.Add(m.Name, m);
            mineralDic.Add(m.IngotId, m);
            mineralDic.Add(m.OreId, m);
            mineralList.Add(m);
            m = new Mineral("Gold", 0.01f);
            mineralDic.Add(m.Name, m);
            mineralDic.Add(m.IngotId, m);
            mineralDic.Add(m.OreId, m);
            mineralList.Add(m);
            m = new Mineral("Iron", 0.7f);
            mineralDic.Add(m.Name, m);
            mineralDic.Add(m.IngotId, m);
            mineralDic.Add(m.OreId, m);
            mineralList.Add(m);
            m = new Mineral("Magnesium", 0.007f);
            mineralDic.Add(m.Name, m);
            mineralDic.Add(m.IngotId, m);
            mineralDic.Add(m.OreId, m);
            mineralList.Add(m);
            m = new Mineral("Nickel", 0.4f);
            mineralDic.Add(m.Name, m);
            mineralDic.Add(m.IngotId, m);
            mineralDic.Add(m.OreId, m);
            mineralList.Add(m);
            m = new Mineral("Platinum", 0.005f);
            mineralDic.Add(m.Name, m);
            mineralDic.Add(m.IngotId, m);
            mineralDic.Add(m.OreId, m);
            mineralList.Add(m);
            m = new Mineral("Silicon", 0.7f);
            mineralDic.Add(m.Name, m);
            mineralDic.Add(m.IngotId, m);
            mineralDic.Add(m.OreId, m);
            mineralList.Add(m);
            m = new Mineral("Silver", 0.01f);
            mineralDic.Add(m.Name, m);
            mineralDic.Add(m.IngotId, m);
            mineralDic.Add(m.OreId, m);
            mineralList.Add(m);
            m = new Mineral("Stone", 0.9f);
            mineralDic.Add(m.Name, m);
            mineralDic.Add(m.IngotId, m);
            mineralDic.Add(m.OreId, m);
            mineralList.Add(m);
            m = new Mineral("Uranium", 0.007f);
            mineralDic.Add(m.Name, m);
            mineralDic.Add(m.IngotId, m);
            mineralDic.Add(m.OreId, m);
            mineralList.Add(m);

            //Components
            Component c;
            c = new Component("200mm Missile Container", "MyObjectBuilder_AmmoMagazine/Missile200mm", "Missile200mm", NeededMineralsFromString("Iron;18.33;Nickel;2.33;Silicon;0.07;Uranium;0.03;Platinum;0.01;Magnesium;0.4"));
            componentDic.Add(c.Name, c);
            componentDic.Add(c.Id, c);
            componentList.Add(c);
            c = new Component("25x184mm NATO Ammo Container", "MyObjectBuilder_AmmoMagazine/NATO_25x184mm", "NATO_25x184mmMagazine", NeededMineralsFromString("Iron;13.33;Nickel;1.67;Magnesium;1"));
            componentDic.Add(c.Name, c);
            componentDic.Add(c.Id, c);
            componentList.Add(c);
            c = new Component("5p56x45mm NATO Magazine", "MyObjectBuilder_AmmoMagazine/NATO_5p56x45mm", "NATO_5p56x45mmMagazine", NeededMineralsFromString("Iron;0.27;Nickel;0.07;Magnesium;0.05"));
            componentDic.Add(c.Name, c);
            componentDic.Add(c.Id, c);
            componentList.Add(c);
            c = new Component("Bulletproof Glass", "MyObjectBuilder_Component/BulletproofGlass", "BulletproofGlass", NeededMineralsFromString("Silicon;5"));
            componentDic.Add(c.Name, c);
            componentDic.Add(c.Id, c);
            componentList.Add(c);
            c = new Component("Canvas", "MyObjectBuilder_Component/Canvas", "Canvas", NeededMineralsFromString("Silicon;11.67;Iron;0.67"));
            componentDic.Add(c.Name, c);
            componentDic.Add(c.Id, c);
            componentList.Add(c);
            c = new Component("Computer", "MyObjectBuilder_Component/Computer", "ComputerComponent", NeededMineralsFromString("Iron;0.17;Silicon;0.07"));
            componentDic.Add(c.Name, c);
            componentDic.Add(c.Id, c);
            componentList.Add(c);
            c = new Component("Construction Component", "MyObjectBuilder_Component/Construction", "ConstructionComponent", NeededMineralsFromString("Iron;3.33"));
            componentDic.Add(c.Name, c);
            componentDic.Add(c.Id, c);
            componentList.Add(c);
            c = new Component("Detector Components", "MyObjectBuilder_Component/Detector", "DetectorComponent", NeededMineralsFromString("Iron;1.67;Nickel;5"));
            componentDic.Add(c.Name, c);
            componentDic.Add(c.Id, c);
            componentList.Add(c);
            c = new Component("Display", "MyObjectBuilder_Component/Display", "Display", NeededMineralsFromString("Iron;0.33;Silicon;1.67"));
            componentDic.Add(c.Name, c);
            componentDic.Add(c.Id, c);
            componentList.Add(c);
            c = new Component("Explosives", "MyObjectBuilder_Component/Explosives", "ExplosivesComponent", NeededMineralsFromString("Silicon;0.17;Magnesium;0.67"));
            componentDic.Add(c.Name, c);
            componentDic.Add(c.Id, c);
            componentList.Add(c);
            c = new Component("Girder", "MyObjectBuilder_Component/Girder", "GirderComponent", NeededMineralsFromString("Iron;2.33"));
            componentDic.Add(c.Name, c);
            componentDic.Add(c.Id, c);
            componentList.Add(c);
            c = new Component("Gravity Generator Components", "MyObjectBuilder_Component/GravityGenerator", "GravityGeneratorComponent", NeededMineralsFromString("Silver;1.67;Gold;3.33;Cobalt;73.33;Iron;200"));
            componentDic.Add(c.Name, c);
            componentDic.Add(c.Id, c);
            componentList.Add(c);
            c = new Component("Interior Plate", "MyObjectBuilder_Component/InteriorPlate", "InteriorPlate", NeededMineralsFromString("Iron;1.17"));
            componentDic.Add(c.Name, c);
            componentDic.Add(c.Id, c);
            componentList.Add(c);
            c = new Component("Large Tube", "MyObjectBuilder_Component/LargeTube", "LargeTube", NeededMineralsFromString("Iron;10"));
            componentDic.Add(c.Name, c);
            componentDic.Add(c.Id, c);
            componentList.Add(c);
            c = new Component("Medical Components", "MyObjectBuilder_Component/Medical", "MedicalComponent", NeededMineralsFromString("Iron;20;Nickel;23.33;Silver;6.67"));
            componentDic.Add(c.Name, c);
            componentDic.Add(c.Id, c);
            componentList.Add(c);
            c = new Component("Metal Grid", "MyObjectBuilder_Component/MetalGrid", "MetalGrid", NeededMineralsFromString("Iron;4;Nickel;1.67;Cobalt;1"));
            componentDic.Add(c.Name, c);
            componentDic.Add(c.Id, c);
            componentList.Add(c);
            c = new Component("Motor", "MyObjectBuilder_Component/Motor", "MotorComponent", NeededMineralsFromString("Iron;6.67;Nickel;1.67"));
            componentDic.Add(c.Name, c);
            componentDic.Add(c.Id, c);
            componentList.Add(c);
            c = new Component("Power Cell", "MyObjectBuilder_Component/PowerCell", "PowerCell", NeededMineralsFromString("Iron;3.33;Silicon;0.33;Nickel;0.67"));
            componentDic.Add(c.Name, c);
            componentDic.Add(c.Id, c);
            componentList.Add(c);
            c = new Component("Radio-Communication Components", "MyObjectBuilder_Component/RadioCommunication", "RadioCommunicationComponent", NeededMineralsFromString("Iron;2.67;Silicon;0.33"));
            componentDic.Add(c.Name, c);
            componentDic.Add(c.Id, c);
            componentList.Add(c);
            c = new Component("Reactor Components", "MyObjectBuilder_Component/Reactor", "ReactorComponent", NeededMineralsFromString("Iron;5;Stone;6.67;Silver;1.67"));
            componentDic.Add(c.Name, c);
            componentDic.Add(c.Id, c);
            componentList.Add(c);
            c = new Component("Shield Component", "MyObjectBuilder_Component/Shield", "ShieldComponent", NeededMineralsFromString("Cobalt;10;Silver;0.83;Gold;1.67;Platinum;0.67"));
            componentDic.Add(c.Name, c);
            componentDic.Add(c.Id, c);
            componentList.Add(c);
            c = new Component("Small Tube", "MyObjectBuilder_Component/SmallTube", "SmallTube", NeededMineralsFromString("Iron;1.67"));
            componentDic.Add(c.Name, c);
            componentDic.Add(c.Id, c);
            componentList.Add(c);
            c = new Component("Solar Cell", "MyObjectBuilder_Component/SolarCell", "SolarCell", NeededMineralsFromString("Nickel;3.33;Silicon;2.67"));
            componentDic.Add(c.Name, c);
            componentDic.Add(c.Id, c);
            componentList.Add(c);
            c = new Component("Steel Plate", "MyObjectBuilder_Component/SteelPlate", "SteelPlate", NeededMineralsFromString("Iron;7"));
            componentDic.Add(c.Name, c);
            componentDic.Add(c.Id, c);
            componentList.Add(c);
            c = new Component("Superconductor Conduits", "MyObjectBuilder_Component/Superconductor", "Superconductor", NeededMineralsFromString("Iron;3.33;Gold;0.67"));
            componentDic.Add(c.Name, c);
            componentDic.Add(c.Id, c);
            componentList.Add(c);
            c = new Component("Thruster Components", "MyObjectBuilder_Component/Thrust", "ThrustComponent", NeededMineralsFromString("Iron;10;Cobalt;3.33;Gold;0.33;Platinum;0.13"));
            componentDic.Add(c.Name, c);
            componentDic.Add(c.Id, c);
            componentList.Add(c);


        }

        private List<KeyValuePair<Mineral, float>> NeededMineralsFromString(string s)
        {
            List<KeyValuePair<Mineral, float>> list = new List<KeyValuePair<Mineral, float>>();

            string[] args = s.Split(';');
            for (int i = 0; i < args.Length; i += 2)
            {
                float y;
                float.TryParse(args[i + 1], out y);
                list.Add(new KeyValuePair<Mineral, float>(mineralDic[args[i]], y));
            }

            return list;
        }

        private void ClearStringBuilders()
        {
            outTextMinerals.Clear();
            outTextComponents.Clear();
            outTextGasInfo.Clear();
            outTextProjectiorInfo.Clear();
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
                    mineralDic[args[0]].FullAmount = y;
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
                    componentDic[args[0]].FullAmount = y;
                }
            }
        }

        private void SortAndCount()
        {
            oxStor = 0; oxCap = 0; hyStor = 0; hyCap = 0; naStor = 0; naCap = 0;
            foreach (Mineral m in mineralList)
            {
                m.ResetAmount();
            }
            foreach (Component c in componentList)
            {
                c.Amount = 0;
            }
            iceAmount = 0;
            assemblers.Clear();

            List<IMyInventory> storage = new List<IMyInventory>();
            List<IMyCargoContainer> cargos = new List<IMyCargoContainer>();
            GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(cargos);
            foreach (IMyCargoContainer car in cargos)
            {
                if (car.CustomData.Contains("MainStorage"))
                {
                    storage.Add(car.GetInventory(0));
                }
            }

            var blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.SearchBlocksOfName("", blocks);
            foreach (var b in blocks)
            {
                if (b.CustomData.Contains(gridCallingCard) && b.HasInventory)
                {
                    if (b.CustomData.Contains("MainStorage"))
                    {
                        continue;
                    }
                    if (b.GetType().Name == "MyAssembler")
                    {
                        assemblers.Add((IMyAssembler)b);
                        SortInventory(b.GetInventory(1), storage);
                        CountInventory(b.GetInventory(0));
                        CountInventory(b.GetInventory(1));
                        continue;
                    }
                    if (b.GetType().Name == "MyRefinery")
                    {
                        SortInventory(b.GetInventory(1), storage);
                        CountInventory(b.GetInventory(0));
                        CountInventory(b.GetInventory(1));
                        continue;
                    }
                    if (b.GetType().Name == "MyGasTank")
                    {
                        IMyGasTank t = (IMyGasTank)b;
                        if (t.Capacity == oxygenTankMaxCapaxity)
                        {
                            oxStor += t.FilledRatio * t.Capacity;
                            oxCap += t.Capacity;
                        }
                        else if (t.Capacity == hydrogenTankMaxCapaxity)
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
                    for (int i = 0; i < b.InventoryCount; i++)
                    {
                        SortInventory(b.GetInventory(i), storage);
                        CountInventory(b.GetInventory(i));
                    }
                }
            }
            foreach (IMyInventory inv in storage)
            {
                CountInventory(inv);
            }
        }

        private void SortInventory(IMyInventory inv, List<IMyInventory> storage)
        {
            int currentStorage = 0;
            int numItems = inv.GetItems().Count;
            for (int i = numItems - 1; i >= 0; i--)
            {
                if (!mineralDic.ContainsKey("" + inv.GetItems()[i].GetDefinitionId()) && !componentDic.ContainsKey("" + inv.GetItems()[i].GetDefinitionId()))
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

        private void CountInventory(IMyInventory inv)
        {
            for (int i = 0; inv.GetItems().Count > i; i++)
            {
                if ("" + inv.GetItems()[i].GetDefinitionId() == "MyObjectBuilder_Ore/Ice")
                {
                    float y = 0;
                    float.TryParse("" + inv.GetItems()[i].Amount, out y);
                    iceAmount += y;
                    continue;
                }   
                if (mineralDic.ContainsKey("" + inv.GetItems()[i].GetDefinitionId()))
                {
                    Mineral m = mineralDic["" + inv.GetItems()[i].GetDefinitionId()];
                    if (("" + inv.GetItems()[i].GetDefinitionId()).Contains("MyObjectBuilder_Ore/"))
                    {
                        float y = 0;
                        float.TryParse("" + inv.GetItems()[i].Amount, out y);
                        m.OreAmount += y;
                        continue;
                    }
                    else
                    {
                        float y = 0;
                        float.TryParse("" + inv.GetItems()[i].Amount, out y);
                        m.IngotAmount += y;
                        continue;
                    }
                }
                if (componentDic.ContainsKey("" + inv.GetItems()[i].GetDefinitionId()))
                {
                    Component c = componentDic["" + inv.GetItems()[i].GetDefinitionId()];
                    float y = 0;
                    float.TryParse("" + inv.GetItems()[i].Amount, out y);
                    c.Amount += y;
                    continue;
                }
            }
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

                foreach (Component c in componentList)
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

        private void CalculateProjection()
        {
            outTextProjectiorInfo.Append("Projection Info: not yet supported!");
        }

        private void WriteText()
        {
            //Gas Info
            outTextGasInfo.Append("ICE:" + "\n" + (iceAmount / 1000).ToString("n1") + "kg" + "\n" + "\n");
            outTextGasInfo.Append("OXYGEN: (" + ((oxStor / oxCap) * 100).ToString("n1") + "%)" + "\n" +
                     oxStor.ToString("n0") + " / " + oxCap.ToString("n0") + " L" + "\n" + "\n" +
                     "HYDROGEN: (" + ((hyStor / hyCap) * 100).ToString("n1") + "%)" + "\n" +
                     hyStor.ToString("n0") + " / " + hyCap.ToString("n0") + " L" + "\n" + "\n");
            if (naCap > 0)
            {
                outTextGasInfo.Append("UNKNOWN: (" + ((naStor / naCap) * 100).ToString("n1") + "%)" + "\n" +
                                 naStor.ToString("n0") + " / " + naCap.ToString("n0") + " L" + "\n" + "\n" +
                                 "TANK MAX CAPACITY MUST " + "\n" + "BE UPDATED IN SCRIPT!!!");
            }


            //Mineral
            foreach (Mineral m in mineralList)
            {
                float factor = m.RefineFactor * (float)Math.Pow(1.09, yieldPointsPerRefinery);
                if (factor > 1)
                    factor = 1;
                outTextMinerals.Append((m.Name + ":").PadRight(mineralNameSpace) + (((m.IngotAmount / m.FullAmount) * 100).ToString("n1") + "%").PadLeft(mineralIngotSpace) + ("+" + (((m.OreAmount * factor) / m.FullAmount) * 100).ToString("n1") + "%").PadLeft(mineralOreSpace) + "\n");
            }


            //Component
            foreach (Component c in componentList)
            {
                outTextComponents.Append((c.Name + ":").PadRight(componentNameSpace) + (((c.Amount / c.FullAmount) * 100).ToString("n1") + "%").PadLeft(componentProsSpace) + ((c.Amount).ToString("n0")).PadLeft(componentAmountSpace) + "\n");
            }
        }

        private void ScreenDump()
        {
            mineralsScreen.WritePublicText(outTextMinerals.ToString());
            gasInfoScreen.WritePublicText(outTextGasInfo.ToString());

            string[] lines = outTextComponents.ToString().Split('\n');
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

            lines = outTextProjectiorInfo.ToString().Split('\n');
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
    }
}
