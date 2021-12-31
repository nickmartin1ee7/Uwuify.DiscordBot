using System.Collections.Concurrent;
using Uwuify.ClassLibrary.Models;

namespace Uwuify.ShardManager.WebApi;

public class ShardManager
{
    private readonly ConcurrentDictionary<int, ShardGroup> _shardGroups = new();
    private int _maxShards;
    private int _groupSize;

    public ShardManager(int maxShards, int groupSize)
    {
        if (maxShards < 1)
            throw new ArgumentOutOfRangeException(nameof(maxShards));

        if (groupSize < 1 || groupSize > maxShards)
            throw new ArgumentOutOfRangeException(nameof(groupSize));

        _maxShards = maxShards;
        _groupSize = groupSize;
    }

    public ShardGroup RequestShardGroup()
    {
        int nextGroupId = 0;
        var shardIds = new List<int>();

        if (_shardGroups.Any())
        {
            // Next available Group Id
            var existingGroupIds = _shardGroups.Keys.ToArray();

            nextGroupId = FindNextId(existingGroupIds);

            // Available ShardIds
            var existingShardIds = _shardGroups.Values.SelectMany(g => g.ShardIds)
                .OrderBy(id => id)
                .ToArray();

            var gapIds = FindGapsInArray(existingShardIds);

            shardIds.AddRange(gapIds.Length >= _groupSize
                ? gapIds[.._groupSize]
                : gapIds);

            if (!shardIds.Any())
            {
                var nextShardId = FindNextId(existingShardIds);

                var tempLastShardId = existingShardIds.Last() + _groupSize;

                var lastShardId = tempLastShardId >= _maxShards
                    ? tempLastShardId - (tempLastShardId - _maxShards) - 1
                    : tempLastShardId;
                shardIds.AddRange(Enumerable.Range(nextShardId, lastShardId - nextShardId + 1).ToList());
            }
        }
        else
        {
            // First group
            for (int i = 0; i < _groupSize; i++)
            {
                shardIds.Add(i);
            }
        }

        var shardGroup = new ShardGroup(nextGroupId, _maxShards, shardIds);

        // Does the new group contain any shards?
        if (!shardGroup.ShardIds.Any())
            throw new OutOfAvailableShardsException();

        _shardGroups.TryAdd(shardGroup.GroupId, shardGroup);
        return shardGroup;
    }

    private int[] FindGapsInArray(int[] arr)
    {
        var last = arr.Last();
        var missingIds = new List<int>();

        for (int i = 0; i < last; i++)
        {
            if (arr.Contains(i))
                continue;

            missingIds.Add(i);
        }

        return missingIds.ToArray();
    }

    private int FindNextId(int[] ids)
    {
        var last = ids.Last();
        int nextId;
        int? missingId = null;

        for (int i = 0; i < last; i++)
        {
            if (ids.Contains(i))
                continue;

            missingId = i;
            break;
        }

        if (missingId.HasValue)
        {
            nextId = missingId.Value;
        }
        else
        {
            nextId = last + 1;
        }

        return nextId;
    }

    public bool UnassignShardGroup(int groupId) =>
        _shardGroups.Remove(groupId, out _);

    public ShardGroup[] GetShardGroups() =>
        _shardGroups.Select(g => g.Value).ToArray();

    public int GetMaxShards() =>
        _maxShards;

    public void SetMaxShards(int newShardCount)
    {
        if (newShardCount < 1)
            throw new ArgumentOutOfRangeException(nameof(newShardCount));

        _maxShards = newShardCount;
        UnassignAllShardGroups();
    }

    public void UnassignAllShardGroups()
    {
        _shardGroups.Clear();
    }

    public int GetInternalShards() => _groupSize;

    public void SetInternalShards(int newInternalShardCount)
    {
        if (newInternalShardCount < 1 || newInternalShardCount > _maxShards)
            throw new ArgumentOutOfRangeException(nameof(newInternalShardCount));

        _groupSize = newInternalShardCount;
    }
}

public class OutOfAvailableShardsException : Exception
{
}