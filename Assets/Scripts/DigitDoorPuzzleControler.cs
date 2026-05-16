using System;
using System.IO;
using UnityEngine;

[DisallowMultipleComponent]
public class DigitDoorPuzzleController : MonoBehaviour
{
    const int FallbackInputSize = 50 * 50;
    const int DigitClassCount = 10; // digits 0..9

    [Header("References")]
    public AN_DoorScript targetDoor;
    public DigitDrawingPanel drawingPanel;

    [Header("Model")]
    public string modelRelativePath = "digit_model.bin";
    [Range(0, 9)] public int correctDigit = 4;

    private LinearSoftmaxDigitModel model;
    private bool modelLoaded;

    void Start()
    {
        LoadModel();
    }

    public void ClearPanel()
    {
        if (drawingPanel == null)
        {
            Debug.LogWarning("DigitDoorPuzzleController: drawing panel is not assigned.");
            return;
        }

        drawingPanel.ClearCanvas();
    }

    public void RecognizeAndTryOpenDoor()
    {
        if (drawingPanel == null)
        {
            Debug.LogWarning("DigitDoorPuzzleController: drawing panel is not assigned.");
            return;
        }
        if (targetDoor == null)
        {
            Debug.LogWarning("DigitDoorPuzzleController: target door is not assigned.");
            return;
        }
        if (!modelLoaded || model == null)
        {
            Debug.LogWarning("DigitDoorPuzzleController: model is not loaded, recognition is unavailable.");
            return;
        }
        if (drawingPanel.IsCanvasEmpty)
        {
            Debug.Log("DigitDoorPuzzleController: canvas is empty, recognition skipped.");
            return;
        }

        float[] input = drawingPanel.GetNormalizedPixelsTopLeft();
        if (input == null || input.Length == 0)
        {
            Debug.LogWarning("DigitDoorPuzzleController: invalid panel data.");
            return;
        }

        if (input.Length != model.InputSize)
        {
            Debug.LogWarning($"DigitDoorPuzzleController: input size mismatch. Expected {model.InputSize}, got {input.Length}.");
            return;
        }

        int predictedDigit;
        try
        {
            predictedDigit = model.Predict(input);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"DigitDoorPuzzleController: recognition failed: {ex.Message}");
            return;
        }

        Debug.Log($"DigitDoorPuzzleController: recognized digit = {predictedDigit}.");
        if (predictedDigit == correctDigit)
            targetDoor.Action();
    }

    public void ReloadModel()
    {
        LoadModel();
    }

    void LoadModel()
    {
        model = null;
        modelLoaded = false;

        string path = Path.Combine(Application.streamingAssetsPath, modelRelativePath);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"DigitDoorPuzzleController: model file not found at '{path}'.");
            return;
        }

        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(path);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"DigitDoorPuzzleController: failed to read model file: {ex.Message}");
            return;
        }

        int expectedInput = drawingPanel != null ? drawingPanel.PixelCount : FallbackInputSize;
        if (!LinearSoftmaxDigitModel.TryCreate(bytes, expectedInput, out model, out string error))
        {
            Debug.LogWarning($"DigitDoorPuzzleController: failed to load model: {error}");
            return;
        }

        modelLoaded = true;
        Debug.Log($"DigitDoorPuzzleController: model loaded from '{path}'.");
    }

    class LinearSoftmaxDigitModel
    {
        public int InputSize { get; }
        public int OutputSize { get; }

        readonly float[] weights;
        readonly float[] bias;

        LinearSoftmaxDigitModel(int inputSize, int outputSize, float[] weights, float[] bias)
        {
            InputSize = inputSize;
            OutputSize = outputSize;
            this.weights = weights;
            this.bias = bias;
        }

        public int Predict(float[] input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.Length != InputSize) throw new ArgumentException("Unexpected input size.");

            int bestClass = 0;
            float bestLogit = float.NegativeInfinity;

            for (int c = 0; c < OutputSize; c++)
            {
                float sum = bias[c];
                int wBase = c * InputSize;
                for (int i = 0; i < InputSize; i++)
                    sum += weights[wBase + i] * input[i];

                if (sum > bestLogit)
                {
                    bestLogit = sum;
                    bestClass = c;
                }
            }

            return bestClass;
        }

        public static bool TryCreate(byte[] modelBytes, int expectedInputSize, out LinearSoftmaxDigitModel model, out string error)
        {
            model = null;
            error = null;

            if (modelBytes == null || modelBytes.Length == 0)
            {
                error = "Model file is empty.";
                return false;
            }
            if (modelBytes.Length % 4 != 0)
            {
                error = "Model file size is not aligned to float32 values.";
                return false;
            }

            int totalFloatCount = modelBytes.Length / 4;
            float[] values = new float[totalFloatCount];
            if (BitConverter.IsLittleEndian)
            {
                Buffer.BlockCopy(modelBytes, 0, values, 0, modelBytes.Length);
            }
            else
            {
                byte[] tmp = new byte[4];
                for (int i = 0; i < totalFloatCount; i++)
                {
                    int byteIndex = i * 4;
                    tmp[0] = modelBytes[byteIndex + 3];
                    tmp[1] = modelBytes[byteIndex + 2];
                    tmp[2] = modelBytes[byteIndex + 1];
                    tmp[3] = modelBytes[byteIndex];
                    values[i] = BitConverter.ToSingle(tmp, 0);
                }
            }

            int requiredWithBias = expectedInputSize * DigitClassCount + DigitClassCount;
            int requiredNoBias = expectedInputSize * DigitClassCount;

            float[] weights;
            float[] bias;

            if (totalFloatCount == requiredWithBias)
            {
                weights = new float[expectedInputSize * DigitClassCount];
                bias = new float[DigitClassCount];
                Array.Copy(values, 0, weights, 0, weights.Length);
                Array.Copy(values, weights.Length, bias, 0, bias.Length);
            }
            else if (totalFloatCount == requiredNoBias)
            {
                weights = values;
                bias = new float[DigitClassCount];
            }
            else
            {
                error = $"Unsupported model format. Expected float count {requiredWithBias} (weights+bias) or {requiredNoBias} (weights only), got {totalFloatCount}.";
                return false;
            }

            model = new LinearSoftmaxDigitModel(expectedInputSize, DigitClassCount, weights, bias);
            return true;
        }
    }
}