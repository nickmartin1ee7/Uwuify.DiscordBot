using System.Collections.Concurrent;

namespace Uwuify.ShardManager.WebApi;

public class ShardManager
{
    private readonly ConcurrentStack<int> _shardStack = new();
    private readonly int _maxShards;

    public ShardManager(int maxShards)
    {
        _maxShards = maxShards;
    }

    public int GetNextShard()
    {
        if (_maxShards <= 0)
            throw new ShardingNotAllowedException();

        if (_shardStack.TryPeek(out var shard))
            shard++;

        _shardStack.Push(shard); // 0
        return ValidateShard(shard);
    }

    // Next shard cannot be equal to or greater than max shard count
    private int ValidateShard(int newShard)
    {
        if (newShard >= _maxShards)
            throw new OutOfAvailableShardsException();

        return newShard;
    }
}

public class ShardingNotAllowedException : Exception
{
}

public class OutOfAvailableShardsException : Exception
{
}