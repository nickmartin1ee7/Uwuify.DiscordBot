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
            throw new OutOfAvailableShardsException("No shards are allowed");

        if (_shardStack.TryPeek(out var shard))
            shard++;

        _shardStack.Push(shard); // 0
        return ValidateShard(shard);
    }

    // Next shard cannot be equal to or greater than max shard count
    private int ValidateShard(int newShard)
    {
        if (newShard >= _maxShards)
            throw new OutOfAvailableShardsException($"Out of available shards ({_maxShards})");

        return newShard;
    }
}

public class OutOfAvailableShardsException : Exception
{
    public OutOfAvailableShardsException(string message) : base(message)
    {
    }
}