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
        if (maxShards <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxShards));

        if (groupSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(groupSize));

        _maxShards = maxShards;
        _groupSize = groupSize;
    }

    public ShardGroup? RequestShardGroup()
    {
        ShardGroup? shardGroup = null;

        // Search for unassigned group gaps
        if (_shardGroups.Keys.Any())
        {
            for (int i = 0; i < _shardGroups.Keys.Max(); i++)
            {
                // Gap found
                if (!_shardGroups.TryGetValue(i, out _))
                {
                    var hasPreviousGroup = _shardGroups.TryGetValue(i - 1, out var previousGroup);
                    _ = _shardGroups.TryGetValue(i + 1, out var nextGroup);

                    var startShardId = hasPreviousGroup
                        ? previousGroup!.ShardIds.Max() + 1
                        : 0;

                    var endShardId = nextGroup!.ShardIds.Min();

                    shardGroup = new ShardGroup(i, _maxShards, Enumerable.Range(startShardId, endShardId - startShardId).ToList());
                    break;
                }
            }
        }
        
        if (shardGroup is null)
        {
            var nextShardId = !_shardGroups.Any()
                ? 0
                : _shardGroups.Values.Max(shardGroups => shardGroups.ShardIds.Max()) + 1;

            var prospectiveLastShardId = nextShardId + _groupSize;

            var lastShardId = prospectiveLastShardId >= _maxShards
                ? prospectiveLastShardId - (prospectiveLastShardId - _maxShards)
                : prospectiveLastShardId;

            var nextGroupId = !_shardGroups.Any()
                ? 0
                : _shardGroups.Keys.Max() + 1;

            shardGroup = new ShardGroup(nextGroupId,
                _maxShards,
                Enumerable.Range(nextShardId, lastShardId - nextShardId).ToList());
        }

        // Does the new group contain any shards?
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

    public void SetMaxShards(int newShardCount)
    {
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
        _groupSize = newInternalShardCount;
        UnassignAllShardGroups();
    }
}

public class OutOfAvailableShardsException : Exception
{
}