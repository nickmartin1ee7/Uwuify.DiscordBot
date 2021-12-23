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
        if (_shardStack.TryPeek(out var lastShard))
        {
            if (lastShard >= _maxShards - 1) // Next shard cannot be equal to or greater than max shard count
                throw new OutOfAvailableShardsException();

            lastShard++;
            _shardStack.Push(lastShard);
            return lastShard;
        }

        _shardStack.Push(lastShard); // 0
        return lastShard;
    }
}

public class OutOfAvailableShardsException : Exception
{
}