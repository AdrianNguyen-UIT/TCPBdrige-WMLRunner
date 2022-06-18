using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using System.Threading.Tasks;
using Windows.Storage.Streams;
#if USE_WINML_NUGET
using Microsoft.AI.MachineLearning;
#else
using Windows.AI.MachineLearning;
#endif
using BlingFire;

namespace WMLRunner
{
    public sealed class MrcViBaseInput
    {
        public TensorInt64Bit input_ids;
        public TensorInt64Bit attention_mask;
    }

    public sealed class MrcViBaseOutput
    {
        public TensorFloat start_logits;
        public TensorFloat end_logits;
    }

    public sealed class MrcViBaseModel
    {
        private LearningModel _model;
        private LearningModelSession _session;

        private ulong _stringToIdsTokenizer = 0;
        private ulong _idsToStringTokenizer = 0;

        public async static Task<MrcViBaseModel> CreateFromStreamAsync(IRandomAccessStreamReference stream)
        {
            MrcViBaseModel model = new MrcViBaseModel();
            model._model = await LearningModel.LoadFromStreamAsync(stream);
            model._session = new LearningModelSession(model._model);

            model._stringToIdsTokenizer = BlingFireUtils.LoadModel(Constant.XMLROBERTA_TEXT_TO_IDS_PATH);
            model._idsToStringTokenizer = BlingFireUtils.LoadModel(Constant.XMLROBERTA_IDS_TO_TEXT_PATH);
            return model;
        }

        private async Task<MrcViBaseOutput> EvaluateAsync(MrcViBaseInput input)
        {
            LearningModelBinding binding = new LearningModelBinding(_session);
            binding.Bind(Constant.MODEL_INPUT_IDS_KEY, input.input_ids);
            binding.Bind(Constant.MODEL_ATTENTION_MASK_KEY, input.attention_mask);

            var result = await _session.EvaluateAsync(binding, new Guid().ToString());

            var output = new MrcViBaseOutput
            {
                start_logits = result.Outputs[Constant.MODEL_START_LOGITS_KEY] as TensorFloat,
                end_logits = result.Outputs[Constant.MODEL_END_LOGITS_KEY] as TensorFloat
            };
            return output;
        }

        private int ArgMax(IEnumerable<float> collection)
        {
            float[] floats = collection.ToArray();
            int maxIndex = 0;
            int count = collection.Count();
            for (int index = 1; index < count; index++)
            {
                if (floats[index] > floats[maxIndex])
                {
                    maxIndex = index;
                }
            }
            return maxIndex;
        }

        public async Task<string> GetAnswer(string data)
        {
            string[] inputs = data.Split(Constant.SEPERATOR_TOKEN);

            byte[] questionInBytes = System.Text.Encoding.UTF8.GetBytes(inputs[0]);
            int[] questionIds = new int[Constant.MAX_BUFFER_SIZE];
            int questionIdsCount = BlingFireUtils2.TextToIds(_stringToIdsTokenizer, questionInBytes, questionInBytes.Length, questionIds, questionIds.Length, 0);
            Array.Resize(ref questionIds, questionIdsCount);

            byte[] contextInBytes = System.Text.Encoding.UTF8.GetBytes(inputs[1]);
            int[] contextIds = new int[Constant.MAX_BUFFER_SIZE];
            int contextIdsCount = BlingFireUtils2.TextToIds(_stringToIdsTokenizer, contextInBytes, contextInBytes.Length, contextIds, contextIds.Length, 0);
            Array.Resize(ref contextIds, contextIdsCount);

            long[] input_ids = new long[questionIdsCount + contextIdsCount + 3];
            input_ids[0] = Constant.XMLROBERT_BEGIN_ID;
            input_ids[questionIdsCount + 1] = Constant.XMLROBERT_SEPERATOR_ID;
            input_ids[input_ids.Length - 1] = Constant.XMLROBERT_END_ID;

            int questionOffset = 1;
            for (int index = 0; index < questionIdsCount; index++)
            {
                input_ids[index + questionOffset] = questionIds[index];
            }

            int contextOffset = questionIdsCount + 2;
            for (int index = 0; index < contextIdsCount; index++)
            {
                input_ids[index + contextOffset] = contextIds[index];
            }

            long[] attention_mask = new long[input_ids.Length];
            Array.Fill(attention_mask, 1);

            try
            {
                MrcViBaseInput mrcViBaseInput = new MrcViBaseInput();
                mrcViBaseInput.input_ids = TensorInt64Bit.CreateFromArray(new long[] { 1, input_ids.Length }, input_ids);
                mrcViBaseInput.attention_mask = TensorInt64Bit.CreateFromArray(new long[] { 1, attention_mask.Length }, attention_mask);
                MrcViBaseOutput mrcViBaseOutput = await EvaluateAsync(mrcViBaseInput);

                var answer_start_scores = mrcViBaseOutput.start_logits.GetAsVectorView();
                var answer_end_scores = mrcViBaseOutput.end_logits.GetAsVectorView();

                var answer_start = ArgMax(answer_start_scores);
                var answer_end = ArgMax(answer_end_scores);

                int[] answer_ids = new int[answer_end - answer_start + 1];
                for (int index = 0; index < answer_ids.Length; index++)
                {
                    answer_ids[index] = (int)input_ids[index + answer_start];
                }

                return BlingFireUtils2.IdsToText(_idsToStringTokenizer, answer_ids);
            }
            catch (Exception)
            {
                return Constant.EMPTY_ANSWER;
            }
        }
    }
}
