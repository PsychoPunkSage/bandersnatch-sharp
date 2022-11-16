namespace Nethermind.Verkle.Tree;

public class ByteArrayComparer : IEqualityComparer<byte[]>
{
    public bool Equals(byte[]? x, byte[]? y)
    {
        if (x == null || y == null)
        {
            return x == y;
        }
        return x.SequenceEqual(y);
    }

    public int GetHashCode(byte[] value)
    {
        HashCode hash = new HashCode();
        hash.AddBytes(value);
        return hash.ToHashCode();
    }
}

public struct MemoryDb
{
    public Dictionary<byte[], byte[]> LeafTable { get; }
    public Dictionary<byte[], SuffixTree> StemTable { get; }
    public Dictionary<byte[], InternalNode> BranchTable { get; }

    public MemoryDb()
    {
        LeafTable = new Dictionary<byte[], byte[]>(new ByteArrayComparer());
        StemTable = new Dictionary<byte[], SuffixTree>(new ByteArrayComparer());
        BranchTable = new Dictionary<byte[], InternalNode>(new ByteArrayComparer());
    }
}
