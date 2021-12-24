namespace Uwuify.ClassLibrary.Models;

public record ShardGroup(int GroupId, int MaxShards, List<int> ShardIds);