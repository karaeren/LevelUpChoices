using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RoR2;

namespace LevelUpChoices
{
    public static class ItemSimilarityManager
    {
        private static readonly HashSet<string> StopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "with", "by", "of", "from",
            "is", "are", "was", "were", "it", "this", "that", "these", "those", "you", "your", "we", "our",
            "he", "she", "they", "their", "has", "have", "had", "can", "could", "will", "would", "do", "does",
            "did", "not", "be", "been", "being", "as", "if", "when", "than", "then", "which", "who", "whom", "what",
            "per", "stack", "maximum", "chance"
        };

        public static Dictionary<ItemIndex, List<ItemIndex>> SimilarItemsMap { get; private set; } = [];

        private static string Stem(string word)
        {
            if (word.EndsWith("ies"))
                return word[..^3] + "y";
            if (word.EndsWith("es"))
                return word[..^2];
            if (word.EndsWith("s") && !word.EndsWith("ss"))
                return word[..^1];
            if (word.EndsWith("ing"))
                return word[..^3];
            if (word.EndsWith("ed"))
                return word[..^2];
            return word;
        }

        private static List<string> Tokenize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return [];

            // Strip HTML tags if any
            text = Regex.Replace(text, "<.*?>", string.Empty);

            // Remove numbers and percentages
            text = Regex.Replace(text, @"\b\d+%?\b", "");

            var matches = Regex.Matches(text.ToLowerInvariant(), @"[a-z]+");
            var tokens = new List<string>();
            foreach (Match match in matches)
            {
                var word = match.Value;
                if (!StopWords.Contains(word))
                {
                    tokens.Add(Stem(word));
                }
            }

            var bigrams = new List<string>();
            for (int i = 0; i < tokens.Count - 1; i++)
            {
                bigrams.Add(tokens[i] + "_" + tokens[i + 1]);
            }
            tokens.AddRange(bigrams);

            return tokens;
        }

        public static void Initialize()
        {
            Log.Info("Initializing ItemSimilarityManager...");

            var validItems = new List<ItemDef>();
            foreach (var itemIndex in ItemCatalog.allItems)
            {
                var def = ItemCatalog.GetItemDef(itemIndex);
                if (def != null && !def.hidden && def.tier != ItemTier.NoTier)
                {
                    validItems.Add(def);
                }
            }

            var itemTokens = new Dictionary<ItemIndex, List<string>>();
            foreach (var item in validItems)
            {
                string name = !string.IsNullOrEmpty(item.nameToken) ? Language.GetString(item.nameToken) : "";
                string desc = !string.IsNullOrEmpty(item.descriptionToken) ? Language.GetString(item.descriptionToken) : "";
                string text = name + " " + desc;
                itemTokens[item.itemIndex] = Tokenize(text);
            }

            var documentFrequency = new Dictionary<string, int>();
            foreach (var tokens in itemTokens.Values)
            {
                foreach (var term in tokens.Distinct())
                {
                    if (documentFrequency.ContainsKey(term))
                        documentFrequency[term]++;
                    else
                        documentFrequency[term] = 1;
                }
            }

            int N = validItems.Count;
            var idf = new Dictionary<string, double>();
            foreach (var kvp in documentFrequency)
            {
                idf[kvp.Key] = Math.Log((double)N / kvp.Value) + 1;
            }

            var tfIdfVectors = new Dictionary<ItemIndex, Dictionary<string, double>>();
            foreach (var kvp in itemTokens)
            {
                var itemIndex = kvp.Key;
                var tokens = kvp.Value;
                var tfIdf = new Dictionary<string, double>();

                var termCounts = tokens.GroupBy(t => t).ToDictionary(g => g.Key, g => g.Count());
                int totalTerms = tokens.Count;

                foreach (var termKvp in termCounts)
                {
                    string term = termKvp.Key;
                    int count = termKvp.Value;
                    double tf = (double)count / totalTerms;
                    tfIdf[term] = tf * idf[term];
                }

                tfIdfVectors[itemIndex] = tfIdf;
            }

            double similarityThreshold = ModConfig.SimilarityThreshold.Value;

            foreach (var item in validItems)
            {
                var vecA = tfIdfVectors[item.itemIndex];
                var similarities = new List<(ItemIndex OtherItem, double Score)>();

                foreach (var otherItem in validItems)
                {
                    if (item.itemIndex == otherItem.itemIndex)
                        continue;

                    var vecB = tfIdfVectors[otherItem.itemIndex];
                    double dotProduct = 0;
                    double normA = 0;
                    double normB = 0;

                    foreach (var kvp in vecA)
                        normA += kvp.Value * kvp.Value;
                    foreach (var kvp in vecB)
                        normB += kvp.Value * kvp.Value;

                    normA = Math.Sqrt(normA);
                    normB = Math.Sqrt(normB);

                    if (normA == 0 || normB == 0)
                        continue;

                    foreach (var kvp in vecA)
                    {
                        if (vecB.TryGetValue(kvp.Key, out double valB))
                        {
                            dotProduct += kvp.Value * valB;
                        }
                    }

                    double cosineSim = dotProduct / (normA * normB);
                    similarities.Add((otherItem.itemIndex, cosineSim));
                }

                var topSimilar = similarities
                    .Where(s => s.Score >= similarityThreshold)
                    .OrderByDescending(s => s.Score)
                    .Take(ModConfig.SimilarItemCount.Value - 1)
                    .Select(s => s.OtherItem)
                    .ToList();

                // Item is its own first similar item (for stacking)
                topSimilar.Insert(0, item.itemIndex);

                SimilarItemsMap[item.itemIndex] = topSimilar;
            }
            Log.Info($"ItemSimilarityManager initialized with {SimilarItemsMap.Count} items mapped.");
        }
    }
}
