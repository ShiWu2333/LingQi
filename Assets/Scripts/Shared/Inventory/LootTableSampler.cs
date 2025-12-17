using System.Collections.Generic;
using UnityEngine;

public static class LootTableSampler
{
    public static List<ItemStack> Roll(LootTableConfig table, int? overrideRolls = null)
    {
        var result = new List<ItemStack>();
        if (table == null || table.entries == null || table.entries.Count == 0)
            return result;

        int rolls = overrideRolls ?? table.rolls;
        if (rolls <= 0) return result;

        // 候选池（用于 uniqueItems）
        var pool = new List<LootTableConfig.Entry>(table.entries);

        for (int r = 0; r < rolls; r++)
        {
            var pick = PickWeighted(pool);
            if (pick == null || pick.item == null) continue;

            int min = Mathf.Max(1, pick.minCount);
            int max = Mathf.Max(min, pick.maxCount);
            int count = Random.Range(min, max + 1);

            // 合并同类
            Merge(result, pick.item, count);

            if (table.uniqueItems)
            {
                pool.Remove(pick);
                if (pool.Count == 0) break;
            }
        }

        return result;
    }

    private static LootTableConfig.Entry PickWeighted(List<LootTableConfig.Entry> entries)
    {
        float total = 0f;
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e == null || e.item == null) continue;
            total += Mathf.Max(0f, e.weight);
        }

        if (total <= 0f) return null;

        float roll = Random.Range(0f, total);
        float acc = 0f;

        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e == null || e.item == null) continue;

            acc += Mathf.Max(0f, e.weight);
            if (roll <= acc)
                return e;
        }

        return null;
    }

    private static void Merge(List<ItemStack> list, ItemConfig item, int count)
    {
        if (item == null || count <= 0) return;

        for (int i = 0; i < list.Count; i++)
        {
            var s = list[i];
            if (s != null && s.item == item)
            {
                s.count += count;
                return;
            }
        }

        list.Add(new ItemStack(item, count));
    }
}
