using System;
using System.Collections.Generic;

/// <summary>
/// Works out which AI players a match needs: how many, and which team slot each takes.
///
/// Humans keep the teams they already have. Bots first top up those teams to the mode's
/// team size, then open new teams, until the match holds the full player count. So a
/// Squad match with 3 humans becomes 3 humans + 1 bot on team 1 and 4 bots on team 2,
/// and a Duo match with 3 humans becomes 2 + (1 human + 1 bot) + 2 bots + 2 bots.
///
/// Plain C# with no Unity dependency, so the maths can be checked on its own.
/// </summary>
public static class LSBotPlanner
{
    public struct HumanSlot
    {
        public int teamId;
        public int memberIndex;

        public HumanSlot(int teamId, int memberIndex)
        {
            this.teamId = teamId;
            this.memberIndex = memberIndex;
        }
    }

    public struct BotSlot
    {
        /// <summary>
        /// Identity of the bot for the match tracker. Always negative, so it can never
        /// collide with a Mirror connection ID (those start at 0 for the host).
        /// </summary>
        public int key;
        public string name;
        public int teamId;
        public int memberIndex;
    }

    public static List<BotSlot> Plan(IList<HumanSlot> humans, int totalPlayers, int maxTeamSize,
                                     IList<string> names, Random random)
    {
        var bots = new List<BotSlot>();

        int humanCount = humans != null ? humans.Count : 0;
        int botsNeeded = totalPlayers - humanCount;

        if (botsNeeded <= 0)
            return bots;

        maxTeamSize = Math.Max(1, maxTeamSize);

        // How full every team already is, and the highest slot used in each.
        var teamSize = new Dictionary<int, int>();
        var highestSlot = new Dictionary<int, int>();

        if (humans != null)
        {
            foreach (HumanSlot human in humans)
            {
                if (human.teamId <= 0)
                    continue;

                teamSize.TryGetValue(human.teamId, out int size);
                teamSize[human.teamId] = size + 1;

                highestSlot.TryGetValue(human.teamId, out int slot);
                highestSlot[human.teamId] = Math.Max(slot, human.memberIndex);
            }
        }

        List<string> namePool = ShuffledNames(names, random);
        int teamId = 1;

        while (bots.Count < botsNeeded)
        {
            teamSize.TryGetValue(teamId, out int size);

            if (size >= maxTeamSize)
            {
                teamId++;
                continue;
            }

            highestSlot.TryGetValue(teamId, out int slot);
            slot++;

            bots.Add(new BotSlot
            {
                key = -(bots.Count + 1),
                name = BotName(namePool, bots.Count),
                teamId = teamId,
                memberIndex = slot
            });

            teamSize[teamId] = size + 1;
            highestSlot[teamId] = slot;
        }

        return bots;
    }

    private static List<string> ShuffledNames(IList<string> names, Random random)
    {
        var pool = new List<string>();

        if (names != null)
        {
            foreach (string name in names)
            {
                if (!string.IsNullOrWhiteSpace(name))
                    pool.Add(name.Trim());
            }
        }

        // Fisher-Yates, so every match gets a different mix.
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            string swap = pool[i];
            pool[i] = pool[j];
            pool[j] = swap;
        }

        return pool;
    }

    private static string BotName(List<string> pool, int index)
    {
        // Numbered once the list runs out, so two bots never share a name.
        string name = index < pool.Count ? pool[index] : "Unit " + (index + 1);
        return "Bot " + name;
    }
}
