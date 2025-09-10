using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Text;

namespace Mma.Helpers;

public static class MLHelper
{
    public static HashSet<string> NgramTokens(string text, int ngram)
    {
        // Create a new ML context, for ML.NET operations. It can be used for
        // exception tracking and logging, as well as the source of randomness.
        var mlContext = new MLContext();

        // Create a small dataset as an IEnumerable.
        var samples = new List<TextData>()
            {

                 new TextData{ Text = text },
            };

        // Convert training data to IDataView.
        var dataview = mlContext.Data.LoadFromEnumerable(samples);

        // A pipeline for converting text into numeric n-gram features.
        // The following call to 'ProduceNgrams' requires the tokenized
        // text /string as input. This is achieved by calling 
        // 'TokenizeIntoWords' first followed by 'ProduceNgrams'. Please note
        // that the length of the output feature vector depends on the n-gram
        // settings.
        var textPipeline = mlContext.Transforms.Text.TokenizeIntoWords("Tokens",
            "Text")
            // 'ProduceNgrams' takes key type as input. Converting the tokens
            // into key type using 'MapValueToKey'.
            .Append(mlContext.Transforms.Conversion.MapValueToKey("Tokens"))
            .Append(mlContext.Transforms.Text.ProduceNgrams("NgramFeatures",
                "Tokens",
                ngramLength: ngram,
                useAllLengths: false,
                weighting: NgramExtractingEstimator.WeightingCriteria.Tf));

        // Fit to data.
        var textTransformer = textPipeline.Fit(dataview);
        var transformedDataView = textTransformer.Transform(dataview);

        // Create the prediction engine to get the n-gram features extracted
        // from the text.
        var predictionEngine = mlContext.Model.CreatePredictionEngine<TextData,
            TransformedTextData>(textTransformer);

        // Convert the text into numeric features.
        var prediction = predictionEngine.Predict(samples[0]);

        // Preview of the produced n-grams.
        // Get the slot names from the column's metadata.
        // The slot names for a vector column corresponds to the names
        // associated with each position in the vector.
        VBuffer<ReadOnlyMemory<char>> slotNames = default;
        transformedDataView.Schema["NgramFeatures"].GetSlotNames(ref slotNames);
        var NgramFeaturesColumn = transformedDataView.GetColumn<VBuffer<
            float>>(transformedDataView.Schema["NgramFeatures"]);
        var slots = slotNames.GetValues();

        HashSet<string> tokens = new();
        foreach (var featureRow in NgramFeaturesColumn)
        {
            foreach (var item in featureRow.Items())
            {
                tokens.Add(slots[item.Key].ToString().Replace("|", " "));
            }
        }

        return tokens;

    }
}

public class TextData
{
    public string Text { get; set; }
}

public class TransformedTextData : TextData
{
    public float[] NgramFeatures { get; set; }
}