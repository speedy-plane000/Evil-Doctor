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

    private IDigitModel model;
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
        if (!DigitModelFactory.TryCreate(bytes, expectedInput, out model, out string error))
        {
            Debug.LogWarning($"DigitDoorPuzzleController: failed to load model: {error}");
            return;
        }

        modelLoaded = true;
        Debug.Log($"DigitDoorPuzzleController: model loaded from '{path}'.");
    }

    interface IDigitModel
    {
        int InputSize { get; }
        int Predict(float[] input);
    }

    static class DigitModelFactory
    {
        const int KotlinModelVersion = 1;
        const int KotlinOutputSize = 10;

        public static bool TryCreate(byte[] modelBytes, int expectedInputSize, out IDigitModel model, out string error)
        {
            model = null;
            error = null;

            if (TryCreateKotlinTwoLayer(modelBytes, expectedInputSize, out model))
                return true;
            if (LinearSoftmaxDigitModel.TryCreate(modelBytes, expectedInputSize, out LinearSoftmaxDigitModel linearModel, out error))
            {
                model = linearModel;
                return true;
            }

            if (string.IsNullOrEmpty(error))
                error = "Unsupported model format.";
            return false;
        }

        static bool TryCreateKotlinTwoLayer(byte[] modelBytes, int expectedInputSize, out IDigitModel model)
        {
            model = null;
            if (modelBytes == null || modelBytes.Length < 8) return false;
            if (!TryReadInt32BigEndian(modelBytes, 0, out int version)) return false;
            if (version != KotlinModelVersion) return false;
            if (((modelBytes.Length - 4) % 4) != 0) return false;

            int parameterCount = (modelBytes.Length - 4) / 4;
            int hiddenNumerator = parameterCount - KotlinOutputSize;
            int hiddenDenominator = expectedInputSize + KotlinOutputSize + 1;
            if (hiddenDenominator <= 0 || hiddenNumerator <= 0 || (hiddenNumerator % hiddenDenominator) != 0)
                return false;

            int hiddenSize = hiddenNumerator / hiddenDenominator;
            if (hiddenSize <= 0) return false;

            int dense1WeightCount = expectedInputSize * hiddenSize;
            int dense1BiasCount = hiddenSize;
            int dense2WeightCount = hiddenSize * KotlinOutputSize;
            int dense2BiasCount = KotlinOutputSize;
            int expectedParameterCount = dense1WeightCount + dense1BiasCount + dense2WeightCount + dense2BiasCount;
            if (parameterCount != expectedParameterCount) return false;

            int byteOffset = 4;
            if (!TryReadFloatArrayBigEndian(modelBytes, ref byteOffset, dense1WeightCount, out float[] dense1Weights)) return false;
            if (!TryReadFloatArrayBigEndian(modelBytes, ref byteOffset, dense1BiasCount, out float[] dense1Bias)) return false;
            if (!TryReadFloatArrayBigEndian(modelBytes, ref byteOffset, dense2WeightCount, out float[] dense2Weights)) return false;
            if (!TryReadFloatArrayBigEndian(modelBytes, ref byteOffset, dense2BiasCount, out float[] dense2Bias)) return false;

            model = new KotlinTwoLayerDigitModel(expectedInputSize, hiddenSize, KotlinOutputSize, dense1Weights, dense1Bias, dense2Weights, dense2Bias);
            return true;
        }

        static bool TryReadInt32BigEndian(byte[] bytes, int offset, out int value)
        {
            value = 0;
            if (bytes == null || offset < 0 || bytes.Length < offset + 4) return false;
            value = (int)(((uint)bytes[offset] << 24) | ((uint)bytes[offset + 1] << 16) | ((uint)bytes[offset + 2] << 8) | bytes[offset + 3]);
            return true;
        }

        static bool TryReadFloatArrayBigEndian(byte[] bytes, ref int offset, int count, out float[] values)
        {
            values = null;
            if (count < 0 || bytes == null) return false;

            int requiredBytes = count * 4;
            if (offset < 0 || bytes.Length < offset + requiredBytes) return false;

            values = new float[count];
            if (BitConverter.IsLittleEndian)
            {
                byte[] tmp = new byte[4];
                for (int i = 0; i < count; i++)
                {
                    tmp[0] = bytes[offset + 3];
                    tmp[1] = bytes[offset + 2];
                    tmp[2] = bytes[offset + 1];
                    tmp[3] = bytes[offset];
                    values[i] = BitConverter.ToSingle(tmp, 0);
                    offset += 4;
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    values[i] = BitConverter.ToSingle(bytes, offset);
                    offset += 4;
                }
            }

            return true;
        }
    }

    class LinearSoftmaxDigitModel : IDigitModel
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

    class KotlinTwoLayerDigitModel : IDigitModel
    {
        public int InputSize { get; }
        public int HiddenSize { get; }
        public int OutputSize { get; }

        readonly float[] dense1Weights;
        readonly float[] dense1Bias;
        readonly float[] dense2Weights;
        readonly float[] dense2Bias;
        readonly float[] hiddenBuffer;

        public KotlinTwoLayerDigitModel(
            int inputSize,
            int hiddenSize,
            int outputSize,
            float[] dense1Weights,
            float[] dense1Bias,
            float[] dense2Weights,
            float[] dense2Bias)
        {
            InputSize = inputSize;
            HiddenSize = hiddenSize;
            OutputSize = outputSize;
            this.dense1Weights = dense1Weights ?? throw new ArgumentNullException(nameof(dense1Weights));
            this.dense1Bias = dense1Bias ?? throw new ArgumentNullException(nameof(dense1Bias));
            this.dense2Weights = dense2Weights ?? throw new ArgumentNullException(nameof(dense2Weights));
            this.dense2Bias = dense2Bias ?? throw new ArgumentNullException(nameof(dense2Bias));
            hiddenBuffer = new float[hiddenSize];
        }

        public int Predict(float[] input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.Length != InputSize) throw new ArgumentException("Unexpected input size.");

            for (int j = 0; j < HiddenSize; j++)
            {
                float sum = dense1Bias[j];
                int wIdx = j;
                for (int i = 0; i < InputSize; i++)
                {
                    sum += input[i] * dense1Weights[wIdx];
                    wIdx += HiddenSize;
                }
                hiddenBuffer[j] = sum > 0f ? sum : 0f;
            }

            int bestClass = 0;
            float bestLogit = float.NegativeInfinity;
            for (int j = 0; j < OutputSize; j++)
            {
                float sum = dense2Bias[j];
                int wIdx = j;
                for (int i = 0; i < HiddenSize; i++)
                {
                    sum += hiddenBuffer[i] * dense2Weights[wIdx];
                    wIdx += OutputSize;
                }
                if (sum > bestLogit)
                {
                    bestLogit = sum;
                    bestClass = j;
                }
            }

            return bestClass;
        }
    }
}