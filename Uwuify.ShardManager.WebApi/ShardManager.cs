using System.Collections.Concurrent;
using Uwuify.ClassLibrary.Models;

namespace Uwuify.ShardManager.WebApi;

public class ShardManager
{
    private readonly ConcurrentDictionary<int, ShardGroup> _shardGroups = new();
    private int _maxShards;

    public ShardManager(int maxShards)
    {
        if (maxShards <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxShards));

        _maxShards = maxShards;
    }

    public ShardGroup? RequestShardGroup(int groupSize)
    {
        var lastShardId = !_shardGroups.Any()
            ? -1
            : _shardGroups.Values.Max(v => v.ShardIds.Max());

        var shardIds = new List<int>();

        // At least one to give
        int startingShard = lastShardId + 1;
        for (int i = 0; i < groupSize && lastShardId + 1 < _maxShards; i++)
        {
            shardIds.Add(startingShard + i);
            lastShardId = shardIds.Max();
        }

        var shardGroup = new ShardGroup((!_shardGroups.Any() ? -1 : _shardGroups.Keys.Max()) + 1, _maxShards, shardIds);

        if (shardGroup.ShardIds.Any())
        {
            _shardGroups.TryAdd(shardGroup.GroupId, shardGroup);
            return shardGroup;
        }

        throw new OutOfAvailableShardsException();
    }

    public bool UnassignShardGroup(int groupId) =>
        _shardGroups.Remove(groupId, out _);

    public ShardGroup[] GetShardGroups() =>
        _shardGroups.Select(g => g.Value).ToArray();

    public int GetMaxShards() =>
        _maxShards;

    public void SetMaxShards(int newShardCount) =>
        _maxShards = newShardCount;

    public void UnassignAllShardGroup()
    {
        _shardGroups.Clear();
    }
}

public class OutOfAvailableShardsException : Exception
{
}