using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;

namespace Mark_3
{
    class ProjectorTest : MyGridProgram
    {

        IMyTextPanel scr;

        public Program()
        {
            scr = GridTerminalSystem.GetBlockWithName("LCD panel Test 1") as IMyTextPanel;
        }

        public void Save()
        {
        }

        public void Main(string argument, UpdateType updateSource)
        {
            scr.WritePublicText("");

            IMyProjector pr = GridTerminalSystem.GetBlockWithName("Projector Ship Builder") as IMyProjector;
            if (pr.Enabled) {
                if (pr.IsProjecting)
                {
                    string proOut = pr.DetailedInfo;

                    if (!proOut.Contains("Blocks remaining:")) { 
                        string blkRem = proOut.Substring(proOut.IndexOf("Blocks remaining:") + ("Blocks remaining:").Length + 1);

                        string[] lines = blkRem.Split('\n');

                        string outTxt = "";
                        for (int i = 0; i < lines.Length; i++)
                        {
                            string[] vars = lines[i].Split(new string[] { ": " }, StringSplitOptions.None);
                            outTxt += i + ": " + vars[0] + " ; " + vars[1] + "\n";
                        }
                        scr.WritePublicText(outTxt);
                    }
                    else
                    {
                        scr.WritePublicText("Ship is done!");
                    }
                }
                else
                {
                    scr.WritePublicText("Projector is not projecting...");
                }
            }
            else
            {
                scr.WritePublicText("Projector is not on...");
            }
        }
    }
}
