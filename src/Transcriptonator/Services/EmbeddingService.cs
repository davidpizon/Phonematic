using System.Numerics.Tensors;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Transcriptonator.Data;
using Transcriptonator.Models;

namespace Transcriptonator.Services;

public class EmbeddingService : IEmbeddingService, IDisposable
{
    private readonly IModelManagerService _modelManager;
    private readonly IConfigService _configService;
    private readonly IDbContextFactory<TranscriptonatorDbContext> _dbFactory;
    private InferenceSession? _session;
    private Dictionary<string, int>? _vocab;
    private bool _disposed;

    private const int MaxTokenLength = 128;
    private const int EmbeddingDimension = 384;

    public EmbeddingService(IModelManagerService modelManager, IConfigService configService, IDbContextFactory<TranscriptonatorDbContext> dbFactory)
    {
        _modelManager = modelManager;
        _configService = configService;
        _dbFactory = dbFactory;
    }

    public float[] GenerateEmbedding(string text)
    {
        EnsureModelLoaded();

        var tokens = Tokenize(text);
        var inputIds = new DenseTensor<long>(new[] { 1, tokens.Length });
        var attentionMask = new DenseTensor<long>(new[] { 1, tokens.Length });
        var tokenTypeIds = new DenseTensor<long>(new[] { 1, tokens.Length });

        for (int i = 0; i < tokens.Length; i++)
        {
            inputIds[0, i] = tokens[i];
            attentionMask[0, i] = 1;
            tokenTypeIds[0, i] = 0;
        }

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", inputIds),
            NamedOnnxValue.CreateFromTensor("attention_mask", attentionMask),
            NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeIds)
        };

        using var results = _session!.Run(inputs);
        var output = results.First().AsTensor<float>();

        // Mean pooling over token dimension
        var embedding = new float[EmbeddingDimension];
        for (int d = 0; d < EmbeddingDimension; d++)
        {
            float sum = 0;
            for (int t = 0; t < tokens.Length; t++)
            {
                sum += output[0, t, d];
            }
            embedding[d] = sum / tokens.Length;
        }

        // L2 normalize
        var norm = MathF.Sqrt(TensorPrimitives.Dot(embedding, embedding));
        if (norm > 0)
        {
            for (int i = 0; i < embedding.Length; i++)
                embedding[i] /= norm;
        }

        return embedding;
    }

    public List<string> ChunkText(string text, int chunkSize, int chunkOverlap)
    {
        var chunks = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return chunks;

        var sentences = SplitIntoSentences(text);
        var currentChunk = new List<string>();
        var currentLength = 0;

        foreach (var sentence in sentences)
        {
            var trimmed = sentence.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            if (currentLength + trimmed.Length > chunkSize && currentChunk.Count > 0)
            {
                chunks.Add(string.Join(" ", currentChunk));

                // Keep overlap
                var overlapChunk = new List<string>();
                var overlapLen = 0;
                for (int i = currentChunk.Count - 1; i >= 0; i--)
                {
                    if (overlapLen + currentChunk[i].Length > chunkOverlap) break;
                    overlapChunk.Insert(0, currentChunk[i]);
                    overlapLen += currentChunk[i].Length;
                }

                currentChunk = overlapChunk;
                currentLength = overlapLen;
            }

            currentChunk.Add(trimmed);
            currentLength += trimmed.Length;
        }

        if (currentChunk.Count > 0)
        {
            chunks.Add(string.Join(" ", currentChunk));
        }

        return chunks;
    }

    public async Task StoreChunksAsync(ProcessedFile file, string fullText, CancellationToken ct = default)
    {
        var config = _configService.Load();
        var chunks = ChunkText(fullText, config.ChunkSize, config.ChunkOverlap);

        using var db = await _dbFactory.CreateDbContextAsync(ct);

        for (int i = 0; i < chunks.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var embedding = GenerateEmbedding(chunks[i]);
            var embeddingBytes = new byte[embedding.Length * sizeof(float)];
            Buffer.BlockCopy(embedding, 0, embeddingBytes, 0, embeddingBytes.Length);

            var chunk = new TranscriptionChunk
            {
                ProcessedFileId = file.Id,
                ChunkIndex = i,
                Text = chunks[i],
                Embedding = embeddingBytes
            };

            db.TranscriptionChunks.Add(chunk);
        }

        await db.SaveChangesAsync(ct);
    }

    private void EnsureModelLoaded()
    {
        if (_session != null) return;

        var modelPath = _modelManager.GetOnnxModelPath();
        _session = new InferenceSession(modelPath);

        var vocabPath = _modelManager.GetOnnxVocabPath();
        _vocab = new Dictionary<string, int>();
        var lines = File.ReadAllLines(vocabPath);
        for (int i = 0; i < lines.Length; i++)
        {
            _vocab[lines[i]] = i;
        }
    }

    private long[] Tokenize(string text)
    {
        // Simple WordPiece-style tokenization
        var tokens = new List<long>();
        tokens.Add(GetTokenId("[CLS]"));

        var words = text.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in words)
        {
            if (tokens.Count >= MaxTokenLength - 1) break;

            if (_vocab!.TryGetValue(word, out var id))
            {
                tokens.Add(id);
            }
            else
            {
                // Try subword tokenization
                var remaining = word;
                var isFirst = true;

                while (remaining.Length > 0 && tokens.Count < MaxTokenLength - 1)
                {
                    var found = false;
                    for (int end = remaining.Length; end > 0; end--)
                    {
                        var subword = isFirst ? remaining[..end] : $"##{remaining[..end]}";
                        if (_vocab.TryGetValue(subword, out var subId))
                        {
                            tokens.Add(subId);
                            remaining = remaining[end..];
                            isFirst = false;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        tokens.Add(GetTokenId("[UNK]"));
                        break;
                    }
                }
            }
        }

        tokens.Add(GetTokenId("[SEP]"));
        return tokens.ToArray();
    }

    private long GetTokenId(string token)
    {
        return _vocab!.TryGetValue(token, out var id) ? id : 0;
    }

    private static List<string> SplitIntoSentences(string text)
    {
        var sentences = new List<string>();
        var current = 0;

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '.' || text[i] == '!' || text[i] == '?' || text[i] == '\n')
            {
                if (i > current)
                {
                    sentences.Add(text[current..(i + 1)]);
                }
                current = i + 1;
            }
        }

        if (current < text.Length)
        {
            sentences.Add(text[current..]);
        }

        return sentences;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _session?.Dispose();
            _disposed = true;
        }
    }
}
