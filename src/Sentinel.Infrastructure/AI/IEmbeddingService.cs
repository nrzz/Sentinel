namespace Sentinel.Infrastructure.AI;

public interface IEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    double CosineSimilarity(float[] left, float[] right);
}
