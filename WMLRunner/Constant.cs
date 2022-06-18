using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMLRunner
{
    public class Constant
    {
        public const int CONNECTION_PORT = 13000;
        public const string CONNECTION_ADDRESS = "127.0.0.1";

        public const string MODEL_PATH = "ms-appx:///Assets/vi-mrc-base.onnx";
        public const string MODEL_INPUT_IDS_KEY = "input_ids";
        public const string MODEL_ATTENTION_MASK_KEY = "attention_mask";
        public const string MODEL_START_LOGITS_KEY = "start_logits";
        public const string MODEL_END_LOGITS_KEY = "end_logits";

        public const int MAX_BUFFER_SIZE = 2048;

        public const string XMLROBERTA_TEXT_TO_IDS_PATH = "xlm_roberta_base.bin";
        public const string XMLROBERTA_IDS_TO_TEXT_PATH = "xlm_roberta_base.i2w";
        public const int XMLROBERT_BEGIN_ID = 0;
        public const int XMLROBERT_SEPERATOR_ID = 2;
        public const int XMLROBERT_END_ID = 2;

        public const string SEPERATOR_TOKEN = "[sep]";

        public const string EMPTY_ANSWER = "\0";
    }
}
