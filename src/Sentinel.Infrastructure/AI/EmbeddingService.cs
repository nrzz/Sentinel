namespace Sentinel.Infrastructure.AI;

public sealed class EmbeddingService : IEmbeddingService
{
    private readonly IAiProvider _provider;

    public EmbeddingService(IAiProvider provider)
    {
        _provider = provider;
    }

    public Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default) =>
        _provider.EmbedAsync(text, cancellationToken);

    public double CosineSimilarity(float[] left, float[] right)
    {
        if (left.Length == 0 || right.Length == 0 || left.Length != right.Length)
        {
            return 0;
        }

        double dot = 0;
        double leftMagnitude = 0;
        double rightMagnitude = 0;

        for (var i = 0; i < left.Length; i++)
        {
            dot += left[i] * right[i];
            leftMagnitude += left[i] * left[i];
            rightMagnitude += right[i] * right[i];
        }

        if (leftMagnitude == 0 || rightMagnitude == 0)
        {
            return 0;
        }

        return dot / (Math.Sqrt(leftMagnitude) * Math.Sqrt(rightMagnitude));
    }
}
