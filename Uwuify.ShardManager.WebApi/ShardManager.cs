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
        ShardGroup? shardGroup = null;

        // Search for unassigned group gaps
        if (_shardGroups.Keys.Any())
        {
            var maxKey = _shardGroups.Keys.Max();
            for (int i = 0; i < maxKey; i++)
            {
                // Gap found
                if (!_shardGroups.TryGetValue(i, out _))
                {
                    var hasPreviousGroup = _shardGroups.TryGetValue(i - 1, out var previousGroup);
                    _ = _shardGroups.TryGetValue(i + 1, out var nextGroup);

                    var startShardId = hasPreviousGroup
                        ? previousGroup!.ShardIds.Max() + 1
                        : 0;

                    var potentialLastShard = nextGroup!.ShardIds.Min();

                    var endShardId = potentialLastShard - startShardId > _groupSize
                        ? startShardId + (potentialLastShard - (potentialLastShard - _groupSize))
                        : potentialLastShard;

                    shardGroup = new ShardGroup(i, _maxShards, Enumerable.Range(startShardId, endShardId - startShardId).ToList());
                    break;
                }
            }
        }
        
        if (shardGroup is null)
        {
            int nextShardId = 0;

            if (_shardGroups.Any())
            {
                //_shardGroups.Values.Max(shardGroups => shardGroups.ShardIds.Max()) + 1;
                int maxAssignedShard = _shardGroups.Values.Max(shardGroups => shardGroups.ShardIds.Max());
                var existingShardIds = _shardGroups.Values.SelectMany(g => g.ShardIds).ToArray();
                int? missingShardId = null;
                for (int i = 0; i < maxAssignedShard; i++)
                {
                    if (existingShardIds[i] != i)
                    {
                        missingShardId = i;
                        break;
                    }
                }

                if (missingShardId.HasValue)
                {
                    nextShardId = missingShardId.Value;
                }
                else
                {
                    nextShardId = existingShardIds.Last() + 1;
                }
            }

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