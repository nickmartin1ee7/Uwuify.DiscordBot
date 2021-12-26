namespace Uwuify.ClassLibrary.Models;

public record ShardGroup(int GroupId, int MaxShards, List<int> ShardIds)
{
    public override string ToString() => $"{nameof(ShardGroup)} {{ {nameof(GroupId)} = {GroupId}, {nameof(MaxShards)} = {MaxShards}, {nameof(ShardIds)} = {{ {string.Join(", ", ShardIds)} }} }}";
}