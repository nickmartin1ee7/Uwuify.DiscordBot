using System.Collections.Concurrent;
using Uwuify.ClassLibrary.Models;

namespace Uwuify.ShardManager.WebApi;

public class ShardManager
{
    private readonly ConcurrentStack<int> _shardStack = new();
    private int _maxShards;

    public ShardManager(int maxShards)
    {
        _maxShards = maxShards;
    }

    public int GetCurrentShards()
    {
        _ = _shardStack.TryPeek(out var shardCount);
        return shardCount;
    }

    public int GetCurrentShardCount() => _maxShards;

    public ShardModel GetNextShard()
    {
        if (_maxShards <= 0)
            throw new ShardingNotAllowedException();

        if (_maxShards == 1)
            throw new ShardingNotRecommendedException();

        if (_shardStack.TryPeek(out var shard))
            shard++;

        _shardStack.Push(shard); // 0

        return new ShardModel
        {
            ShardId = ValidateShard(shard),
            ShardCount = _maxShards
        };
    }

    // Next shard cannot be equal to or greater than max shard count
    private int ValidateShard(int newShard)
    {
        if (newShard >= _maxShards)
            throw new OutOfAvailableShardsException();

        return newShard;
    }

    public void GrowShardCount(int shardCount)
    {
        _maxShards = shardCount;
    }

    public void ResetShards()
    {
        _shardStack.Clear();
    }
}

public class ShardingNotRecommendedException : Exception
{
}

public class ShardingNotAllowedException : Exception
{
}

public class OutOfAvailableShardsException : Exception
{
}